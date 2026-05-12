using Business.Features.Auth.Login;
using Business.Features.Auth.Logout;
using Business.Features.Auth.Refresh;
using Business.Features.Auth.Sessions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SH.Framework.Library.AspNetCore;
using SH.Framework.Library.Cqrs.Implementation;
using WebApi.Common;

namespace WebApi.Endpoints.Auth;

public sealed class LoginEndpoint : IEndpoint
{
    public static string Route => "api/v1/auth/login";

    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPost(Route, async (
                [FromBody] LoginCommand cmd, HttpContext http,
                [FromServices] IProjector projector, CancellationToken ct = default) =>
            {
                cmd.Ip ??= http.Connection.RemoteIpAddress?.ToString();
                return (await projector.SendAsync(cmd, ct)).ToHttp();
            })
        .WithName("Login")
        .WithSummary("Exchange tenant + identifier + password for an access + refresh token pair")
        .WithDescription("Identifier is either email or username. Returns a JWT access token and a refresh token; the refresh token is shown only at issuance. Status checks (Disabled/Locked/PendingVerification) run before password verification so distinct codes can surface; raw bad-password attempts return generic InvalidCredentials and increment the lockout counter.")
        .WithTags("Auth")
        .AllowAnonymous()
        .Produces<Result<LoginResponse>>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status500InternalServerError);
}

public sealed class RefreshEndpoint : IEndpoint
{
    public static string Route => "api/v1/auth/refresh";

    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPost(Route, async (
                [FromBody] RefreshCommand cmd, HttpContext http,
                [FromServices] IProjector projector, CancellationToken ct = default) =>
            {
                cmd.Ip ??= http.Connection.RemoteIpAddress?.ToString();
                return (await projector.SendAsync(cmd, ct)).ToHttp();
            })
        .WithName("Refresh")
        .WithSummary("Rotate a refresh token for a new access + refresh pair")
        .WithDescription("Each successful refresh consumes the presented token and issues a new pair. Presenting an already-consumed or revoked token invalidates the entire token family AND revokes the session (replay-attack defence per RFC 6749 §10.4 / OAuth 2.0 BCP).")
        .WithTags("Auth")
        .AllowAnonymous()
        .Produces<Result<RefreshResponse>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status500InternalServerError);
}

public sealed class LogoutEndpoint : IEndpoint
{
    public static string Route => "api/v1/auth/logout";

    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPost(Route, async (HttpContext http, [FromServices] IProjector projector, CancellationToken ct = default) =>
        {
            var sidClaim = http.User.FindFirst("sid")?.Value;
            if (!Guid.TryParse(sidClaim, out var sessionId))
            {
                return Results.Problem(detail: "Token does not carry a session id.", statusCode: StatusCodes.Status400BadRequest);
            }
            return (await projector.SendAsync(new LogoutCommand { SessionId = sessionId }, ct)).ToHttp();
        })
        .WithName("Logout")
        .WithSummary("Revoke the current session and all its refresh tokens")
        .WithDescription("Reads the session id from the access-token `sid` claim and revokes the session plus every refresh token attached to it.")
        .WithTags("Auth")
        .RequireAuthorization()
        .Produces<Result>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status400BadRequest);
}

public sealed class ListMySessionsEndpoint : IEndpoint
{
    public static string Route => "api/v1/auth/sessions";

    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet(Route, async ([FromServices] IProjector projector, CancellationToken ct = default) =>
            (await projector.SendAsync(new ListMySessionsQuery(), ct)).ToHttp())
        .WithName("ListMySessions")
        .WithSummary("List the caller's active sessions (one per logged-in device)")
        .WithTags("Auth / Sessions")
        .RequireAuthorization()
        .Produces<Result<ListMySessionsResponse>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status400BadRequest);
}

public sealed class RevokeMySessionEndpoint : IEndpoint
{
    public static string Route => "api/v1/auth/sessions/{sessionId:guid}";

    public static void Map(IEndpointRouteBuilder app) =>
        app.MapDelete(Route, async (Guid sessionId, [FromServices] IProjector projector, CancellationToken ct = default) =>
            (await projector.SendAsync(new RevokeMySessionCommand { SessionId = sessionId }, ct)).ToHttp())
        .WithName("RevokeMySession")
        .WithSummary("Revoke one of the caller's sessions")
        .WithTags("Auth / Sessions")
        .RequireAuthorization()
        .Produces<Result>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status400BadRequest);
}
