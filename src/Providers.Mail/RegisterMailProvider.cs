using Business.Providers;
using Business.Providers.Mail;
using Microsoft.Extensions.DependencyInjection;
using Providers.Mail.Smtp;

namespace Providers.Mail;

public static class RegisterMailProvider
{
    public static IServiceCollection AddMailProvider(this IServiceCollection services)
    {
        services.AddSingleton<ISmtpClientPool, SmtpClientPool>();
        services.AddSingleton<IProviderFactory<MailProviderCredential, IMailProvider>, ProviderFactory>();

        return services;
    }
}
