using Business.Common;
using Business.Identity;
using Domain.Entities.Identity;
using SH.Framework.Library.Cqrs.Implementation;

namespace Business.Features.Admin.Users.UpdateUser;

public sealed class UpdateUserCommand : Request<UpdateUserResponse>
{
    public Guid Id { get; set; }
    public string? DisplayName { get; set; }
    public string? Phone { get; set; }
    public UserStatus? Status { get; set; }
}

public sealed class UpdateUserResponse
{
    public required UserDto User { get; init; }
}

public sealed class UpdateUserHandler(IUserStore users, ITenantContext tenantCtx, IUserContext userCtx)
    : RequestHandler<UpdateUserCommand, UpdateUserResponse>
{
    public override async Task<Result<UpdateUserResponse>> HandleAsync(
        UpdateUserCommand request, CancellationToken cancellationToken = default)
    {
        if (tenantCtx.TenantId is not { } tenantId)
        {
            return Result.Failure<UpdateUserResponse>(IdentityResultCode.TenantRequired);
        }

        var user = await users.FindByIdAsync(tenantId, request.Id, cancellationToken);
        if (user is null)
        {
            return Result.Failure<UpdateUserResponse>(IdentityResultCode.UserNotFound);
        }

        if (request.DisplayName is { } displayName && !string.IsNullOrWhiteSpace(displayName))
        {
            user.DisplayName = displayName.Trim();
        }
        if (request.Phone is { } phone)
        {
            user.Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();
            user.PhoneVerifiedAt = null;
        }
        if (request.Status is { } status)
        {
            user.Status = status;
        }

        user.UpdatedAt = DateTime.UtcNow;
        user.UpdatedBy = userCtx.UserId;

        await users.UpdateAsync(user, cancellationToken);

        return Result.Success(new UpdateUserResponse { User = UserDto.From(user) });
    }
}
