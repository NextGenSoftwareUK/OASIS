using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests;

public partial class SQLLiteDBOASISIntegrationTests
{
    private static CosmicScalarPlan<T, TResult> NumericPlan<T, TResult>(SQLLiteDBOASIS provider,
        CosmicScalarQuery<T, TResult> query) where T : IHolon
    {
        var claims = new CosmicScalarCapabilities(Array.Empty<CosmicScalarOperation>(), new[]
        {
            new CosmicScalarSelectionCapability(CosmicScalarOperation.Sum, SQLLiteDBOASIS.CqlProjectionCapabilities),
            new CosmicScalarSelectionCapability(CosmicScalarOperation.Average, SQLLiteDBOASIS.CqlProjectionCapabilities)
        });
        var adapter = (ICosmicQueryScalarStorageProvider)provider;
        var plan = CosmicScalarPlanner.Plan(query, adapter.QueryProviderType,
            new[] { new CosmicScalarTarget(adapter.QueryProviderType, adapter.QueryCapabilities, claims) });
        plan.IsError.Should().BeFalse(plan.Message); return plan.Result;
    }

    private static async Task<Guid[]> SeedNumeric(SQLLiteDBOASIS provider, params object[] values)
    {
        var keys = new List<Guid>();
        for (var index = 0; index < values.Length; index++)
        {
            var row = new Holon { Id = Guid.NewGuid(), Name = "owned numeric " + index,
                MetaData = new() { ["value"] = values[index], ["Rating"] = "unselected invalid" } };
            var saved = await provider.SaveHolonAsync(row, saveChildren: false);
            saved.IsError.Should().BeFalse(saved.Message); keys.Add(row.Id);
        }
        return keys.ToArray();
    }

    [Fact]
    public async Task NativeNumericSumKeepsIntermediateCancellationWithoutEntityHydration()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var ids = await SeedNumeric(provider, long.MaxValue, 1L, -1L);
        var source = CosmicQuery<ScalarMustNotConstruct>.Create().Where(h => ids.Contains(h.Id)).Result;
        var result = await provider.ExecuteCosmicScalarAsync(NumericPlan(provider, source.Sum(h => CosmicQueryMetadata.Value<long>(h, "value")).Result));
        result.IsError.Should().BeFalse(result.Message + result.Exception); result.Result.Should().Be(long.MaxValue);
    }

    [Fact]
    public async Task NativeNumericAverageDoesNotRequireAnInt64Sum()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var ids = await SeedNumeric(provider, long.MaxValue, long.MaxValue);
        var source = CosmicQuery<Holon>.Create().Where(h => ids.Contains(h.Id)).Result;
        var sum = await provider.ExecuteCosmicScalarAsync(NumericPlan(provider, source.Sum(h => CosmicQueryMetadata.Value<long>(h, "value")).Result));
        sum.ErrorCode.Should().Be("CQL_SCALAR_OVERFLOW");
        var average = await provider.ExecuteCosmicScalarAsync(NumericPlan(provider, source.Average(h => CosmicQueryMetadata.Value<long>(h, "value")).Result));
        average.IsError.Should().BeFalse(average.Message + average.Exception); average.Result.Should().Be((decimal)long.MaxValue);
    }

    [Fact]
    public async Task NativeNumericEmptyAndNullableDomainsAreExplicit()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var id = Guid.NewGuid(); var source = CosmicQuery<Holon>.Create().Where(h => h.Id == id).Result;
        var sum = await provider.ExecuteCosmicScalarAsync(NumericPlan(provider, source.Sum(h => CosmicQueryMetadata.Value<long?>(h, "value")).Result));
        sum.IsError.Should().BeFalse(sum.Message); sum.Result.Should().Be(0);
        var average = await provider.ExecuteCosmicScalarAsync(NumericPlan(provider, source.Average(h => CosmicQueryMetadata.Value<long?>(h, "value")).Result));
        average.IsError.Should().BeFalse(average.Message); average.IsLoaded.Should().BeTrue(); average.Result.Should().BeNull();
        var required = await provider.ExecuteCosmicScalarAsync(NumericPlan(provider, source.Average(h => CosmicQueryMetadata.Value<long>(h, "value")).Result));
        required.ErrorCode.Should().Be("CQL_EMPTY_SEQUENCE");
    }

    [Fact]
    public async Task NativeNumericWrongParticipantCannotCancelIntoASuccessfulTotal()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var ids = await SeedNumeric(provider, 1L, -1L, "7");
        var source = CosmicQuery<Holon>.Create().Where(h => ids.Contains(h.Id)).Result;
        var result = await provider.ExecuteCosmicScalarAsync(NumericPlan(provider, source.Sum(h => CosmicQueryMetadata.Value<long?>(h, "value")).Result));
        result.ErrorCode.Should().Be("CQL_DATA_INVALID"); result.IsLoaded.Should().BeFalse();
    }

    [Fact]
    public async Task NativeNumericSourceWindowPrecedesSelectedDomainValidation()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var ids = await SeedNumeric(provider, 7L, "wrong");
        var source = CosmicQuery<Holon>.Create().Where(h => ids.Contains(h.Id)).OrderBy(h => h.Name).Take(1).Result;
        var result = await provider.ExecuteCosmicScalarAsync(NumericPlan(provider, source.Sum(h => CosmicQueryMetadata.Value<long>(h, "value")).Result));
        result.IsError.Should().BeFalse(result.Message + result.Exception); result.Result.Should().Be(7L);
    }
}
