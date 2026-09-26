using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using PicPay.Application.Transfers;
using PicPay.Domain.Users;
using PicPay.IntegrationTests.Infrastructure;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

namespace PicPay.IntegrationTests;

public class TransferTests(ApiFactory factory) : IntegrationTest(factory)
{
    [Fact]
    public async Task Transfer_moves_money_and_returns_201()
    {
        var payer = await CreateUserAsync(balance: 100);
        var payee = await CreateUserAsync(UserType.Merchant);

        var response = await TransferAsync(payer, new TransferRequest(40.25m, payee.Id));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var transfer = (await response.Content.ReadFromJsonAsync<TransferResponse>(TestJson.Options))!;
        Assert.Equal(payer.Id, transfer.Payer);
        Assert.Equal(payee.Id, transfer.Payee);
        Assert.Equal($"/transfer/{transfer.Id}", response.Headers.Location!.OriginalString);
        Assert.Equal(59.75m, await GetBalanceAsync(payer.Id));
        Assert.Equal(40.25m, await GetBalanceAsync(payee.Id));
    }

    [Fact]
    public async Task Transfer_can_be_read_by_payer_and_payee_only()
    {
        var payer = await CreateUserAsync(balance: 10);
        var payee = await CreateUserAsync();
        var outsider = await CreateUserAsync();
        var created = await (await TransferAsync(payer, new TransferRequest(5, payee.Id)))
            .Content.ReadFromJsonAsync<TransferResponse>(TestJson.Options);

        Assert.Equal(HttpStatusCode.OK, (await payer.Client.GetAsync($"/transfer/{created!.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await payee.Client.GetAsync($"/transfer/{created.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await outsider.Client.GetAsync($"/transfer/{created.Id}")).StatusCode);
    }

    [Fact]
    public async Task Transfer_without_token_returns_401()
    {
        var response = await Client.PostAsJsonAsync("/transfer", new TransferRequest(10, Guid.NewGuid()), TestJson.Options);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Payer_in_body_must_match_the_token()
    {
        var attacker = await CreateUserAsync(balance: 0);
        var victim = await CreateUserAsync(balance: 500);

        var response = await TransferAsync(attacker, new TransferRequest(500, attacker.Id, Payer: victim.Id));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(500, await GetBalanceAsync(victim.Id));
        Assert.Equal(0, await GetBalanceAsync(attacker.Id));
    }

    [Fact]
    public async Task Payer_in_body_equal_to_token_is_accepted()
    {
        var payer = await CreateUserAsync(balance: 10);
        var payee = await CreateUserAsync();

        var response = await TransferAsync(payer, new TransferRequest(10, payee.Id, Payer: payer.Id));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Merchant_cannot_send_money()
    {
        var merchant = await CreateUserAsync(UserType.Merchant, balance: 100);
        var payee = await CreateUserAsync();

        var response = await TransferAsync(merchant, new TransferRequest(10, payee.Id));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(100, await GetBalanceAsync(merchant.Id));
    }

    [Fact]
    public async Task Insufficient_funds_returns_422()
    {
        var payer = await CreateUserAsync(balance: 10);
        var payee = await CreateUserAsync();

        var response = await TransferAsync(payer, new TransferRequest(10.01m, payee.Id));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal(10, await GetBalanceAsync(payer.Id));
    }

    [Fact]
    public async Task Unknown_payee_returns_404()
    {
        var payer = await CreateUserAsync(balance: 10);

        var response = await TransferAsync(payer, new TransferRequest(5, Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Self_transfer_returns_400()
    {
        var payer = await CreateUserAsync(balance: 10);

        var response = await TransferAsync(payer, new TransferRequest(5, payer.Id));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1.001)]
    public async Task Invalid_value_returns_400(decimal value)
    {
        var payer = await CreateUserAsync(balance: 10);
        var payee = await CreateUserAsync();

        var response = await TransferAsync(payer, new TransferRequest(value, payee.Id));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Authorizer_denial_returns_403_and_keeps_balances()
    {
        AuthorizerReturns(403, authorized: false);
        var payer = await CreateUserAsync(balance: 50);
        var payee = await CreateUserAsync();

        var response = await TransferAsync(payer, new TransferRequest(20, payee.Id));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(50, await GetBalanceAsync(payer.Id));
        Assert.Equal(0, await GetBalanceAsync(payee.Id));
    }

    [Fact]
    public async Task Authorizer_transient_failure_is_retried()
    {
        ExternalServices
            .Given(Request.Create().WithPath(ApiFactory.AuthorizePath).UsingGet())
            .AtPriority(1)
            .InScenario("flaky-authorizer")
            .WillSetStateTo("recovered")
            .RespondWith(Response.Create().WithStatusCode(503));

        var payer = await CreateUserAsync(balance: 50);
        var payee = await CreateUserAsync();

        var response = await TransferAsync(payer, new TransferRequest(20, payee.Id));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(2, ExternalServices.LogEntries.Count(e => e.RequestMessage?.Path == ApiFactory.AuthorizePath));
    }

    [Fact]
    public async Task Authorizer_down_returns_503_and_keeps_balances()
    {
        AuthorizerReturns(500, authorized: false);
        var payer = await CreateUserAsync(balance: 50);
        var payee = await CreateUserAsync();

        var response = await TransferAsync(payer, new TransferRequest(20, payee.Id));

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal(50, await GetBalanceAsync(payer.Id));
    }

    [Fact]
    public async Task Authorizer_timeout_returns_503()
    {
        AuthorizerReturns(200, authorized: true, delay: TimeSpan.FromSeconds(3));
        var payer = await CreateUserAsync(balance: 50);
        var payee = await CreateUserAsync();

        var response = await TransferAsync(payer, new TransferRequest(20, payee.Id));

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal(50, await GetBalanceAsync(payer.Id));
    }

    [Fact]
    public async Task Repeated_idempotency_key_replays_the_original_transfer()
    {
        var payer = await CreateUserAsync(balance: 100);
        var payee = await CreateUserAsync();
        var request = new TransferRequest(30, payee.Id);

        var first = await TransferAsync(payer, request, "order-123");
        var second = await TransferAsync(payer, request, "order-123");

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        Assert.Equal("true", second.Headers.GetValues("Idempotent-Replayed").Single());
        Assert.Equal(
            (await first.Content.ReadFromJsonAsync<TransferResponse>(TestJson.Options))!.Id,
            (await second.Content.ReadFromJsonAsync<TransferResponse>(TestJson.Options))!.Id);
        Assert.Equal(70, await GetBalanceAsync(payer.Id));
    }

    [Fact]
    public async Task Same_idempotency_key_from_different_payers_does_not_collide()
    {
        var payee = await CreateUserAsync();
        var alice = await CreateUserAsync(balance: 10);
        var bob = await CreateUserAsync(balance: 10);

        Assert.Equal(HttpStatusCode.Created, (await TransferAsync(alice, new TransferRequest(5, payee.Id), "shared-key")).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await TransferAsync(bob, new TransferRequest(5, payee.Id), "shared-key")).StatusCode);
        Assert.Equal(10, await GetBalanceAsync(payee.Id));
    }

    [Fact]
    public async Task Reusing_idempotency_key_with_different_payload_returns_422()
    {
        var payer = await CreateUserAsync(balance: 100);
        var payee = await CreateUserAsync();

        await TransferAsync(payer, new TransferRequest(30, payee.Id), "order-456");
        var response = await TransferAsync(payer, new TransferRequest(31, payee.Id), "order-456");

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal(70, await GetBalanceAsync(payer.Id));
    }

    [Fact]
    public async Task Concurrent_requests_with_same_idempotency_key_debit_once_and_never_fail()
    {
        // O atraso no autorizador garante que todas as requisições passem pela consulta
        // da chave antes de qualquer uma gravar, reproduzindo a corrida do projeto original.
        AuthorizerReturns(200, authorized: true, delay: TimeSpan.FromMilliseconds(300));
        var payer = await CreateUserAsync(balance: 100);
        var payee = await CreateUserAsync();
        var request = new TransferRequest(10, payee.Id);

        var responses = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => TransferAsync(payer, request, "same-key")));

        Assert.All(responses, r => Assert.True(r.IsSuccessStatusCode, $"Unexpected {(int)r.StatusCode}"));
        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.Created);

        var ids = await Task.WhenAll(responses.Select(async r => (await r.Content.ReadFromJsonAsync<TransferResponse>(TestJson.Options))!.Id));
        Assert.Single(ids.Distinct());

        Assert.Equal(90, await GetBalanceAsync(payer.Id));
        Assert.Equal(10, await GetBalanceAsync(payee.Id));
        Assert.Equal(1, await WithDbAsync(db => db.Transactions.CountAsync(t => t.PayerId == payer.Id)));
    }

    [Fact]
    public async Task Concurrent_transfers_never_overdraw()
    {
        AuthorizerReturns(200, authorized: true, delay: TimeSpan.FromMilliseconds(100));
        var payer = await CreateUserAsync(balance: 100);
        var payee = await CreateUserAsync();

        var responses = await Task.WhenAll(Enumerable.Range(0, 10).Select(_ => TransferAsync(payer, new TransferRequest(30, payee.Id))));

        Assert.Equal(3, responses.Count(r => r.StatusCode == HttpStatusCode.Created));
        Assert.Equal(7, responses.Count(r => r.StatusCode == HttpStatusCode.UnprocessableEntity));
        Assert.Equal(10, await GetBalanceAsync(payer.Id));
        Assert.Equal(90, await GetBalanceAsync(payee.Id));
    }

    [Fact]
    public async Task Blank_idempotency_key_returns_400()
    {
        var payer = await CreateUserAsync(balance: 10);
        var payee = await CreateUserAsync();

        var response = await TransferAsync(payer, new TransferRequest(5, payee.Id), " ");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static Task<HttpResponseMessage> TransferAsync(TestUser user, TransferRequest request, string? idempotencyKey = null)
    {
        var message = new HttpRequestMessage(HttpMethod.Post, "/transfer")
        {
            Content = JsonContent.Create(request, options: TestJson.Options)
        };
        if (idempotencyKey is not null)
            message.Headers.Add("Idempotency-Key", idempotencyKey);

        return user.Client.SendAsync(message);
    }
}
