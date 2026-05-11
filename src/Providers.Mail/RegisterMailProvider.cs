using Business.Providers;
using Business.Providers.Mail;
using Microsoft.Extensions.DependencyInjection;

namespace Providers.Mail;

public static class RegisterMailProvider
{
    public static IServiceCollection AddMailProvider(this IServiceCollection services)
    {
        services.AddSingleton<IProviderFactory<MailProviderCredential, IMailProvider>, ProviderFactory>();

        return services;
    }
}
