using Business.Providers;
using Business.Providers.Sms;
using Microsoft.Extensions.DependencyInjection;

namespace Providers.Sms;

public static class RegisterSmsProvider
{
    public static IServiceCollection AddSmsProvider(this IServiceCollection services)
    {
        services.AddSingleton<IProviderFactory<SmsProviderCredential, ISmsProvider>, ProviderFactory>();
        return services;
    }
}
