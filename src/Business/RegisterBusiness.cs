using System.Reflection;
using Business.Configuration;
using Business.Libraries.OpenApi;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using SH.Framework.Library.Cqrs;

namespace Business;

public static class RegisterBusiness
{
    public static IServiceCollection AddBusiness(this IServiceCollection services)
    {
        services.AddOpenApiLibrary();
        services.AddCqrsLibraryConfiguration(Assembly.GetExecutingAssembly());
        services.AddConfigurationStore();

        return services;
    }

    public static WebApplication MapBusiness(this WebApplication app)
    {
        app.MapOpenApiLibrary();

        return app;
    }
}