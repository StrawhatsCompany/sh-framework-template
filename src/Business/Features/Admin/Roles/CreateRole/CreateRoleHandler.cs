using Business.Common;
using Business.Identity;
using Domain.Entities.Identity;
using SH.Framework.Library.Cqrs.Implementation;

namespace Business.Features.Admin.Roles.CreateRole;

public sealed class CreateRoleCommand : Request<CreateRoleResponse>
{
    public string Name { get; set; } = "";
    public string? Description { get; set; }
}

public sealed class CreateRoleResponse
{
    public required RoleDto Role { get; init; }
}

public sealed class CreateRoleHandler(IRoleStore roles, ITenantContext tenantCtx, IUserContext userCtx)
    : RequestHandler<CreateRoleCommand, CreateRoleResponse>
{
    public override async Task<Result<CreateRoleResponse>> HandleAsync(
        CreateRoleCommand request, CancellationToken cancellationToken = default)
    {
        if (tenantCtx.TenantId is not { } tenantId)
        {
            return Result.Failure<CreateRoleResponse>(IdentityResultCode.TenantRequired);
        }

        var name = request.Name.Trim();
        if (name.Length is < 2 or > 64)
        {
            return Result.Failure<CreateRoleResponse>(IdentityResultCode.RoleNameInvalid);
        }
        if (await roles.FindByNameAsync(tenantId, name, cancellationToken) is not null)
        {
            return Result.Failure<CreateRoleResponse>(IdentityResultCode.RoleNameAlreadyExists);
        }

        var role = new Role
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name,
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            IsSystem = false,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = userCtx.UserId,
        };

        await roles.AddAsync(role, cancellationToken);

        return Result.Success(new CreateRoleResponse { Role = RoleDto.From(role) });
    }
}
