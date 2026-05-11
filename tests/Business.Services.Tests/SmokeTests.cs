namespace Business.Services.Tests;

public class SmokeTests
{
    [Fact]
    public void Business_Services_assembly_loads()
    {
        var assembly = typeof(Business.Services.Services).Assembly;
        Assert.NotNull(assembly);
    }
}
