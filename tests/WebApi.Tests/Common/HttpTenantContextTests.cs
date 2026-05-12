using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using WebApi.Common;

namespace WebApi.Tests.Common;

public class HttpTenantContextTests
{
    [Fact]
    public void Returns_null_when_HttpContext_is_null()
    {
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns((HttpContext?)null);

        var ctx = new HttpTenantContext(accessor);

        Assert.Null(ctx.TenantId);
    }

    [Fact]
    public void Returns_null_when_neither_claim_nor_header_present()
    {
        var http = new DefaultHttpContext();
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(http);

        var ctx = new HttpTenantContext(accessor);

        Assert.Null(ctx.TenantId);
    }

    [Fact]
    public void Reads_tid_claim_when_present_and_valid()
    {
        var expected = Guid.NewGuid();
        var http = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("tid", expected.ToString())])),
        };
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(http);

        var ctx = new HttpTenantContext(accessor);

        Assert.Equal(expected, ctx.TenantId);
    }

    [Fact]
    public void Falls_back_to_X_Tenant_Id_header_when_claim_absent()
    {
        var expected = Guid.NewGuid();
        var http = new DefaultHttpContext();
        http.Request.Headers["X-Tenant-Id"] = expected.ToString();
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(http);

        var ctx = new HttpTenantContext(accessor);

        Assert.Equal(expected, ctx.TenantId);
    }

    [Fact]
    public void Prefers_claim_over_header_when_both_present()
    {
        var fromClaim = Guid.NewGuid();
        var fromHeader = Guid.NewGuid();
        var http = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("tid", fromClaim.ToString())])),
        };
        http.Request.Headers["X-Tenant-Id"] = fromHeader.ToString();
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(http);

        var ctx = new HttpTenantContext(accessor);

        Assert.Equal(fromClaim, ctx.TenantId);
    }

    [Fact]
    public void Returns_null_when_claim_is_present_but_not_a_guid()
    {
        var http = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("tid", "not-a-guid")])),
        };
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(http);

        var ctx = new HttpTenantContext(accessor);

        Assert.Null(ctx.TenantId);
    }

    [Fact]
    public void Returns_null_when_header_is_present_but_not_a_guid()
    {
        var http = new DefaultHttpContext();
        http.Request.Headers["X-Tenant-Id"] = "not-a-guid";
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(http);

        var ctx = new HttpTenantContext(accessor);

        Assert.Null(ctx.TenantId);
    }
}
