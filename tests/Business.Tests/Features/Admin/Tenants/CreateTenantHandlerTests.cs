using Business.Common;
using Business.Features.Admin.Tenants.CreateTenant;
using Business.Identity;
using Domain.Entities.Identity;

namespace Business.Tests.Features.Admin.Tenants;

public class CreateTenantHandlerTests
{
    [Fact]
    public async Task Creates_tenant_with_active_status_and_audit_stamps()
    {
        var store = new InMemoryTenantStore();
        var userId = Guid.NewGuid();
        var userCtx = Substitute.For<IUserContext>();
        userCtx.UserId.Returns(userId);
        var handler = new CreateTenantHandler(store, userCtx);

        var result = await handler.HandleAsync(new CreateTenantCommand
        {
            Slug = "acme",
            DisplayName = "Acme Corp",
        });

        Assert.True(result.IsSuccess);
        var created = result.Data!.Tenant;
        Assert.Equal("acme", created.Slug);
        Assert.Equal("Acme Corp", created.DisplayName);
        Assert.Equal(TenantStatus.Active, created.Status);
        var stored = await store.FindByIdAsync(created.Id);
        Assert.NotNull(stored);
        Assert.Equal(userId, stored.CreatedBy);
        Assert.NotEqual(default, stored.CreatedAt);
    }

    [Fact]
    public async Task Defaults_DisplayName_to_slug_when_omitted()
    {
        var handler = new CreateTenantHandler(new InMemoryTenantStore(), Substitute.For<IUserContext>());

        var result = await handler.HandleAsync(new CreateTenantCommand { Slug = "acme", DisplayName = "" });

        Assert.True(result.IsSuccess);
        Assert.Equal("acme", result.Data!.Tenant.DisplayName);
    }

    [Fact]
    public async Task Lowercases_and_trims_slug()
    {
        var handler = new CreateTenantHandler(new InMemoryTenantStore(), Substitute.For<IUserContext>());

        var result = await handler.HandleAsync(new CreateTenantCommand { Slug = "  ACME  ", DisplayName = "x" });

        Assert.True(result.IsSuccess);
        Assert.Equal("acme", result.Data!.Tenant.Slug);
    }

    [Theory]
    [InlineData("a")]               // too short
    [InlineData("-acme")]           // leading hyphen
    [InlineData("acme-")]           // trailing hyphen
    [InlineData("ACME_CORP")]       // underscore not allowed (after lowercase, still has _)
    [InlineData("acme corp")]       // space
    public async Task Rejects_invalid_slugs(string slug)
    {
        var handler = new CreateTenantHandler(new InMemoryTenantStore(), Substitute.For<IUserContext>());

        var result = await handler.HandleAsync(new CreateTenantCommand { Slug = slug, DisplayName = "x" });

        Assert.False(result.IsSuccess);
        Assert.Equal(IdentityResultCode.TenantSlugInvalid.Code, result.Code);
    }

    [Fact]
    public async Task Rejects_duplicate_slug()
    {
        var store = new InMemoryTenantStore();
        await store.AddAsync(new Tenant { Id = Guid.NewGuid(), Slug = "acme", DisplayName = "x" });
        var handler = new CreateTenantHandler(store, Substitute.For<IUserContext>());

        var result = await handler.HandleAsync(new CreateTenantCommand { Slug = "acme", DisplayName = "x" });

        Assert.False(result.IsSuccess);
        Assert.Equal(IdentityResultCode.TenantSlugAlreadyExists.Code, result.Code);
    }
}
