using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Business.Configuration;

public static class RegisterParameterConfiguration
{
    public static IConfigurationBuilder AddPersistenceParameters(
        this IConfigurationBuilder builder,
        IParameterStore store,
        Action<ParameterConfigurationOptions>? configure = null)
    {
        var options = new ParameterConfigurationOptions();
        configure?.Invoke(options);
        builder.Add(new ParameterConfigurationSource(store, options));
        return builder;
    }

    public static IServiceCollection AddParameterStore(this IServiceCollection services)
    {
        services.TryAddSingleton<IParameterStore, InMemoryParameterStore>();
        return services;
    }
}
