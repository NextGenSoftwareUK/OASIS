using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests;

public partial class SQLLiteDBOASISIntegrationTests
{
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task EquivalentRawAndDecimalMetadataSaveIsNativeNoOp(bool synchronous)
    {
        const decimal amount = 1.000000000000000000000000001m;
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var manager = CosmicManager(provider, true); var actor = Guid.NewGuid();
        var source = new Holon { MetaData = new() { ["amount"] = amount } };
        var created = await manager.SaveHolonAsync(source, actor, saveChildren: false, providerType: ProviderType.SQLLiteDBOASIS);
        created.IsError.Should().BeFalse(created.Message + created.Exception);
        var loaded = synchronous ? manager.LoadHolon(source.Id, loadChildren: false, providerType: ProviderType.SQLLiteDBOASIS)
            : await manager.LoadHolonAsync(source.Id, loadChildren: false, providerType: ProviderType.SQLLiteDBOASIS);
        loaded.IsError.Should().BeFalse(loaded.Message + loaded.Exception);
        var row = loaded.Result; var revision = Revision(row); var version = row.Version; var logical = row.VersionId;
        row.MetaData["amount"] = 1.0000000000000000000000000010m;
        row.HasHolonChanged().Should().BeFalse(); HolonChangeTracking.PreviewValueChanges(row).Result.Should().BeEmpty();
        var saved = synchronous ? manager.SaveHolon(row, actor, saveChildren: false, providerType: ProviderType.SQLLiteDBOASIS)
            : await manager.SaveHolonAsync(row, actor, saveChildren: false, providerType: ProviderType.SQLLiteDBOASIS);
        saved.IsError.Should().BeFalse(saved.Message + saved.Exception); saved.IsSaved.Should().BeFalse();
        var stored = (await provider.LoadHolonAsync(row.Id, loadChildren: false)).Result;
        Revision(stored).Should().Be(revision); stored.Version.Should().Be(version); stored.VersionId.Should().Be(logical);
    }
}
