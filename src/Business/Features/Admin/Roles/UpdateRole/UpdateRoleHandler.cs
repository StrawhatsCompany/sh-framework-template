using Business.Common;
using Business.Identity;
using SH.Framework.Library.Cqrs.Implementation;

namespace Business.Features.Admin.Roles.UpdateRole;

public sealed class UpdateRoleCommand : Request<UpdateRoleResponse>
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
}

public sealed class UpdateRoleResponse
{
    public required RoleDto Role { get; init; }
}

public sealed class UpdateRoleHandler(IRoleStore roles, ITenantContext tenantCtx, IUserContext userCtx)
    : RequestHandler<UpdateRoleCommand, UpdateRoleResponse>
{
    public override async Task<Result<UpdateRoleResponse>> HandleAsync(
        UpdateRoleCommand request, CancellationToken cancellationToken = default)
    {
        if (tenantCtx.TenantId is not { } tenantId)
        {
            return Result.Failure<UpdateRoleResponse>(IdentityResultCode.TenantRequired);
        }

        var role = await roles.FindByIdAsync(tenantId, request.Id, cancellationToken);
        if (role is null)
        {
            return Result.Failure<UpdateRoleResponse>(IdentityResultCode.RoleNotFound);
        }
        if (role.IsSystem)
        {
            return Result.Failure<UpdateRoleResponse>(IdentityResultCode.RoleSystemImmutable);
        }

        if (request.Name is { } name && !string.IsNullOrWhiteSpace(name))
        {
            var trimmed = name.Trim();
            if (trimmed.Length is < 2 or > 64)
            {
                return Result.Failure<UpdateRoleResponse>(IdentityResultCode.RoleNameInvalid);
            }
            var existing = await roles.FindByNameAsync(tenantId, trimmed, cancellationToken);
            if (existing is not null && existing.Id != role.Id)
            {
                return Result.Failure<UpdateRoleResponse>(IdentityResultCode.RoleNameAlreadyExists);
            }
            role.Name = trimmed;
        }
        if (request.Description is { } description)
        {
            role.Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        }

        role.UpdatedAt = DateTime.UtcNow;
        role.UpdatedBy = userCtx.UserId;

        await roles.UpdateAsync(role, cancellationToken);

        return Result.Success(new UpdateRoleResponse { Role = RoleDto.From(role) });
    }
}
