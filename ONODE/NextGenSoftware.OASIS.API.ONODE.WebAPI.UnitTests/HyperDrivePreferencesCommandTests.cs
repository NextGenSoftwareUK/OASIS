using Moq;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.Synchronization;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.HyperDrive;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.API.DNA;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public sealed class HyperDriveCommandExecutorTests
{
    private static OASISDNA CreateHyperDriveV2Dna()
    {
        var dna = new OASISDNA();
        dna.OASIS.HyperDriveMode = HyperDriveModes.V2;
        return dna;
    }

    [Fact]
    public void ExecutorRequiresExplicitHyperDriveV2Dna()
    {
        var provider = new Mock<IOASISStorageProvider>();

        Assert.Throws<ArgumentNullException>(() => new HyperDriveCommandExecutor(provider.Object, null!));
        var error = Assert.Throws<ArgumentException>(() =>
            new HyperDriveCommandExecutor(provider.Object, new OASISDNA()));
        Assert.Contains(HyperDriveModes.V2, error.Message);
    }

    [Fact]
    public async Task MalformedGeoHotSpotEvidenceIsRejectedBeforeProviderAccess()
    {
        var provider = new Mock<IOASISStorageProvider>();
        var command = new HostedSyncCommandItem
        {
            OperationId = Guid.NewGuid(), AvatarId = Guid.NewGuid(), EntityId = Guid.NewGuid(),
            EntityType = HyperDriveEntityTypes.GeoHotSpot,
            PayloadJson = HyperDriveJson.Serialize(new HyperDriveGeoHotSpotTriggerCommand
            {
                TriggerType = int.MaxValue,
                ObservedAtUtc = DateTime.UtcNow
            })
        };

        var outcome = await new HyperDriveCommandExecutor(provider.Object, CreateHyperDriveV2Dna())
            .ExecuteAsync(command, default);

        Assert.False(outcome.Succeeded);
        Assert.Equal("GEOHOTSPOT_TRIGGER_EVIDENCE_INVALID", outcome.Code);
        Assert.Contains("TriggerType is invalid", outcome.Message);
        Assert.Empty(provider.Invocations);
    }

    [Fact]
    public async Task ReplayingSameOperationUsesPersistedReceiptAndDoesNotSaveAgain()
    {
        IHolon? stored = new Holon { Id = Guid.NewGuid(), MetaData = new Dictionary<string, object>() };
        var provider = new Mock<IOASISStorageProvider>();
        provider.SetupGet(x => x.ProviderType).Returns(new EnumValue<ProviderType>(ProviderType.LocalFileOASIS));
        provider.SetupGet(x => x.ProviderName).Returns("preferences-test-provider");
        provider.SetupGet(x => x.IsProviderActivated).Returns(true);
        provider.Setup(x => x.ActivateProvider())
            .Returns(new OASISResult<bool>(true));
        provider.Setup(x => x.LoadHolonAsync(It.IsAny<Guid>(), true, true, 0, true, false, 0))
            .ReturnsAsync(() => new OASISResult<IHolon> { Result = stored, IsError = false });
        provider.Setup(x => x.SaveHolonAsync(It.IsAny<IHolon>(), true, true, 0, true, false))
            .ReturnsAsync((IHolon holon, bool _, bool _, int _, bool _, bool _) =>
            {
                stored = holon;
                return new OASISResult<IHolon>(holon) { IsSaved = true };
            });
        var avatarId = Guid.NewGuid();
        var operationId = Guid.NewGuid();
        var command = new HostedSyncCommandItem
        {
            OperationId = operationId, AvatarId = avatarId, EntityId = avatarId,
            EntityType = HyperDriveEntityTypes.AvatarPreferences,
            PayloadJson = HyperDriveJson.Serialize(new HyperDriveAvatarPreferences
            { MasterVolume = 0.4f, GraphicsPreset = "Ultra", UiHighContrast = true })
        };
        var executor = new HyperDriveCommandExecutor(provider.Object, CreateHyperDriveV2Dna());

        var first = await executor.ExecuteAsync(command, default);
        int writesAfterFirstExecution = provider.Invocations.Count(x => x.Method.Name == nameof(IOASISStorageProvider.SaveHolonAsync));
        var replay = await executor.ExecuteAsync(command, default);

        Assert.True(first.Succeeded, first.Message);
        Assert.True(replay.Succeeded, replay.Message);
        Assert.Equal(writesAfterFirstExecution,
            provider.Invocations.Count(x => x.Method.Name == nameof(IOASISStorageProvider.SaveHolonAsync)));
        Assert.Equal(operationId.ToString("D"), stored!.MetaData["HyperDrive.PreferencesOperationId.v1"]);
    }

    [Fact]
    public async Task PreferencesForAnotherAvatarAreRejectedBeforeProviderAccess()
    {
        var provider = new Mock<IOASISStorageProvider>();
        var command = new HostedSyncCommandItem
        {
            OperationId = Guid.NewGuid(), AvatarId = Guid.NewGuid(), EntityId = Guid.NewGuid(),
            EntityType = HyperDriveEntityTypes.AvatarPreferences,
            PayloadJson = HyperDriveJson.Serialize(new HyperDriveAvatarPreferences())
        };

        var outcome = await new HyperDriveCommandExecutor(provider.Object, CreateHyperDriveV2Dna())
            .ExecuteAsync(command, default);

        Assert.False(outcome.Succeeded);
        Assert.Equal("PREFERENCES_AVATAR_MISMATCH", outcome.Code);
        Assert.Empty(provider.Invocations);
    }
}
