using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Context;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Repositories;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.UnitTests;

[TestClass]
public sealed class CqlEntityCompositionOwnerTests
{
    private static CosmicEntityCompositionPlan<Holon> Plan(CosmicEntityCompositionQuery<Holon> query,
        CosmicEntityCompositionOptions? options = null)
        => CosmicEntityCompositionPlanner.Plan(query, ProviderType.SQLLiteDBOASIS,
            new CosmicQueryTarget(ProviderType.SQLLiteDBOASIS, SQLLiteDBOASIS.CqlCapabilities),
            new CosmicEntityCompositionCapabilities(CosmicEntityStageFeatures.Filter | CosmicEntityStageFeatures.Ordering
                | CosmicEntityStageFeatures.Paging, SQLLiteDBOASIS.CqlCapabilities), options).Result;

    [TestMethod]
    public async Task NativeOwnerPreservesSourceWindowsAndRejectsHiddenResourceOverruns()
    {
        var settings = "Data Source=cosmic-entity-owner;Mode=Memory;Cache=Shared;Pooling=False";
        await using var keeper = new SqliteConnection(settings); await keeper.OpenAsync();
        using var provider = new SQLLiteDBOASIS(settings);
        var activated = await provider.ActivateProviderAsync(); Assert.IsFalse(activated.IsError, activated.Message);
        foreach (var name in new[] { "C", "B", "A" })
        {
            var saved = await provider.SaveHolonAsync(new Holon { Id = Guid.NewGuid(), Name = name }, saveChildren: false);
            Assert.IsFalse(saved.IsError, saved.Message);
        }
        var source = CosmicQuery<Holon>.Create().OrderByDescending(row => row.Name).Result.Take(2).Result;
        var query = source.Compose().Result.Where(row => row.Name == "A").Result;
        var empty = await provider.ExecuteCosmicEntityCompositionAsync(Plan(query), 10, CancellationToken.None);
        Assert.IsFalse(empty.IsError, empty.Message + empty.Exception); Assert.IsTrue(empty.IsLoaded); Assert.AreEqual(0, empty.Result.Count);
        var ordered = source.Compose().Result.OrderBy(row => row.Name).Result.Take(1).Result;
        var rows = await provider.ExecuteCosmicEntityCompositionAsync(Plan(ordered), 10, CancellationToken.None);
        Assert.IsFalse(rows.IsError, rows.Message + rows.Exception); Assert.AreEqual("B", rows.Result.Single().Name);
        var hidden = ordered.Take(0).Result;
        foreach (var options in new[] {
            new CosmicEntityCompositionOptions(maximumInputRows: 1),
            new CosmicEntityCompositionOptions(maximumInputPayloadBytes: 1),
            new CosmicEntityCompositionOptions(maximumStageRows: 1) })
        {
            var rejected = await provider.ExecuteCosmicEntityCompositionAsync(Plan(hidden, options), 10, CancellationToken.None);
            Assert.IsTrue(rejected.IsError); Assert.AreEqual("CQL_QUERY_LIMIT", rejected.ErrorCode, rejected.Message + rejected.Exception);
            Assert.IsFalse(rejected.IsLoaded);
        }
        using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
        var cancelled = await provider.ExecuteCosmicEntityCompositionAsync(Plan(ordered), 10, cancellation.Token);
        Assert.AreEqual("CQL_QUERY_CANCELLED", cancelled.ErrorCode);
    }
}
