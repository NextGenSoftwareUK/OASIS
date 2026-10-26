using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.DNA;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public partial class CosmicTypedQueryTests
{
    private static CosmicFederationPlan<T> FederationPlan<T>(Adapter[] adapters, CosmicFederationOptions? options = null)
        where T : Core.Interfaces.IHolon
    {
        var source = CosmicQuery<T>.Create(CosmicQueryScope.Named(adapters.Select(adapter => adapter.QueryProviderType).ToArray()).Result);
        var plan = CosmicFederationPlanner.Plan(source, options ?? CosmicFederationOptions.Create().Result,
            adapters[0].QueryProviderType, adapters.Select(adapter => new CosmicQueryTarget(adapter.QueryProviderType, adapter.QueryCapabilities)).ToArray());
        Assert.False(plan.IsError, plan.Message);
        return plan.Result;
    }

    [Fact]
    public async Task FederationManagerHydratesAllObservedCopiesWithCleanTrackingAndSelectedProvenance()
    {
        var sqlite = new Adapter();
        var mongo = new Adapter { Provider = ProviderType.MongoDBOASIS,
            Row = new Holon { Id = sqlite.Row.Id, MetaData = new() { ["Rating"] = 9 } } };
        var adapters = new[] { sqlite, mongo };
        var options = CosmicFederationOptions.Create(CosmicFederationIdentityMode.ProviderPriority,
            providerPriority: new[] { mongo.Provider, sqlite.Provider }).Result;
        var result = await Manager().ExecuteFederationQueryAsync(FederationPlan<TypedRow>(adapters, options), adapters);
        Assert.False(result.IsError, result.Message + result.Exception);
        var resolved = Assert.Single(result.Result.Rows);
        Assert.Equal(mongo.Provider, resolved.Selected.Provider);
        Assert.Equal(9, Assert.IsType<TypedRow>(resolved.Selected.Row).Rating);
        Assert.Equal(new[] { 7, 9 }, resolved.Copies.Select(copy => Assert.IsType<TypedRow>(copy.Row).Rating));
        Assert.All(resolved.Copies, copy => Assert.False(HolonChangeTracking.HasChanges(copy.Row)));
        Assert.Same(resolved.Selected, resolved.Copies.Single(copy => copy.Provider == mongo.Provider));
        Assert.Equal(1, sqlite.Calls);
        Assert.Equal(1, mongo.Calls);
        mongo.Row.MetaData["Rating"] = 100;
        Assert.Equal(9, Assert.IsType<TypedRow>(resolved.Selected.Row).Rating);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public async Task FederationManagerReusesMetadataDecryptionBeforeTypedHydration(int fault)
    {
        var settings = new EncryptionSettings { Rijndael256EncryptionEnabled = true, Rijndael256Key = new string('e', 32) };
        var payload = fault == 3 ? "{\"Rating\":7,\"Rating\":8}" : "{\"Rating\":7}";
        var ciphertext = fault == 1 ? payload : Core.Helpers.PasswordEncryptionHelper.EncryptValue(payload, settings);
        var adapter = new Adapter { Row = new Holon { Id = Guid.NewGuid(), MetaData = new() { ["__oasis_enc__"] = ciphertext } } };
        if (fault == 4) adapter.Row.MetaData["Rating"] = 99;
        if (fault == 2) settings.Rijndael256Key = new string('w', 32);
        var manager = Manager();
        manager.OASISDNA.OASIS.Security = new SecuritySettings { HolonDataEncryption = settings };
        var result = await manager.ExecuteFederationQueryAsync(FederationPlan<TypedRow>(new[] { adapter }), new[] { adapter });
        if (fault == 0)
        {
            Assert.False(result.IsError, result.Message + result.Exception);
            var row = Assert.IsType<TypedRow>(Assert.Single(result.Result.Rows).Selected.Row);
            Assert.Equal(7, row.Rating);
            Assert.False(row.MetaData.ContainsKey("__oasis_enc__"));
            Assert.False(HolonChangeTracking.HasChanges(row));
        }
        else
        {
            Assert.Equal("HOLON_METADATA_DECRYPTION_FAILED", result.ErrorCode);
            Assert.Null(result.Result);
            Assert.False(result.IsLoaded);
        }
        Assert.Equal(ciphertext, adapter.Row.MetaData["__oasis_enc__"]);
        Assert.Equal(1, adapter.Calls);
    }

    [Fact]
    public async Task FederationTypedSetterFailurePreservesItsRealErrorWithoutPublishingCopies()
    {
        var adapter = new Adapter();
        var result = await Manager().ExecuteFederationQueryAsync(FederationPlan<ThrowingRow>(new[] { adapter }), new[] { adapter });
        Assert.Equal("COSMIC_METADATA_HYDRATION_FAILED", result.ErrorCode);
        Assert.Null(result.Result);
        Assert.False(result.IsLoaded);
        Assert.Equal(7, adapter.Row.MetaData["Rating"]);
    }

    public sealed class ExpandedFederationRow : Holon
    {
        [Newtonsoft.Json.JsonProperty]
        public string Display => new string('x', 50000);
    }

    [Fact]
    public async Task FederationOutputBudgetAppliesAfterTypedMaterialization()
    {
        var adapter = new Adapter();
        var options = CosmicFederationOptions.Create(maximumOutputBytes: 10000).Result;
        var result = await Manager().ExecuteFederationQueryAsync(FederationPlan<ExpandedFederationRow>(new[] { adapter }, options), new[] { adapter });
        Assert.Equal("CQL_FEDERATION_OUTPUT_LIMIT", result.ErrorCode);
        Assert.Null(result.Result);
        Assert.Equal(1, adapter.Calls);
    }

    [Fact]
    public async Task FederationManagerRejectsLegacyModeBeforeAnyRead()
    {
        var adapter = new Adapter();
        var result = await Manager("legacy").ExecuteFederationQueryAsync(FederationPlan<TypedRow>(new[] { adapter }), new[] { adapter });
        Assert.Equal("CQL_V2_REQUIRED", result.ErrorCode);
        Assert.Null(result.Result);
        Assert.Equal(0, adapter.Calls);
    }

    [Fact]
    public async Task RawFederationOutputBudgetIncludesCanonicalRowPayloadNotOnlyProvenance()
    {
        var adapter = new Adapter();
        adapter.Row.MetaData["payload"] = new string('x', 50000);
        var options = CosmicFederationOptions.Create(maximumOutputBytes: 10000).Result;
        var result = await CosmicFederationExecutor.ExecuteAsync(FederationPlan<Holon>(new[] { adapter }, options), new[] { adapter });
        Assert.Equal("CQL_FEDERATION_OUTPUT_LIMIT", result.ErrorCode);
        Assert.Null(result.Result);
        Assert.Equal(1, adapter.Calls);
    }
}
