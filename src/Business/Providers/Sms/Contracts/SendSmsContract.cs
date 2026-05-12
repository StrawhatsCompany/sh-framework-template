namespace Business.Providers.Sms.Contracts;

public class SendSmsContract
{
    public record Request(string ToNumber, string Body, string? FromNumberOverride = null);
    public record Response(string ProviderMessageId, string Status);
}
