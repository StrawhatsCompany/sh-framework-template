using Business.Authentication;
using Business.Authentication.ApiKeys;
using Business.Common;
using Business.Features.Admin.ApiKeys;
using Business.Features.ApiKeys.CreateMyApiKey;
using Business.Features.ApiKeys.ListMyApiKeys;
using Business.Features.ApiKeys.RevokeMyApiKey;
using Domain.Entities.Identity;

namespace Business.Tests.Features.ApiKeys;

public class ApiKeyHandlerTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    [Fact]
    public async Task CreateMyApiKey_returns_plaintext_token_and_persists_only_hash()
    {
        var store = new InMemoryApiKeyStore();
        var handler = NewCreateHandler(store);

        var result = await handler.HandleAsync(new CreateMyApiKeyCommand
        {
            Name = "CI deploy",
            ExpiresAt = DateTime.UtcNow.AddDays(30),
        });

        Assert.True(result.IsSuccess);
        var token = result.Data!.Token;
        Assert.StartsWith("shf_", token);

        var stored = (await store.ListByUserAsync(_tenantId, _userId)).Single();
        Assert.NotEqual(token, stored.KeyHash);
        Assert.Equal(4, stored.Last4.Length);
        Assert.Equal(8, stored.Prefix.Length);
        Assert.Equal(_userId, stored.CreatedBy);
        Assert.Equal(ApiKeyStatus.Active, stored.Status);
    }

    [Fact]
    public async Task CreateMyApiKey_rejects_invalid_name_length()
    {
        var handler = NewCreateHandler(new InMemoryApiKeyStore());

        var result = await handler.HandleAsync(new CreateMyApiKeyCommand { Name = "a" });

        Assert.False(result.IsSuccess);
        Assert.Equal(AuthResultCode.ApiKeyNameInvalid.Code, result.Code);
    }

    [Fact]
    public async Task CreateMyApiKey_rejects_past_expiry()
    {
        var handler = NewCreateHandler(new InMemoryApiKeyStore());

        var result = await handler.HandleAsync(new CreateMyApiKeyCommand
        {
            Name = "valid",
            ExpiresAt = DateTime.UtcNow.AddMinutes(-1),
        });

        Assert.False(result.IsSuccess);
        Assert.Equal(AuthResultCode.ApiKeyExpiryInPast.Code, result.Code);
    }

    [Fact]
    public async Task ListMyApiKeys_scopes_to_caller()
    {
        var store = new InMemoryApiKeyStore();
        await Seed(store, _userId, "alice key");
        await Seed(store, Guid.NewGuid(), "bob key");
        var handler = new ListMyApiKeysHandler(store, TenantCtx(), UserCtx(_userId));

        var result = await handler.HandleAsync(new ListMyApiKeysQuery());

        Assert.True(result.IsSuccess);
        Assert.Single(result.Data!.Items);
        Assert.Equal("alice key", result.Data.Items[0].Name);
    }

    [Fact]
    public async Task RevokeMyApiKey_refuses_other_users_key()
    {
        var store = new InMemoryApiKeyStore();
        var otherUser = Guid.NewGuid();
        var foreignKey = await Seed(store, otherUser, "not yours");
        var handler = new RevokeMyApiKeyHandler(store, TenantCtx(), UserCtx(_userId));

        var result = await handler.HandleAsync(new RevokeMyApiKeyCommand { Id = foreignKey.Id });

        Assert.False(result.IsSuccess);
        Assert.Equal(AuthResultCode.ApiKeyNotFound.Code, result.Code);
        var stillThere = await store.FindByIdAsync(_tenantId, foreignKey.Id);
        Assert.Equal(ApiKeyStatus.Active, stillThere!.Status);
    }

    [Fact]
    public async Task Admin_ListApiKeys_returns_all_tenant_keys()
    {
        var store = new InMemoryApiKeyStore();
        await Seed(store, _userId, "alice");
        await Seed(store, Guid.NewGuid(), "bob");
        var handler = new ListApiKeysHandler(store, TenantCtx());

        var result = await handler.HandleAsync(new ListApiKeysQuery());

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Data!.Items.Count);
    }

    [Fact]
    public async Task Admin_RevokeApiKey_revokes_regardless_of_owner()
    {
        var store = new InMemoryApiKeyStore();
        var otherUser = Guid.NewGuid();
        var key = await Seed(store, otherUser, "victim");
        var handler = new RevokeApiKeyHandler(store, TenantCtx(), UserCtx(_userId));

        var result = await handler.HandleAsync(new RevokeApiKeyCommand { Id = key.Id });

        Assert.True(result.IsSuccess);
        var reloaded = await store.FindByIdAsync(_tenantId, key.Id);
        Assert.Equal(ApiKeyStatus.Revoked, reloaded!.Status);
        Assert.Equal(_userId, reloaded.UpdatedBy);
    }

    private CreateMyApiKeyHandler NewCreateHandler(InMemoryApiKeyStore store) =>
        new(store, new ApiKeyFactory(), TenantCtx(), UserCtx(_userId));

    private ITenantContext TenantCtx()
    {
        var ctx = Substitute.For<ITenantContext>();
        ctx.TenantId.Returns(_tenantId);
        return ctx;
    }

    private static IUserContext UserCtx(Guid? userId)
    {
        var ctx = Substitute.For<IUserContext>();
        ctx.UserId.Returns(userId);
        return ctx;
    }

    private async Task<ApiKey> Seed(InMemoryApiKeyStore store, Guid userId, string name)
    {
        var factory = new ApiKeyFactory();
        var generated = factory.Generate();
        var key = new ApiKey
        {
            Id = Guid.NewGuid(), TenantId = _tenantId, UserId = userId,
            Name = name, Prefix = generated.Prefix, Last4 = generated.Last4, KeyHash = generated.KeyHash,
            Status = ApiKeyStatus.Active, CreatedAt = DateTime.UtcNow,
        };
        await store.AddAsync(key);
        return key;
    }
}
