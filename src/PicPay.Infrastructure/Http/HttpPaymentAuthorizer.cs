using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly.CircuitBreaker;
using Polly.Timeout;
using PicPay.Application.Abstractions;

namespace PicPay.Infrastructure.Http;

internal sealed class HttpPaymentAuthorizer(
    HttpClient http,
    IOptions<AuthorizerOptions> options,
    ILogger<HttpPaymentAuthorizer> logger) : IPaymentAuthorizer
{
    public async Task<AuthorizationDecision> AuthorizeAsync(CancellationToken ct)
    {
        try
        {
            using var response = await http.GetAsync(options.Value.Path, ct);

            if (response.StatusCode == HttpStatusCode.Forbidden)
                return AuthorizationDecision.Denied;

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Authorizer returned {StatusCode}", (int)response.StatusCode);
                return AuthorizationDecision.Unavailable;
            }

            var body = await response.Content.ReadFromJsonAsync<AuthorizeResponse>(ct);
            return body?.Data?.Authorization == true ? AuthorizationDecision.Authorized : AuthorizationDecision.Denied;
        }
        catch (Exception ex) when (ex is HttpRequestException or TimeoutRejectedException or BrokenCircuitException or JsonException
                                   || (ex is OperationCanceledException && !ct.IsCancellationRequested))
        {
            logger.LogWarning(ex, "Authorizer unavailable");
            return AuthorizationDecision.Unavailable;
        }
    }

    private sealed record AuthorizeResponse(string? Status, AuthorizeData? Data);

    private sealed record AuthorizeData(bool Authorization);
}
