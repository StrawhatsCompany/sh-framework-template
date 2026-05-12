using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Business.Providers;
using Business.Providers.Sms;
using Business.Providers.Sms.Contracts;
using Domain.Entities.Identity;
using Microsoft.Extensions.Options;
using SH.Framework.Library.Cqrs.Implementation;

namespace Business.Authentication.Mfa.Sms;

internal sealed class SmsMfaChannel(
    IProviderFactory<SmsProviderCredential, ISmsProvider> smsFactory,
    IOptionsSnapshot<SmsOptions> smsOptions,
    IOptionsSnapshot<SmsMfaOptions> smsMfaOptions,
    IMfaChallengeStore challenges)
    : IMfaChannel
{
    // Simple in-process sliding-window rate limiter keyed on userId. Persistence-backed
    // deployments (or services running multiple replicas) should swap this for a Redis /
    // DB-backed limiter — register IMfaSmsRateLimiter later and inject here.
    private static readonly ConcurrentDictionary<Guid, List<DateTime>> _issueLog = new();

    public MfaFactorKind Kind => MfaFactorKind.Sms;

    public async Task<Result> IssueAsync(MfaFactor factor, MfaChallenge challenge, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(factor.Destination))
        {
            return Result.Failure(MfaResultCode.FactorNotActive);
        }

        var opts = smsMfaOptions.Value;

        if (!CheckAndRecordRate(factor.UserId, opts.RateLimitMaxIssuesPerUser, opts.RateLimitWindow))
        {
            return Result.Failure(MfaResultCode.RateLimited);
        }

        var code = MfaCodeHasher.GenerateNumericCode(opts.CodeLength);
        challenge.CodeHash = MfaCodeHasher.Hash(code);
        challenge.ExpiresAt = DateTime.UtcNow.Add(opts.Ttl);

        var body = opts.BodyTemplate.Replace("{{code}}", code, StringComparison.Ordinal);

        var sms = smsOptions.Value;
        var credential = new SmsProviderCredential
        {
            ProviderType = SmsProviderType.Twilio,
            HostName = "api.twilio.com",
            Port = 443,
            UseSsl = true,
            ApiKey = sms.AccountSid,
            Password = sms.AuthToken,
            UserName = sms.FromNumber,
        };

        var provider = smsFactory.Create(credential);
        var send = await provider.SendAsync(new SendSmsContract.Request(
            ToNumber: factor.Destination,
            Body: body), ct);

        return send.IsSuccess
            ? Result.Success()
            : Result.Failure(MfaResultCode.DispatchFailed);
    }

    public async Task<Result> VerifyAsync(MfaFactor factor, MfaChallenge challenge, string code, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(challenge.CodeHash))
        {
            return Result.Failure(MfaResultCode.InvalidCode);
        }

        var presentedHash = MfaCodeHasher.Hash(code.Trim());
        var ok = CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(presentedHash),
            Encoding.ASCII.GetBytes(challenge.CodeHash));

        if (!ok) return Result.Failure(MfaResultCode.InvalidCode);
        if (challenge.ExpiresAt <= DateTime.UtcNow) return Result.Failure(MfaResultCode.ChallengeExpired);

        await challenges.UpdateAsync(challenge, ct);
        return Result.Success();
    }

    private static bool CheckAndRecordRate(Guid userId, int max, TimeSpan window)
    {
        var now = DateTime.UtcNow;
        var cutoff = now - window;
        var log = _issueLog.GetOrAdd(userId, _ => []);
        lock (log)
        {
            log.RemoveAll(t => t < cutoff);
            if (log.Count >= max) return false;
            log.Add(now);
            return true;
        }
    }
}
