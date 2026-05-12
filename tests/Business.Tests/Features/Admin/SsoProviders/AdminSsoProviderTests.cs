using Business.Authentication.Sso;
using Business.Common;
using Business.Configuration;
using Business.Features.Admin.SsoProviders;
using Domain.Entities.Identity;

namespace Business.Tests.Features.Admin.SsoProviders;

public class AdminSsoProviderTests
{
    private readonly Guid _tenantId = Guid.NewGuid();

    [Fact]
    public async Task CreateSsoProvider_encrypts_client_secret_and_returns_masked_dto()
    {
        var (store, protector, tenantCtx, userCtx) = Setup();
        var handler = new CreateSsoProviderHandler(store, protector, tenantCtx, userCtx);

        var result = await handler.HandleAsync(new CreateSsoProviderCommand
        {
            Name = "google",
            DisplayName = "Sign in with Google",
            Protocol = SsoProtocol.Oidc,
            AuthorizationEndpoint = "https://accounts.google.com/o/oauth2/v2/auth",
            TokenEndpoint = "https://oauth2.googleapis.com/token",
            JwksUri = "https://www.googleapis.com/oauth2/v3/certs",
            Issuer = "https://accounts.google.com",
            ClientId = "client-123",
            ClientSecret = "super-secret",
        });

        Assert.True(result.IsSuccess);
        Assert.Equal("****", result.Data!.Provider.ClientSecretMasked);
        var stored = await store.FindByIdAsync(_tenantId, result.Data.Provider.Id);
        Assert.NotEqual("super-secret", stored!.ClientSecretCipher);
        Assert.Equal("super-secret", protector.Unprotect(stored.ClientSecretCipher));
    }

    [Fact]
    public async Task CreateSsoProvider_rejects_duplicate_name()
    {
        var (store, protector, tenantCtx, userCtx) = Setup();
        await store.AddAsync(new SsoProvider { Id = Guid.NewGuid(), TenantId = _tenantId, Name = "google", DisplayName = "G" });
        var handler = new CreateSsoProviderHandler(store, protector, tenantCtx, userCtx);

        var result = await handler.HandleAsync(new CreateSsoProviderCommand
        {
            Name = "GOOGLE", DisplayName = "x",
            AuthorizationEndpoint = "https://x", TokenEndpoint = "https://x",
            Issuer = "x", ClientId = "x", ClientSecret = "x",
        });

        Assert.False(result.IsSuccess);
        Assert.Equal(SsoResultCode.ProviderNameAlreadyExists.Code, result.Code);
    }

    [Fact]
    public async Task UpdateSsoProvider_rotates_secret_when_provided()
    {
        var (store, protector, tenantCtx, userCtx) = Setup();
        var seed = new SsoProvider
        {
            Id = Guid.NewGuid(), TenantId = _tenantId, Name = "g", DisplayName = "G",
            ClientSecretCipher = protector.Protect("old-secret"),
        };
        await store.AddAsync(seed);
        var handler = new UpdateSsoProviderHandler(store, protector, tenantCtx, userCtx);

        var result = await handler.HandleAsync(new UpdateSsoProviderCommand
        {
            Id = seed.Id, ClientSecret = "new-secret",
        });

        Assert.True(result.IsSuccess);
        var updated = await store.FindByIdAsync(_tenantId, seed.Id);
        Assert.Equal("new-secret", protector.Unprotect(updated!.ClientSecretCipher));
    }

    [Fact]
    public async Task DeleteSsoProvider_soft_deletes()
    {
        var (store, _, tenantCtx, userCtx) = Setup();
        var seed = new SsoProvider { Id = Guid.NewGuid(), TenantId = _tenantId, Name = "g", DisplayName = "G" };
        await store.AddAsync(seed);
        var handler = new DeleteSsoProviderHandler(store, tenantCtx, userCtx);

        var result = await handler.HandleAsync(new DeleteSsoProviderCommand { Id = seed.Id });

        Assert.True(result.IsSuccess);
        Assert.Null(await store.FindByIdAsync(_tenantId, seed.Id));
    }

    private (ISsoProviderStore store, ICredentialProtector protector, ITenantContext tenantCtx, IUserContext userCtx) Setup()
    {
        var store = new InMemorySsoProviderStore();
        var protector = new PlainProtector();
        var tenantCtx = Substitute.For<ITenantContext>();
        tenantCtx.TenantId.Returns(_tenantId);
        var userCtx = Substitute.For<IUserContext>();
        userCtx.UserId.Returns(Guid.NewGuid());
        return (store, protector, tenantCtx, userCtx);
    }

    private sealed class PlainProtector : ICredentialProtector
    {
        public string Protect(string plaintext) => $"enc:{plaintext}";
        public string Unprotect(string ciphertext) => ciphertext.StartsWith("enc:") ? ciphertext[4..] : ciphertext;
    }
}
