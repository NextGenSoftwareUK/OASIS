using NextGenSoftware.OASIS.API.Core.CustomAttrbiutes;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public class CosmicTypedQueryTests
{
    public sealed class TypedRow : Holon { [CustomOASISProperty] public int Rating { get; set; } }
    public sealed class ThrowingRow : Holon { [CustomOASISProperty] public int Rating { get => 0; set => throw new InvalidOperationException("owned setter failure"); } }
    private sealed class Adapter : ICosmicQueryStorageProvider
    {
        public int Calls; public IHolon Row = new Holon { Id = Guid.NewGuid(), MetaData = new() { ["Rating"] = 7 } };
        public ProviderType QueryProviderType => ProviderType.SQLLiteDBOASIS;
        public CosmicQueryCapabilities QueryCapabilities => new(CosmicQueryFeatures.None, Array.Empty<CosmicQueryFieldCapability>());
        public Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(CosmicQueryPlan<T> plan, int maximumResults = 100,
            CancellationToken cancellationToken = default) where T : IHolon
        { Calls++; return Task.FromResult(new OASISResult<IReadOnlyList<IHolon>>(new[] { Row })); }
    }
    private static CosmicQueryPlan<T> Plan<T>(Adapter adapter) where T : IHolon
        => CosmicQueryPlanner.Plan(CosmicQuery<T>.Create(), adapter.QueryProviderType,
            new[] { new CosmicQueryTarget(adapter.QueryProviderType, adapter.QueryCapabilities) }).Result;
    private static HolonManager Manager(string mode = HyperDriveModes.V2)
    {
        var dna = new OASISDNA { OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS { HyperDriveMode = mode } };
        return new HolonManager(null, dna, new ProviderManager(null, dna));
    }
    [Fact]
    public async Task TypedQueriesHydrateDetachedValuesAndPublishCleanTrackingWithProvenance()
    {
        var adapter = new Adapter(); var result = await Manager().ExecuteQueryAsync(Plan<TypedRow>(adapter), new[] { adapter });
        Assert.False(result.IsError, result.Message + result.Exception); Assert.Equal(1, result.Result.TotalRows);
        var group = Assert.Single(result.Result.Providers); Assert.Equal(ProviderType.SQLLiteDBOASIS, group.Provider);
        var row = Assert.Single(group.Rows); Assert.Equal(7, row.Rating); Assert.False(HolonChangeTracking.HasChanges(row));
        Assert.NotSame(adapter.Row, row); adapter.Row.MetaData["Rating"] = 99; Assert.Equal(7, row.Rating);
        row.Rating = 8; Assert.True(HolonChangeTracking.HasChanges(row));
    }
    [Fact]
    public async Task TypedSetterFailureReturnsAnExplicitErrorWithoutMutatingProviderRows()
    {
        var adapter = new Adapter(); var id = adapter.Row.Id;
        var result = await Manager().ExecuteQueryAsync(Plan<ThrowingRow>(adapter), new[] { adapter });
        Assert.True(result.IsError); Assert.False(result.IsLoaded); Assert.Null(result.Result);
        Assert.Equal("COSMIC_METADATA_HYDRATION_FAILED", result.ErrorCode); Assert.NotNull(result.Exception);
        Assert.Contains("owned setter failure", result.Exception.ToString()); Assert.Equal(id, adapter.Row.Id); Assert.Equal(7, adapter.Row.MetaData["Rating"]);
    }
    public sealed class DecimalRow : Holon { [CustomOASISProperty] public decimal Amount { get; set; } }
    public sealed class DoubleRow : Holon { [CustomOASISProperty] public double Amount { get; set; } }

    [Theory]
    [InlineData("1.00000000000000000000000000000000000000001", false)]
    [InlineData("0.000000000000000000000000000001", false)]
    [InlineData("9007199254740993", true)]
    [InlineData("1e-400", true)]
    [InlineData("\"1.00000000000000000000000000000000000000001\"", false)]
    [InlineData("\"0.000000000000000000000000000001\"", false)]
    [InlineData("\"9007199254740993\"", true)]
    [InlineData("\"1e-400\"", true)]
    [InlineData("\"NaN\"", true)]
    [InlineData("\"Infinity\"", true)]
    [InlineData("\"+1\"", false)]
    [InlineData("\"01\"", false)]
    public async Task TypedNumericHydrationRejectsRoundingWithoutPublishingRows(string json, bool floating)
    {
        using var document = System.Text.Json.JsonDocument.Parse(json);
        var adapter = new Adapter { Row = new Holon { Id = Guid.NewGuid(), MetaData = new() { ["Amount"] = document.RootElement.Clone() } } };
        if (floating)
        {
            var result = await Manager().ExecuteQueryAsync(Plan<DoubleRow>(adapter), new[] { adapter });
            Assert.True(result.IsError); Assert.Null(result.Result); Assert.Equal("COSMIC_METADATA_HYDRATION_FAILED", result.ErrorCode);
        }
        else
        {
            var result = await Manager().ExecuteQueryAsync(Plan<DecimalRow>(adapter), new[] { adapter });
            Assert.True(result.IsError); Assert.Null(result.Result); Assert.Equal("COSMIC_METADATA_HYDRATION_FAILED", result.ErrorCode);
        }
        Assert.Equal(json, ((System.Text.Json.JsonElement)adapter.Row.MetaData["Amount"]).GetRawText());
    }

    [Theory]
    [InlineData("1.000000000000000000000000001")]
    [InlineData("1.00")]
    [InlineData("\"1.000000000000000000000000001\"")]
    [InlineData("\"1.00\"")]
    public async Task TypedDecimalHydrationAcceptsExactValues(string json)
    {
        using var document = System.Text.Json.JsonDocument.Parse(json);
        var adapter = new Adapter { Row = new Holon { Id = Guid.NewGuid(), MetaData = new() { ["Amount"] = document.RootElement.Clone() } } };
        var result = await Manager().ExecuteQueryAsync(Plan<DecimalRow>(adapter), new[] { adapter });
        Assert.False(result.IsError, result.Message + result.Exception);
        Assert.Equal(decimal.Parse(document.RootElement.ValueKind == System.Text.Json.JsonValueKind.String ? document.RootElement.GetString()! : json, System.Globalization.CultureInfo.InvariantCulture), Assert.Single(Assert.Single(result.Result.Providers).Rows).Amount);
    }

    public sealed class OwnedAmount { public decimal Amount { get; set; } }
    public sealed class OwnedNumericRow : Holon
    {
        [CustomOASISProperty] public OwnedAmount Detail { get; set; } = new();
        [CustomOASISProperty] public List<decimal> Amounts { get; set; } = new();
        [CustomOASISProperty] public Dictionary<string, decimal> Values { get; set; } = new();
    }

    [Theory]
    [InlineData(0, false)] [InlineData(1, false)] [InlineData(2, false)]
    [InlineData(0, true)] [InlineData(1, true)] [InlineData(2, true)]
    public async Task TypedOwnedNumericHydrationRejectsNestedPrecisionLoss(int shape, bool underflow)
    {
        var number = underflow ? "1e-30" : "1.00000000000000000000000000000000000000001";
        var key = shape == 0 ? "Detail" : shape == 1 ? "Amounts" : "Values";
        var json = shape == 0 ? "{\"amount\":" + number + "}" : shape == 1 ? "[" + number + "]" : "{\"entry\":" + number + "}";
        using var document = System.Text.Json.JsonDocument.Parse(json);
        var adapter = new Adapter { Row = new Holon { Id = Guid.NewGuid(), MetaData = new() { [key] = document.RootElement.Clone() } } };
        var result = await Manager().ExecuteQueryAsync(Plan<OwnedNumericRow>(adapter), new[] { adapter });
        Assert.True(result.IsError); Assert.Null(result.Result); Assert.Equal("COSMIC_METADATA_HYDRATION_FAILED", result.ErrorCode);
        Assert.Equal(json, ((System.Text.Json.JsonElement)adapter.Row.MetaData[key]).GetRawText());
    }

    [Fact]
    public async Task TypedOwnedNumbersPreserveExactValuesAndCaseInsensitiveBinding()
    {
        const decimal amount = 1.000000000000000000000000001m;
        using var detail = System.Text.Json.JsonDocument.Parse("{\"amount\":1.000000000000000000000000001}");
        using var amounts = System.Text.Json.JsonDocument.Parse("[1.000000000000000000000000001]");
        using var values = System.Text.Json.JsonDocument.Parse("{\"entry\":1.000000000000000000000000001}");
        var adapter = new Adapter { Row = new Holon { Id = Guid.NewGuid(), MetaData = new() {
            ["Detail"] = detail.RootElement.Clone(), ["Amounts"] = amounts.RootElement.Clone(), ["Values"] = values.RootElement.Clone() } } };
        var result = await Manager().ExecuteQueryAsync(Plan<OwnedNumericRow>(adapter), new[] { adapter });
        Assert.False(result.IsError, result.Message + result.Exception);
        var row = Assert.Single(Assert.Single(result.Result.Providers).Rows);
        Assert.Equal(amount, row.Detail.Amount); Assert.Equal(amount, Assert.Single(row.Amounts)); Assert.Equal(amount, row.Values["entry"]);
    }

    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)]
    public async Task TypedOwnedNumericStringsCannotSilentlyRound(int shape)
    {
        const string number = "1.00000000000000000000000000000000000000001";
        var key = shape == 0 ? "Detail" : shape == 1 ? "Amounts" : "Values";
        var json = shape == 0 ? "{\"Amount\":\"" + number + "\"}" : shape == 1 ? "[\"" + number + "\"]" : "{\"entry\":\"" + number + "\"}";
        using var document = System.Text.Json.JsonDocument.Parse(json);
        var adapter = new Adapter { Row = new Holon { Id = Guid.NewGuid(), MetaData = new() { [key] = document.RootElement.Clone() } } };
        var result = await Manager().ExecuteQueryAsync(Plan<OwnedNumericRow>(adapter), new[] { adapter });
        Assert.True(result.IsError); Assert.Null(result.Result); Assert.Equal("COSMIC_METADATA_HYDRATION_FAILED", result.ErrorCode);
        Assert.Equal(json, ((System.Text.Json.JsonElement)adapter.Row.MetaData[key]).GetRawText());
    }

    [Fact]
    public async Task TypedOwnedMetadataRejectsAmbiguousPropertyAliases()
    {
        using var document = System.Text.Json.JsonDocument.Parse("{\"Amount\":1,\"amount\":1}");
        var adapter = new Adapter { Row = new Holon { Id = Guid.NewGuid(), MetaData = new() { ["Detail"] = document.RootElement.Clone() } } };
        var result = await Manager().ExecuteQueryAsync(Plan<OwnedNumericRow>(adapter), new[] { adapter });
        Assert.True(result.IsError); Assert.Null(result.Result); Assert.Equal("COSMIC_METADATA_HYDRATION_FAILED", result.ErrorCode);
    }

    public sealed class ExplodingOwnedValue
    {
        public ExplodingOwnedValue() => throw new InvalidOperationException("unexpected owned construction");
        public decimal Amount { get; set; }
    }
    public sealed class OwnedConstructorRow : Holon
    {
        [CustomOASISProperty] public ExplodingOwnedValue Detail { get; set; } = null!;
    }

    [Fact]
    public async Task DuplicateOwnedAssignmentsAreRejectedBeforeInvokingTheirConstructor()
    {
        using var document = System.Text.Json.JsonDocument.Parse("{\"Amount\":1,\"amount\":1}");
        var adapter = new Adapter { Row = new Holon { Id = Guid.NewGuid(), MetaData = new() { ["Detail"] = document.RootElement.Clone() } } };
        var result = await Manager().ExecuteQueryAsync(Plan<OwnedConstructorRow>(adapter), new[] { adapter });
        Assert.True(result.IsError); Assert.Null(result.Result);
        Assert.Contains("Ambiguous owned metadata property", result.Exception.ToString());
        Assert.DoesNotContain("unexpected owned construction", result.Exception.ToString());
    }

    public enum NamedMode { Ready = 1 }
    public sealed class TextAndEnumRow : Holon
    {
        [CustomOASISProperty] public string Amount { get; set; } = "";
        [CustomOASISProperty] public NamedMode Mode { get; set; }
    }

    [Fact]
    public async Task TypedLiteralTextAndNamedEnumHydrationRemainSupported()
    {
        var adapter = new Adapter { Row = new Holon { Id = Guid.NewGuid(), MetaData = new() { ["Amount"] = "001.00", ["Mode"] = "Ready" } } };
        var result = await Manager().ExecuteQueryAsync(Plan<TextAndEnumRow>(adapter), new[] { adapter });
        Assert.False(result.IsError, result.Message + result.Exception);
        var row = Assert.Single(Assert.Single(result.Result.Providers).Rows);
        Assert.Equal("001.00", row.Amount); Assert.Equal(NamedMode.Ready, row.Mode);
    }

    public sealed class OwnedSchemaRow : Holon
    {
        [CustomOASISProperty] public List<NamedMode> Modes { get; set; } = new();
        [CustomOASISProperty] public List<decimal?> Amounts { get; set; } = new();
        [CustomOASISProperty] public Dictionary<string, decimal> Values { get; set; } = new();
    }

    [Fact]
    public async Task OwnedSchemaPreservesNamedEnumsNullableNumbersAndDistinctDictionaryKeys()
    {
        using var modes = System.Text.Json.JsonDocument.Parse("[\"Ready\"]");
        using var amounts = System.Text.Json.JsonDocument.Parse("[\"1.000000000000000000000000001\",null]");
        using var values = System.Text.Json.JsonDocument.Parse("{\"a\":1,\"A\":2}");
        var adapter = new Adapter { Row = new Holon { Id = Guid.NewGuid(), MetaData = new() {
            ["Modes"] = modes.RootElement.Clone(), ["Amounts"] = amounts.RootElement.Clone(), ["Values"] = values.RootElement.Clone() } } };
        var result = await Manager().ExecuteQueryAsync(Plan<OwnedSchemaRow>(adapter), new[] { adapter });
        Assert.False(result.IsError, result.Message + result.Exception);
        var row = Assert.Single(Assert.Single(result.Result.Providers).Rows);
        Assert.Equal(NamedMode.Ready, Assert.Single(row.Modes));
        Assert.Equal(1.000000000000000000000000001m, row.Amounts[0]); Assert.Null(row.Amounts[1]);
        Assert.Equal(1m, row.Values["a"]); Assert.Equal(2m, row.Values["A"]);
    }

    [Fact]
    public async Task TypedCqlRejectsUnsupportedModeBeforeAdapterIO()
    {
        var adapter = new Adapter(); var result = await Manager(HyperDriveModes.Legacy).ExecuteQueryAsync(Plan<TypedRow>(adapter), new[] { adapter });
        Assert.Equal("CQL_V2_REQUIRED", result.ErrorCode); Assert.Null(result.Result); Assert.Equal(0, adapter.Calls);
    }
}
