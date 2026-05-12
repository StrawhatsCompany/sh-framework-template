using Business.Authentication.Authorization;
using Business.Features.Admin.Roles.CreateRole;
using Business.Features.Admin.Roles.DeleteRole;
using Business.Features.Admin.Roles.GetRole;
using Business.Features.Admin.Roles.ListRoles;
using Business.Features.Admin.Roles.SetRolePermissions;
using Business.Features.Admin.Roles.UpdateRole;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SH.Framework.Library.AspNetCore;
using SH.Framework.Library.Cqrs.Implementation;
using WebApi.Common;

namespace WebApi.Endpoints.Admin.Roles;

public sealed class ListRolesEndpoint : IEndpoint
{
    public static string Route => "api/v1/admin/roles";

    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet(Route, async ([FromServices] IProjector projector, CancellationToken ct = default) =>
            (await projector.SendAsync(new ListRolesQuery(), ct)).ToHttp())
        .WithName("ListRoles").WithSummary("List roles in the current tenant").WithTags("Admin / Roles")
        .RequirePermission("admin.roles.read")
        .Produces<Result<ListRolesResponse>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized).ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status400BadRequest).ProducesProblem(StatusCodes.Status500InternalServerError);
}

public sealed class GetRoleEndpoint : IEndpoint
{
    public static string Route => "api/v1/admin/roles/{id:guid}";

    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet(Route, async (Guid id, [FromServices] IProjector projector, CancellationToken ct = default) =>
            (await projector.SendAsync(new GetRoleQuery { Id = id }, ct)).ToHttp())
        .WithName("GetRole").WithSummary("Get a role by id").WithTags("Admin / Roles")
        .RequirePermission("admin.roles.read")
        .Produces<Result<GetRoleResponse>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized).ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status400BadRequest).ProducesProblem(StatusCodes.Status500InternalServerError);
}

public sealed class CreateRoleEndpoint : IEndpoint
{
    public static string Route => "api/v1/admin/roles";

    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPost(Route, async ([FromBody] CreateRoleCommand cmd, [FromServices] IProjector projector, CancellationToken ct = default) =>
            (await projector.SendAsync(cmd, ct)).ToHttp())
        .WithName("CreateRole").WithSummary("Create a role").WithTags("Admin / Roles")
        .RequirePermission("admin.roles.write")
        .Produces<Result<CreateRoleResponse>>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status401Unauthorized).ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status400BadRequest).ProducesProblem(StatusCodes.Status500InternalServerError);
}

public sealed class UpdateRoleEndpoint : IEndpoint
{
    public static string Route => "api/v1/admin/roles/{id:guid}";

    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPatch(Route, async (Guid id, [FromBody] UpdateRoleCommand cmd, [FromServices] IProjector projector, CancellationToken ct = default) =>
        {
            cmd.Id = id;
            return (await projector.SendAsync(cmd, ct)).ToHttp();
        })
        .WithName("UpdateRole").WithSummary("Update a role").WithTags("Admin / Roles")
        .RequirePermission("admin.roles.write")
        .Produces<Result<UpdateRoleResponse>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized).ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status400BadRequest).ProducesProblem(StatusCodes.Status500InternalServerError);
}

public sealed class DeleteRoleEndpoint : IEndpoint
{
    public static string Route => "api/v1/admin/roles/{id:guid}";

    public static void Map(IEndpointRouteBuilder app) =>
        app.MapDelete(Route, async (Guid id, [FromServices] IProjector projector, CancellationToken ct = default) =>
            (await projector.SendAsync(new DeleteRoleCommand { Id = id }, ct)).ToHttp())
        .WithName("DeleteRole").WithSummary("Soft-delete a non-system role").WithTags("Admin / Roles")
        .RequirePermission("admin.roles.write")
        .Produces<Result>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized).ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status400BadRequest).ProducesProblem(StatusCodes.Status500InternalServerError);
}

public sealed class SetRolePermissionsEndpoint : IEndpoint
{
    public static string Route => "api/v1/admin/roles/{id:guid}/permissions";

    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPut(Route, async (Guid id, [FromBody] SetRolePermissionsCommand cmd, [FromServices] IProjector projector, CancellationToken ct = default) =>
        {
            cmd.RoleId = id;
            return (await projector.SendAsync(cmd, ct)).ToHttp();
        })
        .WithName("SetRolePermissions").WithSummary("Replace the role's permission assignments")
        .WithDescription("Body is the full list of permission ids the role grants. Permissions not in the list are revoked.")
        .WithTags("Admin / Roles")
        .RequirePermission("admin.roles.write")
        .Produces<Result>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized).ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status400BadRequest).ProducesProblem(StatusCodes.Status500InternalServerError);
}
