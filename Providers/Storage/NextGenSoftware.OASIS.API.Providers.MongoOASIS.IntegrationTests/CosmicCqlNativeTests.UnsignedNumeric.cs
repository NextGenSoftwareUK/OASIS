using System;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.MongoOASIS.IntegrationTests;

public sealed partial class CosmicCqlNativeTests
{
    [Theory]
    [InlineData(0UL)]
    [InlineData(1UL)]
    [InlineData(4294967295UL)]
    [InlineData(4294967296UL)]
    [InlineData(9007199254740993UL)]
    [InlineData(9223372036854775808UL)]
    [InlineData(ulong.MaxValue)]
    public Task NativeUnsignedNumericRetainsExactBoundary(ulong value)
        => WithMongoGraph(async fixture =>
        {
            // A scalar reduction must never construct a selected holon.
            var id = fixture.Root.Id;
            var source = CosmicQuery<ScalarMustNotConstruct>.Create().Where(h => h.Id == id).Result;

            var sum = await fixture.Provider.ExecuteCosmicScalarAsync(ScalarPlan(
                source.Sum(h => CosmicQueryMetadata.Value<ulong>(h, "unsignedNumeric")).Result));
            Assert.False(sum.IsError, sum.Message + sum.Exception);
            Assert.Equal(value, sum.Result);

            var mean = await fixture.Provider.ExecuteCosmicScalarAsync(ScalarPlan(
                source.Average(h => CosmicQueryMetadata.Value<ulong>(h, "unsignedNumeric")).Result));
            Assert.False(mean.IsError, mean.Message + mean.Exception);
            Assert.Equal((decimal)value, mean.Result);
        }, configure: root => root.MetaData["unsignedNumeric"] = value);

    [Fact]
    public Task NativeUnsignedNumericOverflowDoesNotInvalidateRepresentableMean()
        => WithMongoGraph(async fixture =>
        {
            var ids = new[] { fixture.Root.Id, fixture.Root.Children.First().Id };
            var source = CosmicQuery<Holon>.Create().Where(h => ids.Contains(h.Id)).Result;

            var sum = await fixture.Provider.ExecuteCosmicScalarAsync(ScalarPlan(
                source.Sum(h => CosmicQueryMetadata.Value<ulong>(h, "unsignedNumeric")).Result));
            Assert.Equal("CQL_SCALAR_OVERFLOW", sum.ErrorCode);
            Assert.False(sum.IsLoaded);

            var mean = await fixture.Provider.ExecuteCosmicScalarAsync(ScalarPlan(
                source.Average(h => CosmicQueryMetadata.Value<ulong>(h, "unsignedNumeric")).Result));
            Assert.False(mean.IsError, mean.Message + mean.Exception);
            Assert.Equal((decimal)ulong.MaxValue, mean.Result);
        }, configure: root =>
        {
            root.MetaData["unsignedNumeric"] = ulong.MaxValue;
            root.Children.First().MetaData["unsignedNumeric"] = ulong.MaxValue;
        });

    [Fact]
    public Task NativeUnsignedNumericNullableMissingInputAndEmptyWindowKeepTheirContracts()
        => WithMongoGraph(async fixture =>
        {
            var ids = new[] { fixture.Root.Id, fixture.Root.Children.First().Id };
            var source = CosmicQuery<Holon>.Create().Where(h => ids.Contains(h.Id)).Result;
            var mean = await fixture.Provider.ExecuteCosmicScalarAsync(ScalarPlan(
                source.Average(h => CosmicQueryMetadata.Value<ulong?>(h, "unsignedNumeric")).Result));
            Assert.False(mean.IsError, mean.Message + mean.Exception);
            Assert.Equal((decimal?)ulong.MaxValue, mean.Result);

            var empty = source.Take(0).Result;
            var sum = await fixture.Provider.ExecuteCosmicScalarAsync(ScalarPlan(
                empty.Sum(h => CosmicQueryMetadata.Value<ulong?>(h, "unsignedNumeric")).Result));
            Assert.False(sum.IsError, sum.Message + sum.Exception);
            Assert.Equal((ulong?)0, sum.Result);

            var missingMean = await fixture.Provider.ExecuteCosmicScalarAsync(ScalarPlan(
                empty.Average(h => CosmicQueryMetadata.Value<ulong?>(h, "unsignedNumeric")).Result));
            Assert.False(missingMean.IsError, missingMean.Message + missingMean.Exception);
            Assert.Null(missingMean.Result);
        }, configure: root => root.MetaData["unsignedNumeric"] = ulong.MaxValue);

    [Theory]
    [InlineData(-1L)]
    [InlineData(1.5)]
    [InlineData("18446744073709551615")]
    [InlineData("not numeric")]
    public Task NativeUnsignedNumericRejectsInvalidParticipatingInput(object value)
        => WithMongoGraph(async fixture =>
        {
            var id = fixture.Root.Id;
            var source = CosmicQuery<Holon>.Create().Where(h => h.Id == id).Result;
            var result = await fixture.Provider.ExecuteCosmicScalarAsync(ScalarPlan(
                source.Sum(h => CosmicQueryMetadata.Value<ulong>(h, "unsignedNumeric")).Result));
            Assert.Equal("CQL_DATA_INVALID", result.ErrorCode);
            Assert.False(result.IsLoaded);
        }, configure: root => root.MetaData["unsignedNumeric"] = value);

    [Fact]
    public Task NativeUnsignedGroupedFilterOrderAndPagingRetainExactMeans()
        => WithMongoGraph(async fixture =>
        {
            var ids = new[] { fixture.Root.Id, fixture.Root.Children.First().Id };
            var query = CosmicQuery<Holon>.Create().Where(h => ids.Contains(h.Id))
                .GroupBy(h => h.Name)
                .Select(g => new
                {
                    g.Key,
                    Total = g.Sum(h => CosmicQueryMetadata.Value<ulong>(h, "unsignedNumeric")),
                    Mean = g.Average(h => CosmicQueryMetadata.Value<ulong>(h, "unsignedNumeric"))
                })
                .Where(g => g.Total >= ulong.MaxValue - 1 && g.Mean > long.MaxValue)
                .OrderByDescending(g => g.Mean).ThenBy(g => g.Key).Skip(1).Take(1).Result;

            var result = await fixture.Provider.ExecuteCosmicGroupedAsync(GroupedPlan(query));
            Assert.False(result.IsError, result.Message + result.Exception);
            var row = Assert.Single(result.Result.Where(row => row.IsResult));
            Assert.Equal(ulong.MaxValue - 1, Assert.IsType<ulong>(row.Values[1]));
            Assert.Equal((decimal)(ulong.MaxValue - 1), Assert.IsType<decimal>(row.Values[2]));
        }, configure: root =>
        {
            root.MetaData["unsignedNumeric"] = ulong.MaxValue;
            root.Children.First().MetaData["unsignedNumeric"] = ulong.MaxValue - 1;
        });
    [Fact]
    public Task NativeUnsignedGroupedOverflowCannotHideBehindEmptyResultPage()
        => WithMongoGraph(async fixture =>
        {
            var ids = new[] { fixture.Root.Id, fixture.Root.Children.First().Id };
            var source = CosmicQuery<Holon>.Create().Where(h => ids.Contains(h.Id))
                .GroupBy(h => CosmicQueryMetadata.Value<string>(h, "unsignedBucket"));

            // The numerator exceeds UInt64, while the mean remains representable.
            var average = source.Select(g => g.Average(
                h => CosmicQueryMetadata.Value<ulong>(h, "unsignedNumeric"))).Result;
            var good = await fixture.Provider.ExecuteCosmicGroupedAsync(GroupedPlan(average));
            Assert.False(good.IsError, good.Message + good.Exception);
            Assert.Equal((decimal)ulong.MaxValue, Assert.Single(good.Result).Values[0]);

            // Every participating group must validate before result paging.
            var sum = source.Select(g => g.Sum(
                h => CosmicQueryMetadata.Value<ulong>(h, "unsignedNumeric"))).Take(0).Result;
            var overflow = await fixture.Provider.ExecuteCosmicGroupedAsync(GroupedPlan(sum));
            Assert.Equal("CQL_SCALAR_OVERFLOW", overflow.ErrorCode);
            Assert.Null(overflow.Result);
        }, configure: root =>
        {
            root.MetaData["unsignedNumeric"] = ulong.MaxValue;
            root.MetaData["unsignedBucket"] = "one group";
            root.Children.First().MetaData["unsignedNumeric"] = ulong.MaxValue;
            root.Children.First().MetaData["unsignedBucket"] = "one group";
        });

    [Fact]
    public Task NativeUnsignedNumericWindowExcludesMalformedLaterInputs()
        => WithMongoGraph(async fixture =>
        {
            var ids = new[] { fixture.Root.Id, fixture.Root.Children.First().Id };
            var source = CosmicQuery<ScalarMustNotConstruct>.Create().Where(h => ids.Contains(h.Id))
                .OrderBy(h => h.Name).Take(1).Result;
            var result = await fixture.Provider.ExecuteCosmicScalarAsync(ScalarPlan(
                source.Sum(h => CosmicQueryMetadata.Value<ulong>(h, "unsignedNumeric")).Result));
            Assert.False(result.IsError, result.Message + result.Exception);
            Assert.Equal(ulong.MaxValue, result.Result);
        }, configure: root =>
        {
            root.MetaData["unsignedNumeric"] = "outside selected window";
            root.Children.First().MetaData["unsignedNumeric"] = ulong.MaxValue;
        });

    [Fact]
    public Task NativeUnsignedNullableGroupedMissingInputPublishesZeroAndNull()
        => WithMongoGraph(async fixture =>
        {
            var id = fixture.Root.Id;
            var query = CosmicQuery<Holon>.Create().Where(h => h.Id == id).GroupBy(h => h.Name)
                .Select(g => new
                {
                    Total = g.Sum(h => CosmicQueryMetadata.Value<ulong?>(h, "missingUnsigned")),
                    Mean = g.Average(h => CosmicQueryMetadata.Value<ulong?>(h, "missingUnsigned"))
                }).Where(g => g.Total == 0UL && g.Mean == null).Result;
            var result = await fixture.Provider.ExecuteCosmicGroupedAsync(GroupedPlan(query));
            Assert.False(result.IsError, result.Message + result.Exception);
            var row = Assert.Single(result.Result.Where(row => row.IsResult));
            Assert.Equal(0UL, row.Values[0]);
            Assert.Null(row.Values[1]);
        });

}
