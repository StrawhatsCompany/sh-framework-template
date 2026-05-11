namespace Business.Providers;

public class ProviderResult<TResponse> : ProviderResult
{
    public TResponse? Response { get; set; }

    public override ProviderResult<TResponse> WithRequestJson(string payload, string type = "JSON")
    {
        base.WithRequestJson(payload, type);
        return this;
    }

    public override ProviderResult<TResponse> WithResponseJson(string payload, string type = "JSON")
    {
        base.WithResponseJson(payload, type);
        return this;
    }

    public override ProviderResult<TResponse> WithError(string key, string value)
    {
        base.WithError(key, value);
        return this;
    }
}
