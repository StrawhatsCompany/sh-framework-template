using Business.Authentication.Sessions;
using Domain.Entities.Identity;

namespace Business.Tests.Authentication.Sessions;

public class InMemoryRefreshTokenStoreTests
{
    [Fact]
    public async Task RevokeFamilyAsync_revokes_every_token_in_the_rotation_chain()
    {
        var store = new InMemoryRefreshTokenStore();
        var sessionId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        var t1 = await Add(store, tenantId, sessionId, hash: "h1");
        var t2 = await Add(store, tenantId, sessionId, hash: "h2");
        var t3 = await Add(store, tenantId, sessionId, hash: "h3");

        // Chain: t1 -> t2 -> t3 (t1 was consumed and replaced by t2, t2 by t3)
        t1.Status = RefreshTokenStatus.Rotated; t1.ReplacedById = t2.Id;
        t2.Status = RefreshTokenStatus.Rotated; t2.ReplacedById = t3.Id;
        await store.UpdateAsync(t1);
        await store.UpdateAsync(t2);

        // Replay attack: someone presents t1 again. Family invalidation walks the chain.
        await store.RevokeFamilyAsync(t1.Id);

        Assert.Equal(RefreshTokenStatus.Revoked, (await store.FindByHashAsync("h1"))!.Status);
        Assert.Equal(RefreshTokenStatus.Revoked, (await store.FindByHashAsync("h2"))!.Status);
        Assert.Equal(RefreshTokenStatus.Revoked, (await store.FindByHashAsync("h3"))!.Status);
    }

    [Fact]
    public async Task RevokeFamilyAsync_walks_backwards_from_middle_of_chain()
    {
        var store = new InMemoryRefreshTokenStore();
        var sessionId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        var t1 = await Add(store, tenantId, sessionId, hash: "h1");
        var t2 = await Add(store, tenantId, sessionId, hash: "h2");
        var t3 = await Add(store, tenantId, sessionId, hash: "h3");

        t1.Status = RefreshTokenStatus.Rotated; t1.ReplacedById = t2.Id;
        t2.Status = RefreshTokenStatus.Rotated; t2.ReplacedById = t3.Id;
        await store.UpdateAsync(t1);
        await store.UpdateAsync(t2);

        // Seed reuse-detection at the middle token. Should still revoke t1 and t3.
        await store.RevokeFamilyAsync(t2.Id);

        Assert.Equal(RefreshTokenStatus.Revoked, (await store.FindByHashAsync("h1"))!.Status);
        Assert.Equal(RefreshTokenStatus.Revoked, (await store.FindByHashAsync("h2"))!.Status);
        Assert.Equal(RefreshTokenStatus.Revoked, (await store.FindByHashAsync("h3"))!.Status);
    }

    [Fact]
    public async Task RevokeAllForSessionAsync_revokes_only_that_sessions_tokens()
    {
        var store = new InMemoryRefreshTokenStore();
        var tenantId = Guid.NewGuid();
        var sessionA = Guid.NewGuid();
        var sessionB = Guid.NewGuid();

        await Add(store, tenantId, sessionA, hash: "a1");
        await Add(store, tenantId, sessionA, hash: "a2");
        await Add(store, tenantId, sessionB, hash: "b1");

        await store.RevokeAllForSessionAsync(sessionA);

        Assert.Equal(RefreshTokenStatus.Revoked, (await store.FindByHashAsync("a1"))!.Status);
        Assert.Equal(RefreshTokenStatus.Revoked, (await store.FindByHashAsync("a2"))!.Status);
        Assert.Equal(RefreshTokenStatus.Active, (await store.FindByHashAsync("b1"))!.Status);
    }

    private static async Task<RefreshToken> Add(InMemoryRefreshTokenStore store, Guid tenantId, Guid sessionId, string hash)
    {
        var token = new RefreshToken
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            SessionId = sessionId,
            TokenHash = hash,
            ExpiresAt = DateTime.UtcNow.AddDays(30),
            Status = RefreshTokenStatus.Active,
            CreatedAt = DateTime.UtcNow,
        };
        return await store.AddAsync(token);
    }
}
