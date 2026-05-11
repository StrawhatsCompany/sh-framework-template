using Business.Services.Weather;

namespace Business.Services.Tests;

public class SmokeTests
{
    [Fact]
    public void Business_Services_assembly_loads()
    {
        var assembly = typeof(ForecastService).Assembly;
        Assert.NotNull(assembly);
    }
}
