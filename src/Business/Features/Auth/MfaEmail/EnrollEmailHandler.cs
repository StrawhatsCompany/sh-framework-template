using Business.Authentication;
using Business.Authentication.Mfa;
using Business.Common;
using Business.Identity;
using Domain.Entities.Identity;
using SH.Framework.Library.Cqrs.Implementation;

namespace Business.Features.Auth.MfaEmail;

public sealed class EnrollEmailCommand : Request<EnrollEmailResponse> { }

public sealed class EnrollEmailResponse
{
    public required Guid FactorId { get; init; }
    public required string Destination { get; init; }
}

public sealed class EnrollEmailHandler(
    IMfaFactorStore factors,
    IUserStore users,
    ITenantContext tenantCtx,
    IUserContext userCtx)
    : RequestHandler<EnrollEmailCommand, EnrollEmailResponse>
{
    public override async Task<Result<EnrollEmailResponse>> HandleAsync(
        EnrollEmailCommand request, CancellationToken cancellationToken = default)
    {
        if (tenantCtx.TenantId is not { } tenantId || userCtx.UserId is not { } userId)
        {
            return Result.Failure<EnrollEmailResponse>(AuthResultCode.InvalidCredentials);
        }

        var user = await users.FindByIdAsync(tenantId, userId, cancellationToken);
        if (user is null)
        {
            return Result.Failure<EnrollEmailResponse>(IdentityResultCode.UserNotFound);
        }
        // Email must already be verified — no point sending MFA codes to an unverified address.
        if (user.EmailVerifiedAt is null)
        {
            return Result.Failure<EnrollEmailResponse>(MfaResultCode.FactorNotActive);
        }

        var existing = await factors.ListByUserAsync(tenantId, userId, cancellationToken);
        if (existing.Any(f => f.Kind == MfaFactorKind.Email && f.Status != MfaFactorStatus.Disabled))
        {
            return Result.Failure<EnrollEmailResponse>(MfaResultCode.FactorAlreadyEnrolled);
        }

        // No confirm step: email is already verified, so the factor goes straight to Active.
        var now = DateTime.UtcNow;
        var factor = new MfaFactor
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserId = userId,
            Kind = MfaFactorKind.Email,
            Destination = user.Email,
            Status = MfaFactorStatus.Active,
            VerifiedAt = now,
            CreatedAt = now,
            CreatedBy = userId,
        };
        await factors.AddAsync(factor, cancellationToken);

        return Result.Success(new EnrollEmailResponse
        {
            FactorId = factor.Id,
            Destination = user.Email,
        });
    }
}
