namespace Business.Providers.Mail;

public sealed class MailOptions
{
    public const string SectionName = "Mail";

    public required string HostName { get; init; }
    public required int Port { get; init; }
    public bool UseSsl { get; init; }
    public required string FromAddress { get; init; }
    public string? FromName { get; init; }
}
