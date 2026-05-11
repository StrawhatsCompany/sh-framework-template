using Business.Configuration;

namespace Business.Tests.Configuration;

public class InMemoryParameterStoreTests
{
    [Fact]
    public async Task Set_then_get_round_trips()
    {
        var store = new InMemoryParameterStore();

        await store.SetAsync("Authentication:AccessToken:Lifetime", "00:15:00");
        var fetched = await store.GetAsync("Authentication:AccessToken:Lifetime");

        Assert.NotNull(fetched);
        Assert.Equal("00:15:00", fetched.Value);
    }

    [Fact]
    public async Task Get_is_case_insensitive_on_key()
    {
        var store = new InMemoryParameterStore();
        await store.SetAsync("Authentication:AccessToken:Lifetime", "00:15:00");

        var fetched = await store.GetAsync("authentication:accesstoken:lifetime");

        Assert.NotNull(fetched);
    }

    [Fact]
    public async Task Set_overwrites_existing_value()
    {
        var store = new InMemoryParameterStore();
        await store.SetAsync("k", "v1");

        await store.SetAsync("k", "v2");

        Assert.Equal("v2", (await store.GetAsync("k"))!.Value);
        Assert.Single(await store.GetAllAsync());
    }

    [Fact]
    public async Task Module_propagates_when_provided()
    {
        var store = new InMemoryParameterStore();

        await store.SetAsync("Authentication:AccessToken:Lifetime", "00:15:00", module: "Authentication");

        Assert.Equal("Authentication", (await store.GetAsync("Authentication:AccessToken:Lifetime"))!.Module);
    }

    [Fact]
    public async Task Remove_drops_the_entry()
    {
        var store = new InMemoryParameterStore();
        await store.SetAsync("k", "v");

        await store.RemoveAsync("k");

        Assert.Null(await store.GetAsync("k"));
    }
}
