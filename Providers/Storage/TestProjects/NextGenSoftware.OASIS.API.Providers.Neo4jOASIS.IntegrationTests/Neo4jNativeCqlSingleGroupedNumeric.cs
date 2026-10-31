using System;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public partial class Neo4jNativeCqlTests
{
    [DataTestMethod]
    [DataRow(0)] [DataRow(1)] [DataRow(2)] [DataRow(3)] [DataRow(4)] [DataRow(5)]
    public Task NativeSingleGroupedBoundaryMatchesItsExactKey(int sample)
    {
        var value = new float[] { 0f, float.Epsilon, -float.Epsilon, float.MaxValue, float.MinValue, 0.1f }[sample];
        return WithDecimalNumericRows(new object[] { value }, async (fixture, ids) =>
        {
            var query = CosmicQuery<ScalarMustNotConstruct>.Create().Where(h => ids.Contains(h.Id))
                .GroupBy(h => CosmicQueryMetadata.Value<float>(h, "decimalNumeric"))
                .Select(g => new { g.Key, Total = g.Sum(h => CosmicQueryMetadata.Value<float>(h, "decimalNumeric")) })
                .Where(g => g.Total == g.Key).OrderBy(g => g.Total).Result;
            var result = await GraphManager(fixture.Provider).ExecuteGroupedQueryAsync(GroupedPlan(query), new[] { fixture.Provider });
            Assert.IsFalse(result.IsError, result.Message + result.Exception);
            Assert.AreEqual(value, result.Result.Providers.Single().Rows.Single().Total);
        });
    }

    [TestMethod]
    public Task NativeSingleGroupedRoundedMeansFilterOrderAndPage()
        => WithDecimalNumericRows(new object[] { float.Epsilon, 2f * float.Epsilon, 1f, 2f }, async (fixture, ids) =>
        {
            var query = CosmicQuery<Holon>.Create().Where(h => ids.Contains(h.Id))
                .GroupBy(h => CosmicQueryMetadata.Value<string>(h, "decimalGroup"))
                .Select(g => new { g.Key, Total = g.Sum(h => CosmicQueryMetadata.Value<float>(h, "decimalNumeric")),
                    Mean = g.Average(h => CosmicQueryMetadata.Value<float>(h, "decimalNumeric")) })
                .Where(g => g.Total >= 3f * float.Epsilon && g.Mean >= 2f * float.Epsilon)
                .OrderByDescending(g => g.Mean).ThenBy(g => g.Key).Skip(1).Take(1).Result;
            var result = await GraphManager(fixture.Provider).ExecuteGroupedQueryAsync(GroupedPlan(query), new[] { fixture.Provider });
            Assert.IsFalse(result.IsError, result.Message + result.Exception);
            var row = result.Result.Providers.Single().Rows.Single();
            Assert.AreEqual("tiny", row.Key); Assert.AreEqual(3f * float.Epsilon, row.Total); Assert.AreEqual(2f * float.Epsilon, row.Mean);
        }, groups: new[] { "tiny", "tiny", "whole", "whole" });

    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public Task NativeSingleGroupedOverflowCannotHideOffPage(bool filtered)
        => WithDecimalNumericRows(new object[] { float.MaxValue, float.MaxValue }, async (fixture, ids) =>
        {
            var selected = CosmicQuery<Holon>.Create().Where(h => ids.Contains(h.Id))
                .GroupBy(h => CosmicQueryMetadata.Value<string>(h, "decimalGroup"))
                .Select(g => new { Total = g.Sum(h => CosmicQueryMetadata.Value<float>(h, "decimalNumeric")) });
            var query = filtered ? selected.Where(g => g.Total == 0f).Result : selected.Take(0).Result;
            var result = await GraphManager(fixture.Provider).ExecuteGroupedQueryAsync(GroupedPlan(query), new[] { fixture.Provider });
            Assert.AreEqual("CQL_SCALAR_OVERFLOW", result.ErrorCode); Assert.IsNull(result.Result);
        });

    [TestMethod]
    public Task NativeSingleGroupedNullableZeroAndNullRetainDistinctComparisons()
        => WithDecimalNumericRows(new object[] { null!, float.MaxValue }, async (fixture, ids) =>
        {
            var query = CosmicQuery<Holon>.Create().Where(h => ids.Contains(h.Id))
                .GroupBy(h => CosmicQueryMetadata.Value<string>(h, "decimalGroup"))
                .Select(g => new { g.Key, Total = g.Sum(h => CosmicQueryMetadata.Value<float?>(h, "decimalNumeric")),
                    Mean = g.Average(h => CosmicQueryMetadata.Value<float?>(h, "decimalNumeric")) })
                .Where(g => g.Total == 0f && g.Mean == null).OrderBy(g => g.Mean).Result;
            var result = await GraphManager(fixture.Provider).ExecuteGroupedQueryAsync(GroupedPlan(query), new[] { fixture.Provider });
            Assert.IsFalse(result.IsError, result.Message + result.Exception);
            var row = result.Result.Providers.Single().Rows.Single();
            Assert.AreEqual("empty", row.Key); Assert.AreEqual((float?)0f, row.Total); Assert.IsNull(row.Mean);
        }, groups: new[] { "empty", "full" });

    [TestMethod]
    public Task NativeSingleGroupedMixedPacketsKeepTheirOwnDomains()
        => WithDecimalNumericRows(new object[] { 1.25m, 1.25m }, async (fixture, ids) =>
        {
            var query = CosmicQuery<Holon>.Create().Where(h => ids.Contains(h.Id))
                .GroupBy(h => CosmicQueryMetadata.Value<string>(h, "decimalGroup"))
                .Select(g => new { Integer = g.Sum(h => CosmicQueryMetadata.Value<long>(h, "stock")),
                    Decimal = g.Sum(h => CosmicQueryMetadata.Value<decimal>(h, "decimalNumeric")),
                    Total = g.Sum(h => CosmicQueryMetadata.Value<float>(h, "decimalNumeric")),
                    Mean = g.Average(h => CosmicQueryMetadata.Value<float>(h, "decimalNumeric")),
                    Minimum = g.Min(h => CosmicQueryMetadata.Value<float>(h, "decimalNumeric")) }).Result;
            var result = await GraphManager(fixture.Provider).ExecuteGroupedQueryAsync(GroupedPlan(query), new[] { fixture.Provider });
            Assert.IsFalse(result.IsError, result.Message + result.Exception);
            var row = result.Result.Providers.Single().Rows.Single();
            Assert.AreEqual(14L, row.Integer); Assert.AreEqual(2.5m, row.Decimal); Assert.AreEqual(2.5f, row.Total);
            Assert.AreEqual(1.25f, row.Mean); Assert.AreEqual(1.25f, row.Minimum);
        });

    [TestMethod]
    public Task NativeSingleGroupedSourceWindowExcludesMalformedLaterRows()
        => WithDecimalNumericRows(new object[] { 1.25f, "outside window" }, async (fixture, ids) =>
        {
            var query = CosmicQuery<Holon>.Create().Where(h => ids.Contains(h.Id)).OrderBy(h => h.Name).Take(1)
                .GroupBy(h => CosmicQueryMetadata.Value<string>(h, "decimalGroup"))
                .Select(g => g.Sum(h => CosmicQueryMetadata.Value<float>(h, "decimalNumeric"))).Result;
            var result = await GraphManager(fixture.Provider).ExecuteGroupedQueryAsync(GroupedPlan(query), new[] { fixture.Provider });
            Assert.IsFalse(result.IsError, result.Message + result.Exception); Assert.AreEqual(1.25f, result.Result.Providers.Single().Rows.Single());
        });

    [TestMethod]
    public Task NativeSingleGroupedExactCancellationPreservesSubnormal()
        => WithDecimalNumericRows(new object[] { float.MaxValue, float.MaxValue, float.MinValue, float.MinValue, float.Epsilon }, async (fixture, ids) =>
        {
            var query = CosmicQuery<Holon>.Create().Where(h => ids.Contains(h.Id))
                .GroupBy(h => CosmicQueryMetadata.Value<string>(h, "decimalGroup"))
                .Select(g => new { Total = g.Sum(h => CosmicQueryMetadata.Value<float>(h, "decimalNumeric")) })
                .Where(g => g.Total == float.Epsilon).OrderBy(g => g.Total).Result;
            var result = await GraphManager(fixture.Provider).ExecuteGroupedQueryAsync(GroupedPlan(query), new[] { fixture.Provider });
            Assert.IsFalse(result.IsError, result.Message + result.Exception); Assert.AreEqual(float.Epsilon, result.Result.Providers.Single().Rows.Single().Total);
        });

    [TestMethod]
    public Task NativeSingleGroupedHalfwayMeanMatchesSelectedPredicate()
        => WithDecimalNumericRows(new object[] { float.Epsilon, 0f }, async (fixture, ids) =>
        {
            var query = CosmicQuery<Holon>.Create().Where(h => ids.Contains(h.Id))
                .GroupBy(h => CosmicQueryMetadata.Value<string>(h, "decimalGroup"))
                .Select(g => new { Mean = g.Average(h => CosmicQueryMetadata.Value<float>(h, "decimalNumeric")) })
                .Where(g => g.Mean == 0f).Result;
            var result = await GraphManager(fixture.Provider).ExecuteGroupedQueryAsync(GroupedPlan(query), new[] { fixture.Provider });
            Assert.IsFalse(result.IsError, result.Message + result.Exception); Assert.AreEqual(0f, result.Result.Providers.Single().Rows.Single().Mean);
        });

    [TestMethod]
    public Task NativeSingleGroupedCancellationLeavesOwnerReusable()
        => WithDecimalNumericRows(new object[] { 1.25f }, async (fixture, ids) =>
        {
            var query = CosmicQuery<Holon>.Create().Where(h => ids.Contains(h.Id))
                .GroupBy(h => CosmicQueryMetadata.Value<string>(h, "decimalGroup"))
                .Select(g => g.Sum(h => CosmicQueryMetadata.Value<float>(h, "decimalNumeric"))).Result;
            var plan = GroupedPlan(query);
            using var cancellation = new System.Threading.CancellationTokenSource(); cancellation.Cancel();
            var cancelled = await fixture.Provider.ExecuteCosmicGroupedAsync(plan, cancellationToken: cancellation.Token);
            Assert.AreEqual("CQL_QUERY_CANCELLED", cancelled.ErrorCode);
            var fresh = await fixture.Provider.ExecuteCosmicGroupedAsync(plan);
            Assert.IsFalse(fresh.IsError, fresh.Message + fresh.Exception);
        });

    [TestMethod]
    public Task NativeSingleGroupedPrivacyExcludesMalformedHiddenInputs()
    {
        var actor = Guid.NewGuid(); var foreign = Guid.NewGuid();
        return WithNativeGraph(async fixture =>
        {
            var ids = new[] { fixture.Root }.Concat(fixture.Root.Children).Select(row => row.Id).ToArray();
            var query = CosmicQuery<Holon>.Create().WithReadAccess(CosmicQueryReadAccess.OwnerOrPublic(actor).Result)
                .Where(h => ids.Contains(h.Id)).GroupBy(h => CosmicQueryMetadata.Value<string>(h, "floatingGroup"))
                .Select(g => new { Count = g.Count(), Mean = g.Average(h => CosmicQueryMetadata.Value<float>(h, "floatingNumeric")) }).Result;
            var result = await GraphManager(fixture.Provider).ExecuteGroupedQueryAsync(GroupedPlan(query), new[] { fixture.Provider });
            Assert.IsFalse(result.IsError, result.Message + result.Exception);
            var row = result.Result.Providers.Single().Rows.Single(); Assert.AreEqual(2, row.Count); Assert.AreEqual(1.875f, row.Mean);
        }, manifested: false, configure: root =>
        {
            var rows = new[] { root }.Concat(root.Children).ToArray();
            for (var index = 0; index < rows.Length; index++)
            {
                rows[index].CreatedByAvatarId = index == 0 ? actor : foreign;
                rows[index].IsPublic = index == 1;
                rows[index].MetaData["floatingGroup"] = "one";
                rows[index].MetaData["floatingNumeric"] = index == 0 ? (object)1.25f : index == 1 ? 2.5f : "hidden malformed";
            }
        });
    }
}
