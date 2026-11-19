using System.Net;
using System.Text;
using Microsoft.Extensions.Configuration;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription;
using Stripe;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests.Subscription;

public sealed class SubscriptionPlanPriceReaderTests
{
    [Fact]
    public async Task StripeCatalogCoalescesPriceReadsAndRefreshesOnExpiryOrConfigurationChange()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>
        {
            ["STRIPE_SECRET_KEY"] = "sk_test_catalog_fixture",
            ["STRIPE_PRICE_BRONZE"] = "price_bronze"
        }).Build();
        using var transport = new PriceTransport();
        using var http = new HttpClient(transport);
        var clock = new CatalogueClock();
        var reader = new StripeSubscriptionPlanPriceReader(configuration,
            key => new StripeClient(key, httpClient: new SystemNetHttpClient(http, maxNetworkRetries: 0)), clock);

        var first = reader.GetAsync("bronze", CancellationToken.None);
        await transport.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var next = reader.GetAsync("bronze", CancellationToken.None);
        transport.AllowResponse.SetResult();
        Assert.All(await Task.WhenAll(first, next), price => Assert.Equal(9m, price.Amount));
        Assert.Equal(1, transport.Requests);

        clock.Advance(TimeSpan.FromSeconds(31));
        Assert.Equal(9m, (await reader.GetAsync("bronze", CancellationToken.None)).Amount);
        Assert.Equal(2, transport.Requests);

        configuration["STRIPE_PRICE_BRONZE"] = "price_bronze_new";
        Assert.Equal(12m, (await reader.GetAsync("bronze", CancellationToken.None)).Amount);
        Assert.Equal(3, transport.Requests);

        configuration["STRIPE_SECRET_KEY"] = "sk_test_catalog_other";
        Assert.Equal(12m, (await reader.GetAsync("bronze", CancellationToken.None)).Amount);
        Assert.Equal(4, transport.Requests);
    }

    private sealed class CatalogueClock : TimeProvider
    {
        private DateTimeOffset _now = DateTimeOffset.UnixEpoch;
        public override DateTimeOffset GetUtcNow() => _now;
        public void Advance(TimeSpan duration) => _now += duration;
    }

    private sealed class PriceTransport : HttpMessageHandler
    {
        private int _requests;
        public int Requests => Volatile.Read(ref _requests);
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource AllowResponse { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _requests);
            Started.TrySetResult();
            await AllowResponse.Task.WaitAsync(cancellationToken);
            var id = request.RequestUri!.Segments[^1];
            var amount = id == "price_bronze" ? 900 : 1200;
            var json = System.Text.Json.JsonSerializer.Serialize(new
            {
                id, @object = "price", active = true, unit_amount = amount, currency = "gbp",
                recurring = new { interval = "month", interval_count = 1 }
            });
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
        }
    }
}
