using Business.Authentication.Sso;
using Business.Common;
using Business.Identity;
using Domain.Entities.Identity;
using SH.Framework.Library.Cqrs.Implementation;

namespace Business.Features.Auth.SsoList;

public sealed class ListPublicSsoProvidersQuery : Request<ListPublicSsoProvidersResponse> { }

public sealed class ListPublicSsoProvidersResponse
{
    public required IReadOnlyList<PublicSsoProviderDto> Items { get; init; }
}

public sealed record PublicSsoProviderDto(Guid Id, string Name, string DisplayName);

public sealed class ListPublicSsoProvidersHandler(ISsoProviderStore store, ITenantContext tenantCtx)
    : RequestHandler<ListPublicSsoProvidersQuery, ListPublicSsoProvidersResponse>
{
    public override async Task<Result<ListPublicSsoProvidersResponse>> HandleAsync(
        ListPublicSsoProvidersQuery request, CancellationToken cancellationToken = default)
    {
        if (tenantCtx.TenantId is not { } tenantId)
        {
            return Result.Failure<ListPublicSsoProvidersResponse>(IdentityResultCode.TenantRequired);
        }
        var list = await store.ListAsync(tenantId, cancellationToken);
        var active = list
            .Where(p => p.Status == SsoProviderStatus.Active)
            .Select(p => new PublicSsoProviderDto(p.Id, p.Name, p.DisplayName))
            .ToList();
        return Result.Success(new ListPublicSsoProvidersResponse { Items = active });
    }
}
