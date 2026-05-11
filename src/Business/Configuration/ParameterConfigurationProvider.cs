using Microsoft.Extensions.Configuration;

namespace Business.Configuration;

public sealed class ParameterConfigurationProvider : ConfigurationProvider, IDisposable
{
    private readonly IParameterStore _store;
    private readonly ParameterConfigurationOptions _options;
    private readonly Timer? _timer;
    private bool _disposed;

    public ParameterConfigurationProvider(IParameterStore store, ParameterConfigurationOptions options)
    {
        _store = store;
        _options = options;

        if (_options.ReloadOnChange && _options.ReloadInterval > TimeSpan.Zero)
        {
            _timer = new Timer(_ => SafeReload(), state: null, dueTime: _options.ReloadInterval, period: _options.ReloadInterval);
        }
    }

    public override void Load()
    {
        var parameters = _store.GetAllAsync().GetAwaiter().GetResult();
        Data = parameters.ToDictionary(
            p => p.Key,
            p => (string?)p.Value,
            StringComparer.OrdinalIgnoreCase);
    }

    private void SafeReload()
    {
        try
        {
            Load();
            OnReload();
        }
        catch
        {
            // Swallow — a transient store failure should not crash the host. Next tick retries.
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _timer?.Dispose();
    }
}
