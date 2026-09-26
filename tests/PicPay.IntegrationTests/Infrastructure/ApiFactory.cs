using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Testcontainers.PostgreSql;
using WireMock.Server;

namespace PicPay.IntegrationTests.Infrastructure;

public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string AuthorizePath = "/api/v2/authorize";
    public const string NotifyPath = "/api/v1/notify";

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17-alpine").Build();

    public WireMockServer ExternalServices { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        ExternalServices = WireMockServer.Start();
        await _postgres.StartAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await DisposeAsync();
        await _postgres.DisposeAsync();
        ExternalServices.Stop();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Postgres"] = _postgres.GetConnectionString(),
            ["Database:MigrateOnStartup"] = "true",
            ["Jwt:Secret"] = "integration-tests-secret-0123456789abcdef",
            ["Serilog:MinimumLevel:Default"] = "Warning",

            ["Authorizer:BaseUrl"] = ExternalServices.Url,
            ["Authorizer:Path"] = AuthorizePath,
            ["Authorizer:TimeoutSeconds"] = "1",
            ["Authorizer:MaxRetries"] = "2",
            ["Authorizer:RetryDelayMilliseconds"] = "10",

            ["Notifier:BaseUrl"] = ExternalServices.Url,
            ["Notifier:Path"] = NotifyPath,
            ["Notifier:TimeoutSeconds"] = "1",
            ["Notifier:MaxRetries"] = "0",

            ["Outbox:PollingIntervalMilliseconds"] = "100",
            ["Outbox:MaxAttempts"] = "3",
            ["Outbox:RetryBaseDelaySeconds"] = "0.2",
            ["Outbox:RetryMaxDelaySeconds"] = "1",

            ["RateLimiting:AuthPermitLimit"] = "10000",
            ["RateLimiting:TransferBurst"] = "10000",
            ["RateLimiting:TransferTokensPerMinute"] = "10000"
        }));
    }
}

[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<ApiFactory>
{
    public const string Name = "api";
}
