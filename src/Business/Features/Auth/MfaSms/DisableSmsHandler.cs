using Business.Authentication;
using Business.Authentication.Mfa;
using Business.Common;
using Domain.Entities.Identity;
using SH.Framework.Library.Cqrs.Implementation;

namespace Business.Features.Auth.MfaSms;

public sealed class DisableSmsCommand : Request
{
    public Guid FactorId { get; set; }
}

public sealed class DisableSmsHandler(
    IMfaFactorStore factors,
    ITenantContext tenantCtx,
    IUserContext userCtx)
    : RequestHandler<DisableSmsCommand>
{
    public override async Task<Result> HandleAsync(DisableSmsCommand request, CancellationToken cancellationToken = default)
    {
        if (tenantCtx.TenantId is not { } tenantId || userCtx.UserId is not { } userId)
        {
            return Result.Failure(AuthResultCode.InvalidCredentials);
        }

        var factor = await factors.FindByIdAsync(tenantId, request.FactorId, cancellationToken);
        if (factor is null || factor.UserId != userId || factor.Kind != MfaFactorKind.Sms)
        {
            return Result.Failure(MfaResultCode.FactorNotFound);
        }

        var ok = await factors.SoftDeleteAsync(tenantId, request.FactorId, userId, cancellationToken);
        return ok ? Result.Success() : Result.Failure(MfaResultCode.FactorNotFound);
    }
}
