namespace Business.Providers.Mail;

public sealed class MailOptions
{
    public const string SectionName = "Mail";

    public required string HostName { get; init; }
    public required int Port { get; init; }
    public bool UseSsl { get; init; }
    public required string FromAddress { get; init; }
    public string? FromName { get; init; }

    // Secrets — must come from user-secrets (dev) or environment / secret store (prod).
    // Never set these in appsettings.json.
    public string? Username { get; init; }
    public string? Password { get; init; }
}
