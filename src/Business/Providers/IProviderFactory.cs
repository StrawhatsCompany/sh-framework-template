namespace Business.Providers;

public interface IProviderFactory<in TCredential, out TProvider>
{
    public TProvider Create(TCredential credential);
}
