using SH.Framework.Library.Cqrs.Implementation;

namespace Providers.Sms;

public static class SmsProviderResultCode
{
    private const string Category = "SMS";

    public static ResultCode CredentialMissing => ResultCode.Instance(5100, Category, "SMS provider credentials are missing");
    public static ResultCode SendFailed => ResultCode.Instance(5101, Category, "Failed to dispatch SMS via provider");
    public static ResultCode RecipientInvalid => ResultCode.Instance(5102, Category, "Recipient phone number is invalid");
}
