using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests;

public partial class SQLLiteDBOASISIntegrationTests
{
    [Theory]
    [InlineData(false, false)] [InlineData(true, false)]
    [InlineData(false, true)] [InlineData(true, true)]
    public async Task EncryptedConditionalCreateReplaysOriginalReceiptAcrossFreshManagers(bool synchronous, bool encrypted)
    {
        var operation = Guid.NewGuid(); var actor = Guid.NewGuid();
        Holon Input() => new() { Name = "encrypted retry", MetaData = new() { ["private-note"] = "same logical intent" } };
        Guid id; Guid version; Guid revision;
        using (var provider = new SQLLiteDBOASIS(TestConnectionString))
        {
            (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
            var manager = CosmicManager(provider, true);
            if (encrypted) manager.MetadataEncryptionPolicy = NativeMetadataPolicy(new NativeMetadataResolver());
            var source = Input();
            var first = synchronous
                ? manager.SaveHolon(source, actor, new CosmicWriteOptions(operation), saveChildren: false, providerType: ProviderType.SQLLiteDBOASIS)
                : await manager.SaveHolonAsync(source, actor, new CosmicWriteOptions(operation), saveChildren: false, providerType: ProviderType.SQLLiteDBOASIS);
            first.IsError.Should().BeFalse(first.Message + first.Exception);
            id = source.Id; version = source.VersionId; revision = Revision(source);
        }
        using var restarted = new SQLLiteDBOASIS(TestConnectionString);
        (await restarted.ActivateProviderAsync()).IsError.Should().BeFalse();
        var retryManager = CosmicManager(restarted, true);
        if (encrypted) retryManager.MetadataEncryptionPolicy = NativeMetadataPolicy(new NativeMetadataResolver());
        var retrySource = Input();
        var replay = synchronous
            ? retryManager.SaveHolon(retrySource, actor, new CosmicWriteOptions(operation), saveChildren: false, providerType: ProviderType.SQLLiteDBOASIS)
            : await retryManager.SaveHolonAsync(retrySource, actor, new CosmicWriteOptions(operation), saveChildren: false, providerType: ProviderType.SQLLiteDBOASIS);
        replay.IsError.Should().BeFalse(replay.ErrorCode + ": " + replay.Message + replay.Exception);
        retrySource.Id.Should().Be(id); retrySource.VersionId.Should().Be(version); Revision(retrySource).Should().Be(revision);
        retrySource.MetaData["private-note"].ToString().Should().Be("same logical intent");
        var current = (await restarted.LoadHolonAsync(id, loadChildren: false)).Result;
        current.VersionId.Should().Be(version); Revision(current).Should().Be(revision);
    }
}
