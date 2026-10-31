using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests;

public partial class SQLLiteDBOASISIntegrationTests
{
    private static CosmicScalarPlan<T, TResult> MinMaxPlan<T, TResult>(SQLLiteDBOASIS provider,
        CosmicScalarQuery<T, TResult> query) where T : IHolon
    {
        // This caller claim deliberately cannot enable the actual adapter. Its
        // own scalar/read declarations must independently pass readmission.
        var expected = new CosmicScalarCapabilities(Array.Empty<CosmicScalarOperation>(), new[]
        {
            new CosmicScalarSelectionCapability(CosmicScalarOperation.Min, SQLLiteDBOASIS.CqlProjectionCapabilities),
            new CosmicScalarSelectionCapability(CosmicScalarOperation.Max, SQLLiteDBOASIS.CqlProjectionCapabilities)
        });
        var adapter = (ICosmicQueryScalarStorageProvider)provider;
        var plan = CosmicScalarPlanner.Plan(query, adapter.QueryProviderType,
            new[] { new CosmicScalarTarget(adapter.QueryProviderType, adapter.QueryCapabilities, expected) });
        plan.IsError.Should().BeFalse(plan.Message);
        return plan.Result;
    }

    [Fact]
    public async Task NativeMinMaxRetainsExactNegativeInt64WithoutEntityHydration()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var values = new[] { long.MinValue, -9007199254740993L, 9007199254740993L, long.MaxValue };
        var ids = new List<Guid>();
        foreach (var value in values)
        {
            var row = new Holon { Id = Guid.NewGuid(), Name = "owned minmax", MetaData = new() { ["stock"] = value, ["Rating"] = "unselected invalid" } };
            (await provider.SaveHolonAsync(row, saveChildren: false)).IsError.Should().BeFalse(); ids.Add(row.Id);
        }
        var keys = ids.ToArray();
        var source = CosmicQuery<ScalarMustNotConstruct>.Create().Where(h => keys.Contains(h.Id)).Result;
        var min = await provider.ExecuteCosmicScalarAsync(MinMaxPlan(provider, source.Min(h => CosmicQueryMetadata.Value<long>(h, "stock")).Result));
        min.IsError.Should().BeFalse(min.Message + min.Exception); min.Result.Should().Be(long.MinValue);
        var max = await provider.ExecuteCosmicScalarAsync(MinMaxPlan(provider, source.Max(h => CosmicQueryMetadata.Value<long>(h, "stock")).Result));
        max.IsError.Should().BeFalse(max.Message + max.Exception); max.Result.Should().Be(long.MaxValue);
    }

    [Fact]
    public async Task NativeMinMaxEmptyRequiredNullableAndStringDomainsRemainDistinct()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var id = Guid.NewGuid(); var source = CosmicQuery<Holon>.Create().Where(h => h.Id == id).Result;
        var required = await provider.ExecuteCosmicScalarAsync(MinMaxPlan(provider, source.Min(h => CosmicQueryMetadata.Value<long>(h, "stock")).Result));
        required.ErrorCode.Should().Be("CQL_EMPTY_SEQUENCE");
        var optional = await provider.ExecuteCosmicScalarAsync(MinMaxPlan(provider, source.Max(h => CosmicQueryMetadata.Value<long?>(h, "stock")).Result));
        optional.IsError.Should().BeFalse(optional.Message + optional.Exception); optional.IsLoaded.Should().BeTrue(); optional.Result.Should().BeNull();
        var text = await provider.ExecuteCosmicScalarAsync(MinMaxPlan(provider, source.Min(h => h.Name).Result));
        text.IsError.Should().BeFalse(text.Message + text.Exception); text.Result.Should().BeNull();
    }

    [Fact]
    public async Task NativeMinMaxValidatesOnlySelectedWindowValues()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var ids = new List<Guid>();
        foreach (var pair in new[] { ("A-good", (object)7L), ("B-wrong", (object)"wrong") })
        {
            var row = new Holon { Id = Guid.NewGuid(), Name = pair.Item1, MetaData = new() { ["stock"] = pair.Item2 } };
            (await provider.SaveHolonAsync(row, saveChildren: false)).IsError.Should().BeFalse(); ids.Add(row.Id);
        }
        var keys = ids.ToArray();
        var source = CosmicQuery<Holon>.Create().Where(h => keys.Contains(h.Id)).OrderBy(h => h.Name).Take(1).Result;
        var min = await provider.ExecuteCosmicScalarAsync(MinMaxPlan(provider, source.Min(h => CosmicQueryMetadata.Value<long>(h, "stock")).Result));
        min.IsError.Should().BeFalse(min.Message + min.Exception); min.Result.Should().Be(7L);
    }

    [Fact]
    public async Task NativeMinMaxUsesUnicodeScalarOrderForStrings()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var ids = new List<Guid>();
        foreach (var text in new[] { "\ue000", "\U00010000" })
        {
            var row = new Holon { Id = Guid.NewGuid(), Name = text };
            (await provider.SaveHolonAsync(row, saveChildren: false)).IsError.Should().BeFalse(); ids.Add(row.Id);
        }
        var keys = ids.ToArray();
        var source = CosmicQuery<Holon>.Create().Where(h => keys.Contains(h.Id)).Result;
        var min = await provider.ExecuteCosmicScalarAsync(MinMaxPlan(provider, source.Min(h => h.Name).Result));
        min.IsError.Should().BeFalse(min.Message + min.Exception); min.Result.Should().Be("\ue000");
        var max = await provider.ExecuteCosmicScalarAsync(MinMaxPlan(provider, source.Max(h => h.Name).Result));
        max.IsError.Should().BeFalse(max.Message + max.Exception); max.Result.Should().Be("\U00010000");
    }

    [Fact]
    public async Task NativeMinMaxCannotHideAnInvalidNonWinningInput()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var ids = new List<Guid>();
        foreach (var value in new object[] { 0L, "wrong" })
        {
            var row = new Holon { Id = Guid.NewGuid(), Name = "owned malformed minmax", MetaData = new() { ["stock"] = value } };
            (await provider.SaveHolonAsync(row, saveChildren: false)).IsError.Should().BeFalse(); ids.Add(row.Id);
        }
        var keys = ids.ToArray();
        var query = CosmicQuery<Holon>.Create().Where(h => keys.Contains(h.Id)).Min(h => CosmicQueryMetadata.Value<long>(h, "stock")).Result;
        var invalid = await provider.ExecuteCosmicScalarAsync(MinMaxPlan(provider, query));
        invalid.ErrorCode.Should().Be("CQL_DATA_INVALID"); invalid.IsLoaded.Should().BeFalse();
    }
}
