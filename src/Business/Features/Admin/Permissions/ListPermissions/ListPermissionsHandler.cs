using Business.Identity;
using SH.Framework.Library.Cqrs.Implementation;

namespace Business.Features.Admin.Permissions.ListPermissions;

public sealed class ListPermissionsQuery : Request<ListPermissionsResponse>
{
    public string? Category { get; set; }
}

public sealed class ListPermissionsResponse
{
    public required IReadOnlyList<PermissionDto> Items { get; init; }
}

public sealed class ListPermissionsHandler(IPermissionStore permissions)
    : RequestHandler<ListPermissionsQuery, ListPermissionsResponse>
{
    public override async Task<Result<ListPermissionsResponse>> HandleAsync(
        ListPermissionsQuery request, CancellationToken cancellationToken = default)
    {
        var all = await permissions.ListAsync(cancellationToken);
        var filtered = string.IsNullOrWhiteSpace(request.Category)
            ? all
            : all.Where(p => string.Equals(p.Category, request.Category, StringComparison.OrdinalIgnoreCase));

        return Result.Success(new ListPermissionsResponse
        {
            Items = filtered.Select(PermissionDto.From).ToList(),
        });
    }
}
