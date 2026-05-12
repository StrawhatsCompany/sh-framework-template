using Business.Authentication;
using Business.Authentication.Sessions;
using Business.Common;
using SH.Framework.Library.Cqrs.Implementation;

namespace Business.Features.Auth.Sessions;

public sealed class ListMySessionsQuery : Request<ListMySessionsResponse> { }

public sealed class ListMySessionsResponse
{
    public required IReadOnlyList<SessionDto> Items { get; init; }
}

public sealed class ListMySessionsHandler(
    ISessionStore sessions,
    ITenantContext tenantCtx,
    IUserContext userCtx)
    : RequestHandler<ListMySessionsQuery, ListMySessionsResponse>
{
    public override async Task<Result<ListMySessionsResponse>> HandleAsync(
        ListMySessionsQuery request, CancellationToken cancellationToken = default)
    {
        if (tenantCtx.TenantId is not { } tenantId || userCtx.UserId is not { } userId)
        {
            return Result.Failure<ListMySessionsResponse>(AuthResultCode.SessionNotFound);
        }
        var list = await sessions.ListActiveByUserAsync(tenantId, userId, cancellationToken);
        return Result.Success(new ListMySessionsResponse
        {
            Items = list.Select(SessionDto.From).ToList(),
        });
    }
}
