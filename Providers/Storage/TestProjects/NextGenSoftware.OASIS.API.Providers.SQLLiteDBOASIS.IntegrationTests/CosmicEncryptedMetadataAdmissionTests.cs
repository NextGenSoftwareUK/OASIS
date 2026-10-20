using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Serialization;
using NextGenSoftware.OASIS.API.DNA;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests;

public partial class SQLLiteDBOASISIntegrationTests
{
    [Theory]
    [InlineData(false, 0)] [InlineData(true, 0)]
    [InlineData(false, 1)] [InlineData(true, 1)]
    [InlineData(false, 2)] [InlineData(true, 2)]
    [InlineData(false, 3)] [InlineData(true, 3)]
    public async Task V2EncryptedMetadataAdmissionRejectsMalformedEnvelopesWithoutChangingStoredValues(bool synchronous, int fault)
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
        var loaded = synchronous ? manager.LoadHolon<Holon>(source.Id, loadChildren: false, providerType: ProviderType.SQLLiteDBOASIS)
            : await manager.LoadHolonAsync<Holon>(source.Id, loadChildren: false, providerType: ProviderType.SQLLiteDBOASIS);
        loaded.IsError.Should().BeTrue(); loaded.Result.Should().BeNull();
        loaded.ErrorCode.Should().Be("HOLON_METADATA_DECRYPTION_FAILED");
        var after = (await provider.LoadHolonAsync(source.Id, loadChildren: false)).Result;
        StoredRecordJson.Serialize(after).Should().Be(beforeJson);
        after.MetaData["__oasis_enc__"].ToString().Should().Be(ciphertext);
        after.MetaData["Status"].ToString().Should().Be("clear");
    }
}
