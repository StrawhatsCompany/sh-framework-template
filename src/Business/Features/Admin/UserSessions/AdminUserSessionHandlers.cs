using Business.Authentication;
using Business.Authentication.Sessions;
using Business.Common;
using Business.Features.Auth.Sessions;
using Business.Identity;
using SH.Framework.Library.Cqrs.Implementation;

namespace Business.Features.Admin.UserSessions;

public sealed class ListUserSessionsQuery : Request<ListUserSessionsResponse>
{
    public Guid UserId { get; set; }
}

public sealed class ListUserSessionsResponse
{
    public required IReadOnlyList<SessionDto> Items { get; init; }
}

public sealed class ListUserSessionsHandler(
    ISessionStore sessions,
    IUserStore users,
    ITenantContext tenantCtx)
    : RequestHandler<ListUserSessionsQuery, ListUserSessionsResponse>
{
    public override async Task<Result<ListUserSessionsResponse>> HandleAsync(
        ListUserSessionsQuery request, CancellationToken cancellationToken = default)
    {
        if (tenantCtx.TenantId is not { } tenantId)
        {
            return Result.Failure<ListUserSessionsResponse>(IdentityResultCode.TenantRequired);
        }
        if (await users.FindByIdAsync(tenantId, request.UserId, cancellationToken) is null)
        {
            return Result.Failure<ListUserSessionsResponse>(IdentityResultCode.UserNotFound);
        }

        var list = await sessions.ListActiveByUserAsync(tenantId, request.UserId, cancellationToken);
        return Result.Success(new ListUserSessionsResponse
        {
            Items = list.Select(SessionDto.From).ToList(),
        });
    }
}

public sealed class RevokeUserSessionCommand : Request
{
    public Guid UserId { get; set; }
    public Guid SessionId { get; set; }
}

public sealed class RevokeUserSessionHandler(
    ISessionStore sessions,
    IRefreshTokenStore refreshTokens,
    ITenantContext tenantCtx)
    : RequestHandler<RevokeUserSessionCommand>
{
    public override async Task<Result> HandleAsync(RevokeUserSessionCommand request, CancellationToken cancellationToken = default)
    {
        if (tenantCtx.TenantId is not { } tenantId)
        {
            return Result.Failure(IdentityResultCode.TenantRequired);
        }

        var session = await sessions.FindByIdAsync(tenantId, request.SessionId, cancellationToken);
        if (session is null || session.UserId != request.UserId)
        {
            return Result.Failure(AuthResultCode.SessionNotFound);
        }

        await sessions.RevokeAsync(tenantId, session.Id, "admin-revoke", cancellationToken);
        await refreshTokens.RevokeAllForSessionAsync(session.Id, cancellationToken);
        return Result.Success();
    }
}

public sealed class RevokeAllUserSessionsCommand : Request<RevokeAllUserSessionsResponse>
{
    public Guid UserId { get; set; }
}

public sealed class RevokeAllUserSessionsResponse
{
    public required int RevokedCount { get; init; }
}

public sealed class RevokeAllUserSessionsHandler(
    ISessionStore sessions,
    IRefreshTokenStore refreshTokens,
    ITenantContext tenantCtx)
    : RequestHandler<RevokeAllUserSessionsCommand, RevokeAllUserSessionsResponse>
{
    public override async Task<Result<RevokeAllUserSessionsResponse>> HandleAsync(
        RevokeAllUserSessionsCommand request, CancellationToken cancellationToken = default)
    {
        if (tenantCtx.TenantId is not { } tenantId)
        {
            return Result.Failure<RevokeAllUserSessionsResponse>(IdentityResultCode.TenantRequired);
        }

        var active = await sessions.ListActiveByUserAsync(tenantId, request.UserId, cancellationToken);
        var count = await sessions.RevokeAllForUserAsync(tenantId, request.UserId, "admin-revoke-all", cancellationToken);
        foreach (var s in active)
        {
            await refreshTokens.RevokeAllForSessionAsync(s.Id, cancellationToken);
        }
        return Result.Success(new RevokeAllUserSessionsResponse { RevokedCount = count });
    }
}
