using Business.Authentication;
using Business.Authentication.Sessions;
using Business.Common;
using SH.Framework.Library.Cqrs.Implementation;

namespace Business.Features.Auth.Logout;

public sealed class LogoutHandler(
    ISessionStore sessions,
    IRefreshTokenStore refreshTokens,
    ITenantContext tenantCtx)
    : RequestHandler<LogoutCommand>
{
    public override async Task<Result> HandleAsync(LogoutCommand request, CancellationToken cancellationToken = default)
    {
        if (tenantCtx.TenantId is not { } tenantId)
        {
            return Result.Failure(AuthResultCode.SessionNotFound);
        }

        var session = await sessions.FindByIdAsync(tenantId, request.SessionId, cancellationToken);
        if (session is null)
        {
            return Result.Failure(AuthResultCode.SessionNotFound);
        }

        await sessions.RevokeAsync(tenantId, session.Id, "user-logout", cancellationToken);
        await refreshTokens.RevokeAllForSessionAsync(session.Id, cancellationToken);
        return Result.Success();
    }
}
