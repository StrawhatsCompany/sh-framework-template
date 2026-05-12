using Business.Configuration;
using Domain.Entities.Identity;
using SH.Framework.Library.Cqrs.Implementation;

namespace Business.Authentication.Mfa.Totp;

internal sealed class TotpMfaChannel(ITotpService totp, ICredentialProtector protector) : IMfaChannel
{
    public MfaFactorKind Kind => MfaFactorKind.Totp;

    /// <summary>
    /// TOTP needs no dispatch — the user's authenticator app generates the code from the shared
    /// secret. This is intentionally a no-op success so the orchestrator's <c>IssueAsync</c>
    /// signature stays uniform across channels.
    /// </summary>
    public Task<Result> IssueAsync(MfaFactor factor, MfaChallenge challenge, CancellationToken ct = default) =>
        Task.FromResult(Result.Success());

    public Task<Result> VerifyAsync(MfaFactor factor, MfaChallenge challenge, string code, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(factor.SecretCipher))
        {
            return Task.FromResult(Result.Failure(MfaResultCode.FactorNotActive));
        }

        string secret;
        try { secret = protector.Unprotect(factor.SecretCipher); }
        catch { return Task.FromResult(Result.Failure(MfaResultCode.InvalidCode)); }

        return Task.FromResult(totp.Verify(secret, code.Trim())
            ? Result.Success()
            : Result.Failure(MfaResultCode.InvalidCode));
    }
}
