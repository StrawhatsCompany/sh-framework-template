using Business.Services.Weather;
using SH.Framework.Library.Cqrs.Implementation;

namespace Business.Features.Weather.GetForecastsByCity;

public sealed class GetForecastByCityHandler(IForecastService forecasts)
    : RequestHandler<GetForecastsByCityQuery, GetForecastsByCityResponse>
{
    public override async Task<Result<GetForecastsByCityResponse>> HandleAsync(
        GetForecastsByCityQuery request, CancellationToken cancellationToken = default)
    {
        var forecast = await forecasts.GetForecastAsync(request.City, cancellationToken);
        return Result.Success(GetForecastsByCityResponse.Create(request.City, forecast));
    }
}
