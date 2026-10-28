using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.Infrastructure;

namespace NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.IntegrationTests;

[TestClass]
public class CqlRowCommandTests
{
    private static CosmicQueryPlan<Holon> Plan(CosmicQuery<Holon> query,
        ProviderType provider = ProviderType.AzureCosmosDBOASIS, CosmicQueryCapabilities? capabilities = null)
    {
        var result = CosmicQueryPlanner.Plan(query, provider,
            new[] { new CosmicQueryTarget(provider, capabilities ?? CosmosCqlRowCommand.Capabilities) });
        Assert.IsFalse(result.IsError, result.Message);
        return result.Result;
    }

    [TestMethod]
    public void BindsVisibilityBeforeWindowAndKeepsIdentityTieBreak()
    {
        var actor = Guid.NewGuid();
        var access = CosmicQueryReadAccess.OwnerOrPublic(actor);
        Assert.IsFalse(access.IsError, access.Message);
        var query = CosmicQuery<Holon>.Create().WithReadAccess(access.Result)
            .Where(h => h.Version >= 2).OrderBy(h => h.Version).Skip(1).Take(3);
        Assert.IsFalse(query.IsError, query.Message);
        var command = CosmosCqlRowCommand.Compile(Plan(query.Result), 10);
        StringAssert.Contains(command.QueryText, "c[\"CreatedByAvatarId\"]");
        StringAssert.Contains(command.QueryText, "ORDER BY c[\"Version\"] ASC, c[\"Id\"] ASC");
        var parameters = command.GetQueryParameters().ToDictionary(p => p.Name, p => p.Value);
        Assert.IsTrue(parameters.Values.Contains(actor.ToString("D")));
        Assert.AreEqual(1, parameters["@cqlOffset"]);
        Assert.AreEqual(3, parameters["@cqlLimit"]);
    }

    [TestMethod]
    public void UnwindowedQueryRequestsOverflowSentinel()
    {
        var command = CosmosCqlRowCommand.Compile(Plan(CosmicQuery<Holon>.Create()), 10);
        Assert.AreEqual(11, command.GetQueryParameters().Single(p => p.Name == "@cqlLimit").Value);
        StringAssert.Contains(command.QueryText, "ORDER BY c[\"Id\"] ASC");
    }

    [TestMethod]
    public void ForeignPhysicalTargetRejectsEvenWithSameCapabilities()
    {
        var plan = Plan(CosmicQuery<Holon>.Create(), ProviderType.MongoDBOASIS);
        Assert.ThrowsException<NotSupportedException>(() => CosmosCqlRowCommand.Compile(plan, 10));
    }

    [TestMethod]
    public void MorePermissivePlanCannotGrantMetadataExecution()
    {
        var capabilities = new CosmicQueryCapabilities(CosmicQueryFeatures.Metadata,
            Array.Empty<CosmicQueryFieldCapability>(),
            new[] { new CosmicQueryMetadataCapability(typeof(decimal), new[] { CosmicQueryComparisonOperator.GreaterThan }) });
        var query = CosmicQuery<Holon>.Create().Where(h => CosmicQueryMetadata.Value<decimal>(h, "price") > 1m);
        Assert.IsFalse(query.IsError, query.Message);
        var plan = Plan(query.Result, capabilities: capabilities);
        Assert.ThrowsException<NotSupportedException>(() => CosmosCqlRowCommand.Compile(plan, 10));
    }

    [TestMethod]
    public void RedundantKeysDoNotRequireImpossibleDuplicatePathIndexes()
    {
        var query = CosmicQuery<Holon>.Create().OrderBy(h => h.Version)
            .ThenByDescending(h => h.Version).ThenBy(h => h.Id).ThenByDescending(h => h.Version);
        Assert.IsFalse(query.IsError, query.Message);
        var command = CosmosCqlRowCommand.Compile(Plan(query.Result), 10);
        StringAssert.Contains(command.QueryText, "ORDER BY c[\"Version\"] ASC, c[\"Id\"] ASC OFFSET");
    }

    [TestMethod]
    public void DeploymentDefinitionsAreDetachedAndDoNotMutateSharedState()
    {
        var first = AzureCosmosDBOASIS.GetCqlCompositeIndexDefinitions();
        first[0][0].Path = "/caller-change";
        var second = AzureCosmosDBOASIS.GetCqlCompositeIndexDefinitions();
        Assert.AreEqual(2, second.Count);
        Assert.AreEqual("/Version", second[0][0].Path);
        Assert.AreEqual("/Id", second[0][1].Path);
    }
}
