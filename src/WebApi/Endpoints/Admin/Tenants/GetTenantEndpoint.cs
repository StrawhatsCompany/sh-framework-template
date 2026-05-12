using Business.Authentication.Authorization;
using Business.Features.Admin.Tenants.GetTenant;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SH.Framework.Library.AspNetCore;
using SH.Framework.Library.Cqrs.Implementation;
using WebApi.Common;

namespace WebApi.Endpoints.Admin.Tenants;

public sealed class GetTenantEndpoint : IEndpoint
{
    public static string Route => "api/v1/admin/tenants/{id:guid}";

    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(Route, async (Guid id, [FromServices] IProjector projector, CancellationToken ct = default) =>
                (await projector.SendAsync(new GetTenantQuery { Id = id }, ct)).ToHttp())
            .WithName("GetTenant")
            .WithSummary("Get a tenant by id")
            .WithTags("Admin / Tenants")
            .RequirePermission("admin.tenants.read")
            .Produces<Result<GetTenantResponse>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError);
    }
}
