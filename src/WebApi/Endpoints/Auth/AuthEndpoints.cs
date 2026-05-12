using Business.Features.Auth.Login;
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
        app.MapPost(Route, async ([FromBody] LoginCommand cmd, [FromServices] IProjector projector, CancellationToken ct = default) =>
            (await projector.SendAsync(cmd, ct)).ToHttp())
        .WithName("Login")
        .WithSummary("Exchange tenant + identifier + password for a JWT access token")
        .WithDescription("Identifier is either email or username. Status checks (Disabled, Locked, PendingVerification) run before password verification so distinct codes can surface; raw bad-password attempts return generic InvalidCredentials and increment the lockout counter.")
        .WithTags("Auth")
        .AllowAnonymous()
        .Produces<Result<LoginResponse>>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status500InternalServerError);
}

public sealed class LogoutEndpoint : IEndpoint
{
    public static string Route => "api/v1/auth/logout";

    // Best-effort marker for now — there's no Session entity yet (lands in #76). The client
    // discards the token; on #76 this endpoint also revokes the session row.
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPost(Route, () => Results.Ok(Result.Success()))
        .WithName("Logout")
        .WithSummary("Discard the current token (best-effort until session revocation lands in #76)")
        .WithTags("Auth")
        .Produces<Result>(StatusCodes.Status200OK);
}
