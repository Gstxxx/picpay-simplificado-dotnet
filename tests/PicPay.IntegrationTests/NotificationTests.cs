using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using PicPay.Application.Transfers;
using PicPay.Domain.Notifications;
using PicPay.IntegrationTests.Infrastructure;
using WireMock.Matchers;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

namespace PicPay.IntegrationTests;

public class NotificationTests(ApiFactory factory) : IntegrationTest(factory)
{
    [Fact]
    public async Task Transfer_enqueues_notification_that_the_worker_delivers()
    {
        var payer = await CreateUserAsync(balance: 100);
        var payee = await CreateUserAsync();

        var transfer = await TransferAsync(payer, payee, 12.34m);

        var message = await EventuallyAsync(
            () => OutboxFor(transfer.Id),
            m => m?.Status == NotificationStatus.Sent);

        Assert.Equal(NotificationStatus.Sent, message!.Status);
        Assert.Equal(payee.Email, message.Recipient);
        Assert.Contains("12,34", message.Message);
        Assert.Contains(ExternalServices.LogEntries, e =>
            e.RequestMessage?.Path == ApiFactory.NotifyPath && e.RequestMessage.Body?.Contains(payee.Email) == true);
    }

    [Fact]
    public async Task Notification_failure_does_not_undo_the_transfer_and_is_retried()
    {
        var payer = await CreateUserAsync(balance: 100);
        var payee = await CreateUserAsync();

        ExternalServices
            .Given(Request.Create().WithPath(ApiFactory.NotifyPath).UsingPost()
                .WithBody(new JsonPartialMatcher(new { email = payee.Email })))
            .AtPriority(1)
            .InScenario($"notify-{payee.Id}")
            .WillSetStateTo("recovered")
            .RespondWith(Response.Create().WithStatusCode(500));

        var transfer = await TransferAsync(payer, payee, 10);

        var message = await EventuallyAsync(
            () => OutboxFor(transfer.Id),
            m => m?.Status == NotificationStatus.Sent);

        Assert.Equal(NotificationStatus.Sent, message!.Status);
        Assert.Equal(2, message.Attempts);
        Assert.Equal(90, await GetBalanceAsync(payer.Id));
    }

    [Fact]
    public async Task Notification_is_marked_failed_after_max_attempts()
    {
        var payer = await CreateUserAsync(balance: 100);
        var payee = await CreateUserAsync();

        ExternalServices
            .Given(Request.Create().WithPath(ApiFactory.NotifyPath).UsingPost()
                .WithBody(new JsonPartialMatcher(new { email = payee.Email })))
            .AtPriority(1)
            .RespondWith(Response.Create().WithStatusCode(500));

        var transfer = await TransferAsync(payer, payee, 10);

        var message = await EventuallyAsync(
            () => OutboxFor(transfer.Id),
            m => m?.Status == NotificationStatus.Failed);

        Assert.Equal(NotificationStatus.Failed, message!.Status);
        Assert.Equal(3, message.Attempts);
        Assert.NotNull(message.LastError);
    }

    private async Task<TransferResponse> TransferAsync(TestUser payer, TestUser payee, decimal value)
    {
        var response = await payer.Client.PostAsJsonAsync("/transfer", new TransferRequest(value, payee.Id), TestJson.Options);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<TransferResponse>(TestJson.Options))!;
    }

    private Task<NotificationOutbox?> OutboxFor(Guid transactionId) =>
        WithDbAsync(db => db.NotificationOutbox.AsNoTracking().FirstOrDefaultAsync(n => n.TransactionId == transactionId));
}
