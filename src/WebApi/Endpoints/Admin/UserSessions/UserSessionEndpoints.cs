using Business.Authentication.Authorization;
using Business.Features.Admin.UserSessions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SH.Framework.Library.AspNetCore;
using SH.Framework.Library.Cqrs.Implementation;
using WebApi.Common;

namespace WebApi.Endpoints.Admin.UserSessions;

public sealed class ListUserSessionsEndpoint : IEndpoint
{
    public static string Route => "api/v1/admin/users/{userId:guid}/sessions";

    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet(Route, async (Guid userId, [FromServices] IProjector projector, CancellationToken ct = default) =>
            (await projector.SendAsync(new ListUserSessionsQuery { UserId = userId }, ct)).ToHttp())
        .WithName("ListUserSessions").WithSummary("List active sessions for a user").WithTags("Admin / Users")
        .RequirePermission("admin.users.sessions.read")
        .Produces<Result<ListUserSessionsResponse>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized).ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status400BadRequest).ProducesProblem(StatusCodes.Status500InternalServerError);
}

public sealed class RevokeUserSessionEndpoint : IEndpoint
{
    public static string Route => "api/v1/admin/users/{userId:guid}/sessions/{sessionId:guid}";

    public static void Map(IEndpointRouteBuilder app) =>
        app.MapDelete(Route, async (Guid userId, Guid sessionId, [FromServices] IProjector projector, CancellationToken ct = default) =>
            (await projector.SendAsync(new RevokeUserSessionCommand { UserId = userId, SessionId = sessionId }, ct)).ToHttp())
        .WithName("RevokeUserSession").WithSummary("Revoke a specific session for a user").WithTags("Admin / Users")
        .RequirePermission("admin.users.sessions.write")
        .Produces<Result>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized).ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status400BadRequest).ProducesProblem(StatusCodes.Status500InternalServerError);
}

public sealed class RevokeAllUserSessionsEndpoint : IEndpoint
{
    public static string Route => "api/v1/admin/users/{userId:guid}/sessions";

    public static void Map(IEndpointRouteBuilder app) =>
        app.MapDelete(Route, async (Guid userId, [FromServices] IProjector projector, CancellationToken ct = default) =>
            (await projector.SendAsync(new RevokeAllUserSessionsCommand { UserId = userId }, ct)).ToHttp())
        .WithName("RevokeAllUserSessions").WithSummary("Revoke every active session for a user").WithTags("Admin / Users")
        .RequirePermission("admin.users.sessions.write")
        .Produces<Result<RevokeAllUserSessionsResponse>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized).ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status400BadRequest).ProducesProblem(StatusCodes.Status500InternalServerError);
}
