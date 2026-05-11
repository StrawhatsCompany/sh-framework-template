namespace Business.Providers;

public class ProviderCredential<TProviderType> : ProviderCredential
{
    public required TProviderType ProviderType { get; init; }
}
