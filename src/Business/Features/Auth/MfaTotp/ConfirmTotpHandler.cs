using Business.Authentication;
using Business.Authentication.Mfa;
using Business.Authentication.Mfa.Totp;
using Business.Common;
using Business.Configuration;
using Domain.Entities.Identity;
using SH.Framework.Library.Cqrs.Implementation;

namespace Business.Features.Auth.MfaTotp;

public sealed class ConfirmTotpCommand : Request
{
    public Guid FactorId { get; set; }
    public string Code { get; set; } = "";
}

public sealed class ConfirmTotpHandler(
    IMfaFactorStore factors,
    ITotpService totp,
    ICredentialProtector protector,
    ITenantContext tenantCtx,
    IUserContext userCtx)
    : RequestHandler<ConfirmTotpCommand>
{
    public override async Task<Result> HandleAsync(ConfirmTotpCommand request, CancellationToken cancellationToken = default)
    {
        if (tenantCtx.TenantId is not { } tenantId || userCtx.UserId is not { } userId)
        {
            return Result.Failure(AuthResultCode.InvalidCredentials);
        }

        var factor = await factors.FindByIdAsync(tenantId, request.FactorId, cancellationToken);
        if (factor is null || factor.UserId != userId || factor.Kind != MfaFactorKind.Totp)
        {
            return Result.Failure(MfaResultCode.FactorNotFound);
        }
        if (factor.Status != MfaFactorStatus.PendingEnrollment)
        {
            return Result.Failure(MfaResultCode.FactorAlreadyEnrolled);
        }
        if (string.IsNullOrEmpty(factor.SecretCipher))
        {
            return Result.Failure(MfaResultCode.FactorNotActive);
        }

        string secret;
        try { secret = protector.Unprotect(factor.SecretCipher); }
        catch { return Result.Failure(MfaResultCode.InvalidCode); }

        if (!totp.Verify(secret, request.Code.Trim()))
        {
            return Result.Failure(MfaResultCode.InvalidCode);
        }

        var now = DateTime.UtcNow;
        factor.Status = MfaFactorStatus.Active;
        factor.VerifiedAt = now;
        factor.UpdatedBy = userId;
        await factors.UpdateAsync(factor, cancellationToken);
        return Result.Success();
    }
}
