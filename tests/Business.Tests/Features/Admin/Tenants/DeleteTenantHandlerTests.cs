using Business.Common;
using Business.Features.Admin.Tenants.DeleteTenant;
using Business.Identity;
using Domain.Entities.Identity;

namespace Business.Tests.Features.Admin.Tenants;

public class DeleteTenantHandlerTests
{
    [Fact]
    public async Task Soft_deletes_existing_tenant_and_stamps_DeletedBy()
    {
        var store = new InMemoryTenantStore();
        var tenant = new Tenant { Id = Guid.NewGuid(), Slug = "acme" };
        await store.AddAsync(tenant);
        var deleter = Guid.NewGuid();
        var userCtx = Substitute.For<IUserContext>();
        userCtx.UserId.Returns(deleter);
        var handler = new DeleteTenantHandler(store, userCtx);

        var result = await handler.HandleAsync(new DeleteTenantCommand { Id = tenant.Id });

        Assert.True(result.IsSuccess);
        Assert.NotNull(tenant.DeletedAt);
        Assert.Equal(deleter, tenant.DeletedBy);
        Assert.Null(await store.FindByIdAsync(tenant.Id));
    }

    [Fact]
    public async Task Returns_TenantNotFound_for_missing_id()
    {
        var handler = new DeleteTenantHandler(new InMemoryTenantStore(), Substitute.For<IUserContext>());

        var result = await handler.HandleAsync(new DeleteTenantCommand { Id = Guid.NewGuid() });

        Assert.False(result.IsSuccess);
        Assert.Equal(IdentityResultCode.TenantNotFound.Code, result.Code);
    }
}
