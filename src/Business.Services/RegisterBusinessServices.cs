using Business.Services.Weather;
using Microsoft.Extensions.DependencyInjection;

namespace Business.Services;

public static class RegisterBusinessServices
{
    public static IServiceCollection AddBusinessServices(this IServiceCollection services)
    {
        services.AddScoped<IForecastService, ForecastService>();

        return services;
    }
}
