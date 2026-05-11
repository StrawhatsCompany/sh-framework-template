using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace Business.Libraries.Authentication.Mfa;

internal sealed class MfaChallengeIssuer(
    IMfaCodeStore store,
    IEnumerable<IMfaChannel> channels,
    IOptions<MfaOptions> options,
    TimeProvider time) : IMfaChallengeIssuer
{
    private readonly IReadOnlyDictionary<string, IMfaChannel> _channels =
        channels.GroupBy(c => c.ChannelType, StringComparer.Ordinal).ToDictionary(g => g.Key, g => g.Last(), StringComparer.Ordinal);

    public async Task<MfaChallenge> IssueAsync(string userId, string channelType, CancellationToken cancellationToken = default)
    {
        if (!_channels.TryGetValue(channelType, out var channel))
        {
            throw new InvalidOperationException(
                $"No IMfaChannel registered for channel type '{channelType}'. Register one via DI.");
        }

        var opts = options.Value;
        var plaintextCode = GenerateCode(opts.CodeLength);
        var now = time.GetUtcNow();
        var challenge = new MfaChallenge(
            ChallengeId: Guid.NewGuid().ToString("N"),
            UserId: userId,
            ChannelType: channelType,
            IssuedAt: now,
            ExpiresAt: now.Add(opts.Expiry),
            CodeHash: HashCode(plaintextCode));

        var send = await channel.SendAsync(challenge, plaintextCode, cancellationToken);
        if (!send.IsSent)
        {
            throw new InvalidOperationException(
                $"MFA channel '{channelType}' refused the send: {send.FailureReason ?? "unspecified"}.");
        }

        await store.StoreAsync(challenge, cancellationToken);
        return challenge;
    }

    public async Task<MfaResult> VerifyAsync(string challengeId, string submittedCode, CancellationToken cancellationToken = default)
    {
        var challenge = await store.GetAsync(challengeId, cancellationToken);
        if (challenge is null)
        {
            return MfaResult.NoSuchChallenge;
        }

        var opts = options.Value;
        if (challenge.ExpiresAt < time.GetUtcNow())
        {
            await store.RemoveAsync(challengeId, cancellationToken);
            return MfaResult.Expired;
        }

        if (challenge.Attempts >= opts.MaxAttempts)
        {
            await store.RemoveAsync(challengeId, cancellationToken);
            return MfaResult.TooManyAttempts;
        }

        if (!_channels.TryGetValue(challenge.ChannelType, out var channel))
        {
            throw new InvalidOperationException(
                $"No IMfaChannel registered for channel type '{challenge.ChannelType}'.");
        }

        var channelVerify = await channel.VerifyAsync(challenge, submittedCode, cancellationToken);
        var hashMatches = ConstantTimeEquals(challenge.CodeHash, HashCode(submittedCode));

        if (channelVerify.IsValid && hashMatches)
        {
            await store.RemoveAsync(challengeId, cancellationToken);
            return MfaResult.Success;
        }

        await store.UpdateAsync(challenge with { Attempts = challenge.Attempts + 1 }, cancellationToken);
        return MfaResult.Invalid;
    }

    private static string GenerateCode(int length)
    {
        if (length < 4 || length > 10)
        {
            throw new ArgumentOutOfRangeException(nameof(length), "MFA code length must be between 4 and 10.");
        }
        var buffer = new char[length];
        for (var i = 0; i < length; i++)
        {
            buffer[i] = (char)('0' + RandomNumberGenerator.GetInt32(0, 10));
        }
        return new string(buffer);
    }

    private static string HashCode(string code)
    {
        var bytes = Encoding.UTF8.GetBytes(code);
        return Convert.ToHexString(SHA256.HashData(bytes));
    }

    private static bool ConstantTimeEquals(string a, string b)
    {
        if (a.Length != b.Length) return false;
        var diff = 0;
        for (var i = 0; i < a.Length; i++)
        {
            diff |= a[i] ^ b[i];
        }
        return diff == 0;
    }
}
