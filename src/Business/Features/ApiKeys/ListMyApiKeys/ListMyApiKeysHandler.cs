using Business.Authentication;
using Business.Authentication.ApiKeys;
using Business.Common;
using Business.Features.ApiKeys;
using SH.Framework.Library.Cqrs.Implementation;

namespace Business.Features.ApiKeys.ListMyApiKeys;

public sealed class ListMyApiKeysQuery : Request<ListMyApiKeysResponse> { }

public sealed class ListMyApiKeysResponse
{
    public required IReadOnlyList<ApiKeyDto> Items { get; init; }
}

public sealed class ListMyApiKeysHandler(
    IApiKeyStore apiKeys,
    ITenantContext tenantCtx,
    IUserContext userCtx)
    : RequestHandler<ListMyApiKeysQuery, ListMyApiKeysResponse>
{
    public override async Task<Result<ListMyApiKeysResponse>> HandleAsync(
        ListMyApiKeysQuery request, CancellationToken cancellationToken = default)
    {
        if (tenantCtx.TenantId is not { } tenantId || userCtx.UserId is not { } userId)
        {
            return Result.Failure<ListMyApiKeysResponse>(AuthResultCode.InvalidCredentials);
        }
        var list = await apiKeys.ListByUserAsync(tenantId, userId, cancellationToken);
        return Result.Success(new ListMyApiKeysResponse
        {
            Items = list.Select(ApiKeyDto.From).ToList(),
        });
    }
}
