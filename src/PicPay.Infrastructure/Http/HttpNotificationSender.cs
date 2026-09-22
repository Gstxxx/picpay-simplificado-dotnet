using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using PicPay.Application.Abstractions;

namespace PicPay.Infrastructure.Http;

public sealed class NotifierOptions : ExternalServiceOptions
{
    public const string Section = "Notifier";
}

internal sealed class HttpNotificationSender(HttpClient http, IOptions<NotifierOptions> options) : INotificationSender
{
    public async Task SendAsync(string recipient, string message, CancellationToken ct)
    {
        using var response = await http.PostAsJsonAsync(options.Value.Path, new { email = recipient, message }, ct);
        response.EnsureSuccessStatusCode();
    }
}
