using System;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public sealed partial class Neo4jNativeCqlTests
{
    [DataTestMethod]
    [DataRow(0UL)]
    [DataRow(1UL)]
    [DataRow(4294967295UL)]
    [DataRow(4294967296UL)]
    [DataRow(9007199254740993UL)]
    [DataRow(9223372036854775808UL)]
    [DataRow(ulong.MaxValue)]
    public Task NativeUnsignedNumericRetainsExactBoundary(ulong value)
        => WithNativeGraph(async fixture =>
        {
            // A scalar reduction must never construct a selected holon.
            var id = fixture.Root.Id;
            var source = CosmicQuery<ScalarMustNotConstruct>.Create().Where(h => h.Id == id).Result;

            var sum = await fixture.Provider.ExecuteCosmicScalarAsync(ScalarPlan(
                source.Sum(h => CosmicQueryMetadata.Value<ulong>(h, "unsignedNumeric")).Result));
            Assert.IsFalse(sum.IsError, sum.Message + sum.Exception);
            Assert.AreEqual(value, sum.Result);

            var mean = await fixture.Provider.ExecuteCosmicScalarAsync(ScalarPlan(
                source.Average(h => CosmicQueryMetadata.Value<ulong>(h, "unsignedNumeric")).Result));
            Assert.IsFalse(mean.IsError, mean.Message + mean.Exception);
            Assert.AreEqual((decimal)value, mean.Result);
        }, configure: root => root.MetaData["unsignedNumeric"] = value);

    [TestMethod]
    public Task NativeUnsignedNumericOverflowDoesNotInvalidateRepresentableMean()
        => WithNativeGraph(async fixture =>
        {
            var ids = new[] { fixture.Root.Id, fixture.Root.Children.First().Id };
            var source = CosmicQuery<Holon>.Create().Where(h => ids.Contains(h.Id)).Result;

            var sum = await fixture.Provider.ExecuteCosmicScalarAsync(ScalarPlan(
                source.Sum(h => CosmicQueryMetadata.Value<ulong>(h, "unsignedNumeric")).Result));
            Assert.AreEqual("CQL_SCALAR_OVERFLOW", sum.ErrorCode);
            Assert.IsFalse(sum.IsLoaded);

            var mean = await fixture.Provider.ExecuteCosmicScalarAsync(ScalarPlan(
                source.Average(h => CosmicQueryMetadata.Value<ulong>(h, "unsignedNumeric")).Result));
            Assert.IsFalse(mean.IsError, mean.Message + mean.Exception);
            Assert.AreEqual((decimal)ulong.MaxValue, mean.Result);
        }, configure: root =>
        {
            root.MetaData["unsignedNumeric"] = ulong.MaxValue;
            root.Children.First().MetaData["unsignedNumeric"] = ulong.MaxValue;
        });

    [TestMethod]
    public Task NativeUnsignedNumericNullableMissingInputAndEmptyWindowKeepTheirContracts()
        => WithNativeGraph(async fixture =>
        {
            var ids = new[] { fixture.Root.Id, fixture.Root.Children.First().Id };
            var source = CosmicQuery<Holon>.Create().Where(h => ids.Contains(h.Id)).Result;
            var mean = await fixture.Provider.ExecuteCosmicScalarAsync(ScalarPlan(
                source.Average(h => CosmicQueryMetadata.Value<ulong?>(h, "unsignedNumeric")).Result));
            Assert.IsFalse(mean.IsError, mean.Message + mean.Exception);
            Assert.AreEqual((decimal?)ulong.MaxValue, mean.Result);

            var empty = source.Take(0).Result;
            var sum = await fixture.Provider.ExecuteCosmicScalarAsync(ScalarPlan(
                empty.Sum(h => CosmicQueryMetadata.Value<ulong?>(h, "unsignedNumeric")).Result));
            Assert.IsFalse(sum.IsError, sum.Message + sum.Exception);
            Assert.AreEqual((ulong?)0, sum.Result);

            var missingMean = await fixture.Provider.ExecuteCosmicScalarAsync(ScalarPlan(
                empty.Average(h => CosmicQueryMetadata.Value<ulong?>(h, "unsignedNumeric")).Result));
            Assert.IsFalse(missingMean.IsError, missingMean.Message + missingMean.Exception);
            Assert.IsNull(missingMean.Result);
        }, configure: root => root.MetaData["unsignedNumeric"] = ulong.MaxValue);

    [DataTestMethod]
    [DataRow(-1L)]
    [DataRow(1.5)]
    [DataRow("18446744073709551615")]
    [DataRow("not numeric")]
    public Task NativeUnsignedNumericRejectsInvalidParticipatingInput(object value)
        => WithNativeGraph(async fixture =>
        {
            var id = fixture.Root.Id;
            var source = CosmicQuery<Holon>.Create().Where(h => h.Id == id).Result;
            var result = await fixture.Provider.ExecuteCosmicScalarAsync(ScalarPlan(
                source.Sum(h => CosmicQueryMetadata.Value<ulong>(h, "unsignedNumeric")).Result));
            Assert.AreEqual("CQL_DATA_INVALID", result.ErrorCode);
            Assert.IsFalse(result.IsLoaded);
        }, configure: root => root.MetaData["unsignedNumeric"] = value);

    [TestMethod]
    public Task NativeUnsignedGroupedFilterOrderAndPagingRetainExactMeans()
        => WithNativeGraph(async fixture =>
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
            Assert.IsFalse(result.IsError, result.Message + result.Exception);
            var row = result.Result.Where(row => row.IsResult).Single();
            Assert.AreEqual(ulong.MaxValue - 1, (ulong)row.Values[1]);
            Assert.AreEqual((decimal)(ulong.MaxValue - 1), (decimal)row.Values[2]);
        }, configure: root =>
        {
            root.MetaData["unsignedNumeric"] = ulong.MaxValue;
            root.Children.First().MetaData["unsignedNumeric"] = ulong.MaxValue - 1;
        });
    [TestMethod]
    public Task NativeUnsignedGroupedOverflowCannotHideBehindEmptyResultPage()
        => WithNativeGraph(async fixture =>
        {
            var ids = new[] { fixture.Root.Id, fixture.Root.Children.First().Id };
            var source = CosmicQuery<Holon>.Create().Where(h => ids.Contains(h.Id))
                .GroupBy(h => CosmicQueryMetadata.Value<string>(h, "unsignedBucket"));

            // The numerator exceeds UInt64, while the mean remains representable.
            var average = source.Select(g => g.Average(
                h => CosmicQueryMetadata.Value<ulong>(h, "unsignedNumeric"))).Result;
            var good = await fixture.Provider.ExecuteCosmicGroupedAsync(GroupedPlan(average));
            Assert.IsFalse(good.IsError, good.Message + good.Exception);
            Assert.AreEqual((decimal)ulong.MaxValue, good.Result.Single().Values[0]);

            // Every participating group must validate before result paging.
            var sum = source.Select(g => g.Sum(
                h => CosmicQueryMetadata.Value<ulong>(h, "unsignedNumeric"))).Take(0).Result;
            var overflow = await fixture.Provider.ExecuteCosmicGroupedAsync(GroupedPlan(sum));
            Assert.AreEqual("CQL_SCALAR_OVERFLOW", overflow.ErrorCode);
            Assert.IsNull(overflow.Result);
        }, configure: root =>
        {
            root.MetaData["unsignedNumeric"] = ulong.MaxValue;
            root.MetaData["unsignedBucket"] = "one group";
            root.Children.First().MetaData["unsignedNumeric"] = ulong.MaxValue;
            root.Children.First().MetaData["unsignedBucket"] = "one group";
        });

    [TestMethod]
    public Task NativeUnsignedNumericWindowExcludesMalformedLaterInputs()
        => WithNativeGraph(async fixture =>
        {
            var ids = new[] { fixture.Root.Id, fixture.Root.Children.First().Id };
            var source = CosmicQuery<ScalarMustNotConstruct>.Create().Where(h => ids.Contains(h.Id))
                .OrderBy(h => h.Name).Take(1).Result;
            var result = await fixture.Provider.ExecuteCosmicScalarAsync(ScalarPlan(
                source.Sum(h => CosmicQueryMetadata.Value<ulong>(h, "unsignedNumeric")).Result));
            Assert.IsFalse(result.IsError, result.Message + result.Exception);
            Assert.AreEqual(ulong.MaxValue, result.Result);
        }, configure: root =>
        {
            root.MetaData["unsignedNumeric"] = "outside selected window";
            root.Children.First().MetaData["unsignedNumeric"] = ulong.MaxValue;
        });

    [TestMethod]
    public Task NativeUnsignedNullableGroupedMissingInputPublishesZeroAndNull()
        => WithNativeGraph(async fixture =>
        {
            var id = fixture.Root.Id;
            var query = CosmicQuery<Holon>.Create().Where(h => h.Id == id).GroupBy(h => h.Name)
                .Select(g => new
                {
                    Total = g.Sum(h => CosmicQueryMetadata.Value<ulong?>(h, "missingUnsigned")),
                    Mean = g.Average(h => CosmicQueryMetadata.Value<ulong?>(h, "missingUnsigned"))
                }).Where(g => g.Total == 0UL && g.Mean == null).Result;
            var result = await fixture.Provider.ExecuteCosmicGroupedAsync(GroupedPlan(query));
            Assert.IsFalse(result.IsError, result.Message + result.Exception);
            var row = result.Result.Where(row => row.IsResult).Single();
            Assert.AreEqual(0UL, row.Values[0]);
            Assert.IsNull(row.Values[1]);
        });

    [TestMethod]
    public Task NativeUnsignedGroupedSumCanCompareWithItsExactUnsignedKey()
        => WithNativeGraph(async fixture =>
        {
            var id = fixture.Root.Id;
            var query = CosmicQuery<Holon>.Create().Where(h => h.Id == id)
                .GroupBy(h => CosmicQueryMetadata.Value<ulong>(h, "unsignedNumeric"))
                .Select(g => new
                {
                    g.Key,
                    Total = g.Sum(h => CosmicQueryMetadata.Value<ulong>(h, "unsignedNumeric"))
                }).Where(g => g.Total == g.Key).Result;
            var result = await fixture.Provider.ExecuteCosmicGroupedAsync(GroupedPlan(query));
            Assert.IsFalse(result.IsError, result.Message + result.Exception);
            var row = result.Result.Where(row => row.IsResult).Single();
            Assert.AreEqual(ulong.MaxValue, row.Values[0]);
            Assert.AreEqual(ulong.MaxValue, row.Values[1]);
        }, configure: root => root.MetaData["unsignedNumeric"] = ulong.MaxValue);

    [TestMethod]
    public Task NativeUnsignedGroupedFractionalMeanFiltersAndOrdersWithoutRounding()
        => WithNativeGraph(async fixture =>
        {
            var ids = new[] { fixture.Root.Id, fixture.Root.Children.First().Id };
            var query = CosmicQuery<Holon>.Create().Where(h => ids.Contains(h.Id))
                .GroupBy(h => CosmicQueryMetadata.Value<string>(h, "unsignedBucket"))
                .Select(g => new
                {
                    Mean = g.Average(h => CosmicQueryMetadata.Value<ulong>(h, "unsignedNumeric"))
                }).Where(g => g.Mean == 18446744073709551614.5m)
                .OrderBy(g => g.Mean).Take(1).Result;
            var result = await fixture.Provider.ExecuteCosmicGroupedAsync(GroupedPlan(query));
            Assert.IsFalse(result.IsError, result.Message + result.Exception);
            var row = result.Result.Where(row => row.IsResult).Single();
            Assert.AreEqual(18446744073709551614.5m, row.Values[0]);
        }, configure: root =>
        {
            root.MetaData["unsignedNumeric"] = ulong.MaxValue;
            root.MetaData["unsignedBucket"] = "one group";
            root.Children.First().MetaData["unsignedNumeric"] = ulong.MaxValue - 1;
            root.Children.First().MetaData["unsignedBucket"] = "one group";
        });

}
