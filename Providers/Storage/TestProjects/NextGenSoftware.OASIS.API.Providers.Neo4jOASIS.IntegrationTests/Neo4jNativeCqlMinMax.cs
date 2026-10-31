using System;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Provider = NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Neo4jOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public partial class Neo4jNativeCqlTests
{
    private static CosmicScalarPlan<T, TResult> MinMaxPlan<T, TResult>(CosmicScalarQuery<T, TResult> query) where T : IHolon
    {
        var claims = new CosmicScalarCapabilities(Array.Empty<CosmicScalarOperation>(), new[]
        {
            new CosmicScalarSelectionCapability(CosmicScalarOperation.Min, Provider.CqlProjectionCapabilities),
            new CosmicScalarSelectionCapability(CosmicScalarOperation.Max, Provider.CqlProjectionCapabilities)
        });
        var plan = CosmicScalarPlanner.Plan(query, Core.Enums.ProviderType.Neo4jOASIS,
            new[] { new CosmicScalarTarget(Core.Enums.ProviderType.Neo4jOASIS, Provider.CqlCapabilities, claims) });
        Assert.IsFalse(plan.IsError, plan.Message); return plan.Result;
    }

    [TestMethod]
    public Task NativeMinMaxRetainsExactNegativeInt64WithoutHydration()
        => WithNativeGraph(async fixture =>
        {
            var query = CosmicQuery<ScalarMustNotConstruct>.Create()
                .Where(h => h.Name.StartsWith("owned CQL graph", StringComparison.Ordinal)).Result;
            var min = await fixture.Provider.ExecuteCosmicScalarAsync(MinMaxPlan(query.Min(h => CosmicQueryMetadata.Value<long>(h, "stock")).Result));
            Assert.IsFalse(min.IsError, min.Message + min.Exception); Assert.AreEqual(long.MinValue, min.Result);
            var max = await fixture.Provider.ExecuteCosmicScalarAsync(MinMaxPlan(query.Max(h => CosmicQueryMetadata.Value<long>(h, "stock")).Result));
            Assert.IsFalse(max.IsError, max.Message + max.Exception); Assert.AreEqual(long.MaxValue, max.Result);
        }, configure: root =>
        {
            root.MetaData["stock"] = long.MaxValue; root.MetaData["Rating"] = "unselected wrong type";
            root.Children.First().MetaData["stock"] = long.MinValue;
            root.Children.ElementAt(1).MetaData["stock"] = -9007199254740993L;
            root.Children.First().Children.First().MetaData["stock"] = 9007199254740993L;
        });

    [TestMethod]
    public Task NativeMinMaxRequiredNullableAndStringEmptyInputsAreDistinct()
        => WithNativeGraph(async fixture =>
        {
            var id = Guid.NewGuid(); var source = CosmicQuery<Holon>.Create().Where(h => h.Id == id).Result;
            var required = await fixture.Provider.ExecuteCosmicScalarAsync(MinMaxPlan(source.Min(h => CosmicQueryMetadata.Value<long>(h, "stock")).Result));
            Assert.AreEqual("CQL_EMPTY_SEQUENCE", required.ErrorCode);
            var optional = await fixture.Provider.ExecuteCosmicScalarAsync(MinMaxPlan(source.Max(h => CosmicQueryMetadata.Value<long?>(h, "stock")).Result));
            Assert.IsFalse(optional.IsError, optional.Message + optional.Exception); Assert.IsTrue(optional.IsLoaded); Assert.IsNull(optional.Result);
            var text = await fixture.Provider.ExecuteCosmicScalarAsync(MinMaxPlan(source.Min(h => h.Name).Result));
            Assert.IsFalse(text.IsError, text.Message + text.Exception); Assert.IsNull(text.Result);
        }, width: 0);

    [TestMethod]
    public Task NativeMinMaxDoesNotHideWrongNonWinningValues()
        => WithNativeGraph(async fixture =>
        {
            var ids = new[] { fixture.Root.Id, fixture.Root.Children.First().Id };
            var source = CosmicQuery<Holon>.Create().Where(h => ids.Contains(h.Id)).Result;
            var invalid = await fixture.Provider.ExecuteCosmicScalarAsync(MinMaxPlan(source.Min(h => CosmicQueryMetadata.Value<long>(h, "stock")).Result));
            Assert.AreEqual("CQL_DATA_INVALID", invalid.ErrorCode); Assert.IsFalse(invalid.IsLoaded);
        }, configure: root => { root.MetaData["stock"] = 0L; root.Children.First().MetaData["stock"] = "wrong"; });

    [TestMethod]
    public Task NativeMinMaxPreservesSourceWindowBeforeValueValidation()
        => WithNativeGraph(async fixture =>
        {
            var source = CosmicQuery<Holon>.Create().Where(h => h.Name.StartsWith("owned CQL graph", StringComparison.Ordinal))
                .OrderByDescending(h => h.Name).Take(1).Result;
            var min = await fixture.Provider.ExecuteCosmicScalarAsync(MinMaxPlan(source.Min(h => CosmicQueryMetadata.Value<long>(h, "stock")).Result));
            Assert.IsFalse(min.IsError, min.Message + min.Exception); Assert.AreEqual(7L, min.Result);
        }, configure: root => { root.MetaData["stock"] = 7L; foreach (var child in root.Children) child.MetaData["stock"] = "outside window"; });

    [TestMethod]
    public Task NativeMinMaxUsesUnicodeScalarOrder()
        => WithNativeGraph(async fixture =>
        {
            var ids = new[] { fixture.Root.Id, fixture.Root.Children.First().Id };
            var source = CosmicQuery<Holon>.Create().Where(h => ids.Contains(h.Id)).Result;
            var min = await fixture.Provider.ExecuteCosmicScalarAsync(MinMaxPlan(source.Min(h => CosmicQueryMetadata.Value<string>(h, "text")).Result));
            Assert.IsFalse(min.IsError, min.Message + min.Exception); Assert.AreEqual("\ue000", min.Result);
            var max = await fixture.Provider.ExecuteCosmicScalarAsync(MinMaxPlan(source.Max(h => CosmicQueryMetadata.Value<string>(h, "text")).Result));
            Assert.IsFalse(max.IsError, max.Message + max.Exception); Assert.AreEqual("\U00010000", max.Result);
        }, configure: root => { root.MetaData["text"] = "\ue000"; root.Children.First().MetaData["text"] = "\U00010000"; });
}
