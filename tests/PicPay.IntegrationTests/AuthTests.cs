using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using PicPay.Application.Auth;
using PicPay.Application.Users;
using PicPay.Domain.Users;
using PicPay.IntegrationTests.Infrastructure;

namespace PicPay.IntegrationTests;

public class AuthTests(ApiFactory factory) : IntegrationTest(factory)
{
    [Fact]
    public async Task Register_returns_user_without_password_hash()
    {
        var response = await Client.PostAsJsonAsync("/auth/register",
            new RegisterRequest("Maria", "52998224725", "maria@test.dev", Password, UserType.Common), TestJson.Options);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("password", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"balance\":0", body);
    }

    [Fact]
    public async Task Register_with_existing_email_returns_409()
    {
        var existing = await CreateUserAsync();

        var response = await Client.PostAsJsonAsync("/auth/register",
            new RegisterRequest("Outro", "11144477735", existing.Email, Password, UserType.Common), TestJson.Options);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Register_with_invalid_payload_returns_400()
    {
        var response = await Client.PostAsJsonAsync("/auth/register",
            new RegisterRequest("", "123", "x", "1", UserType.Merchant), TestJson.Options);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_with_wrong_password_returns_401()
    {
        var user = await CreateUserAsync();

        var response = await Client.PostAsJsonAsync("/auth/login", new LoginRequest(user.Email, "wrong-password"), TestJson.Options);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_with_unknown_email_returns_401()
    {
        var response = await Client.PostAsJsonAsync("/auth/login", new LoginRequest("ghost@test.dev", Password), TestJson.Options);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_returns_the_authenticated_user()
    {
        var user = await CreateUserAsync(balance: 42.5m);

        var me = await user.Client.GetFromJsonAsync<UserResponse>("/users/me", TestJson.Options);

        Assert.Equal(user.Id, me!.Id);
        Assert.Equal(42.5m, me.Balance);
    }

    [Fact]
    public async Task Me_without_token_returns_401()
    {
        var response = await Client.GetAsync("/users/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_with_tampered_token_returns_401()
    {
        var user = await CreateUserAsync();
        var token = user.Client.DefaultRequestHeaders.Authorization!.Parameter!;
        using var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", token[..^2] + "xx");

        var response = await client.GetAsync("/users/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_is_rate_limited()
    {
        using var limited = Factory.WithWebHostBuilder(b => b.ConfigureAppConfiguration((_, c) =>
            c.AddInMemoryCollection(new Dictionary<string, string?> { ["RateLimiting:AuthPermitLimit"] = "3" })));
        using var client = limited.CreateClient();

        var statuses = new List<HttpStatusCode>();
        for (var i = 0; i < 5; i++)
            statuses.Add((await client.PostAsJsonAsync("/auth/login", new LoginRequest("x@test.dev", "whatever"), TestJson.Options)).StatusCode);

        Assert.Equal(3, statuses.Count(s => s == HttpStatusCode.Unauthorized));
        Assert.Equal(2, statuses.Count(s => s == HttpStatusCode.TooManyRequests));
    }
}
