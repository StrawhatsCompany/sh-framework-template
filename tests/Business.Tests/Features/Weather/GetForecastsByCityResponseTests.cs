using Business.Features.Weather.GetForecastsByCity;
using Domain.Entities.Weather;

namespace Business.Tests.Features.Weather;

public class GetForecastsByCityResponseTests
{
    [Fact]
    public void Create_maps_forecasts_to_dtos_and_preserves_city()
    {
        var forecasts = new[]
        {
            new Forecast { Date = new DateOnly(2026, 5, 11), TemperatureC = 22, Summary = "Mild" },
            new Forecast { Date = new DateOnly(2026, 5, 12), TemperatureC = 24, Summary = "Warm" },
        };

        var response = GetForecastsByCityResponse.Create("Istanbul", forecasts);

        Assert.Equal("Istanbul", response.City);
        Assert.Collection(response.Forecasts,
            dto => Assert.Equal("Mild", dto.Summary),
            dto => Assert.Equal("Warm", dto.Summary));
    }
}
