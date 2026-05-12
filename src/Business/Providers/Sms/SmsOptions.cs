namespace Business.Providers.Sms;

public sealed class SmsOptions
{
    public const string SectionName = "Sms";

    public required string FromNumber { get; init; }

    /// <summary>
    /// Twilio account SID. Sourced from user-secrets / env / secret store — never appsettings.
    /// </summary>
    public string? AccountSid { get; init; }

    /// <summary>
    /// Twilio auth token. Sourced from user-secrets / env / secret store — never appsettings.
    /// </summary>
    public string? AuthToken { get; init; }
}
