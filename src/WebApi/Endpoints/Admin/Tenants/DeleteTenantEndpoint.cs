using Business.Features.Admin.Tenants.DeleteTenant;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SH.Framework.Library.AspNetCore;
using SH.Framework.Library.Cqrs.Implementation;
using WebApi.Common;

namespace WebApi.Endpoints.Admin.Tenants;

public sealed class DeleteTenantEndpoint : IEndpoint
{
    public static string Route => "api/v1/admin/tenants/{id:guid}";

    public static void Map(IEndpointRouteBuilder app)
    {
        // TODO(perms): gate with [HasPermission("admin.tenants.write")] once #74 lands.
        app.MapDelete(Route, async (Guid id, [FromServices] IProjector projector, CancellationToken ct = default) =>
                (await projector.SendAsync(new DeleteTenantCommand { Id = id }, ct)).ToHttp())
            .WithName("DeleteTenant")
            .WithSummary("Soft-delete a tenant")
            .WithDescription("Sets DeletedAt and DeletedBy. Subsequent reads return TenantNotFound.")
            .WithTags("Admin / Tenants")
            .Produces<Result>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError);
    }
}
