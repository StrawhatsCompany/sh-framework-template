namespace Business.Services.Weather;

public class ForecastService: IForecastService
{
    private static readonly List<string> Summaries =
        ["Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"];
    
    public Task<List<Domain.Entities.Weather.Forecast>> GetForecastAsync(string city, CancellationToken cancellationToken = default)
    {
        var forecasts = Enumerable.Range(1, 5).Select(index =>
                new Domain.Entities.Weather.Forecast()
                {
                    Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                    TemperatureC = Random.Shared.Next(-20, 55),
                    Summary = Summaries[Random.Shared.Next(Summaries.Count)]
                })
            .ToList();
        
        return Task.FromResult(forecasts);
    }
}