using Business.Features.Admin.Permissions.CreatePermission;
using Business.Features.Admin.Permissions.DeletePermission;
using Business.Features.Admin.Permissions.GetPermission;
using Business.Features.Admin.Permissions.ListPermissions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SH.Framework.Library.AspNetCore;
using SH.Framework.Library.Cqrs.Implementation;
using WebApi.Common;

namespace WebApi.Endpoints.Admin.Permissions;

// TODO(perms): every route here lands behind [HasPermission("admin.permissions.*")] once #75 brings authentication.

public sealed class ListPermissionsEndpoint : IEndpoint
{
    public static string Route => "api/v1/admin/permissions";

    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet(Route, async ([FromQuery] string? category, [FromServices] IProjector projector, CancellationToken ct = default) =>
            (await projector.SendAsync(new ListPermissionsQuery { Category = category }, ct)).ToHttp())
        .WithName("ListPermissions").WithSummary("List permissions in the global catalog").WithTags("Admin / Permissions")
        .Produces<Result<ListPermissionsResponse>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status500InternalServerError);
}

public sealed class GetPermissionEndpoint : IEndpoint
{
    public static string Route => "api/v1/admin/permissions/{id:guid}";

    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet(Route, async (Guid id, [FromServices] IProjector projector, CancellationToken ct = default) =>
            (await projector.SendAsync(new GetPermissionQuery { Id = id }, ct)).ToHttp())
        .WithName("GetPermission").WithSummary("Get a permission by id").WithTags("Admin / Permissions")
        .Produces<Result<GetPermissionResponse>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status500InternalServerError);
}

public sealed class CreatePermissionEndpoint : IEndpoint
{
    public static string Route => "api/v1/admin/permissions";

    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPost(Route, async ([FromBody] CreatePermissionCommand cmd, [FromServices] IProjector projector, CancellationToken ct = default) =>
            (await projector.SendAsync(cmd, ct)).ToHttp())
        .WithName("CreatePermission").WithSummary("Add a permission to the catalog")
        .WithDescription("Name must be dotted lowercase (e.g. orders.read, billing.invoices.refund).")
        .WithTags("Admin / Permissions")
        .Produces<Result<CreatePermissionResponse>>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status500InternalServerError);
}

public sealed class DeletePermissionEndpoint : IEndpoint
{
    public static string Route => "api/v1/admin/permissions/{id:guid}";

    public static void Map(IEndpointRouteBuilder app) =>
        app.MapDelete(Route, async (Guid id, [FromServices] IProjector projector, CancellationToken ct = default) =>
            (await projector.SendAsync(new DeletePermissionCommand { Id = id }, ct)).ToHttp())
        .WithName("DeletePermission").WithSummary("Remove a permission from the catalog")
        .WithDescription("Removes the permission row globally. Existing role-permission joins will need to be cleaned up separately.")
        .WithTags("Admin / Permissions")
        .Produces<Result>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status500InternalServerError);
}
