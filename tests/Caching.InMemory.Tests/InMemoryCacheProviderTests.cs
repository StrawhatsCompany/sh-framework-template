using Business.Caching;
using Caching.InMemory;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Caching.InMemory.Tests;

public class InMemoryCacheProviderTests
{
    [Fact]
    public async Task Get_returns_default_when_key_is_absent()
    {
        var provider = NewProvider();

        var value = await provider.GetAsync<string>("absent");

        Assert.Null(value);
    }

    [Fact]
    public async Task Set_then_get_round_trips_the_value()
    {
        var provider = NewProvider();

        await provider.SetAsync("greeting", "hello");
        var value = await provider.GetAsync<string>("greeting");

        Assert.Equal("hello", value);
    }

    [Fact]
    public async Task Set_then_get_round_trips_a_complex_type()
    {
        var provider = NewProvider();
        var payload = new { Id = 7, Name = "x" };

        await provider.SetAsync("obj", payload);
        var value = await provider.GetAsync<object>("obj");

        Assert.NotNull(value);
    }

    [Fact]
    public async Task Exists_reports_presence_correctly()
    {
        var provider = NewProvider();

        Assert.False(await provider.ExistsAsync("k"));
        await provider.SetAsync("k", 1);
        Assert.True(await provider.ExistsAsync("k"));
    }

    [Fact]
    public async Task Remove_clears_the_key()
    {
        var provider = NewProvider();
        await provider.SetAsync("k", 1);

        await provider.RemoveAsync("k");

        Assert.False(await provider.ExistsAsync("k"));
    }

    [Fact]
    public async Task Remove_is_safe_for_missing_keys()
    {
        var provider = NewProvider();

        await provider.RemoveAsync("never-set");
    }

    [Fact]
    public async Task KeyPrefix_isolates_namespaces()
    {
        var ours = NewProvider(prefix: "ours");
        var theirs = NewProvider(prefix: "theirs", sharedBackingStore: true);

        await ours.SetAsync("shared", "ours-value");
        await theirs.SetAsync("shared", "theirs-value");

        Assert.Equal("ours-value", await ours.GetAsync<string>("shared"));
        Assert.Equal("theirs-value", await theirs.GetAsync<string>("shared"));
    }

    [Fact]
    public async Task Explicit_ttl_overrides_default()
    {
        var memory = new MemoryCache(new MemoryCacheOptions());
        var options = Options.Create(new CacheOptions { DefaultTtl = TimeSpan.FromMinutes(10) });
        var provider = new InMemoryCacheProvider(memory, options);

        await provider.SetAsync("k", "v", ttl: TimeSpan.FromMilliseconds(50));
        await Task.Delay(150);

        Assert.False(await provider.ExistsAsync("k"));
    }

    private static MemoryCache? _sharedMemory;

    private static InMemoryCacheProvider NewProvider(string prefix = "", bool sharedBackingStore = false)
    {
        var memory = sharedBackingStore
            ? (_sharedMemory ??= new MemoryCache(new MemoryCacheOptions()))
            : new MemoryCache(new MemoryCacheOptions());
        var options = Options.Create(new CacheOptions { KeyPrefix = prefix });
        return new InMemoryCacheProvider(memory, options);
    }
}
