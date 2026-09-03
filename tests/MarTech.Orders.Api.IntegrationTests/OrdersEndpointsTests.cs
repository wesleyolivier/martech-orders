using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using MarTech.Orders.Application.Common;
using MarTech.Orders.Application.Orders.Contracts;

namespace MarTech.Orders.Api.IntegrationTests;

public sealed class OrdersEndpointsTests(OrdersApiFactory factory) : IClassFixture<OrdersApiFactory>
{
    private static readonly Guid CustomerId = Guid.Parse("2f0a5c3d-9b7e-4a11-8c62-5d4e3f2a1b09");

    [Fact]
    public async Task Login_WithSeededCredentials_ReturnsToken()
    {
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/auth/login",
            new { email = "dev@martech.com", password = "Senha@123" });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<LoginPayload>();
        payload.ShouldNotBeNull();
        payload.AccessToken.ShouldNotBeNullOrWhiteSpace();
        payload.TokenType.ShouldBe("Bearer");
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/auth/login",
            new { email = "dev@martech.com", password = "wrong" });

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetOrders_WithoutToken_ReturnsUnauthorized()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync(new Uri("/api/orders", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PlaceOrder_ThenReadItBack_RoundTripsTheDomainTotal()
    {
        using var client = await AuthenticatedClientAsync();

        var created = await client.PostAsJsonAsync("/api/orders", new
        {
            customerId = CustomerId,
            items = new[]
            {
                new { productName = "Keyboard", quantity = 2, unitPrice = 149.90m },
                new { productName = "Mouse", quantity = 1, unitPrice = 89.50m }
            }
        });

        created.StatusCode.ShouldBe(HttpStatusCode.Created);
        created.Headers.Location.ShouldNotBeNull();

        var order = await created.Content.ReadFromJsonAsync<OrderResponse>();
        order.ShouldNotBeNull();
        order.TotalAmount.ShouldBe(389.30m);
        order.Status.ShouldBe("Pending");

        var fetched = await client.GetFromJsonAsync<OrderResponse>($"/api/orders/{order.Id}");
        fetched.ShouldNotBeNull();
        fetched.TotalAmount.ShouldBe(389.30m);
        fetched.CreatedAt.Kind.ShouldBe(DateTimeKind.Utc);
        fetched.Items.Count.ShouldBe(2);
    }

    [Fact]
    public async Task CancelOrder_TwiceReturnsConflictOnTheSecondAttempt()
    {
        using var client = await AuthenticatedClientAsync();

        var order = await PlaceOrderAsync(client);

        var first = await client.PatchAsync(new Uri($"/api/orders/{order.Id}/cancel", UriKind.Relative), null);
        first.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var second = await client.PatchAsync(new Uri($"/api/orders/{order.Id}/cancel", UriKind.Relative), null);
        second.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task GetOrder_WithUnknownId_ReturnsNotFound()
    {
        using var client = await AuthenticatedClientAsync();

        var response = await client.GetAsync(new Uri($"/api/orders/{Guid.CreateVersion7()}", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PlaceOrder_WithoutItems_ReturnsValidationProblem()
    {
        using var client = await AuthenticatedClientAsync();

        var response = await client.PostAsJsonAsync("/api/orders", new
        {
            customerId = CustomerId,
            items = Array.Empty<object>()
        });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var problem = await response.Content.ReadAsStringAsync();
        problem.ShouldContain("at least one item");
    }

    [Fact]
    public async Task ListOrders_RespectsThePageSize()
    {
        using var client = await AuthenticatedClientAsync();

        await PlaceOrderAsync(client);
        await PlaceOrderAsync(client);

        var page = await client.GetFromJsonAsync<PagedResult<OrderSummaryResponse>>("/api/orders?page=1&pageSize=1");

        page.ShouldNotBeNull();
        page.Items.Count.ShouldBe(1);
        page.PageSize.ShouldBe(1);
        page.TotalCount.ShouldBeGreaterThanOrEqualTo(2);
        page.HasNextPage.ShouldBeTrue();
    }

    [Fact]
    public async Task ListOrders_WithInvalidPaging_ReturnsValidationProblem()
    {
        using var client = await AuthenticatedClientAsync();

        var response = await client.GetAsync(new Uri("/api/orders?page=0&pageSize=1000", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Application_UsesTheDatabaseConfiguredForTheTestHost()
    {
        using var client = await AuthenticatedClientAsync();

        await PlaceOrderAsync(client);

        File.Exists(factory.DatabasePath).ShouldBeTrue();
    }

    private async Task<HttpClient> AuthenticatedClientAsync()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/auth/login",
            new { email = "dev@martech.com", password = "Senha@123" });

        var payload = await response.Content.ReadFromJsonAsync<LoginPayload>();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", payload!.AccessToken);

        return client;
    }

    private static async Task<OrderResponse> PlaceOrderAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/orders", new
        {
            customerId = CustomerId,
            items = new[] { new { productName = "Monitor", quantity = 1, unitPrice = 1299.99m } }
        });

        return (await response.Content.ReadFromJsonAsync<OrderResponse>())!;
    }

    private sealed record LoginPayload(string AccessToken, DateTime ExpiresAtUtc, string TokenType);
}
