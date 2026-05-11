using WebApi.Endpoints.Weather;

namespace WebApi.Tests;

public class SmokeTests
{
    [Fact]
    public void Endpoint_route_is_defined()
    {
        Assert.False(string.IsNullOrWhiteSpace(GetForecastsByCityEndpoint.Route));
    }
}
