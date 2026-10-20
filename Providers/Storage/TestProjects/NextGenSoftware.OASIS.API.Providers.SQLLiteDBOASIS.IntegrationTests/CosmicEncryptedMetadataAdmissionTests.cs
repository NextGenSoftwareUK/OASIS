using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Serialization;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.DNA;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests;

public partial class SQLLiteDBOASISIntegrationTests
{
    [Theory]
    [InlineData(false, 0, false)] [InlineData(true, 0, false)]
    [InlineData(false, 1, false)] [InlineData(true, 1, false)]
    [InlineData(false, 2, false)] [InlineData(true, 2, false)]
    [InlineData(false, 3, false)] [InlineData(true, 3, false)]
    [InlineData(false, 0, true)] [InlineData(false, 1, true)]
    [InlineData(false, 2, true)] [InlineData(false, 3, true)]
    public async Task V2EncryptedMetadataAdmissionRejectsMalformedEnvelopesWithoutChangingStoredValues(bool synchronous, int fault, bool query)
    {
        var settings = new EncryptionSettings { Rijndael256EncryptionEnabled = true, Rijndael256Key = new string('e',32) };
        var payload = fault switch {
            1 => "{\"Status\":\"shadow\"}",
            2 => "{\"private-note\":1,\"private-note\":2}",
            3 => "{\"__oasis_enc__\":\"replacement\"}",
            _ => "{\"private-note\":\"plaintext\"}" };
        var ciphertext = fault == 0 ? payload : PasswordEncryptionHelper.EncryptValue(payload, settings);
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var source = new Holon { Id = Guid.NewGuid(), MetaData = new() { ["Status"] = "clear", ["__oasis_enc__"] = ciphertext } };
        var saved = await provider.SaveHolonAsync(source, saveChildren: false);
        saved.IsError.Should().BeFalse(saved.Message + saved.Exception);
        var before = (await provider.LoadHolonAsync(source.Id, loadChildren: false)).Result;
        var beforeJson = StoredRecordJson.Serialize(before);
        var manager = CosmicManager(provider, true);
        manager.OASISDNA.OASIS.Security = new SecuritySettings { HolonDataEncryption = settings };
        void AssertFailure<T>(OASISResult<T> loaded)
        {
            loaded.IsError.Should().BeTrue(); loaded.IsLoaded.Should().BeFalse(); loaded.Result.Should().BeNull();
            loaded.ErrorCode.Should().Be("HOLON_METADATA_DECRYPTION_FAILED");
        }
        if (query)
        {
            ICosmicQueryStorageProvider adapter = provider; var id = source.Id;
            var expression = CosmicQuery<Holon>.Create().Where(h => h.Id == id).Result;
            var plan = CosmicQueryPlanner.Plan(expression, adapter.QueryProviderType,
                new[] { new CosmicQueryTarget(adapter.QueryProviderType, adapter.QueryCapabilities) });
            plan.IsError.Should().BeFalse(plan.Message);
            AssertFailure(await manager.ExecuteQueryAsync(plan.Result, new[] { adapter }));
        }
        else
            AssertFailure(synchronous ? manager.LoadHolon<Holon>(source.Id, loadChildren: false, providerType: ProviderType.SQLLiteDBOASIS)
                : await manager.LoadHolonAsync<Holon>(source.Id, loadChildren: false, providerType: ProviderType.SQLLiteDBOASIS));
        var after = (await provider.LoadHolonAsync(source.Id, loadChildren: false)).Result;
        StoredRecordJson.Serialize(after).Should().Be(beforeJson);
        after.MetaData["__oasis_enc__"].ToString().Should().Be(ciphertext);
        after.MetaData["Status"].ToString().Should().Be("clear");
    }
    [Fact]
    public async Task TypedCqlDecryptsPersistedCustomValuesBeforeAcceptingTracking()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var manager = CosmicManager(provider, true);
        manager.OASISDNA.OASIS.Security = new SecuritySettings { HolonDataEncryption = new EncryptionSettings {
            Rijndael256EncryptionEnabled = true, Rijndael256Key = new string('e',32) } };
        const decimal price = 1.000000000000000000000000001m;
        var source = new NativeDecimalRow { Price = price };
        var saved = await manager.SaveHolonAsync(source, Guid.NewGuid(), saveChildren: false, providerType: ProviderType.SQLLiteDBOASIS);
        saved.IsError.Should().BeFalse(saved.Message + saved.Exception);
        var stored = (await provider.LoadHolonAsync(source.Id, loadChildren: false)).Result;
        stored.MetaData.Should().ContainKey("__oasis_enc__").And.NotContainKey("Price");
        ICosmicQueryStorageProvider adapter = provider; var id = source.Id;
        var expression = CosmicQuery<NativeDecimalRow>.Create().Where(h => h.Id == id).Result;
        var plan = CosmicQueryPlanner.Plan(expression, adapter.QueryProviderType,
            new[] { new CosmicQueryTarget(adapter.QueryProviderType, adapter.QueryCapabilities) });
        plan.IsError.Should().BeFalse(plan.Message);
        var result = await manager.ExecuteQueryAsync(plan.Result, new[] { adapter });
        result.IsError.Should().BeFalse(result.Message + result.Exception);
        var row = result.Result.Providers.Single().Rows.Single(); row.Price.Should().Be(price);
        row.MetaData.Should().NotContainKey("__oasis_enc__"); HolonChangeTracking.HasChanges(row).Should().BeFalse();
        StoredRecordJson.Serialize((await provider.LoadHolonAsync(source.Id, loadChildren: false)).Result)
            .Should().Be(StoredRecordJson.Serialize(stored));
    }

}
