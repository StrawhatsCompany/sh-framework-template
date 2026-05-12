using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using WebApi.Common;

namespace WebApi.Tests.Common;

public class HttpUserContextTests
{
    [Fact]
    public void Returns_null_when_HttpContext_is_null()
    {
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns((HttpContext?)null);

        var ctx = new HttpUserContext(accessor);

        Assert.Null(ctx.UserId);
    }

    [Fact]
    public void Returns_null_when_no_NameIdentifier_claim_present()
    {
        var http = new DefaultHttpContext();
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(http);

        var ctx = new HttpUserContext(accessor);

        Assert.Null(ctx.UserId);
    }

    [Fact]
    public void Returns_null_when_NameIdentifier_claim_is_not_a_guid()
    {
        var http = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "not-a-guid")])),
        };
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(http);

        var ctx = new HttpUserContext(accessor);

        Assert.Null(ctx.UserId);
    }

    [Fact]
    public void Returns_guid_when_NameIdentifier_claim_is_a_valid_guid()
    {
        var expected = Guid.NewGuid();
        var http = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, expected.ToString())])),
        };
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(http);

        var ctx = new HttpUserContext(accessor);

        Assert.Equal(expected, ctx.UserId);
    }
}
