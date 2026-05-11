using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Business.Libraries.OpenApi;

public static class RegisterOpenApiLibrary
{
    public static IServiceCollection AddOpenApiLibrary(this IServiceCollection services)
    {
        services.AddOpenApi();
        return services;
    }

    public static WebApplication MapOpenApiLibrary(this WebApplication app)
    {
        if (!app.Environment.IsProduction())
        {
            app.MapOpenApi();
        }

        return app;
    }
}