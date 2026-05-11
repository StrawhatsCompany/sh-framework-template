using Business.Features.Weather.GetForecastsByCity;
using Microsoft.AspNetCore.Mvc;
using SH.Framework.Library.AspNetCore;
using WebApi.Common;

namespace WebApi.Endpoints.Weather;

public sealed class GetForecastsByCityEndpoint : IEndpoint
{
    public static string Route => "/weather/forecasts/{city:alpha}";

    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(Route, async (string city, [FromServices] IProjector projector, CancellationToken ct = default) =>
                (await projector.SendAsync(new GetForecastsByCityQuery { City = city }, ct)).ToHttp())
            .WithName("GetWeatherForecastsByCityName");
    }
}
