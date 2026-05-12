using System.Text.RegularExpressions;
using Business.Common;
using Business.Identity;
using Domain.Entities.Identity;
using SH.Framework.Library.Cqrs.Implementation;

namespace Business.Features.Admin.Permissions.CreatePermission;

public sealed class CreatePermissionCommand : Request<CreatePermissionResponse>
{
    public string Name { get; set; } = "";
    public string? Description { get; set; }
}

public sealed class CreatePermissionResponse
{
    public required PermissionDto Permission { get; init; }
}

public sealed partial class CreatePermissionHandler(IPermissionStore permissions, IUserContext userCtx)
    : RequestHandler<CreatePermissionCommand, CreatePermissionResponse>
{
    // Dotted lowercase: admin.users.write, orders.read, billing.invoices.refund.
    // 2-3 segments by convention but no upper bound enforced; segments alnum + dash.
    [GeneratedRegex(@"^[a-z0-9][a-z0-9-]*(\.[a-z0-9][a-z0-9-]*)+$", RegexOptions.Compiled)]
    private static partial Regex NamePattern();

    public override async Task<Result<CreatePermissionResponse>> HandleAsync(
        CreatePermissionCommand request, CancellationToken cancellationToken = default)
    {
        var name = request.Name.Trim().ToLowerInvariant();
        if (!NamePattern().IsMatch(name))
        {
            return Result.Failure<CreatePermissionResponse>(IdentityResultCode.PermissionNameInvalid);
        }
        if (await permissions.FindByNameAsync(name, cancellationToken) is not null)
        {
            return Result.Failure<CreatePermissionResponse>(IdentityResultCode.PermissionNameAlreadyExists);
        }

        var permission = new Permission
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            Category = name.Split('.', 2)[0],
            CreatedAt = DateTime.UtcNow,
            CreatedBy = userCtx.UserId,
        };

        await permissions.AddAsync(permission, cancellationToken);

        return Result.Success(new CreatePermissionResponse { Permission = PermissionDto.From(permission) });
    }
}
