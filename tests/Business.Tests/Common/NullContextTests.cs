using Business.Common;

namespace Business.Tests.Common;

public class NullContextTests
{
    [Fact]
    public void NullUserContext_returns_null_UserId()
    {
        Assert.Null(new NullUserContext().UserId);
    }

    [Fact]
    public void NullTenantContext_returns_null_TenantId()
    {
        Assert.Null(new NullTenantContext().TenantId);
    }
}
