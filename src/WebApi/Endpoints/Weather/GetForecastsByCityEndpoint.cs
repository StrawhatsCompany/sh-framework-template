using Business.Features.Weather.GetForecastsByCity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SH.Framework.Library.AspNetCore;
using SH.Framework.Library.Cqrs.Implementation;
using WebApi.Common;

namespace WebApi.Endpoints.Weather;

public sealed class GetForecastsByCityEndpoint : IEndpoint
{
    public static string Route => "/weather/forecasts/{city:alpha}";

    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet(Route, async (string city, [FromServices] IProjector projector, CancellationToken ct = default) =>
                (await projector.SendAsync(new GetForecastsByCityQuery { City = city }, ct)).ToHttp())
            .WithName("GetForecastsByCity")
            .WithSummary("Get weather forecasts for a city")
            .WithDescription("Returns a list of forecast entries for the given city. The route is case-insensitive and accepts alphabetical characters only.")
            .WithTags("Weather")
            .Produces<Result<GetForecastsByCityResponse>>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError);
    }
}
