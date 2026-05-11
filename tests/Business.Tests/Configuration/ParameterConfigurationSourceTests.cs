using Business.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;

namespace Business.Tests.Configuration;

public class ParameterConfigurationSourceTests
{
    [Fact]
    public async Task Parameters_surface_through_IConfiguration()
    {
        var store = new InMemoryParameterStore();
        await store.SetAsync("Authentication:AccessToken:Lifetime", "00:15:00");

        var configuration = new ConfigurationBuilder()
            .AddPersistenceParameters(store, o => { o.ReloadOnChange = false; })
            .Build();

        Assert.Equal("00:15:00", configuration["Authentication:AccessToken:Lifetime"]);
    }

    [Fact]
    public async Task Persistence_overrides_appsettings_when_added_after()
    {
        var store = new InMemoryParameterStore();
        await store.SetAsync("Authentication:AccessToken:Lifetime", "00:30:00");

        var memorySource = new Dictionary<string, string?>
        {
            ["Authentication:AccessToken:Lifetime"] = "00:05:00",
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(memorySource)
            .AddPersistenceParameters(store, o => { o.ReloadOnChange = false; })
            .Build();

        Assert.Equal("00:30:00", configuration["Authentication:AccessToken:Lifetime"]);
    }

    [Fact]
    public async Task Order_matters_appsettings_wins_when_added_after()
    {
        var store = new InMemoryParameterStore();
        await store.SetAsync("k", "from-store");

        var configuration = new ConfigurationBuilder()
            .AddPersistenceParameters(store, o => { o.ReloadOnChange = false; })
            .AddInMemoryCollection(new Dictionary<string, string?> { ["k"] = "from-appsettings" })
            .Build();

        Assert.Equal("from-appsettings", configuration["k"]);
    }

    [Fact]
    public async Task Reload_triggers_change_token_when_store_updates()
    {
        var store = new InMemoryParameterStore();
        await store.SetAsync("k", "v1");
        var options = new ParameterConfigurationOptions
        {
            ReloadInterval = TimeSpan.FromMilliseconds(50),
            ReloadOnChange = true,
        };
        var provider = new ParameterConfigurationProvider(store, options);
        provider.Load();
        var tcs = new TaskCompletionSource();
        ChangeToken.OnChange(provider.GetReloadToken, () => tcs.TrySetResult());

        await store.SetAsync("k", "v2");

        var firedWithinTimeout = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromSeconds(2))) == tcs.Task;
        Assert.True(firedWithinTimeout, "Reload token did not fire within the timeout window.");
        provider.TryGet("k", out var value);
        Assert.Equal("v2", value);
        provider.Dispose();
    }
}
