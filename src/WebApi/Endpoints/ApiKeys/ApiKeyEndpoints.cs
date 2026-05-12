using Business.Authentication.Authorization;
using Business.Features.ApiKeys.CreateMyApiKey;
using Business.Features.ApiKeys.ListMyApiKeys;
using Business.Features.ApiKeys.RevokeMyApiKey;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SH.Framework.Library.AspNetCore;
using SH.Framework.Library.Cqrs.Implementation;
using WebApi.Common;

namespace WebApi.Endpoints.ApiKeys;

public sealed class CreateMyApiKeyEndpoint : IEndpoint
{
    public static string Route => "api/v1/api-keys";

    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPost(Route, async ([FromBody] CreateMyApiKeyCommand cmd, [FromServices] IProjector projector, CancellationToken ct = default) =>
            (await projector.SendAsync(cmd, ct)).ToHttp())
        .WithName("CreateMyApiKey")
        .WithSummary("Mint a new API key for the caller")
        .WithDescription("Returns the full plaintext token ONCE. Subsequent reads expose only Prefix + Last4. Store the token securely; the framework cannot recover it.")
        .WithTags("API Keys")
        .RequirePermission("api-keys.write")
        .Produces<Result<CreateMyApiKeyResponse>>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status401Unauthorized).ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status400BadRequest).ProducesProblem(StatusCodes.Status500InternalServerError);
}

public sealed class ListMyApiKeysEndpoint : IEndpoint
{
    public static string Route => "api/v1/api-keys";

    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet(Route, async ([FromServices] IProjector projector, CancellationToken ct = default) =>
            (await projector.SendAsync(new ListMyApiKeysQuery(), ct)).ToHttp())
        .WithName("ListMyApiKeys")
        .WithSummary("List the caller's API keys (prefix + last4 only; plaintext is never returned again)")
        .WithTags("API Keys")
        .RequirePermission("api-keys.read")
        .Produces<Result<ListMyApiKeysResponse>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized).ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status400BadRequest).ProducesProblem(StatusCodes.Status500InternalServerError);
}

public sealed class RevokeMyApiKeyEndpoint : IEndpoint
{
    public static string Route => "api/v1/api-keys/{id:guid}";

    public static void Map(IEndpointRouteBuilder app) =>
        app.MapDelete(Route, async (Guid id, [FromServices] IProjector projector, CancellationToken ct = default) =>
            (await projector.SendAsync(new RevokeMyApiKeyCommand { Id = id }, ct)).ToHttp())
        .WithName("RevokeMyApiKey")
        .WithSummary("Revoke one of the caller's API keys")
        .WithTags("API Keys")
        .RequirePermission("api-keys.write")
        .Produces<Result>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized).ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status400BadRequest).ProducesProblem(StatusCodes.Status500InternalServerError);
}
