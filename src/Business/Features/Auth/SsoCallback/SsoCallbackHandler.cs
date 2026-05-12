using System.Text.Json;
using Business.Authentication;
using Business.Authentication.Jwt;
using Business.Authentication.Sessions;
using Business.Authentication.Sso;
using Business.Configuration;
using Business.Identity;
using Domain.Entities.Identity;
using Microsoft.Extensions.Options;
using SH.Framework.Library.Cqrs.Implementation;

namespace Business.Features.Auth.SsoCallback;

public sealed class SsoCallbackCommand : Request<SsoCallbackResponse>
{
    public string ProviderName { get; set; } = "";
    public string Code { get; set; } = "";
    public string State { get; set; } = "";
    public string EncryptedStateCookie { get; set; } = "";
    public string RedirectUri { get; set; } = "";   // the absolute URL the IdP redirected back to
    public string? Ip { get; set; }
    public string? DeviceLabel { get; set; }
}

public sealed class SsoCallbackResponse
{
    public required string AccessToken { get; init; }
    public required DateTime ExpiresAt { get; init; }
    public required string RefreshToken { get; init; }
    public required DateTime RefreshTokenExpiresAt { get; init; }
    public required Guid SessionId { get; init; }
    public required Guid UserId { get; init; }
    public required Guid TenantId { get; init; }
    public string? ReturnUrl { get; init; }
}

public sealed class SsoCallbackHandler(
    ISsoProviderStore providers,
    IUserSsoIdentityStore identities,
    IUserStore users,
    IOidcTokenExchange exchange,
    ICredentialProtector protector,
    IJwtTokenIssuer jwt,
    ISessionStore sessions,
    IRefreshTokenStore refreshTokens,
    IRefreshTokenFactory refreshTokenFactory,
    IOptionsSnapshot<SsoOptions> ssoOptions,
    IOptionsSnapshot<JwtOptions> jwtOptions)
    : RequestHandler<SsoCallbackCommand, SsoCallbackResponse>
{
    public override async Task<Result<SsoCallbackResponse>> HandleAsync(
        SsoCallbackCommand request, CancellationToken cancellationToken = default)
    {
        // 1. Decrypt + validate state cookie.
        string cookieJson;
        try { cookieJson = protector.Unprotect(request.EncryptedStateCookie); }
        catch { return Result.Failure<SsoCallbackResponse>(SsoResultCode.InvalidStateCookie); }
        var payload = SsoStateCookie.TryDeserialize(cookieJson);
        if (payload is null || payload.ExpiresAt <= DateTime.UtcNow)
        {
            return Result.Failure<SsoCallbackResponse>(SsoResultCode.InvalidStateCookie);
        }
        if (!string.Equals(payload.State, request.State, StringComparison.Ordinal))
        {
            return Result.Failure<SsoCallbackResponse>(SsoResultCode.InvalidStateCookie);
        }

        // 2. Resolve provider.
        var provider = await providers.FindByIdAsync(payload.TenantId, payload.ProviderId, cancellationToken);
        if (provider is null || provider.Status != SsoProviderStatus.Active)
        {
            return Result.Failure<SsoCallbackResponse>(SsoResultCode.ProviderNotFound);
        }
        if (!string.Equals(provider.Name, request.ProviderName, StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<SsoCallbackResponse>(SsoResultCode.ProviderNotFound);
        }

        // 3. Exchange code + validate id_token.
        string clientSecret;
        try { clientSecret = protector.Unprotect(provider.ClientSecretCipher); }
        catch { return Result.Failure<SsoCallbackResponse>(SsoResultCode.CodeExchangeFailed); }

        var exchangeResult = await exchange.ExchangeAsync(
            provider, clientSecret, request.Code, payload.CodeVerifier,
            request.RedirectUri, payload.Nonce, cancellationToken);
        if (!exchangeResult.IsSuccess || exchangeResult.Claims is null)
        {
            return Result.Failure<SsoCallbackResponse>(SsoResultCode.IdTokenInvalid);
        }

        // 4. Map claims via provider.ClaimMappingJson — default to OIDC standard claims.
        var mapping = ParseClaimMapping(provider.ClaimMappingJson);
        var subject = AsString(exchangeResult.Claims, mapping.GetValueOrDefault("sub", "sub"));
        var email = AsString(exchangeResult.Claims, mapping.GetValueOrDefault("email", "email"));
        var username = AsString(exchangeResult.Claims, mapping.GetValueOrDefault("username", "preferred_username"));
        var displayName = AsString(exchangeResult.Claims, mapping.GetValueOrDefault("displayName", "name"));
        if (string.IsNullOrEmpty(subject))
        {
            return Result.Failure<SsoCallbackResponse>(SsoResultCode.IdTokenInvalid);
        }

        // 5. Look up existing UserSsoIdentity, then fall back to email match.
        var identity = await identities.FindByProviderSubjectAsync(payload.TenantId, provider.Id, subject, cancellationToken);
        User? user;
        if (identity is not null)
        {
            user = await users.FindByIdAsync(payload.TenantId, identity.UserId, cancellationToken);
        }
        else
        {
            user = !string.IsNullOrEmpty(email)
                ? await users.FindByEmailAsync(payload.TenantId, email, cancellationToken)
                : null;

            var opts = ssoOptions.Value;
            if (user is null)
            {
                if (!opts.AutoCreateUser)
                {
                    return Result.Failure<SsoCallbackResponse>(SsoResultCode.UserProvisioningRefused);
                }
                if (string.IsNullOrEmpty(email))
                {
                    return Result.Failure<SsoCallbackResponse>(SsoResultCode.IdTokenInvalid);
                }

                user = new User
                {
                    Id = Guid.NewGuid(),
                    TenantId = payload.TenantId,
                    Email = email,
                    Username = !string.IsNullOrEmpty(username) ? username : email,
                    DisplayName = !string.IsNullOrEmpty(displayName) ? displayName : email,
                    PasswordHash = null,
                    Status = UserStatus.Active,
                    EmailVerifiedAt = opts.AutoVerifyEmail ? DateTime.UtcNow : null,
                    CreatedAt = DateTime.UtcNow,
                };
                await users.AddAsync(user, cancellationToken);
            }

            // Link the identity.
            await identities.AddAsync(new UserSsoIdentity
            {
                Id = Guid.NewGuid(),
                TenantId = payload.TenantId,
                UserId = user.Id,
                SsoProviderId = provider.Id,
                ExternalSubject = subject,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = user.Id,
            }, cancellationToken);
        }

        if (user is null || user.Status != UserStatus.Active)
        {
            return Result.Failure<SsoCallbackResponse>(AuthResultCode.UserDisabled);
        }

        // 6. Mint Session + RefreshToken + JWT.
        var now = DateTime.UtcNow;
        var jwtOpts = jwtOptions.Value;
        var session = new Session
        {
            Id = Guid.NewGuid(),
            TenantId = payload.TenantId,
            UserId = user.Id,
            AuthMethod = SessionAuthMethod.Sso,
            DeviceLabel = string.IsNullOrWhiteSpace(request.DeviceLabel) ? null : request.DeviceLabel.Trim(),
            IpFirst = request.Ip,
            IpLast = request.Ip,
            LastSeenAt = now,
            ExpiresAt = now.Add(jwtOpts.RefreshTokenLifetime),
            Status = SessionStatus.Active,
            CreatedAt = now,
            CreatedBy = user.Id,
        };
        await sessions.AddAsync(session, cancellationToken);

        var (refreshPlain, refreshHash) = refreshTokenFactory.Generate();
        var refresh = new RefreshToken
        {
            Id = Guid.NewGuid(),
            TenantId = payload.TenantId,
            SessionId = session.Id,
            TokenHash = refreshHash,
            ExpiresAt = session.ExpiresAt,
            Status = RefreshTokenStatus.Active,
            CreatedAt = now,
        };
        await refreshTokens.AddAsync(refresh, cancellationToken);

        user.LastLoginAt = now;
        await users.UpdateAsync(user, cancellationToken);

        var token = jwt.Issue(user, session.Id);

        return Result.Success(new SsoCallbackResponse
        {
            AccessToken = token.AccessToken,
            ExpiresAt = token.ExpiresAt,
            RefreshToken = refreshPlain,
            RefreshTokenExpiresAt = refresh.ExpiresAt,
            SessionId = session.Id,
            UserId = user.Id,
            TenantId = payload.TenantId,
            ReturnUrl = payload.ReturnUrl,
        });
    }

    private static Dictionary<string, string> ParseClaimMapping(string json)
    {
        if (string.IsNullOrWhiteSpace(json) || json == "{}") return new();
        try
        {
            using var doc = JsonDocument.Parse(json);
            var result = new Dictionary<string, string>();
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                if (prop.Value.ValueKind == JsonValueKind.String)
                {
                    result[prop.Name] = prop.Value.GetString() ?? "";
                }
            }
            return result;
        }
        catch
        {
            return new();
        }
    }

    private static string AsString(IReadOnlyDictionary<string, object> claims, string claimName) =>
        claims.TryGetValue(claimName, out var val) ? val?.ToString() ?? "" : "";
}
