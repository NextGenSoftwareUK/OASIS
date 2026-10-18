using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.TRONOASIS.IntegrationTests;

public sealed class TronGridShastaHistoryTests
{
    [Fact]
    public async Task PublicReadOnlyHistoryFollowsRealFingerprintsOnExpectedGenesis()
    {
        using var http = new HttpClient { BaseAddress = new Uri("https://api.shasta.trongrid.io/"), Timeout = TimeSpan.FromSeconds(20) };
        using var bounded = new CancellationTokenSource(TimeSpan.FromSeconds(50));
        const long upperBound = 1791676800000;
        var reader = new TronGridTokenHistoryClient(http,
            "0000000000000000de1aa88295e1fcf982742f773e0419c5a9c134c994a9059e", pageSize: 2);
        // Public account used in the official API reference; no credentials or writes.
        var records = await reader.ReadAsync("TUoHaVjx7n5xz8LwPRDckgFrDWhMhuSuJM", upperBound, bounded.Token);
        Assert.True(records.Count > 2, "Expected multiple real fingerprint pages, not a single-page smoke check.");
        Assert.All(records, record =>
        {
            Assert.InRange(record.GetProperty("block_timestamp").GetInt64(), 0, upperBound);
            Assert.False(string.IsNullOrWhiteSpace(record.GetProperty("transaction_id").GetString()));
            // The real indexer also returns records whose token_info is empty.
            // History retrieval must preserve these; inventory discovery requires separate validation.
            Assert.Equal(System.Text.Json.JsonValueKind.Object, record.GetProperty("token_info").ValueKind);
        });
        Assert.Contains(records, record => record.GetProperty("token_info").TryGetProperty("address", out var address)
            && !string.IsNullOrWhiteSpace(address.GetString()));
    }
}
