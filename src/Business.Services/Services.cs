using Business.Services.Weather;
using Microsoft.Extensions.DependencyInjection;

namespace Business.Services;

public class Services(IServiceProvider sp): IServices
{
    public IForecastService Forecast => sp.GetRequiredService<IForecastService>();
}