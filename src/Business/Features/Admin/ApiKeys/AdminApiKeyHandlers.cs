using Business.Authentication;
using Business.Authentication.ApiKeys;
using Business.Common;
using Business.Features.ApiKeys;
using Business.Identity;
using SH.Framework.Library.Cqrs.Implementation;

namespace Business.Features.Admin.ApiKeys;

public sealed class ListApiKeysQuery : Request<ListApiKeysResponse>
{
    public Guid? UserId { get; set; }
}

public sealed class ListApiKeysResponse
{
    public required IReadOnlyList<ApiKeyDto> Items { get; init; }
}

public sealed class ListApiKeysHandler(IApiKeyStore apiKeys, ITenantContext tenantCtx)
    : RequestHandler<ListApiKeysQuery, ListApiKeysResponse>
{
    public override async Task<Result<ListApiKeysResponse>> HandleAsync(
        ListApiKeysQuery request, CancellationToken cancellationToken = default)
    {
        if (tenantCtx.TenantId is not { } tenantId)
        {
            return Result.Failure<ListApiKeysResponse>(IdentityResultCode.TenantRequired);
        }
        var list = await apiKeys.ListByTenantAsync(tenantId, request.UserId, cancellationToken);
        return Result.Success(new ListApiKeysResponse
        {
            Items = list.Select(ApiKeyDto.From).ToList(),
        });
    }
}

public sealed class RevokeApiKeyCommand : Request
{
    public Guid Id { get; set; }
}

public sealed class RevokeApiKeyHandler(IApiKeyStore apiKeys, ITenantContext tenantCtx, IUserContext userCtx)
    : RequestHandler<RevokeApiKeyCommand>
{
    public override async Task<Result> HandleAsync(RevokeApiKeyCommand request, CancellationToken cancellationToken = default)
    {
        if (tenantCtx.TenantId is not { } tenantId)
        {
            return Result.Failure(IdentityResultCode.TenantRequired);
        }
        var ok = await apiKeys.RevokeAsync(tenantId, request.Id, userCtx.UserId, cancellationToken);
        return ok ? Result.Success() : Result.Failure(AuthResultCode.ApiKeyNotFound);
    }
}
