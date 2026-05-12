using Business.Common;
using Business.Identity;
using SH.Framework.Library.Cqrs.Implementation;

namespace Business.Features.Admin.Users.GetUser;

public sealed class GetUserQuery : Request<GetUserResponse>
{
    public Guid Id { get; set; }
}

public sealed class GetUserResponse
{
    public required UserDto User { get; init; }
}

public sealed class GetUserHandler(IUserStore users, ITenantContext tenantCtx)
    : RequestHandler<GetUserQuery, GetUserResponse>
{
    public override async Task<Result<GetUserResponse>> HandleAsync(
        GetUserQuery request, CancellationToken cancellationToken = default)
    {
        if (tenantCtx.TenantId is not { } tenantId)
        {
            return Result.Failure<GetUserResponse>(IdentityResultCode.TenantRequired);
        }

        var user = await users.FindByIdAsync(tenantId, request.Id, cancellationToken);
        return user is null
            ? Result.Failure<GetUserResponse>(IdentityResultCode.UserNotFound)
            : Result.Success(new GetUserResponse { User = UserDto.From(user) });
    }
}
