using Domain.Entities.Identity;
using SH.Framework.Library.Cqrs.Implementation;

namespace Business.Authentication.Mfa;

/// <summary>
/// One implementation per <see cref="MfaFactorKind"/>. The orchestrator resolves the right
/// channel by kind. For TOTP, <see cref="IssueAsync"/> is a no-op (the code is computed from
/// the secret on demand); Email and SMS channels generate a code, hash it onto the challenge,
/// and dispatch via their provider (lands in #79 / #80).
/// </summary>
public interface IMfaChannel
{
    MfaFactorKind Kind { get; }

    Task<Result> IssueAsync(MfaFactor factor, MfaChallenge challenge, CancellationToken ct = default);

    Task<Result> VerifyAsync(MfaFactor factor, MfaChallenge challenge, string code, CancellationToken ct = default);
}
