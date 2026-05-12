using Business.Providers;
using Business.Providers.Sms;
using Business.Providers.Sms.Contracts;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace Providers.Sms.Twilio;

internal sealed class TwilioSmsProvider(SmsProviderCredential credential) : ISmsProvider
{
    public async Task<ProviderResult<SendSmsContract.Response>> SendAsync(
        SendSmsContract.Request request, CancellationToken cancellationToken = default)
    {
        // ApiKey holds Twilio's Account SID; Password holds the auth token.
        if (string.IsNullOrEmpty(credential.ApiKey) || string.IsNullOrEmpty(credential.Password))
        {
            return ProviderResult.Failure<SendSmsContract.Response>(SmsProviderResultCode.CredentialMissing);
        }

        TwilioClient.Init(credential.ApiKey, credential.Password);

        var from = request.FromNumberOverride ?? credential.UserName;
        if (string.IsNullOrEmpty(from))
        {
            return ProviderResult.Failure<SendSmsContract.Response>(SmsProviderResultCode.CredentialMissing);
        }

        try
        {
            var message = await MessageResource.CreateAsync(
                to: new PhoneNumber(request.ToNumber),
                from: new PhoneNumber(from),
                body: request.Body);

            return ProviderResult.Success(new SendSmsContract.Response(
                ProviderMessageId: message.Sid,
                Status: message.Status.ToString()));
        }
        catch (Exception ex) when (ex.Message.Contains("not a valid phone number", StringComparison.OrdinalIgnoreCase))
        {
            return ProviderResult.Failure<SendSmsContract.Response>(SmsProviderResultCode.RecipientInvalid);
        }
        catch
        {
            return ProviderResult.Failure<SendSmsContract.Response>(SmsProviderResultCode.SendFailed);
        }
    }
}
