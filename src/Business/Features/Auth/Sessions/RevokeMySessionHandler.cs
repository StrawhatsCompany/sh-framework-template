using Business.Authentication;
using Business.Authentication.Sessions;
using Business.Common;
using SH.Framework.Library.Cqrs.Implementation;

namespace Business.Features.Auth.Sessions;

public sealed class RevokeMySessionCommand : Request
{
    public Guid SessionId { get; set; }
}

public sealed class RevokeMySessionHandler(
    ISessionStore sessions,
    IRefreshTokenStore refreshTokens,
    ITenantContext tenantCtx,
    IUserContext userCtx)
    : RequestHandler<RevokeMySessionCommand>
{
    public override async Task<Result> HandleAsync(RevokeMySessionCommand request, CancellationToken cancellationToken = default)
    {
        if (tenantCtx.TenantId is not { } tenantId || userCtx.UserId is not { } userId)
        {
            return Result.Failure(AuthResultCode.SessionNotFound);
        }

        var session = await sessions.FindByIdAsync(tenantId, request.SessionId, cancellationToken);
        if (session is null || session.UserId != userId)
        {
            return Result.Failure(AuthResultCode.SessionNotFound);
        }

        await sessions.RevokeAsync(tenantId, session.Id, "user-self-revoke", cancellationToken);
        await refreshTokens.RevokeAllForSessionAsync(session.Id, cancellationToken);
        return Result.Success();
    }
}
