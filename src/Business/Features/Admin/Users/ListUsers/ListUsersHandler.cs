using Business.Common;
using Business.Identity;
using Domain.Entities.Identity;
using SH.Framework.Library.Cqrs.Implementation;

namespace Business.Features.Admin.Users.ListUsers;

public sealed class ListUsersQuery : Request<ListUsersResponse>
{
    public UserStatus? Status { get; set; }
}

public sealed class ListUsersResponse
{
    public required IReadOnlyList<UserDto> Items { get; init; }
}

public sealed class ListUsersHandler(IUserStore users, ITenantContext tenantCtx)
    : RequestHandler<ListUsersQuery, ListUsersResponse>
{
    public override async Task<Result<ListUsersResponse>> HandleAsync(
        ListUsersQuery request, CancellationToken cancellationToken = default)
    {
        if (tenantCtx.TenantId is not { } tenantId)
        {
            return Result.Failure<ListUsersResponse>(IdentityResultCode.TenantRequired);
        }

        var all = await users.ListAsync(tenantId, request.Status, cancellationToken);
        return Result.Success(new ListUsersResponse
        {
            Items = all.Select(UserDto.From).ToList(),
        });
    }
}
