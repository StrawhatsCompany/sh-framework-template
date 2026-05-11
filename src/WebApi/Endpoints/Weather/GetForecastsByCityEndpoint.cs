using Business.Features.Weather.GetForecastsByCity;
using Microsoft.AspNetCore.Mvc;
using SH.Framework.Library.AspNetCore;

namespace WebApi.Endpoints.Weather;

public class GetForecastsByCityEndpoint: IEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(Route, async (string city, [FromServices] IProjector projector, CancellationToken ct = default) =>
        {
            var result = await projector.SendAsync(new GetForecastsByCityQuery { City = city }, ct);
            return Results.Ok(result);
        })
        .WithName("GetWeatherForecastsByCityName");
    }

    public static string Route => "/weather/forecasts/{city:alpha}";
}