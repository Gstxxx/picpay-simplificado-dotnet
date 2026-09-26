using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PicPay.Application.Auth;
using PicPay.Application.Users;
using PicPay.Domain.Users;
using PicPay.Infrastructure.Persistence;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace PicPay.IntegrationTests.Infrastructure;

[Collection(ApiCollection.Name)]
public abstract class IntegrationTest
{
    protected const string Password = "senha1234";

    protected IntegrationTest(ApiFactory factory)
    {
        Factory = factory;
        Client = factory.CreateClient();

        ExternalServices.Reset();
        AuthorizerReturns(200, authorized: true);
        ExternalServices
            .Given(Request.Create().WithPath(ApiFactory.NotifyPath).UsingPost())
            .AtPriority(100)
            .RespondWith(Response.Create().WithStatusCode(204));
    }

    protected ApiFactory Factory { get; }
    protected HttpClient Client { get; }
    protected WireMockServer ExternalServices => Factory.ExternalServices;

    protected void AuthorizerReturns(int status, bool authorized, TimeSpan? delay = null)
    {
        var response = Response.Create()
            .WithStatusCode(status)
            .WithBodyAsJson(new { status = authorized ? "success" : "fail", data = new { authorization = authorized } });

        if (delay is { } d)
            response = response.WithDelay(d);

        ExternalServices
            .Given(Request.Create().WithPath(ApiFactory.AuthorizePath).UsingGet())
            .AtPriority(10)
            .RespondWith(response);
    }

    protected async Task<TestUser> CreateUserAsync(UserType type = UserType.Common, decimal balance = 0)
    {
        var document = type == UserType.Common
            ? Random.Shared.NextInt64(10_000_000_000, 99_999_999_999).ToString()
            : Random.Shared.NextInt64(10_000_000_000_000, 99_999_999_999_999).ToString();
        var email = $"user-{Guid.NewGuid():N}@test.dev";

        var register = await Client.PostAsJsonAsync("/auth/register",
            new RegisterRequest("Usuário Teste", document, email, Password, type), TestJson.Options);
        register.EnsureSuccessStatusCode();
        var user = (await register.Content.ReadFromJsonAsync<UserResponse>(TestJson.Options))!;

        if (balance > 0)
            await SetBalanceAsync(user.Id, balance);

        var login = await Client.PostAsJsonAsync("/auth/login", new LoginRequest(email, Password), TestJson.Options);
        login.EnsureSuccessStatusCode();
        var token = (await login.Content.ReadFromJsonAsync<TokenResponse>(TestJson.Options))!;

        var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);

        return new TestUser(user.Id, email, client);
    }

    protected async Task<decimal> GetBalanceAsync(Guid userId)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.Users.Where(u => u.Id == userId).Select(u => u.Balance).SingleAsync();
    }

    protected async Task<T> WithDbAsync<T>(Func<AppDbContext, Task<T>> query)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        return await query(scope.ServiceProvider.GetRequiredService<AppDbContext>());
    }

    protected static async Task<T> EventuallyAsync<T>(Func<Task<T>> probe, Func<T, bool> condition, TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(10));
        while (true)
        {
            var value = await probe();
            if (condition(value) || DateTime.UtcNow > deadline)
                return value;

            await Task.Delay(100);
        }
    }

    private async Task SetBalanceAsync(Guid userId, decimal balance)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Users.Where(u => u.Id == userId).ExecuteUpdateAsync(s => s.SetProperty(u => u.Balance, balance));
    }
}

public sealed record TestUser(Guid Id, string Email, HttpClient Client);
