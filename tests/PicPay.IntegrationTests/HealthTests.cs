using System.Net;
using PicPay.IntegrationTests.Infrastructure;

namespace PicPay.IntegrationTests;

public class HealthTests(ApiFactory factory) : IntegrationTest(factory)
{
    [Theory]
    [InlineData("/healthz")]
    [InlineData("/readyz")]
    public async Task Health_endpoints_return_healthy(string path)
    {
        var response = await Client.GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }
}
