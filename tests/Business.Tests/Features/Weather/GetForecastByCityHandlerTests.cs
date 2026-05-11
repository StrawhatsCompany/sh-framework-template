using Business.Features.Weather.GetForecastsByCity;
using Business.Services.Weather;
using Domain.Entities.Weather;

namespace Business.Tests.Features.Weather;

public class GetForecastByCityHandlerTests
{
    [Fact]
    public async Task HandleAsync_returns_success_with_response_mapped_from_service()
    {
        var forecasts = Substitute.For<IForecastService>();
        forecasts.GetForecastAsync("Istanbul", Arg.Any<CancellationToken>())
            .Returns([
                new Forecast { Date = new DateOnly(2026, 5, 11), TemperatureC = 22, Summary = "Mild" }
            ]);
        var handler = new GetForecastByCityHandler(forecasts);

        var result = await handler.HandleAsync(new GetForecastsByCityQuery { City = "Istanbul" });

        Assert.True(result.IsSuccess);
        Assert.Equal("Istanbul", result.Data!.City);
        Assert.Single(result.Data.Forecasts);
    }

    [Fact]
    public async Task HandleAsync_passes_city_through_to_service()
    {
        var forecasts = Substitute.For<IForecastService>();
        forecasts.GetForecastAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new List<Forecast>());
        var handler = new GetForecastByCityHandler(forecasts);

        await handler.HandleAsync(new GetForecastsByCityQuery { City = "Berlin" });

        await forecasts.Received(1).GetForecastAsync("Berlin", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_propagates_cancellation_token()
    {
        var forecasts = Substitute.For<IForecastService>();
        forecasts.GetForecastAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new List<Forecast>());
        var handler = new GetForecastByCityHandler(forecasts);
        using var cts = new CancellationTokenSource();

        await handler.HandleAsync(new GetForecastsByCityQuery { City = "Berlin" }, cts.Token);

        await forecasts.Received(1).GetForecastAsync(Arg.Any<string>(), cts.Token);
    }
}
