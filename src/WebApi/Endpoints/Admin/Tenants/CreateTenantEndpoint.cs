using Business.Authentication.Authorization;
using Business.Features.Admin.Tenants.CreateTenant;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SH.Framework.Library.AspNetCore;
using SH.Framework.Library.Cqrs.Implementation;
using WebApi.Common;

namespace WebApi.Endpoints.Admin.Tenants;

public sealed class CreateTenantEndpoint : IEndpoint
{
    public static string Route => "api/v1/admin/tenants";

    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost(Route, async (
                    [FromBody] CreateTenantCommand command,
                    [FromServices] IProjector projector,
                    CancellationToken ct = default) =>
                (await projector.SendAsync(command, ct)).ToHttp())
            .WithName("CreateTenant")
            .WithSummary("Create a tenant")
            .WithDescription("Slug must be 2-64 lowercase alphanumeric characters or hyphens; must be unique. DisplayName defaults to the slug if omitted.")
            .WithTags("Admin / Tenants")
            .RequirePermission("admin.tenants.write")
            .Produces<Result<CreateTenantResponse>>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError);
    }
}
