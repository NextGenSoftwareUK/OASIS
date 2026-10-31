using System;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public partial class Neo4jNativeCqlTests
{
    [TestMethod]
    public Task NativeNumericPreservesWindowAndDoesNotHydrateEntities()
        => WithNativeGraph(async fixture =>
        {
            var source = CosmicQuery<ScalarMustNotConstruct>.Create()
                .Where(h => h.Name.StartsWith("owned CQL graph", StringComparison.Ordinal))
                .OrderByDescending(h => h.Name).Take(1).Result;
            var sum = await fixture.Provider.ExecuteCosmicScalarAsync(NumericPlan(source.Sum(h => CosmicQueryMetadata.Value<long>(h, "stock")).Result));
            Assert.IsFalse(sum.IsError, sum.Message + sum.Exception); Assert.AreEqual(7L, sum.Result);
        }, configure: root => { root.MetaData["stock"] = 7L; foreach (var child in root.Children) child.MetaData["stock"] = "outside window"; });

    [TestMethod]
    public Task NativeNumericNullableMissingValuesDoNotBecomeRequiredZeros()
        => WithNativeGraph(async fixture =>
        {
            var ids = new[] { fixture.Root.Id, fixture.Root.Children.First().Id };
            var source = CosmicQuery<Holon>.Create().Where(h => ids.Contains(h.Id)).Result;
            var sum = await fixture.Provider.ExecuteCosmicScalarAsync(NumericPlan(source.Sum(h => CosmicQueryMetadata.Value<long?>(h, "optionalNumeric")).Result));
            var average = await fixture.Provider.ExecuteCosmicScalarAsync(NumericPlan(source.Average(h => CosmicQueryMetadata.Value<long?>(h, "optionalNumeric")).Result));
            Assert.IsFalse(sum.IsError, sum.Message + sum.Exception); Assert.AreEqual((long?)9, sum.Result);
            Assert.IsFalse(average.IsError, average.Message + average.Exception); Assert.AreEqual((decimal?)9, average.Result);
            var required = await fixture.Provider.ExecuteCosmicScalarAsync(NumericPlan(source.Sum(h => CosmicQueryMetadata.Value<long>(h, "optionalNumeric")).Result));
            Assert.AreEqual("CQL_DATA_INVALID", required.ErrorCode); Assert.IsFalse(required.IsLoaded);
        }, configure: root => root.MetaData["optionalNumeric"] = 9L);

    [TestMethod]
    public Task NativeNumericRejectsParticipatingNumericStrings()
        => WithNativeGraph(async fixture =>
        {
            var ids = new[] { fixture.Root.Id, fixture.Root.Children.First().Id };
            var source = CosmicQuery<Holon>.Create().Where(h => ids.Contains(h.Id)).Result;
            var result = await fixture.Provider.ExecuteCosmicScalarAsync(NumericPlan(source.Sum(h => CosmicQueryMetadata.Value<long?>(h, "numeric")).Result));
            Assert.AreEqual("CQL_DATA_INVALID", result.ErrorCode); Assert.IsFalse(result.IsLoaded);
        }, configure: root => { root.MetaData["numeric"] = 2L; root.Children.First().MetaData["numeric"] = "3"; });
}
