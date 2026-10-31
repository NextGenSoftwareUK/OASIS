using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.CustomAttrbiutes;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Serialization;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests;

public partial class SQLLiteDBOASISIntegrationTests
{
    public sealed class NativeDecimalRow : Holon
    { [CustomOASISProperty] public decimal Price { get; set; } }

    [Theory]
    [InlineData(false, false)] [InlineData(true, false)] [InlineData(false, true)] [InlineData(true, true)]
    public async Task V2DecimalEditsAreSavedExactlyAndNativeReloadStartsClean(bool synchronous, bool encrypted)
    {
        const decimal before = 1.000000000000000000000000001m;
        const decimal after = 1.000000000000000000000000002m;
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var manager = CosmicManager(provider, true); var actor = Guid.NewGuid();
        if (encrypted) manager.OASISDNA.OASIS.Security = new NextGenSoftware.OASIS.API.DNA.SecuritySettings {
            HolonDataEncryption = new NextGenSoftware.OASIS.API.DNA.EncryptionSettings {
                Rijndael256EncryptionEnabled = true, Rijndael256Key = new string('p', 32) } };
        var source = new NativeDecimalRow { Price = before, MetaData = new() { ["amount"] = before } };
        var created = synchronous ? manager.SaveHolon(source, actor, saveChildren: false, providerType: ProviderType.SQLLiteDBOASIS)
            : await manager.SaveHolonAsync(source, actor, saveChildren: false, providerType: ProviderType.SQLLiteDBOASIS);
        created.IsError.Should().BeFalse(created.Message + created.Exception);
        var loaded = synchronous ? manager.LoadHolon<NativeDecimalRow>(source.Id, loadChildren: false, providerType: ProviderType.SQLLiteDBOASIS)
            : await manager.LoadHolonAsync<NativeDecimalRow>(source.Id, loadChildren: false, providerType: ProviderType.SQLLiteDBOASIS);
        loaded.IsError.Should().BeFalse(loaded.Message + loaded.Exception);
        var row = loaded.Result; row.Price.Should().Be(before); row.HasHolonChanged().Should().BeFalse();
        row.Price = after; row.MetaData["amount"] = after;
        row.HasHolonChanged().Should().BeTrue();
        var preview = HolonChangeTracking.PreviewValueChanges(row); preview.IsError.Should().BeFalse(preview.Message);
        var price = preview.Result.Single(d => d.Path == "/Price");
        price.PreviousJson.Should().Be(before.ToString(System.Globalization.CultureInfo.InvariantCulture));
        price.CurrentJson.Should().Be(after.ToString(System.Globalization.CultureInfo.InvariantCulture));
        var saved = synchronous ? manager.SaveHolon(row, actor, saveChildren: false, providerType: ProviderType.SQLLiteDBOASIS)
            : await manager.SaveHolonAsync(row, actor, saveChildren: false, providerType: ProviderType.SQLLiteDBOASIS);
        saved.IsError.Should().BeFalse(saved.Message + saved.Exception); saved.IsSaved.Should().BeTrue();
        var reloaded = await manager.LoadHolonAsync<NativeDecimalRow>(row.Id, loadChildren: false, providerType: ProviderType.SQLLiteDBOASIS);
        reloaded.IsError.Should().BeFalse(reloaded.Message + reloaded.Exception); reloaded.Result.Price.Should().Be(after);
        StoredRecordJson.Serialize(reloaded.Result.MetaData).Should().Contain(after.ToString(System.Globalization.CultureInfo.InvariantCulture));
        HolonChangeTracking.PreviewValueChanges(reloaded.Result).Result.Should().BeEmpty();
    }
}
