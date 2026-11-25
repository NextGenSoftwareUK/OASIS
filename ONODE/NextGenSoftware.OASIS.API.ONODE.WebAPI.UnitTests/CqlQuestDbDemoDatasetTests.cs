using System.Net;
using System.Text;
using System.Text.Json;
using NextGenSoftware.OASIS.API.Providers.QuestDBOASIS;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

/// <summary>The read-only public demo (demo.questdb.io) has no provisioned fixture table; its
/// trades table is read one latest row per symbol. Each dataset sends one fixed SQL shape.</summary>
public class CqlQuestDbDemoDatasetTests
{
    private static readonly string[] Symbols = { "BTC-USDT", "ETH-USDT", "SOL-USDT" };

    private sealed class Demo : HttpMessageHandler
    {
        public readonly List<string> Sql = new();
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            Assert.Equal("/api/v1/sql/execute", request.RequestUri!.AbsolutePath);
            var sql = Uri.UnescapeDataString(request.RequestUri.Query.Substring("?query=".Length));
            Sql.Add(sql);
            var body = JsonSerializer.Serialize(new {
                query = sql, columns = new[] { new { name = "symbol", type = "SYMBOL" } }, timestamp = -1,
                dataset = Symbols.Select(symbol => new[] { symbol }).ToArray(), count = Symbols.Length });
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) {
                Content = new StringContent(body, Encoding.UTF8, "application/json") });
        }
    }

    [Theory]
    [InlineData(QuestDbSymbolDataset.DemoTrades,
        "select symbol from trades where symbol in ('BTC-USDT', 'ETH-USDT', 'SOL-USDT') latest on timestamp partition by symbol order by symbol")]
    [InlineData(QuestDbSymbolDataset.Fixture,
        "select symbol from cql_demo_symbols where symbol in ('BTC-USDT', 'ETH-USDT', 'SOL-USDT') order by symbol")]
    public async Task EachDatasetSendsItsFixedSqlAndActivates(QuestDbSymbolDataset dataset, string expected)
    {
        var demo = new Demo();
        using var client = new HttpClient(demo);
        using var source = new QuestDbSymbolQuerySource(client, new Uri("https://demo.questdb.io/api/v1/sql/execute"),
            Guid.NewGuid(), Symbols, dataset);
        var activation = await source.ActivateQuerySourceAsync(default);
        Assert.False(activation.IsError, activation.Message);
        Assert.True(source.IsQuerySourceActivated);
        Assert.Equal(expected, Assert.Single(demo.Sql));
    }

    [Fact]
    public void ConfigurationAcceptsOnlyKnownDatasets()
    {
        string Json(object? dataset) => dataset == null
            ? JsonSerializer.Serialize(new { schema = 1, endpoint = "https://demo.questdb.io/api/v1/sql/execute",
                sourceOwner = Guid.NewGuid(), symbols = Symbols })
            : JsonSerializer.Serialize(new { schema = 1, endpoint = "https://demo.questdb.io/api/v1/sql/execute",
                sourceOwner = Guid.NewGuid(), symbols = Symbols, dataset });
        using (QuestDbSymbolQuerySource.FromJson(Json(null))) { }
        using (QuestDbSymbolQuerySource.FromJson(Json("fixture"))) { }
        using (QuestDbSymbolQuerySource.FromJson(Json("questdb-demo-trades"))) { }
        Assert.Throws<ArgumentException>(() => QuestDbSymbolQuerySource.FromJson(Json("select * from trades")));
    }
}
