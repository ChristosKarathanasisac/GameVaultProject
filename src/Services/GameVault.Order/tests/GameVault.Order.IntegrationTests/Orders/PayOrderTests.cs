using System.Net;
using System.Net.Http.Json;
using GameVault.Contracts.Responses.Order;
using GameVault.Order.Application.Abstractions;
using GameVault.Order.Application.Payments;
using GameVault.Order.Domain.Enums;
using GameVault.Order.Infrastructure.Persistence;
using GameVault.Order.IntegrationTests.Fixtures;
using GameVault.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NSubstitute.ClearExtensions;

namespace GameVault.Order.IntegrationTests.Orders;

[Collection(nameof(OrderApiCollection))]
public sealed class PayOrderTests : IAsyncLifetime
{
    private readonly OrderApiFactory _factory;
    private readonly Guid _customerId = Guid.NewGuid();
    private HttpClient _client = null!;

    public PayOrderTests(OrderApiFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        _factory.CatalogClientMock.ClearSubstitute(ClearOptions.All);
        _factory.PaymentGatewayMock.ClearSubstitute(ClearOptions.All);
        await _factory.ResetDatabaseAsync();
        _client = _factory.CreateAuthenticatedClient(_customerId);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    // ── Auth ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task PayOrder_Returns401_WhenNotAuthenticated()
    {
        var anonymousClient = _factory.CreateClient();
        var orderId = await _factory.SeedReservedOrderAsync(_customerId);

        var response = await anonymousClient.PostAsync($"/api/orders/{orderId}/pay", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ── Ownership ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task PayOrder_Returns404_WhenOrderBelongsToAnotherCustomer()
    {
        var otherCustomerId = Guid.NewGuid();
        var orderId = await _factory.SeedReservedOrderAsync(otherCustomerId);

        var response = await _client.PostAsync($"/api/orders/{orderId}/pay", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PayOrder_Returns404_WhenOrderDoesNotExist()
    {
        var response = await _client.PostAsync($"/api/orders/{Guid.NewGuid()}/pay", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ── Invalid status ────────────────────────────────────────────────────────

    [Fact]
    public async Task PayOrder_Returns409_WhenOrderIsNotReserved()
    {
        var orderId = await _factory.SeedOrderAsync(_customerId);

        var response = await _client.PostAsync($"/api/orders/{orderId}/pay", null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    // ── Happy path: payment succeeds → Paid ───────────────────────────────────

    [Fact]
    public async Task PayOrder_Returns200_WithPaidOrder_WhenPaymentAndConfirmsSucceed()
    {
        var orderId = await _factory.SeedReservedOrderAsync(_customerId);
        SetupSuccessfulPayment(orderId);
        SetupSuccessfulConfirmForAll(orderId);

        var response = await _client.PostAsync($"/api/orders/{orderId}/pay", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var order = await response.Content.ReadFromJsonAsync<OrderResponse>();
        Assert.NotNull(order);
        Assert.Equal(OrderStatus.Paid.ToString(), order.Status);
    }

    [Fact]
    public async Task PayOrder_PersistsPaidStatus_InDatabase_WhenSuccessful()
    {
        var orderId = await _factory.SeedReservedOrderAsync(_customerId);
        SetupSuccessfulPayment(orderId);
        SetupSuccessfulConfirmForAll(orderId);

        await _client.PostAsync($"/api/orders/{orderId}/pay", null);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
        var order = await db.Orders.FirstOrDefaultAsync(o => o.Id == orderId);
        Assert.NotNull(order);
        Assert.Equal(OrderStatus.Paid, order.Status);
    }

    // ── Payment declined → PaymentFailed ──────────────────────────────────────

    [Fact]
    public async Task PayOrder_Returns409_WhenPaymentDeclined()
    {
        var orderId = await _factory.SeedReservedOrderAsync(_customerId);
        _factory.PaymentGatewayMock
            .ChargeAsync(orderId, Arg.Any<decimal>(), Arg.Any<CancellationToken>())
            .Returns(Result<PaymentResult>.Failure(
                Error.Conflict("Order.Payment.Declined", "The payment was declined.")));
        _factory.CatalogClientMock
            .ReleaseReservationAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Result<Unit>.Success(Unit.Value));

        var response = await _client.PostAsync($"/api/orders/{orderId}/pay", null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task PayOrder_PersistsPaymentFailedStatus_InDatabase_WhenPaymentDeclined()
    {
        var orderId = await _factory.SeedReservedOrderAsync(_customerId);
        _factory.PaymentGatewayMock
            .ChargeAsync(orderId, Arg.Any<decimal>(), Arg.Any<CancellationToken>())
            .Returns(Result<PaymentResult>.Failure(
                Error.Conflict("Order.Payment.Declined", "The payment was declined.")));
        _factory.CatalogClientMock
            .ReleaseReservationAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Result<Unit>.Success(Unit.Value));

        await _client.PostAsync($"/api/orders/{orderId}/pay", null);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
        var order = await db.Orders.FirstOrDefaultAsync(o => o.Id == orderId);
        Assert.NotNull(order);
        Assert.Equal(OrderStatus.PaymentFailed, order.Status);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void SetupSuccessfulPayment(Guid orderId)
    {
        _factory.PaymentGatewayMock
            .ChargeAsync(orderId, Arg.Any<decimal>(), Arg.Any<CancellationToken>())
            .Returns(Result<PaymentResult>.Success(new PaymentResult(orderId)));
    }

    private void SetupSuccessfulConfirmForAll(Guid orderId)
    {
        _factory.CatalogClientMock
            .ConfirmReservationAsync(orderId, Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Result<Unit>.Success(Unit.Value));
    }
}
