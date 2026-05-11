using Business.Services.Weather;

namespace Business.Services;

public interface IServices
{
    IForecastService Forecast { get; }
}
