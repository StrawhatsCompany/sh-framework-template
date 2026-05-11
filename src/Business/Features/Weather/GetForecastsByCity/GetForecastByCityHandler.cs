using Business.Services;
using SH.Framework.Library.Cqrs.Implementation;

namespace Business.Features.Weather.GetForecastsByCity;

public sealed class GetForecastByCityHandler(IServices services): RequestHandler<GetForecastsByCityQuery, GetForecastsByCityResponse>
{
    public override async Task<Result<GetForecastsByCityResponse>> HandleAsync(GetForecastsByCityQuery request, CancellationToken cancellationToken = new CancellationToken())
    {
        var forecasts = await services.Forecast.GetForecastAsync(request.City, cancellationToken);
        return Result.Success(GetForecastsByCityResponse.Create(request.City, forecasts));
    }
}