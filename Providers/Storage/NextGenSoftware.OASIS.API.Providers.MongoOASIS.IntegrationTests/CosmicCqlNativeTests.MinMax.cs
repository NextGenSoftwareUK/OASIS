using System;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using Xunit;
using Provider = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS;

namespace NextGenSoftware.OASIS.API.Providers.MongoOASIS.IntegrationTests;

public sealed partial class CosmicCqlNativeTests
{
    private static CosmicScalarPlan<T, TResult> MinMaxPlan<T, TResult>(CosmicScalarQuery<T, TResult> query) where T : IHolon
    {
        var claims = new CosmicScalarCapabilities(Array.Empty<CosmicScalarOperation>(), new[]
        {
            new CosmicScalarSelectionCapability(CosmicScalarOperation.Min, Provider.CqlProjectionCapabilities),
            new CosmicScalarSelectionCapability(CosmicScalarOperation.Max, Provider.CqlProjectionCapabilities)
        });
        var plan = CosmicScalarPlanner.Plan(query, Core.Enums.ProviderType.MongoDBOASIS,
            new[] { new CosmicScalarTarget(Core.Enums.ProviderType.MongoDBOASIS, Provider.CqlCapabilities, claims) });
        Assert.False(plan.IsError, plan.Message); return plan.Result;
    }

    [Fact]
    public Task NativeMinMaxRetainsExactNegativeInt64WithoutHydration()
        => WithMongoGraph(async fixture =>
        {
            var query = CosmicQuery<ScalarMustNotConstruct>.Create()
                .Where(h => h.Name.StartsWith("owned CQL graph", StringComparison.Ordinal)).Result;
            var min = await fixture.Provider.ExecuteCosmicScalarAsync(MinMaxPlan(query.Min(h => CosmicQueryMetadata.Value<long>(h, "stock")).Result));
            Assert.False(min.IsError, min.Message + min.Exception); Assert.Equal(long.MinValue, min.Result);
            var max = await fixture.Provider.ExecuteCosmicScalarAsync(MinMaxPlan(query.Max(h => CosmicQueryMetadata.Value<long>(h, "stock")).Result));
            Assert.False(max.IsError, max.Message + max.Exception); Assert.Equal(long.MaxValue, max.Result);
        }, configure: root =>
        {
            root.MetaData["stock"] = long.MaxValue; root.MetaData["Rating"] = "unselected wrong type";
            root.Children.First().MetaData["stock"] = long.MinValue;
            root.Children.ElementAt(1).MetaData["stock"] = -9007199254740993L;
            root.Children.First().Children.First().MetaData["stock"] = 9007199254740993L;
        });

    [Fact]
    public Task NativeMinMaxRequiredNullableAndStringEmptyInputsAreDistinct()
        => WithMongoGraph(async fixture =>
        {
            var id = Guid.NewGuid(); var source = CosmicQuery<Holon>.Create().Where(h => h.Id == id).Result;
            var required = await fixture.Provider.ExecuteCosmicScalarAsync(MinMaxPlan(source.Min(h => CosmicQueryMetadata.Value<long>(h, "stock")).Result));
            Assert.Equal("CQL_EMPTY_SEQUENCE", required.ErrorCode);
            var optional = await fixture.Provider.ExecuteCosmicScalarAsync(MinMaxPlan(source.Max(h => CosmicQueryMetadata.Value<long?>(h, "stock")).Result));
            Assert.False(optional.IsError, optional.Message + optional.Exception); Assert.True(optional.IsLoaded); Assert.Null(optional.Result);
            var text = await fixture.Provider.ExecuteCosmicScalarAsync(MinMaxPlan(source.Min(h => h.Name).Result));
            Assert.False(text.IsError, text.Message + text.Exception); Assert.Null(text.Result);
        }, width: 0);

    [Fact]
    public Task NativeMinMaxDoesNotHideWrongNonWinningValues()
        => WithMongoGraph(async fixture =>
        {
            var ids = new[] { fixture.Root.Id, fixture.Root.Children.First().Id };
            var source = CosmicQuery<Holon>.Create().Where(h => ids.Contains(h.Id)).Result;
            var invalid = await fixture.Provider.ExecuteCosmicScalarAsync(MinMaxPlan(source.Min(h => CosmicQueryMetadata.Value<long>(h, "stock")).Result));
            Assert.Equal("CQL_DATA_INVALID", invalid.ErrorCode); Assert.False(invalid.IsLoaded);
        }, configure: root => { root.MetaData["stock"] = 0L; root.Children.First().MetaData["stock"] = "wrong"; });

    [Fact]
    public Task NativeMinMaxPreservesSourceWindowBeforeValueValidation()
        => WithMongoGraph(async fixture =>
        {
            var source = CosmicQuery<Holon>.Create().Where(h => h.Name.StartsWith("owned CQL graph", StringComparison.Ordinal))
                .OrderByDescending(h => h.Name).Take(1).Result;
            var min = await fixture.Provider.ExecuteCosmicScalarAsync(MinMaxPlan(source.Min(h => CosmicQueryMetadata.Value<long>(h, "stock")).Result));
            Assert.False(min.IsError, min.Message + min.Exception); Assert.Equal(7L, min.Result);
        }, configure: root => { root.MetaData["stock"] = 7L; foreach (var child in root.Children) child.MetaData["stock"] = "outside window"; });

    [Fact]
    public Task NativeMinMaxUsesUnicodeScalarOrder()
        => WithMongoGraph(async fixture =>
        {
            var ids = new[] { fixture.Root.Id, fixture.Root.Children.First().Id };
            var source = CosmicQuery<Holon>.Create().Where(h => ids.Contains(h.Id)).Result;
            var min = await fixture.Provider.ExecuteCosmicScalarAsync(MinMaxPlan(source.Min(h => CosmicQueryMetadata.Value<string>(h, "text")).Result));
            Assert.False(min.IsError, min.Message + min.Exception); Assert.Equal("\ue000", min.Result);
            var max = await fixture.Provider.ExecuteCosmicScalarAsync(MinMaxPlan(source.Max(h => CosmicQueryMetadata.Value<string>(h, "text")).Result));
            Assert.False(max.IsError, max.Message + max.Exception); Assert.Equal("\U00010000", max.Result);
        }, configure: root => { root.MetaData["text"] = "\ue000"; root.Children.First().MetaData["text"] = "\U00010000"; });
}
