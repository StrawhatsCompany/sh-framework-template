using System.Reflection;
using Business.Common;
using Business.Configuration;
using Business.Identity;
using Business.Libraries.OpenApi;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SH.Framework.Library.Cqrs;

namespace Business;

public static class RegisterBusiness
{
    public static IServiceCollection AddBusiness(this IServiceCollection services)
    {
        services.AddOpenApiLibrary();
        services.AddCqrsLibraryConfiguration(Assembly.GetExecutingAssembly());
        services.AddConfigurationStore();
        services.AddIdentity();

        // Null-safe defaults — HTTP-aware implementations override these in WebApi/Program.cs.
        // Background jobs, seeders, and migrations get null UserId/TenantId, which the entity
        // foundations (IHasAuditColumns nullable Guid) tolerate by design.
        services.TryAddScoped<IUserContext, NullUserContext>();
        services.TryAddScoped<ITenantContext, NullTenantContext>();

        return services;
    }

    public static WebApplication MapBusiness(this WebApplication app)
    {
        app.MapOpenApiLibrary();

        return app;
    }
}