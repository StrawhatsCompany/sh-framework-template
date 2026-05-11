using Microsoft.Extensions.Configuration;

namespace Business.Configuration;

public sealed class ParameterConfigurationSource : IConfigurationSource
{
    private readonly IParameterStore _store;
    private readonly ParameterConfigurationOptions _options;

    public ParameterConfigurationSource(IParameterStore store, ParameterConfigurationOptions? options = null)
    {
        _store = store;
        _options = options ?? new ParameterConfigurationOptions();
    }

    public IConfigurationProvider Build(IConfigurationBuilder builder) =>
        new ParameterConfigurationProvider(_store, _options);
}
