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
    [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)] [InlineData(4)] [InlineData(5)]
    public Task NativeSingleScalarPreservesFiniteBoundaries(int sample)
    {
        var value = new float[] { 0f, float.Epsilon, -float.Epsilon, float.MaxValue, float.MinValue, 0.1f }[sample];
        return WithDecimalNumericRows(new object[] { value }, async (fixture, ids) =>
        {
            var source = CosmicQuery<ScalarMustNotConstruct>.Create().Where(h => ids.Contains(h.Id)).Result;
            var sum = await fixture.Provider.ExecuteCosmicScalarAsync(ScalarPlan(source.Sum(h => CosmicQueryMetadata.Value<float>(h, "decimalNumeric")).Result));
            Assert.False(sum.IsError, sum.Message + sum.Exception); Assert.Equal(value, sum.Result);
            var mean = await fixture.Provider.ExecuteCosmicScalarAsync(ScalarPlan(source.Average(h => CosmicQueryMetadata.Value<float>(h, "decimalNumeric")).Result));
            Assert.False(mean.IsError, mean.Message + mean.Exception); Assert.Equal(value, mean.Result);
        });
    }

    [Fact]
    public Task NativeSingleScalarExactCancellationRetainsSubnormal()
        => WithDecimalNumericRows(new object[] { float.MaxValue, float.MaxValue, float.MinValue, float.MinValue, float.Epsilon }, async (fixture, ids) =>
        {
            var source = CosmicQuery<Holon>.Create().Where(h => ids.Contains(h.Id)).Result;
            var sum = await fixture.Provider.ExecuteCosmicScalarAsync(ScalarPlan(source.Sum(h => CosmicQueryMetadata.Value<float>(h, "decimalNumeric")).Result));
            Assert.False(sum.IsError, sum.Message + sum.Exception); Assert.Equal(float.Epsilon, sum.Result);
            var mean = await fixture.Provider.ExecuteCosmicScalarAsync(ScalarPlan(source.Average(h => CosmicQueryMetadata.Value<float>(h, "decimalNumeric")).Result));
            Assert.False(mean.IsError, mean.Message + mean.Exception); Assert.Equal(0f, mean.Result);
        });

    [Fact]
    public Task NativeSingleScalarOverflowStillAllowsFiniteMean()
        => WithDecimalNumericRows(new object[] { float.MaxValue, float.MaxValue }, async (fixture, ids) =>
        {
            var source = CosmicQuery<Holon>.Create().Where(h => ids.Contains(h.Id)).Result;
            var sum = await fixture.Provider.ExecuteCosmicScalarAsync(ScalarPlan(source.Sum(h => CosmicQueryMetadata.Value<float>(h, "decimalNumeric")).Result));
            Assert.Equal("CQL_SCALAR_OVERFLOW", sum.ErrorCode);
            var mean = await fixture.Provider.ExecuteCosmicScalarAsync(ScalarPlan(source.Average(h => CosmicQueryMetadata.Value<float>(h, "decimalNumeric")).Result));
            Assert.False(mean.IsError, mean.Message + mean.Exception); Assert.Equal(float.MaxValue, mean.Result);
        });

    [Fact]
    public Task NativeSingleScalarNullableAndBothEmptyPathsRetainTypes()
        => WithDecimalNumericRows(new object[] { null!, 1.25f }, async (fixture, ids) =>
        {
            var source = CosmicQuery<Holon>.Create().Where(h => ids.Contains(h.Id)).Result;
            var mean = await fixture.Provider.ExecuteCosmicScalarAsync(ScalarPlan(source.Average(h => CosmicQueryMetadata.Value<float?>(h, "decimalNumeric")).Result));
            Assert.False(mean.IsError, mean.Message + mean.Exception); Assert.Equal((float?)1.25f, mean.Result);
            foreach (var empty in new[] { source.Take(0).Result, source.Where(h => h.Id == Guid.Empty).Result })
            {
                var sum = await fixture.Provider.ExecuteCosmicScalarAsync(ScalarPlan(empty.Sum(h => CosmicQueryMetadata.Value<float?>(h, "decimalNumeric")).Result));
                Assert.False(sum.IsError, sum.Message + sum.Exception); Assert.Equal((float?)0f, sum.Result);
                var absent = await fixture.Provider.ExecuteCosmicScalarAsync(ScalarPlan(empty.Average(h => CosmicQueryMetadata.Value<float?>(h, "decimalNumeric")).Result));
                Assert.False(absent.IsError, absent.Message + absent.Exception); Assert.Null(absent.Result);
                var required = await fixture.Provider.ExecuteCosmicScalarAsync(ScalarPlan(empty.Average(h => CosmicQueryMetadata.Value<float>(h, "decimalNumeric")).Result));
                Assert.Equal("CQL_EMPTY_SEQUENCE", required.ErrorCode);
            }
        });

    [Theory]
    [InlineData("1.25")] [InlineData("invalid")] [InlineData(true)] [InlineData(null)]
    public Task NativeSingleScalarRejectsMalformedOriginalValues(object value)
        => WithDecimalNumericRows(new[] { value }, async (fixture, ids) =>
        {
            var source = CosmicQuery<Holon>.Create().Where(h => ids.Contains(h.Id)).Result;
            var result = await fixture.Provider.ExecuteCosmicScalarAsync(ScalarPlan(source.Sum(h => CosmicQueryMetadata.Value<float>(h, "decimalNumeric")).Result));
            Assert.Equal("CQL_DATA_INVALID", result.ErrorCode); Assert.False(result.IsLoaded);
        });

    [Fact]
    public Task NativeSingleScalarSourceWindowExcludesMalformedLaterValues()
        => WithDecimalNumericRows(new object[] { 1.25f, "outside window" }, async (fixture, ids) =>
        {
            var source = CosmicQuery<Holon>.Create().Where(h => ids.Contains(h.Id)).OrderBy(h => h.Name).Take(1).Result;
            var sum = await fixture.Provider.ExecuteCosmicScalarAsync(ScalarPlan(source.Sum(h => CosmicQueryMetadata.Value<float>(h, "decimalNumeric")).Result));
            Assert.False(sum.IsError, sum.Message + sum.Exception); Assert.Equal(1.25f, sum.Result);
        });

    [Fact]
    public Task NativeSingleScalarAverageRoundsHalfwayToEven()
        => WithDecimalNumericRows(new object[] { float.Epsilon, 0f }, async (fixture, ids) =>
        {
            var source = CosmicQuery<Holon>.Create().Where(h => ids.Contains(h.Id)).Result;
            var mean = await fixture.Provider.ExecuteCosmicScalarAsync(ScalarPlan(source.Average(h => CosmicQueryMetadata.Value<float>(h, "decimalNumeric")).Result));
            Assert.False(mean.IsError, mean.Message + mean.Exception); Assert.Equal(0f, mean.Result);
        });

    [Fact]
    public Task NativeSingleScalarPrivacyExcludesMalformedHiddenInputs()
    {
        var actor = Guid.NewGuid(); var foreign = Guid.NewGuid();
        return WithMongoGraph(async fixture =>
        {
            var ids = new[] { fixture.Root }.Concat(fixture.Root.Children).Select(row => row.Id).ToArray();
            var source = CosmicQuery<Holon>.Create().WithReadAccess(CosmicQueryReadAccess.OwnerOrPublic(actor).Result)
                .Where(h => ids.Contains(h.Id)).Result;
            var sum = await fixture.Provider.ExecuteCosmicScalarAsync(ScalarPlan(source.Sum(h => CosmicQueryMetadata.Value<float>(h, "floatingNumeric")).Result));
            Assert.False(sum.IsError, sum.Message + sum.Exception); Assert.Equal(3.75f, sum.Result);
        }, manifest: false, configure: root =>
        {
            var rows = new[] { root }.Concat(root.Children).ToArray();
            for (var index = 0; index < rows.Length; index++)
            {
                rows[index].CreatedByAvatarId = index == 0 ? actor : foreign;
                rows[index].IsPublic = index == 1;
                rows[index].MetaData["floatingNumeric"] = index == 0 ? (object)1.25f : index == 1 ? 2.5f : "hidden malformed";
            }
        });
    }

    [Fact]
    public Task NativeSingleScalarWorkIsIndependentOfGraphPublicationLimit()
        => WithDecimalNumericRows(Enumerable.Repeat((object)1f, 137).ToArray(), async (fixture, ids) =>
        {
            var graph = CosmicQueryGraphOptions.Create(CosmicQueryGraphConsistency.ProviderSnapshot, maximumNodes: 1).Result;
            var source = CosmicQuery<Holon>.Create().WithChildren(graph)
                .Where(h => h.Name.StartsWith("owned CQL graph decimal", StringComparison.Ordinal)).Result;
            var sum = await fixture.Provider.ExecuteCosmicScalarAsync(ScalarPlan(source.Sum(h => CosmicQueryMetadata.Value<float>(h, "decimalNumeric")).Result));
            Assert.False(sum.IsError, sum.Message + sum.Exception); Assert.Equal(137f, sum.Result);
        });

    [Fact]
    public Task NativeSingleScalarCancellationLeavesOwnerReusable()
        => WithDecimalNumericRows(new object[] { 1.25f }, async (fixture, ids) =>
        {
            var source = CosmicQuery<Holon>.Create().Where(h => ids.Contains(h.Id)).Result;
            var plan = ScalarPlan(source.Sum(h => CosmicQueryMetadata.Value<float>(h, "decimalNumeric")).Result);
            using var cancellation = new System.Threading.CancellationTokenSource(); cancellation.Cancel();
            var cancelled = await fixture.Provider.ExecuteCosmicScalarAsync(plan, cancellation.Token);
            Assert.Equal("CQL_QUERY_CANCELLED", cancelled.ErrorCode);
            var fresh = await fixture.Provider.ExecuteCosmicScalarAsync(plan);
            Assert.False(fresh.IsError, fresh.Message + fresh.Exception); Assert.Equal(1.25f, fresh.Result);
        });
}
