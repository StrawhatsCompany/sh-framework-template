using Business.Features.Admin.Tenants.ListTenants;
using Domain.Entities.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SH.Framework.Library.AspNetCore;
using SH.Framework.Library.Cqrs.Implementation;
using WebApi.Common;

namespace WebApi.Endpoints.Admin.Tenants;

public sealed class ListTenantsEndpoint : IEndpoint
{
    public static string Route => "api/v1/admin/tenants";

    public static void Map(IEndpointRouteBuilder app)
    {
        // TODO(perms): gate with [HasPermission("admin.tenants.read")] once #74 lands.
        app.MapGet(Route, async (
                    [FromQuery] TenantStatus? status,
                    [FromServices] IProjector projector,
                    CancellationToken ct = default) =>
                (await projector.SendAsync(new ListTenantsQuery { Status = status }, ct)).ToHttp())
            .WithName("ListTenants")
            .WithSummary("List tenants")
            .WithDescription("Returns all tenants, optionally filtered by status. Soft-deleted tenants are excluded.")
            .WithTags("Admin / Tenants")
            .Produces<Result<ListTenantsResponse>>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError);
    }
}
