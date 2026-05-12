using Business.Common;
using Business.Features.Admin.Tenants.UpdateTenant;
using Business.Identity;
using Domain.Entities.Identity;

namespace Business.Tests.Features.Admin.Tenants;

public class UpdateTenantHandlerTests
{
    [Fact]
    public async Task Updates_displayName_and_status_and_stamps_audit()
    {
        var store = new InMemoryTenantStore();
        var tenant = new Tenant { Id = Guid.NewGuid(), Slug = "acme", DisplayName = "old", Status = TenantStatus.Active };
        await store.AddAsync(tenant);
        var updater = Guid.NewGuid();
        var userCtx = Substitute.For<IUserContext>();
        userCtx.UserId.Returns(updater);
        var handler = new UpdateTenantHandler(store, userCtx);

        var result = await handler.HandleAsync(new UpdateTenantCommand
        {
            Id = tenant.Id,
            DisplayName = "new",
            Status = TenantStatus.Suspended,
        });

        Assert.True(result.IsSuccess);
        Assert.Equal("new", result.Data!.Tenant.DisplayName);
        Assert.Equal(TenantStatus.Suspended, result.Data.Tenant.Status);
        Assert.Equal(updater, tenant.UpdatedBy);
        Assert.NotNull(tenant.UpdatedAt);
    }

    [Fact]
    public async Task Null_fields_leave_existing_values_unchanged()
    {
        var store = new InMemoryTenantStore();
        var tenant = new Tenant { Id = Guid.NewGuid(), Slug = "acme", DisplayName = "keep", Status = TenantStatus.Suspended };
        await store.AddAsync(tenant);
        var handler = new UpdateTenantHandler(store, Substitute.For<IUserContext>());

        var result = await handler.HandleAsync(new UpdateTenantCommand { Id = tenant.Id });

        Assert.True(result.IsSuccess);
        Assert.Equal("keep", result.Data!.Tenant.DisplayName);
        Assert.Equal(TenantStatus.Suspended, result.Data.Tenant.Status);
    }

    [Fact]
    public async Task Returns_TenantNotFound_for_missing_id()
    {
        var handler = new UpdateTenantHandler(new InMemoryTenantStore(), Substitute.For<IUserContext>());

        var result = await handler.HandleAsync(new UpdateTenantCommand { Id = Guid.NewGuid(), DisplayName = "x" });

        Assert.False(result.IsSuccess);
        Assert.Equal(IdentityResultCode.TenantNotFound.Code, result.Code);
    }
}
