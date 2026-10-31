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
    public Task NativeDoubleScalarPreservesFiniteBoundaries(int sample)
    {
        var value = new double[] { 0d, double.Epsilon, -double.Epsilon, double.MaxValue, double.MinValue, 0.1d }[sample];
        return WithDecimalNumericRows(new object[] { value }, async (fixture, ids) =>
        {
            var source = CosmicQuery<ScalarMustNotConstruct>.Create().Where(h => ids.Contains(h.Id)).Result;
            var sum = await fixture.Provider.ExecuteCosmicScalarAsync(ScalarPlan(source.Sum(h => CosmicQueryMetadata.Value<double>(h, "decimalNumeric")).Result));
            Assert.IsFalse(sum.IsError, sum.Message + sum.Exception); Assert.AreEqual(value, sum.Result);
            var mean = await fixture.Provider.ExecuteCosmicScalarAsync(ScalarPlan(source.Average(h => CosmicQueryMetadata.Value<double>(h, "decimalNumeric")).Result));
            Assert.IsFalse(mean.IsError, mean.Message + mean.Exception); Assert.AreEqual(value, mean.Result);
        });
    }

    [TestMethod]
    public Task NativeDoubleScalarExactCancellationRetainsSubnormal()
        => WithDecimalNumericRows(new object[] { double.MaxValue, double.MaxValue, double.MinValue, double.MinValue, double.Epsilon }, async (fixture, ids) =>
        {
            var source = CosmicQuery<Holon>.Create().Where(h => ids.Contains(h.Id)).Result;
            var sum = await fixture.Provider.ExecuteCosmicScalarAsync(ScalarPlan(source.Sum(h => CosmicQueryMetadata.Value<double>(h, "decimalNumeric")).Result));
            Assert.IsFalse(sum.IsError, sum.Message + sum.Exception); Assert.AreEqual(double.Epsilon, sum.Result);
            var mean = await fixture.Provider.ExecuteCosmicScalarAsync(ScalarPlan(source.Average(h => CosmicQueryMetadata.Value<double>(h, "decimalNumeric")).Result));
            Assert.IsFalse(mean.IsError, mean.Message + mean.Exception); Assert.AreEqual(0d, mean.Result);
        });

    [TestMethod]
    public Task NativeDoubleScalarOverflowStillAllowsFiniteMean()
        => WithDecimalNumericRows(new object[] { double.MaxValue, double.MaxValue }, async (fixture, ids) =>
        {
            var source = CosmicQuery<Holon>.Create().Where(h => ids.Contains(h.Id)).Result;
            var sum = await fixture.Provider.ExecuteCosmicScalarAsync(ScalarPlan(source.Sum(h => CosmicQueryMetadata.Value<double>(h, "decimalNumeric")).Result));
            Assert.AreEqual("CQL_SCALAR_OVERFLOW", sum.ErrorCode);
            var mean = await fixture.Provider.ExecuteCosmicScalarAsync(ScalarPlan(source.Average(h => CosmicQueryMetadata.Value<double>(h, "decimalNumeric")).Result));
            Assert.IsFalse(mean.IsError, mean.Message + mean.Exception); Assert.AreEqual(double.MaxValue, mean.Result);
        });

    [TestMethod]
    public Task NativeDoubleScalarNullableAndBothEmptyPathsRetainTypes()
        => WithDecimalNumericRows(new object[] { null!, 1.25d }, async (fixture, ids) =>
        {
            var source = CosmicQuery<Holon>.Create().Where(h => ids.Contains(h.Id)).Result;
            var mean = await fixture.Provider.ExecuteCosmicScalarAsync(ScalarPlan(source.Average(h => CosmicQueryMetadata.Value<double?>(h, "decimalNumeric")).Result));
            Assert.IsFalse(mean.IsError, mean.Message + mean.Exception); Assert.AreEqual((double?)1.25d, mean.Result);
            foreach (var empty in new[] { source.Take(0).Result, source.Where(h => h.Id == Guid.Empty).Result })
            {
                var sum = await fixture.Provider.ExecuteCosmicScalarAsync(ScalarPlan(empty.Sum(h => CosmicQueryMetadata.Value<double?>(h, "decimalNumeric")).Result));
                Assert.IsFalse(sum.IsError, sum.Message + sum.Exception); Assert.AreEqual((double?)0d, sum.Result);
                var absent = await fixture.Provider.ExecuteCosmicScalarAsync(ScalarPlan(empty.Average(h => CosmicQueryMetadata.Value<double?>(h, "decimalNumeric")).Result));
                Assert.IsFalse(absent.IsError, absent.Message + absent.Exception); Assert.IsNull(absent.Result);
                var required = await fixture.Provider.ExecuteCosmicScalarAsync(ScalarPlan(empty.Average(h => CosmicQueryMetadata.Value<double>(h, "decimalNumeric")).Result));
                Assert.AreEqual("CQL_EMPTY_SEQUENCE", required.ErrorCode);
            }
        });

    [DataTestMethod]
    [DataRow("1.25")] [DataRow("invalid")] [DataRow(true)] [DataRow(null)]
    public Task NativeDoubleScalarRejectsMalformedOriginalValues(object value)
        => WithDecimalNumericRows(new[] { value }, async (fixture, ids) =>
        {
            var source = CosmicQuery<Holon>.Create().Where(h => ids.Contains(h.Id)).Result;
            var result = await fixture.Provider.ExecuteCosmicScalarAsync(ScalarPlan(source.Sum(h => CosmicQueryMetadata.Value<double>(h, "decimalNumeric")).Result));
            Assert.AreEqual("CQL_DATA_INVALID", result.ErrorCode); Assert.IsFalse(result.IsLoaded);
        });

    [TestMethod]
    public Task NativeDoubleScalarSourceWindowExcludesMalformedLaterValues()
        => WithDecimalNumericRows(new object[] { 1.25d, "outside window" }, async (fixture, ids) =>
        {
            var source = CosmicQuery<Holon>.Create().Where(h => ids.Contains(h.Id)).OrderBy(h => h.Name).Take(1).Result;
            var sum = await fixture.Provider.ExecuteCosmicScalarAsync(ScalarPlan(source.Sum(h => CosmicQueryMetadata.Value<double>(h, "decimalNumeric")).Result));
            Assert.IsFalse(sum.IsError, sum.Message + sum.Exception); Assert.AreEqual(1.25d, sum.Result);
        });

    [TestMethod]
    public Task NativeDoubleScalarAverageRoundsHalfwayToEven()
        => WithDecimalNumericRows(new object[] { double.Epsilon, 0d }, async (fixture, ids) =>
        {
            var source = CosmicQuery<Holon>.Create().Where(h => ids.Contains(h.Id)).Result;
            var mean = await fixture.Provider.ExecuteCosmicScalarAsync(ScalarPlan(source.Average(h => CosmicQueryMetadata.Value<double>(h, "decimalNumeric")).Result));
            Assert.IsFalse(mean.IsError, mean.Message + mean.Exception); Assert.AreEqual(0d, mean.Result);
        });

    [TestMethod]
    public Task NativeDoubleScalarPrivacyExcludesMalformedHiddenInputs()
    {
        var actor = Guid.NewGuid(); var foreign = Guid.NewGuid();
        return WithNativeGraph(async fixture =>
        {
            var ids = new[] { fixture.Root }.Concat(fixture.Root.Children).Select(row => row.Id).ToArray();
            var source = CosmicQuery<Holon>.Create().WithReadAccess(CosmicQueryReadAccess.OwnerOrPublic(actor).Result)
                .Where(h => ids.Contains(h.Id)).Result;
            var sum = await fixture.Provider.ExecuteCosmicScalarAsync(ScalarPlan(source.Sum(h => CosmicQueryMetadata.Value<double>(h, "floatingNumeric")).Result));
            Assert.IsFalse(sum.IsError, sum.Message + sum.Exception); Assert.AreEqual(3.75d, sum.Result);
        }, manifested: false, configure: root =>
        {
            var rows = new[] { root }.Concat(root.Children).ToArray();
            for (var index = 0; index < rows.Length; index++)
            {
                rows[index].CreatedByAvatarId = index == 0 ? actor : foreign;
                rows[index].IsPublic = index == 1;
                rows[index].MetaData["floatingNumeric"] = index == 0 ? (object)1.25d : index == 1 ? 2.5d : "hidden malformed";
            }
        });
    }

    [TestMethod]
    public Task NativeDoubleScalarWorkIsIndependentOfGraphPublicationLimit()
        => WithDecimalNumericRows(Enumerable.Repeat((object)1d, 137).ToArray(), async (fixture, ids) =>
        {
            var graph = CosmicQueryGraphOptions.Create(CosmicQueryGraphConsistency.ProviderDefault, maximumNodes: 1).Result;
            var source = CosmicQuery<Holon>.Create().WithChildren(graph)
                .Where(h => h.Name.StartsWith("owned CQL graph decimal", StringComparison.Ordinal)).Result;
            var sum = await fixture.Provider.ExecuteCosmicScalarAsync(ScalarPlan(source.Sum(h => CosmicQueryMetadata.Value<double>(h, "decimalNumeric")).Result));
            Assert.IsFalse(sum.IsError, sum.Message + sum.Exception); Assert.AreEqual(137d, sum.Result);
        });

    [TestMethod]
    public Task NativeDoubleScalarCancellationLeavesOwnerReusable()
        => WithDecimalNumericRows(new object[] { 1.25d }, async (fixture, ids) =>
        {
            var source = CosmicQuery<Holon>.Create().Where(h => ids.Contains(h.Id)).Result;
            var plan = ScalarPlan(source.Sum(h => CosmicQueryMetadata.Value<double>(h, "decimalNumeric")).Result);
            using var cancellation = new System.Threading.CancellationTokenSource(); cancellation.Cancel();
            var cancelled = await fixture.Provider.ExecuteCosmicScalarAsync(plan, cancellation.Token);
            Assert.AreEqual("CQL_QUERY_CANCELLED", cancelled.ErrorCode);
            var fresh = await fixture.Provider.ExecuteCosmicScalarAsync(plan);
            Assert.IsFalse(fresh.IsError, fresh.Message + fresh.Exception); Assert.AreEqual(1.25d, fresh.Result);
        });
}
