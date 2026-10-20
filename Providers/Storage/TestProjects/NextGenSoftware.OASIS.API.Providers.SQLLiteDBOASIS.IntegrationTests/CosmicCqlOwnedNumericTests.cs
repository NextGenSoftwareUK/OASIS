using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.CustomAttrbiutes;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Core.Serialization;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests;

public partial class SQLLiteDBOASISIntegrationTests
{
    public sealed class CqlOwnedAmount { public decimal Amount { get; set; } }
    public sealed class CqlOwnedNumericRow : Holon
    {
        [CustomOASISProperty] public CqlOwnedAmount Detail { get; set; } = new();
        [CustomOASISProperty] public List<decimal> Amounts { get; set; } = new();
    }

    [Theory]
    [InlineData(false, false)] [InlineData(true, false)]
    [InlineData(false, true)] [InlineData(true, true)]
    public async Task TypedCqlOwnedNumericValuesPreservePrecisionOrRejectWithoutChangingStoredRows(bool unrepresentable, bool numericStrings)
    {
        var number = unrepresentable ? "1.00000000000000000000000000000000000000001" : "1.000000000000000000000000001";
        var encoded = numericStrings ? "\"" + number + "\"" : number;
        using var detail = System.Text.Json.JsonDocument.Parse("{\"Amount\":" + encoded + "}");
        using var amounts = System.Text.Json.JsonDocument.Parse("[" + encoded + "]");
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var source = new Holon { Id = Guid.NewGuid(), Name = "owned numeric CQL", MetaData = new() {
            ["Detail"] = detail.RootElement.Clone(), ["Amounts"] = amounts.RootElement.Clone() } };
        var saved = await provider.SaveHolonAsync(source, saveChildren: false);
        saved.IsError.Should().BeFalse(saved.Message + saved.Exception);
        var before = (await provider.LoadHolonAsync(source.Id, loadChildren: false)).Result;
        var beforeJson = StoredRecordJson.Serialize(before);
        ICosmicQueryStorageProvider adapter = provider;
        var id = source.Id;
        var query = CosmicQuery<CqlOwnedNumericRow>.Create().Where(h => h.Id == id).Result;
        var plan = CosmicQueryPlanner.Plan(query, adapter.QueryProviderType,
            new[] { new CosmicQueryTarget(adapter.QueryProviderType, adapter.QueryCapabilities) });
        plan.IsError.Should().BeFalse(plan.Message);
        var result = await CosmicManager(provider, true).ExecuteQueryAsync(plan.Result, new[] { adapter });
        if (unrepresentable)
        {
            result.IsError.Should().BeTrue(); result.Result.Should().BeNull();
            result.ErrorCode.Should().Be("COSMIC_METADATA_HYDRATION_FAILED");
        }
        else
        {
            result.IsError.Should().BeFalse(result.Message + result.Exception);
            var row = result.Result.Providers.Single().Rows.Single();
            row.Detail.Amount.Should().Be(1.000000000000000000000000001m);
            row.Amounts.Single().Should().Be(row.Detail.Amount);
            HolonChangeTracking.HasChanges(row).Should().BeFalse();
        }
        var after = (await provider.LoadHolonAsync(source.Id, loadChildren: false)).Result;
        StoredRecordJson.Serialize(after).Should().Be(beforeJson);
        StoredRecordJson.Serialize(after.MetaData).Should().Contain(number);
    }
}
