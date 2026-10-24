using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Provider = NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Neo4jOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public partial class Neo4jNativeCqlTests
{
    [TestMethod]
    public Task NativeGroupedAverageFiltersPublishedRepeatingFraction()
        => WithNativeGraph(async fixture =>
        {
            var query = GroupFilterSource().GroupBy(h => CosmicQueryMetadata.Value<string>(h, "category"))
                .Select(g => new { g.Key, Mean = g.Average(h => CosmicQueryMetadata.Value<long>(h, "stock")) })
                .Where(row => row.Mean == 1m / 3m).OrderBy(row => row.Mean).Take(1).Result;
            var row = FilterSingle(await RunGroupedFilter(fixture, query));
            Assert.AreEqual("third", row.Key); Assert.AreEqual(1m / 3m, row.Mean);
        }, configure: root => SetGroupedFilterRows(root, ("third", 0L), ("third", 0L), ("third", 1L), ("negative", -1L)));

    [TestMethod]
    public Task NativeGroupedAverageOrdersExactInt64MeansAndMatchingPage()
        => WithNativeGraph(async fixture =>
        {
            var query = GroupFilterSource().GroupBy(h => CosmicQueryMetadata.Value<string>(h, "category"))
                .Select(g => new { g.Key, Mean = g.Average(h => CosmicQueryMetadata.Value<long>(h, "stock")) })
                .Where(row => row.Mean > decimal.MinValue && row.Mean < decimal.MaxValue)
                .OrderByDescending(row => row.Mean).Skip(1).Take(1).Result;
            var row = FilterSingle(await RunGroupedFilter(fixture, query));
            Assert.AreEqual("half", row.Key); Assert.AreEqual((decimal)long.MaxValue - 0.5m, row.Mean);
        }, configure: root => SetGroupedFilterRows(root, ("half", long.MaxValue), ("half", long.MaxValue - 1),
            ("maximum", long.MaxValue), ("minimum", long.MinValue)));

    [TestMethod]
    public Task NativeGroupedNullableAverageMembershipAndFieldComparisons()
        => WithNativeGraph(async fixture =>
        {
            var selected = GroupFilterSource().GroupBy(h => CosmicQueryMetadata.Value<string>(h, "category"))
                .Select(g => new { g.Key, Mean = g.Average(h => CosmicQueryMetadata.Value<long?>(h, "stock")),
                    OtherMean = g.Average(h => CosmicQueryMetadata.Value<int?>(h, "stock")) }).Result;
            decimal?[] values = { null, 1.5m };
            var query = selected.Where(row => values.Contains(row.Mean)).OrderBy(row => row.Mean).Result;
            CollectionAssert.AreEqual(new[] { "empty", "half" }, (await RunGroupedFilter(fixture, query)).Select(row => row.Key).ToArray());
            var compared = selected.Where(row => row.Mean == row.OtherMean).Result;
            CollectionAssert.AreEqual(new[] { "empty", "half", "whole" }, (await RunGroupedFilter(fixture, compared)).Select(row => row.Key).ToArray());
        }, configure: root => SetGroupedFilterRows(root, ("empty", null), ("half", 1L), ("half", 2L), ("whole", 4L)));

    [DataTestMethod]
    [DataRow(true, true)] [DataRow(true, false)] [DataRow(false, true)] [DataRow(false, false)]
    public Task NativeGroupedAverageOrdersEveryIntegerInput(bool int32, bool nullable)
        => WithNativeGraph(async fixture =>
        {
            var grouping = GroupFilterSource().GroupBy(h => CosmicQueryMetadata.Value<string>(h, "category")).Result;
            if (nullable)
            {
                var selected = int32 ? grouping.Select(g => g.Average(h => CosmicQueryMetadata.Value<int?>(h, "stock"))).Result
                    : grouping.Select(g => g.Average(h => CosmicQueryMetadata.Value<long?>(h, "stock"))).Result;
                CollectionAssert.AreEqual(new decimal?[] { null, 2m }, (await RunGroupedFilter(fixture, selected.OrderBy(value => value).Result)).ToArray());
            }
            else
            {
                var selected = int32 ? grouping.Select(g => g.Average(h => CosmicQueryMetadata.Value<int>(h, "stock"))).Result
                    : grouping.Select(g => g.Average(h => CosmicQueryMetadata.Value<long>(h, "stock"))).Result;
                CollectionAssert.AreEqual(new[] { 2m, 1m }, (await RunGroupedFilter(fixture, selected.OrderByDescending(value => value).Result)).ToArray());
            }
        }, configure: root => SetGroupedFilterRows(root, ("a", nullable ? null : 1L), ("b", 1L), ("b", 2L), ("b", 3L)));

    [TestMethod]
    public Task NativeGroupedAverageKernelMatchesCoreRoundingAndCoefficientOverflow()
        => WithNativeGraph(async fixture =>
        {
            var samples = new List<Dictionary<string, object>>();
            var expected = new List<string?>();
            void Add(BigInteger total, long count)
            {
                BigInteger high = 0, low = 0;
                if (count != 0)
                {
                    var floor = BigInteger.DivRem(total, count, out var remainder);
                    if (remainder < 0) { floor--; remainder += count; }
                    var next = remainder == 0 ? floor : floor + 1;
                    var limb = CosmicNumericScalarResults.LimbBase;
                    high = floor / limb * (count - remainder) + next / limb * remainder;
                    low = floor % limb * (count - remainder) + next % limb * remainder;
                }
                var reference = CosmicNumericScalarResults.FromParts(CosmicScalarOperation.Average, typeof(long?), (long)high, (long)low, count);
                Assert.IsFalse(reference.IsError, reference.Message);
                var id = expected.Count;
                expected.Add(reference.Result == null ? null : CosmicNumericScalarResults.DecimalOrderKey((decimal)reference.Result));
                samples.Add(new Dictionary<string, object> { { "id", id }, { "h", (long)high }, { "l", (long)low }, { "n", count } });
            }
            Add(0, 0); Add(1, 3); Add(-1, 3); Add(2, 3); Add(-2, 3);
            Add((BigInteger)long.MaxValue * int.MaxValue, int.MaxValue);
            Add((BigInteger)long.MinValue * int.MaxValue, int.MaxValue);
            Add((BigInteger)9000000000000000000L * 1024 + 1, 1024);
            Add((BigInteger)9000000000000000000L * 1024 + 3, 1024);
            Add(-((BigInteger)9000000000000000000L * 1024 + 1), 1024);
            var coefficient = BigInteger.Parse(decimal.MaxValue.ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
            var scale = BigInteger.Pow(10, 10);
            var foundOverflowRound = false;
            for (var delta = 0; delta < 1000 && !foundOverflowRound; delta++)
            {
                var count = int.MaxValue - delta;
                var numerator = ((2 * coefficient + 1) * count + 2 * scale - 1) / (2 * scale);
                if (numerator * scale >= (coefficient + 1) * count) continue;
                Add(numerator, count); Add(-numerator, count);
                foundOverflowRound = true;
            }
            Assert.IsTrue(foundOverflowRound, "Exercise carry beyond Decimal's96-bit coefficient before reducing scale.");
            var random = new Random(20261024);
            for (var index = 0; index < 512; index++)
            {
                var count = random.Next(1, int.MaxValue);
                var center = random.NextInt64(long.MinValue + 1, long.MaxValue);
                Add((BigInteger)center * count + random.NextInt64(-count / 2, count / 2 + 1), count);
            }
            // Pure parameter projection: no additional nodes, writes or fixtures.
            using var session = fixture.Driver.AsyncSession();
            var expression = Provider.GroupedAverageKey("sample.h", "sample.l", "sample.n");
            var cursor = await session.RunAsync("UNWIND $samples AS sample RETURN sample.id AS id," + expression + " AS key ORDER BY id", new { samples });
            var rows = await cursor.ToListAsync();
            Assert.AreEqual(expected.Count, rows.Count);
            for (var index = 0; index < rows.Count; index++)
                Assert.AreEqual(expected[index], rows[index]["key"] as string, "Reference packet " + index);
        });
}
