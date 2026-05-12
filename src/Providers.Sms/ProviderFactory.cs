using Business.Providers;
using Business.Providers.Sms;
using Providers.Sms.Twilio;

namespace Providers.Sms;

internal sealed class ProviderFactory : IProviderFactory<SmsProviderCredential, ISmsProvider>
{
    public ISmsProvider Create(SmsProviderCredential credential) => credential.ProviderType switch
    {
        SmsProviderType.Twilio => new TwilioSmsProvider(credential),
        _ => throw new NotSupportedException($"SMS provider type {credential.ProviderType} is not supported."),
    };
}
