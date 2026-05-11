namespace Business.Providers;

public class ProviderCredential
{
    public string HostName { get; set; }
    public int Port { get; set; }
    public string? UserName {  get; set; }
    public string? Password { get; set; }
    public string? ApiKey { get; set; }
    public string? ApiSecret { get; set; }
    public bool UseSsl { get; set; } = false;
}

public class ProviderCredential<TProviderType>: ProviderCredential
{
    public TProviderType ProviderType { get; set; }
}