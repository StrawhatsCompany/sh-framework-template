namespace Business.Services.Weather;

public interface IForecastService
{
    Task<List<Domain.Entities.Weather.Forecast>> GetForecastAsync(string city, CancellationToken cancellationToken = default);
}