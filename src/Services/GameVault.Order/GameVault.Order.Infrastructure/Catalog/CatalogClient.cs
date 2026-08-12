using System.Net;
using System.Net.Http.Json;
using Dapr.Client;
using GameVault.Contracts.Requests.Catalog;
using GameVault.Contracts.Responses.Catalog;
using GameVault.Core.Correlation;
using GameVault.Order.Application.Abstractions;
using GameVault.SharedKernel.Results;
using Microsoft.AspNetCore.Http;

namespace GameVault.Order.Infrastructure.Catalog;

public sealed class CatalogClient : ICatalogClient
{
    private readonly DaprClient _daprClient;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CatalogClient(DaprClient daprClient, IHttpContextAccessor httpContextAccessor)
    {
        _daprClient = daprClient;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<Result<GameResponse>> GetProductAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        var request = _daprClient.CreateInvokeMethodRequest(
            HttpMethod.Get,
            CatalogConsts.AppId,
            $"api/products/{productId}");

        PropagateCorrelationId(request);

        var response = await _daprClient.InvokeMethodWithResponseAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return Error.NotFound("Catalog.ProductNotFound", "The product was not found in the catalog.");

        response.EnsureSuccessStatusCode();

        var product = await response.Content.ReadFromJsonAsync<GameResponse>(cancellationToken: cancellationToken);
        return product!;
    }

    public async Task<Result<ReservationResponse>> ReserveStockAsync(
        Guid productId,
        Guid orderId,
        int quantity,
        CancellationToken cancellationToken = default)
    {
        var request = _daprClient.CreateInvokeMethodRequest(
            HttpMethod.Post,
            CatalogConsts.AppId,
            $"api/products/{productId}/reservations");
        request.Content = JsonContent.Create(new ReserveStockRequest(orderId, quantity));

        PropagateCorrelationId(request);

        var response = await _daprClient.InvokeMethodWithResponseAsync(request, cancellationToken);

        if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Conflict)
            return Error.Conflict("Catalog.ReservationFailed", "Stock reservation failed.");

        response.EnsureSuccessStatusCode();

        var reservation = await response.Content.ReadFromJsonAsync<ReservationResponse>(cancellationToken: cancellationToken);
        return reservation!;
    }

    public async Task<Result<Unit>> ReleaseReservationAsync(
        Guid orderId,
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        var request = _daprClient.CreateInvokeMethodRequest(
            HttpMethod.Post,
            CatalogConsts.AppId,
            $"api/orders/{orderId}/products/{productId}/reservations/release");

        PropagateCorrelationId(request);

        var response = await _daprClient.InvokeMethodWithResponseAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
            return Error.Failure("Catalog.ReleaseFailed", "Failed to release stock reservation.");

        return Unit.Value;
    }

    public async Task<Result<Unit>> ConfirmReservationAsync(
        Guid orderId,
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        var request = _daprClient.CreateInvokeMethodRequest(
            HttpMethod.Post,
            CatalogConsts.AppId,
            $"api/orders/{orderId}/products/{productId}/reservations/confirm");

        PropagateCorrelationId(request);

        var response = await _daprClient.InvokeMethodWithResponseAsync(request, cancellationToken);

        if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Conflict)
            return Error.Conflict("Catalog.ConfirmFailed", "Failed to confirm stock reservation.");

        if (!response.IsSuccessStatusCode)
            return Error.Failure("Catalog.ConfirmFailed", "Failed to confirm stock reservation.");

        return Unit.Value;
    }

    private void PropagateCorrelationId(HttpRequestMessage request)
    {
        var correlationId = _httpContextAccessor.HttpContext?
            .Request.Headers[CorrelationIdConsts.HeaderName]
            .FirstOrDefault();

        if (correlationId is not null)
            request.Headers.TryAddWithoutValidation(CorrelationIdConsts.HeaderName, correlationId);
    }
}
