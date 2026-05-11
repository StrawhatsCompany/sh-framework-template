using Domain.Entities.Weather;

namespace Domain.Tests.Entities.Weather;

public class ForecastTests
{
    [Fact]
    public void Forecast_can_hold_date_temperature_and_summary()
    {
        var forecast = new Forecast
        {
            Date = new DateOnly(2026, 5, 11),
            TemperatureC = 22,
            Summary = "Mild"
        };

        Assert.Equal(new DateOnly(2026, 5, 11), forecast.Date);
        Assert.Equal(22, forecast.TemperatureC);
        Assert.Equal("Mild", forecast.Summary);
    }
}
