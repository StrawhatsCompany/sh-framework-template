using Business.Authentication;
using Business.Authentication.Mfa;
using Business.Common;
using Business.Identity;
using Domain.Entities.Identity;
using SH.Framework.Library.Cqrs.Implementation;

namespace Business.Features.Auth.MfaSms;

public sealed class EnrollSmsCommand : Request<EnrollSmsResponse>
{
    public string Phone { get; set; } = "";
}

public sealed class EnrollSmsResponse
{
    public required Guid FactorId { get; init; }
    public required Guid ChallengeId { get; init; }
}

public sealed class EnrollSmsHandler(
    IMfaFactorStore factors,
    IMfaOrchestrator orchestrator,
    IUserStore users,
    ITenantContext tenantCtx,
    IUserContext userCtx)
    : RequestHandler<EnrollSmsCommand, EnrollSmsResponse>
{
    public override async Task<Result<EnrollSmsResponse>> HandleAsync(
        EnrollSmsCommand request, CancellationToken cancellationToken = default)
    {
        if (tenantCtx.TenantId is not { } tenantId || userCtx.UserId is not { } userId)
        {
            return Result.Failure<EnrollSmsResponse>(AuthResultCode.InvalidCredentials);
        }

        var user = await users.FindByIdAsync(tenantId, userId, cancellationToken);
        if (user is null)
        {
            return Result.Failure<EnrollSmsResponse>(IdentityResultCode.UserNotFound);
        }

        var phone = request.Phone.Trim();
        if (string.IsNullOrEmpty(phone))
        {
            return Result.Failure<EnrollSmsResponse>(MfaResultCode.FactorNotActive);
        }

        var existing = await factors.ListByUserAsync(tenantId, userId, cancellationToken);
        if (existing.Any(f => f.Kind == MfaFactorKind.Sms && f.Status != MfaFactorStatus.Disabled))
        {
            return Result.Failure<EnrollSmsResponse>(MfaResultCode.FactorAlreadyEnrolled);
        }

        // Factor lands in PendingEnrollment. The user must confirm a code (via the
        // SmsMfaChannel-dispatched message) to flip it to Active. We need the factor row to
        // exist BEFORE the orchestrator can issue a challenge against it — so we have to
        // temporarily mark it Active for the issue call, then revert. Cleaner alternative:
        // bypass the orchestrator and dispatch directly. We do the latter — the SmsMfaChannel
        // handles dispatch and code-hash persistence on the challenge.
        var factor = new MfaFactor
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserId = userId,
            Kind = MfaFactorKind.Sms,
            Destination = phone,
            Status = MfaFactorStatus.Active,    // temporarily active to allow the orchestrator's issue
            CreatedAt = DateTime.UtcNow,
            CreatedBy = userId,
        };
        await factors.AddAsync(factor, cancellationToken);

        var issue = await orchestrator.IssueAsync(tenantId, userId, factor.Id, cancellationToken);
        // Now revert to PendingEnrollment until the user confirms.
        factor.Status = MfaFactorStatus.PendingEnrollment;
        await factors.UpdateAsync(factor, cancellationToken);

        if (!issue.IsSuccess)
        {
            return Result.Failure<EnrollSmsResponse>(ResultCode.Instance(issue.Code, issue.CategorizedCode, issue.Description));
        }

        return Result.Success(new EnrollSmsResponse
        {
            FactorId = factor.Id,
            ChallengeId = issue.Data!.Id,
        });
    }
}
