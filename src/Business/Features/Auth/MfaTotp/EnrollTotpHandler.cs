using Business.Authentication;
using Business.Authentication.Mfa;
using Business.Authentication.Mfa.Totp;
using Business.Common;
using Business.Configuration;
using Business.Identity;
using Domain.Entities.Identity;
using Microsoft.Extensions.Options;
using SH.Framework.Library.Cqrs.Implementation;

namespace Business.Features.Auth.MfaTotp;

public sealed class EnrollTotpCommand : Request<EnrollTotpResponse> { }

public sealed class EnrollTotpResponse
{
    public required Guid FactorId { get; init; }
    public required string Secret { get; init; }
    public required string OtpAuthUri { get; init; }
}

public sealed class EnrollTotpHandler(
    IMfaFactorStore factors,
    IUserStore users,
    ITotpService totp,
    ICredentialProtector protector,
    ITenantContext tenantCtx,
    IUserContext userCtx,
    IOptionsSnapshot<MfaOptions> options)
    : RequestHandler<EnrollTotpCommand, EnrollTotpResponse>
{
    public override async Task<Result<EnrollTotpResponse>> HandleAsync(
        EnrollTotpCommand request, CancellationToken cancellationToken = default)
    {
        if (tenantCtx.TenantId is not { } tenantId || userCtx.UserId is not { } userId)
        {
            return Result.Failure<EnrollTotpResponse>(AuthResultCode.InvalidCredentials);
        }

        var user = await users.FindByIdAsync(tenantId, userId, cancellationToken);
        if (user is null)
        {
            return Result.Failure<EnrollTotpResponse>(IdentityResultCode.UserNotFound);
        }

        var existing = await factors.ListByUserAsync(tenantId, userId, cancellationToken);
        if (existing.Any(f => f.Kind == MfaFactorKind.Totp && f.Status != MfaFactorStatus.Disabled))
        {
            return Result.Failure<EnrollTotpResponse>(MfaResultCode.FactorAlreadyEnrolled);
        }

        var secret = totp.GenerateSecret();
        var factor = new MfaFactor
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserId = userId,
            Kind = MfaFactorKind.Totp,
            SecretCipher = protector.Protect(secret),
            Status = MfaFactorStatus.PendingEnrollment,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = userId,
        };
        await factors.AddAsync(factor, cancellationToken);

        var label = !string.IsNullOrEmpty(user.Email) ? user.Email : user.Username;
        var otpAuth = totp.BuildOtpAuthUri(options.Value.TotpIssuer, label, secret);

        return Result.Success(new EnrollTotpResponse
        {
            FactorId = factor.Id,
            Secret = secret,
            OtpAuthUri = otpAuth,
        });
    }
}
