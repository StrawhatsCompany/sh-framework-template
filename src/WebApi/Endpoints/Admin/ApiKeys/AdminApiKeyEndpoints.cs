using Business.Authentication.Authorization;
using Business.Features.Admin.ApiKeys;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SH.Framework.Library.AspNetCore;
using SH.Framework.Library.Cqrs.Implementation;
using WebApi.Common;

namespace WebApi.Endpoints.Admin.ApiKeys;

public sealed class ListApiKeysEndpoint : IEndpoint
{
    public static string Route => "api/v1/admin/api-keys";

    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet(Route, async ([FromQuery] Guid? userId, [FromServices] IProjector projector, CancellationToken ct = default) =>
            (await projector.SendAsync(new ListApiKeysQuery { UserId = userId }, ct)).ToHttp())
        .WithName("ListApiKeys").WithSummary("List API keys across the tenant, optionally filtered by user").WithTags("Admin / API Keys")
        .RequirePermission("admin.api-keys.read")
        .Produces<Result<ListApiKeysResponse>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized).ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status400BadRequest).ProducesProblem(StatusCodes.Status500InternalServerError);
}

public sealed class RevokeApiKeyEndpoint : IEndpoint
{
    public static string Route => "api/v1/admin/api-keys/{id:guid}";

    public static void Map(IEndpointRouteBuilder app) =>
        app.MapDelete(Route, async (Guid id, [FromServices] IProjector projector, CancellationToken ct = default) =>
            (await projector.SendAsync(new RevokeApiKeyCommand { Id = id }, ct)).ToHttp())
        .WithName("RevokeApiKey").WithSummary("Revoke an API key (admin)").WithTags("Admin / API Keys")
        .RequirePermission("admin.api-keys.write")
        .Produces<Result>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized).ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status400BadRequest).ProducesProblem(StatusCodes.Status500InternalServerError);
}
