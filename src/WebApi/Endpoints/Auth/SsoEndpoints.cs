using Business.Authentication.Sso;
using Business.Features.Auth.SsoCallback;
using Business.Features.Auth.SsoList;
using Business.Features.Auth.SsoStart;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SH.Framework.Library.AspNetCore;
using SH.Framework.Library.Cqrs.Implementation;
using WebApi.Common;

namespace WebApi.Endpoints.Auth;

public sealed class ListPublicSsoProvidersEndpoint : IEndpoint
{
    public static string Route => "api/v1/auth/sso/providers";

    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet(Route, async ([FromServices] IProjector projector, CancellationToken ct = default) =>
            (await projector.SendAsync(new ListPublicSsoProvidersQuery(), ct)).ToHttp())
        .WithName("ListPublicSsoProviders")
        .WithSummary("List the tenant's active SSO providers (id + name + display name)")
        .WithTags("Auth / SSO")
        .AllowAnonymous()
        .Produces<Result<ListPublicSsoProvidersResponse>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest);
}

public sealed class SsoStartEndpoint : IEndpoint
{
    public static string Route => "api/v1/auth/sso/{providerName}/start";

    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet(Route, async (
                string providerName,
                [FromQuery] string? returnUrl,
                HttpContext http,
                [FromServices] IProjector projector,
                [FromServices] IOptionsSnapshot<SsoOptions> ssoOptions,
                CancellationToken ct = default) =>
            {
                var result = await projector.SendAsync(new SsoStartCommand
                {
                    ProviderName = providerName,
                    ReturnUrl = returnUrl,
                }, ct);
                if (!result.IsSuccess) return result.ToHttp();

                var data = result.Data!;
                http.Response.Cookies.Append(data.CookieName, data.EncryptedStateCookie, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = !http.Request.IsHttps || http.Request.Host.Host == "localhost"
                        ? http.Request.IsHttps : true,
                    SameSite = SameSiteMode.Lax,
                    Expires = data.CookieExpiresAt,
                    Path = "/api/v1/auth/sso/",
                });

                // The IdP redirect_uri in the authorization URL is relative; replace with absolute.
                var redirect = $"{http.Request.Scheme}://{http.Request.Host}/api/v1/auth/sso/{providerName}/callback";
                var absUri = data.AuthorizationUri.Replace(
                    $"redirect_uri={System.Web.HttpUtility.UrlEncode($"/api/v1/auth/sso/{providerName}/callback")}",
                    $"redirect_uri={System.Web.HttpUtility.UrlEncode(redirect)}",
                    StringComparison.Ordinal);
                return Results.Redirect(absUri);
            })
        .WithName("SsoStart")
        .WithSummary("Initiate an SSO authorization code flow (302 redirect to the IdP)")
        .WithDescription("Sets an HttpOnly state cookie carrying PKCE verifier + nonce, then redirects to the provider's authorization endpoint with state + code_challenge.")
        .WithTags("Auth / SSO")
        .AllowAnonymous()
        .Produces(StatusCodes.Status302Found)
        .ProducesProblem(StatusCodes.Status400BadRequest);
}

public sealed class SsoCallbackEndpoint : IEndpoint
{
    public static string Route => "api/v1/auth/sso/{providerName}/callback";

    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet(Route, async (
                string providerName,
                [FromQuery] string code,
                [FromQuery] string state,
                HttpContext http,
                [FromServices] IProjector projector,
                [FromServices] IOptionsSnapshot<SsoOptions> ssoOptions,
                CancellationToken ct = default) =>
            {
                var encrypted = http.Request.Cookies[ssoOptions.Value.StateCookieName];
                if (string.IsNullOrEmpty(encrypted))
                {
                    return Results.Problem(detail: "State cookie missing.", statusCode: StatusCodes.Status400BadRequest);
                }

                var redirectUri = $"{http.Request.Scheme}://{http.Request.Host}/api/v1/auth/sso/{providerName}/callback";
                var result = await projector.SendAsync(new SsoCallbackCommand
                {
                    ProviderName = providerName,
                    Code = code,
                    State = state,
                    EncryptedStateCookie = encrypted,
                    RedirectUri = redirectUri,
                    Ip = http.Connection.RemoteIpAddress?.ToString(),
                    DeviceLabel = http.Request.Headers.UserAgent.ToString(),
                }, ct);

                http.Response.Cookies.Delete(ssoOptions.Value.StateCookieName, new CookieOptions { Path = "/api/v1/auth/sso/" });
                return result.ToHttp();
            })
        .WithName("SsoCallback")
        .WithSummary("Complete the SSO authorization code flow")
        .WithDescription("Validates state, exchanges the code at the IdP token endpoint with PKCE, validates id_token signature + iss + aud + exp + nonce, provisions or links the User, mints the local Session + access + refresh tokens.")
        .WithTags("Auth / SSO")
        .AllowAnonymous()
        .Produces<Result<SsoCallbackResponse>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest);
}
