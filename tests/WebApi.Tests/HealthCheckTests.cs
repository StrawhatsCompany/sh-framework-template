using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace WebApi.Tests;

public class HealthCheckTests : IClassFixture<SHWebApplicationFactory>
{
    private readonly SHWebApplicationFactory _factory;

    public HealthCheckTests(SHWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Liveness_endpoint_returns_200()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Readiness_endpoint_returns_200_when_no_tagged_checks_are_registered()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
