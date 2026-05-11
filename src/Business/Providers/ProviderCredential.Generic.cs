namespace Business.Providers;

public class ProviderCredential<TProviderType> : ProviderCredential
{
    public TProviderType ProviderType { get; set; }
}
