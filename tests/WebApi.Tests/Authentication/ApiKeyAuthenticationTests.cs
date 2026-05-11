using System.Net;
using System.Security.Claims;
using Business.Libraries.Authentication.ApiKey;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace WebApi.Tests.Authentication;

public class ApiKeyAuthenticationTests : IClassFixture<SHWebApplicationFactory>
{
    private readonly SHWebApplicationFactory _factory;

    public ApiKeyAuthenticationTests(SHWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Missing_header_returns_401()
    {
        using var client = BuildClient(_ => false, []);
        var response = await client.GetAsync("/_test/protected-api-key");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Unknown_key_returns_401()
    {
        using var client = BuildClient(key => false, []);
        client.DefaultRequestHeaders.Add("X-Api-Key", "wrong");

        var response = await client.GetAsync("/_test/protected-api-key");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Valid_key_returns_200_and_claim_is_on_the_principal()
    {
        using var client = BuildClient(
            isValid: key => key == "good-key",
            claims: [new Claim(ClaimTypes.NameIdentifier, "service-a")]);
        client.DefaultRequestHeaders.Add("X-Api-Key", "good-key");

        var response = await client.GetAsync("/_test/protected-api-key");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("service-a", await response.Content.ReadAsStringAsync());
    }

    private HttpClient BuildClient(Func<string, bool> isValid, IReadOnlyList<Claim> claims) =>
        _factory.WithWebHostBuilder(host =>
        {
            host.ConfigureServices(services =>
            {
                services.RemoveAll<IApiKeyValidator>();
                services.AddSingleton<IApiKeyValidator>(new StubValidator(isValid, claims));
            });
            host.Configure(app =>
            {
                app.UseRouting();
                app.UseAuthentication();
                app.UseAuthorization();
                app.UseEndpoints(endpoints =>
                {
                    endpoints.MapGet("/_test/protected-api-key", (ClaimsPrincipal user) =>
                            user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "anonymous")
                        .RequireAuthorization(new AuthorizeAttribute { AuthenticationSchemes = ApiKeyDefaults.AuthenticationScheme });
                });
            });
        }).CreateClient();

    private sealed class StubValidator(Func<string, bool> isValid, IReadOnlyList<Claim> claims) : IApiKeyValidator
    {
        public Task<ApiKeyValidationResult> ValidateAsync(string apiKey, CancellationToken cancellationToken = default) =>
            Task.FromResult(isValid(apiKey)
                ? ApiKeyValidationResult.Valid(claims)
                : ApiKeyValidationResult.Invalid);
    }
}
