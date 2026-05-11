using Business.Features.Weather.Dtos;
using Domain.Entities.Weather;

namespace Business.Features.Weather.GetForecastsByCity;

public sealed class GetForecastsByCityResponse
{
    public required string City { get; set; }
    public required IEnumerable<ForecastDto> Forecasts { get; set; }
    
    public static GetForecastsByCityResponse Create(string city, IEnumerable<Forecast> forecasts)
    {
        return new GetForecastsByCityResponse
        {
            City = city,
            Forecasts = forecasts.Select(x => new ForecastDto(x.Date, x.TemperatureC, x.Summary))
        };
    }
}