using System;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.MongoOASIS.IntegrationTests;

public sealed partial class CosmicCqlNativeTests
{
    [Fact]
    public Task NativeNumericPreservesWindowAndDoesNotHydrateEntities()
        => WithMongoGraph(async fixture =>
        {
            var source = CosmicQuery<ScalarMustNotConstruct>.Create()
                .Where(h => h.Name.StartsWith("owned CQL graph", StringComparison.Ordinal))
                .OrderByDescending(h => h.Name).Take(1).Result;
            var sum = await fixture.Provider.ExecuteCosmicScalarAsync(NumericPlan(source.Sum(h => CosmicQueryMetadata.Value<long>(h, "stock")).Result));
            Assert.False(sum.IsError, sum.Message + sum.Exception); Assert.Equal(7L, sum.Result);
        }, configure: root => { root.MetaData["stock"] = 7L; foreach (var child in root.Children) child.MetaData["stock"] = "outside window"; });

    [Fact]
    public Task NativeNumericNullableMissingValuesDoNotBecomeRequiredZeros()
        => WithMongoGraph(async fixture =>
        {
            var ids = new[] { fixture.Root.Id, fixture.Root.Children.First().Id };
            var source = CosmicQuery<Holon>.Create().Where(h => ids.Contains(h.Id)).Result;
            var sum = await fixture.Provider.ExecuteCosmicScalarAsync(NumericPlan(source.Sum(h => CosmicQueryMetadata.Value<long?>(h, "optionalNumeric")).Result));
            var average = await fixture.Provider.ExecuteCosmicScalarAsync(NumericPlan(source.Average(h => CosmicQueryMetadata.Value<long?>(h, "optionalNumeric")).Result));
            Assert.False(sum.IsError, sum.Message + sum.Exception); Assert.Equal((long?)9, sum.Result);
            Assert.False(average.IsError, average.Message + average.Exception); Assert.Equal((decimal?)9, average.Result);
            var required = await fixture.Provider.ExecuteCosmicScalarAsync(NumericPlan(source.Sum(h => CosmicQueryMetadata.Value<long>(h, "optionalNumeric")).Result));
            Assert.Equal("CQL_DATA_INVALID", required.ErrorCode); Assert.False(required.IsLoaded);
        }, configure: root => root.MetaData["optionalNumeric"] = 9L);

    [Fact]
    public Task NativeNumericRejectsParticipatingNumericStrings()
        => WithMongoGraph(async fixture =>
        {
            var ids = new[] { fixture.Root.Id, fixture.Root.Children.First().Id };
            var source = CosmicQuery<Holon>.Create().Where(h => ids.Contains(h.Id)).Result;
            var result = await fixture.Provider.ExecuteCosmicScalarAsync(NumericPlan(source.Sum(h => CosmicQueryMetadata.Value<long?>(h, "numeric")).Result));
            Assert.Equal("CQL_DATA_INVALID", result.ErrorCode); Assert.False(result.IsLoaded);
        }, configure: root => { root.MetaData["numeric"] = 2L; root.Children.First().MetaData["numeric"] = "3"; });
}
