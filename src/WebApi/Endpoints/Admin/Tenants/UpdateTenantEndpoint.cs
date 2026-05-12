using Business.Authentication.Authorization;
using Business.Features.Admin.Tenants.UpdateTenant;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SH.Framework.Library.AspNetCore;
using SH.Framework.Library.Cqrs.Implementation;
using WebApi.Common;

namespace WebApi.Endpoints.Admin.Tenants;

public sealed class UpdateTenantEndpoint : IEndpoint
{
    public static string Route => "api/v1/admin/tenants/{id:guid}";

    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPatch(Route, async (
                    Guid id,
                    [FromBody] UpdateTenantCommand command,
                    [FromServices] IProjector projector,
                    CancellationToken ct = default) =>
            {
                command.Id = id;
                return (await projector.SendAsync(command, ct)).ToHttp();
            })
            .WithName("UpdateTenant")
            .WithSummary("Update a tenant")
            .WithDescription("Updates DisplayName and/or Status. Null fields are left unchanged.")
            .WithTags("Admin / Tenants")
            .RequirePermission("admin.tenants.write")
            .Produces<Result<UpdateTenantResponse>>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError);
    }
}
