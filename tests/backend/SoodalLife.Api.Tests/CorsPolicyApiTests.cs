using SoodalLife.Api.Infrastructure.Security;

namespace SoodalLife.Api.Tests;

public sealed class CorsPolicyApiTests
{
    [Fact]
    public void CustomerRootOrigin_AlsoAllowsOfficialWwwOrigin()
    {
        var origins = OfficialCorsOrigins.Expand(["https://soodallife.kr"]);

        Assert.Contains("https://soodallife.kr", origins);
        Assert.Contains("https://www.soodallife.kr", origins);
    }

    [Fact]
    public void ExistingOrigins_ArePreservedWithoutDuplicates()
    {
        var origins = OfficialCorsOrigins.Expand([
            "https://soodallife.kr/",
            "https://www.soodallife.kr",
            "https://partner.soodallife.kr",
        ]);

        Assert.Equal(3, origins.Length);
        Assert.Contains("https://partner.soodallife.kr", origins);
    }
}
