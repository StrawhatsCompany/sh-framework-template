using System.Text.RegularExpressions;
using Business.Authentication.Sso;
using Business.Common;
using Business.Configuration;
using Business.Identity;
using Domain.Entities.Identity;
using SH.Framework.Library.Cqrs.Implementation;

namespace Business.Features.Admin.SsoProviders;

// ---- List

public sealed class ListSsoProvidersQuery : Request<ListSsoProvidersResponse> { }
public sealed class ListSsoProvidersResponse
{
    public required IReadOnlyList<SsoProviderDto> Items { get; init; }
}

public sealed class ListSsoProvidersHandler(ISsoProviderStore store, ITenantContext tenantCtx)
    : RequestHandler<ListSsoProvidersQuery, ListSsoProvidersResponse>
{
    public override async Task<Result<ListSsoProvidersResponse>> HandleAsync(
        ListSsoProvidersQuery request, CancellationToken cancellationToken = default)
    {
        if (tenantCtx.TenantId is not { } tenantId)
        {
            return Result.Failure<ListSsoProvidersResponse>(IdentityResultCode.TenantRequired);
        }
        var list = await store.ListAsync(tenantId, cancellationToken);
        return Result.Success(new ListSsoProvidersResponse { Items = list.Select(SsoProviderDto.From).ToList() });
    }
}

// ---- Get

public sealed class GetSsoProviderQuery : Request<GetSsoProviderResponse>
{
    public Guid Id { get; set; }
}
public sealed class GetSsoProviderResponse
{
    public required SsoProviderDto Provider { get; init; }
}

public sealed class GetSsoProviderHandler(ISsoProviderStore store, ITenantContext tenantCtx)
    : RequestHandler<GetSsoProviderQuery, GetSsoProviderResponse>
{
    public override async Task<Result<GetSsoProviderResponse>> HandleAsync(
        GetSsoProviderQuery request, CancellationToken cancellationToken = default)
    {
        if (tenantCtx.TenantId is not { } tenantId)
        {
            return Result.Failure<GetSsoProviderResponse>(IdentityResultCode.TenantRequired);
        }
        var p = await store.FindByIdAsync(tenantId, request.Id, cancellationToken);
        return p is null
            ? Result.Failure<GetSsoProviderResponse>(SsoResultCode.ProviderNotFound)
            : Result.Success(new GetSsoProviderResponse { Provider = SsoProviderDto.From(p) });
    }
}

// ---- Create

public sealed class CreateSsoProviderCommand : Request<CreateSsoProviderResponse>
{
    public string Name { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public SsoProtocol Protocol { get; set; } = SsoProtocol.Oidc;
    public string? DiscoveryUrl { get; set; }
    public string AuthorizationEndpoint { get; set; } = "";
    public string TokenEndpoint { get; set; } = "";
    public string? UserInfoEndpoint { get; set; }
    public string? JwksUri { get; set; }
    public string Issuer { get; set; } = "";
    public string ClientId { get; set; } = "";
    public string ClientSecret { get; set; } = "";
    public string Scopes { get; set; } = "openid profile email";
    public string ClaimMappingJson { get; set; } = "{}";
}
public sealed class CreateSsoProviderResponse
{
    public required SsoProviderDto Provider { get; init; }
}

public sealed partial class CreateSsoProviderHandler(
    ISsoProviderStore store,
    ICredentialProtector protector,
    ITenantContext tenantCtx,
    IUserContext userCtx)
    : RequestHandler<CreateSsoProviderCommand, CreateSsoProviderResponse>
{
    [GeneratedRegex("^[a-z0-9][a-z0-9-]{0,62}[a-z0-9]$", RegexOptions.Compiled)]
    private static partial Regex NamePattern();

    public override async Task<Result<CreateSsoProviderResponse>> HandleAsync(
        CreateSsoProviderCommand request, CancellationToken cancellationToken = default)
    {
        if (tenantCtx.TenantId is not { } tenantId)
        {
            return Result.Failure<CreateSsoProviderResponse>(IdentityResultCode.TenantRequired);
        }

        var name = request.Name.Trim().ToLowerInvariant();
        if (!NamePattern().IsMatch(name))
        {
            return Result.Failure<CreateSsoProviderResponse>(IdentityResultCode.TenantSlugInvalid);
        }
        if (await store.FindByNameAsync(tenantId, name, cancellationToken) is not null)
        {
            return Result.Failure<CreateSsoProviderResponse>(SsoResultCode.ProviderNameAlreadyExists);
        }

        var provider = new SsoProvider
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name,
            DisplayName = string.IsNullOrWhiteSpace(request.DisplayName) ? name : request.DisplayName.Trim(),
            Protocol = request.Protocol,
            DiscoveryUrl = request.DiscoveryUrl,
            AuthorizationEndpoint = request.AuthorizationEndpoint.Trim(),
            TokenEndpoint = request.TokenEndpoint.Trim(),
            UserInfoEndpoint = string.IsNullOrWhiteSpace(request.UserInfoEndpoint) ? null : request.UserInfoEndpoint.Trim(),
            JwksUri = string.IsNullOrWhiteSpace(request.JwksUri) ? null : request.JwksUri.Trim(),
            Issuer = request.Issuer.Trim(),
            ClientId = request.ClientId.Trim(),
            ClientSecretCipher = protector.Protect(request.ClientSecret),
            Scopes = request.Scopes,
            ClaimMappingJson = string.IsNullOrWhiteSpace(request.ClaimMappingJson) ? "{}" : request.ClaimMappingJson,
            Status = SsoProviderStatus.Active,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = userCtx.UserId,
        };
        await store.AddAsync(provider, cancellationToken);

        return Result.Success(new CreateSsoProviderResponse { Provider = SsoProviderDto.From(provider) });
    }
}

// ---- Update

public sealed class UpdateSsoProviderCommand : Request<UpdateSsoProviderResponse>
{
    public Guid Id { get; set; }
    public string? DisplayName { get; set; }
    public string? DiscoveryUrl { get; set; }
    public string? AuthorizationEndpoint { get; set; }
    public string? TokenEndpoint { get; set; }
    public string? UserInfoEndpoint { get; set; }
    public string? JwksUri { get; set; }
    public string? Issuer { get; set; }
    public string? ClientId { get; set; }
    /// <summary>Set to a new value to rotate the client secret; null leaves the existing cipher.</summary>
    public string? ClientSecret { get; set; }
    public string? Scopes { get; set; }
    public string? ClaimMappingJson { get; set; }
    public SsoProviderStatus? Status { get; set; }
}
public sealed class UpdateSsoProviderResponse
{
    public required SsoProviderDto Provider { get; init; }
}

public sealed class UpdateSsoProviderHandler(
    ISsoProviderStore store,
    ICredentialProtector protector,
    ITenantContext tenantCtx,
    IUserContext userCtx)
    : RequestHandler<UpdateSsoProviderCommand, UpdateSsoProviderResponse>
{
    public override async Task<Result<UpdateSsoProviderResponse>> HandleAsync(
        UpdateSsoProviderCommand request, CancellationToken cancellationToken = default)
    {
        if (tenantCtx.TenantId is not { } tenantId)
        {
            return Result.Failure<UpdateSsoProviderResponse>(IdentityResultCode.TenantRequired);
        }
        var p = await store.FindByIdAsync(tenantId, request.Id, cancellationToken);
        if (p is null)
        {
            return Result.Failure<UpdateSsoProviderResponse>(SsoResultCode.ProviderNotFound);
        }

        if (request.DisplayName is { } dn && !string.IsNullOrWhiteSpace(dn)) p.DisplayName = dn.Trim();
        if (request.DiscoveryUrl is not null) p.DiscoveryUrl = string.IsNullOrWhiteSpace(request.DiscoveryUrl) ? null : request.DiscoveryUrl.Trim();
        if (request.AuthorizationEndpoint is { } a && !string.IsNullOrWhiteSpace(a)) p.AuthorizationEndpoint = a.Trim();
        if (request.TokenEndpoint is { } t && !string.IsNullOrWhiteSpace(t)) p.TokenEndpoint = t.Trim();
        if (request.UserInfoEndpoint is not null) p.UserInfoEndpoint = string.IsNullOrWhiteSpace(request.UserInfoEndpoint) ? null : request.UserInfoEndpoint.Trim();
        if (request.JwksUri is not null) p.JwksUri = string.IsNullOrWhiteSpace(request.JwksUri) ? null : request.JwksUri.Trim();
        if (request.Issuer is { } iss && !string.IsNullOrWhiteSpace(iss)) p.Issuer = iss.Trim();
        if (request.ClientId is { } cid && !string.IsNullOrWhiteSpace(cid)) p.ClientId = cid.Trim();
        if (!string.IsNullOrEmpty(request.ClientSecret)) p.ClientSecretCipher = protector.Protect(request.ClientSecret);
        if (request.Scopes is { } sc && !string.IsNullOrWhiteSpace(sc)) p.Scopes = sc;
        if (request.ClaimMappingJson is { } cm && !string.IsNullOrWhiteSpace(cm)) p.ClaimMappingJson = cm;
        if (request.Status is { } st) p.Status = st;

        p.UpdatedAt = DateTime.UtcNow;
        p.UpdatedBy = userCtx.UserId;
        await store.UpdateAsync(p, cancellationToken);

        return Result.Success(new UpdateSsoProviderResponse { Provider = SsoProviderDto.From(p) });
    }
}

// ---- Delete

public sealed class DeleteSsoProviderCommand : Request
{
    public Guid Id { get; set; }
}

public sealed class DeleteSsoProviderHandler(
    ISsoProviderStore store, ITenantContext tenantCtx, IUserContext userCtx)
    : RequestHandler<DeleteSsoProviderCommand>
{
    public override async Task<Result> HandleAsync(DeleteSsoProviderCommand request, CancellationToken cancellationToken = default)
    {
        if (tenantCtx.TenantId is not { } tenantId)
        {
            return Result.Failure(IdentityResultCode.TenantRequired);
        }
        var ok = await store.SoftDeleteAsync(tenantId, request.Id, userCtx.UserId, cancellationToken);
        return ok ? Result.Success() : Result.Failure(SsoResultCode.ProviderNotFound);
    }
}
