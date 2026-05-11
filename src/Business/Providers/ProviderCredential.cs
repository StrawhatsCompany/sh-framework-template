namespace Business.Providers;

public class ProviderCredential
{
    public required string HostName { get; init; }
    public required int Port { get; init; }
    public string? UserName { get; init; }
    public string? Password { get; init; }
    public string? ApiKey { get; init; }
    public string? ApiSecret { get; init; }
    public bool UseSsl { get; init; }
}
