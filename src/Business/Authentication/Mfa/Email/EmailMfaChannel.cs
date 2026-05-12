using System.Security.Cryptography;
using System.Text;
using Business.Providers.Mail;
using Business.Providers.Mail.Contracts;
using Business.Providers.Mail.Dtos;
using Business.Providers;
using Domain.Entities.Identity;
using Microsoft.Extensions.Options;
using SH.Framework.Library.Cqrs.Implementation;

namespace Business.Authentication.Mfa.Email;

internal sealed class EmailMfaChannel(
    IProviderFactory<MailProviderCredential, IMailProvider> mailFactory,
    IOptionsSnapshot<MailOptions> mailOptions,
    IOptionsSnapshot<EmailMfaOptions> emailMfaOptions,
    IMfaChallengeStore challenges)
    : IMfaChannel
{
    public MfaFactorKind Kind => MfaFactorKind.Email;

    public async Task<Result> IssueAsync(MfaFactor factor, MfaChallenge challenge, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(factor.Destination))
        {
            return Result.Failure(MfaResultCode.FactorNotActive);
        }

        var opts = emailMfaOptions.Value;
        var code = MfaCodeHasher.GenerateNumericCode(opts.CodeLength);

        // Persist hash + override challenge expiry to match the channel's TTL (the orchestrator
        // picks a default; channels can tighten).
        challenge.CodeHash = MfaCodeHasher.Hash(code);
        challenge.ExpiresAt = DateTime.UtcNow.Add(opts.Ttl);

        // Render template. {{code}} + {{ttlMin}} are the supported placeholders.
        var body = opts.BodyTemplate
            .Replace("{{code}}", code, StringComparison.Ordinal)
            .Replace("{{ttlMin}}", ((int)opts.Ttl.TotalMinutes).ToString(), StringComparison.Ordinal);

        var mail = mailOptions.Value;
        var credential = new MailProviderCredential
        {
            ProviderType = MailProviderType.Smtp,
            HostName = mail.HostName,
            Port = mail.Port,
            UseSsl = mail.UseSsl,
            UserName = mail.Username,
            Password = mail.Password,
        };

        var provider = mailFactory.Create(credential);
        var send = await provider.SendAsync(new SendMailContract.Request(
            From: new MailAddress(mail.FromAddress, mail.FromName),
            To: [new MailAddress(factor.Destination, null)],
            Subject: opts.Subject,
            MailBody: MailBody.Instance().WithTextBody(body)), ct);

        return send.IsSuccess ? Result.Success() : Result.Failure(MfaResultCode.KindUnsupported);
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

        if (!ok)
        {
            return Result.Failure(MfaResultCode.InvalidCode);
        }

        // Defensive expiry check — orchestrator already does this but the channel can be invoked
        // independently in future (e.g. verification flow reuse).
        if (challenge.ExpiresAt <= DateTime.UtcNow)
        {
            return Result.Failure(MfaResultCode.ChallengeExpired);
        }

        await challenges.UpdateAsync(challenge, ct);
        return Result.Success();
    }
}
