using FluentAssertions;
using Moq;
using System.IdentityModel.Tokens.Jwt;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using Xunit;
using Newtonsoft.Json.Linq;

namespace NextGenSoftware.OASIS.API.Core.UnitTests.HyperDrive;

public sealed class HyperDriveProviderExecutionTests
{
    [Fact]
    public void AvatarJwtIssuanceUsesTheManagersRuntimeDna()
    {
        var dna = CreateDna(HyperDriveModes.V2);
        dna.OASIS.Security = new SecuritySettings
        {
            SecretKey = new string('e', 64),
            JwtTokenExpirationMinutes = 21,
            Oidc = new OidcSettings { Issuer = "edge-runtime" }
        };
        var providerManager = new ProviderManager(null, dna);
        var manager = new AvatarManager(null, dna, providerManager);
        var avatar = new Avatar { Id = Guid.NewGuid(), Email = "edge@example.test", FirstName = "Edge" };

        var token = manager.GenerateJWTToken(avatar);
        var parsed = new JwtSecurityTokenHandler().ReadJwtToken(token);

        parsed.Issuer.Should().Be("edge-runtime");
        manager.OASISDNA.Should().BeSameAs(dna);
        providerManager.OASISDNA.Should().BeSameAs(dna);
    }

    [Fact]
    public void IsolatedAvatarRuntimeFailsClosedWhenItsJwtKeyIsMissing()
    {
        var dna = CreateDna(HyperDriveModes.V2);
        dna.OASIS.Security = new SecuritySettings();
        var providerManager = new ProviderManager(null, dna);
        var manager = new AvatarManager(null, dna, providerManager);

        var action = () => manager.GenerateJWTToken(new Avatar { Id = Guid.NewGuid() });

        action.Should().Throw<ArgumentNullException>()
            .WithMessage("*isolated AvatarManager runtime must be configured with its own JWT signing key*");
        manager.OASISDNA.Should().BeSameAs(dna);
        providerManager.OASISDNA.Should().BeSameAs(dna);
    }

    [Fact]
    public void V2ManagerProviderConfigurationDoesNotReplaceTheCurrentProvider()
    {
        var current = CreateActiveProvider(ProviderType.MongoDBOASIS, "current-mongo");
        var candidate = CreateActiveProvider(ProviderType.SQLLiteDBOASIS, "candidate-sqlite");
        current.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        var providerManager = new ProviderManager(null, new OASISDNA
        {
            OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS
            {
                StorageProviders = new StorageProviderSettings { LogSwitchingProviders = false }
            }
        });
        providerManager.RegisterProvider(current.Object);
        var currentResult = providerManager.SetAndActivateCurrentStorageProvider(current.Object);
        currentResult.IsError.Should().BeFalse(currentResult.Message);

        OASISManager.ConfigureStorageProvider(providerManager, candidate.Object, useHyperDriveV2: true);

        providerManager.CurrentStorageProvider.Should().BeSameAs(current.Object);
        providerManager.CurrentStorageProviderType.Value.Should().Be(ProviderType.MongoDBOASIS);
        providerManager.IsProviderRegistered(ProviderType.SQLLiteDBOASIS).Should().BeTrue();
        candidate.Object.IsProviderActivated.Should().BeTrue();
    }

    [Fact]
    public void InjectedManagerRegistersItsProviderOnlyInItsOwnRuntime()
    {
        var dna = CreateDna(HyperDriveModes.V2);
        var isolated = new ProviderManager(null, dna);
        var provider = CreateActiveProvider(ProviderType.IPFSOASIS, "isolated-ipfs");
        var singletonProviderBefore = ProviderManager.Instance.GetStorageProvider(ProviderType.IPFSOASIS);

        var manager = new HolonManager(provider.Object, dna, isolated);

        manager.OASISDNA.Should().BeSameAs(dna);
        isolated.GetStorageProvider(ProviderType.IPFSOASIS).Should().BeSameAs(provider.Object);
        ProviderManager.Instance.GetStorageProvider(ProviderType.IPFSOASIS).Should().BeSameAs(singletonProviderBefore);
    }

    [Fact]
    public async Task V2ManagerUsesItsInjectedProviderWhenTheOperationProviderIsDefault()
    {
        var dna = CreateDna(HyperDriveModes.V2);
        var providerManager = new ProviderManager(null, dna);
        var current = CreateActiveProvider(ProviderType.MongoDBOASIS, "current-mongo");
        var injected = CreateActiveProvider(ProviderType.IPFSOASIS, "injected-ipfs");
        var holonId = Guid.NewGuid();
        var holon = new Mock<IHolon>().Object;
        current.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        injected.Setup(x => x.LoadHolonAsync(holonId, true, true, 0, true, false, 0))
            .ReturnsAsync(new OASISResult<IHolon> { Result = holon });
        providerManager.RegisterProvider(current.Object);
        providerManager.SetAndActivateCurrentStorageProvider(current.Object).IsError.Should().BeFalse();

        var manager = new HolonManager(injected.Object, dna, providerManager);
        var result = await manager.LoadHolonAsync(holonId);

        result.IsError.Should().BeFalse(result.Message);
        result.Result.Should().BeSameAs(holon);
        injected.Verify(x => x.LoadHolonAsync(holonId, true, true, 0, true, false, 0), Times.Once);
        current.Verify(x => x.LoadHolonAsync(It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<bool>(),
            It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<int>()), Times.Never);
        providerManager.CurrentStorageProviderType.Value.Should().Be(ProviderType.MongoDBOASIS);
    }

    [Fact]
    public void LegacyAvatarKarmaUsesOnlyTheManagersInjectedRuntime()
    {
        var avatar = new AvatarDetail { Id = Guid.NewGuid() };
        var record = new KarmaAkashicRecord();
        var dna = CreateDna(HyperDriveModes.Legacy);
        var providerManager = new ProviderManager(null, dna);
        var provider = CreateActiveProvider(ProviderType.SQLLiteDBOASIS, "isolated-karma-sqlite");
        provider.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        provider.Setup(x => x.AddKarmaToAvatar(
                avatar, KarmaTypePositive.HelpOtherPerson, KarmaSourceType.Game,
                "quest", "helped another player", null))
            .Returns(new OASISResult<KarmaAkashicRecord>(record));
        providerManager.RegisterProvider(provider.Object).Should().BeTrue();
        var singletonBefore = ProviderManager.Instance.CurrentStorageProvider;
        var manager = new AvatarManager(null, dna, providerManager);

        var result = manager.AddKarmaToAvatar(
            avatar, KarmaTypePositive.HelpOtherPerson, KarmaSourceType.Game,
            "quest", "helped another player", providerType: ProviderType.SQLLiteDBOASIS);

        result.IsError.Should().BeFalse(result.Message);
        result.Result.Should().BeSameAs(record);
        providerManager.CurrentStorageProvider.Should().BeSameAs(provider.Object);
        ProviderManager.Instance.CurrentStorageProvider.Should().BeSameAs(singletonBefore);
        provider.VerifyAll();
    }

    [Fact]
    public async Task AvatarDetailLegacyReplicationCompletesInsideTheOperationAndUsesInjectedRuntime()
    {
        var avatar = new AvatarDetail { Id = Guid.NewGuid(), Username = "edge-player" };
        var current = CreateActiveProvider(ProviderType.MongoDBOASIS, "isolated-current-mongo");
        var replica = CreateActiveProvider(ProviderType.SQLLiteDBOASIS, "isolated-replica-sqlite");
        current.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        current.Setup(x => x.SaveAvatarDetailAsync(avatar))
            .ReturnsAsync(new OASISResult<IAvatarDetail>(avatar) { IsSaved = true });
        replica.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        replica.Setup(x => x.ActivateProviderAsync()).ReturnsAsync(new OASISResult<bool>(true));
        replica.Setup(x => x.SaveAvatarDetailAsync(avatar))
            .ReturnsAsync(new OASISResult<IAvatarDetail>(avatar) { IsSaved = true });
        var dna = CreateDna(HyperDriveModes.Legacy);
        var providerManager = new ProviderManager(null, dna);
        providerManager.RegisterProvider(current.Object).Should().BeTrue();
        providerManager.RegisterProvider(replica.Object).Should().BeTrue();
        providerManager.SetAndActivateCurrentStorageProvider(current.Object).IsError.Should().BeFalse();
        providerManager.SetAutoReplicationForProviders(true,
            new[] { ProviderType.SQLLiteDBOASIS }).Should().BeTrue();
        providerManager.IsAutoLoadBalanceEnabled = false;
        var singletonBefore = ProviderManager.Instance.CurrentStorageProvider;
        var manager = new AvatarManager(null, dna, providerManager);
        var result = await manager.SaveAvatarDetailAsync(avatar, AutoReplicationMode.True,
            waitForAutoReplicationResult: false, providerType: ProviderType.MongoDBOASIS);

        result.IsError.Should().BeFalse(result.Message);
        ProviderManager.Instance.CurrentStorageProvider.Should().BeSameAs(singletonBefore);
        current.Verify(x => x.SaveAvatarDetailAsync(avatar), Times.Once);
        replica.Verify(x => x.SaveAvatarDetailAsync(avatar), Times.Once);
    }

    [Fact]
    public async Task HolonLegacyReplicationUsesInjectedRuntimeForPolicyAndProviders()
    {
        var avatarId = Guid.NewGuid();
        var holon = new Holon { Id = Guid.NewGuid(), Name = "replicated-edge-holon" };
        var primary = CreateActiveProvider(ProviderType.MongoDBOASIS, "isolated-holon-primary");
        var replica = CreateActiveProvider(ProviderType.SQLLiteDBOASIS, "isolated-holon-replica");
        primary.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        primary.Setup(x => x.ActivateProviderAsync()).ReturnsAsync(new OASISResult<bool>(true));
        replica.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        replica.Setup(x => x.ActivateProviderAsync()).ReturnsAsync(new OASISResult<bool>(true));
        primary.Setup(x => x.SaveHolonAsync(It.IsAny<IHolon>(), true, true, 0, true, false))
            .ReturnsAsync((IHolon value, bool _, bool _, int _, bool _, bool _) =>
                new OASISResult<IHolon>(value) { IsSaved = true });
        replica.Setup(x => x.SaveHolonAsync(It.IsAny<IHolon>(), true, true, 0, true, false))
            .ReturnsAsync((IHolon value, bool _, bool _, int _, bool _, bool _) =>
                new OASISResult<IHolon>(value) { IsSaved = true });
        var dna = CreateDna(HyperDriveModes.Legacy);
        var providerManager = new ProviderManager(null, dna);
        providerManager.RegisterProvider(primary.Object).Should().BeTrue();
        providerManager.RegisterProvider(replica.Object).Should().BeTrue();
        providerManager.SetAndActivateCurrentStorageProvider(primary.Object).IsError.Should().BeFalse();
        providerManager.SetAutoReplicationForProviders(true,
            new[] { ProviderType.SQLLiteDBOASIS }).Should().BeTrue();
        providerManager.IsAutoLoadBalanceEnabled = false;
        var singletonBefore = ProviderManager.Instance.CurrentStorageProvider;

        var result = await new HolonManager(null, dna, providerManager).SaveHolonAsync(
            holon, avatarId, providerType: ProviderType.MongoDBOASIS);

        result.IsError.Should().BeFalse(result.Message);
        ProviderManager.Instance.CurrentStorageProvider.Should().BeSameAs(singletonBefore);
        primary.Verify(x => x.SaveHolonAsync(It.IsAny<IHolon>(), true, true, 0, true, false), Times.Once);
        replica.Verify(x => x.SaveHolonAsync(It.IsAny<IHolon>(), true, true, 0, true, false), Times.Once);
    }

    [Fact]
    public void HolonLegacyHardDeleteReplicatesToTheConfiguredInjectedProvider()
    {
        var avatarId = Guid.NewGuid();
        var holon = new Holon { Id = Guid.NewGuid(), Name = "deleted-edge-holon" };
        var primary = CreateActiveProvider(ProviderType.MongoDBOASIS, "isolated-delete-primary");
        var replica = CreateActiveProvider(ProviderType.SQLLiteDBOASIS, "isolated-delete-replica");
        primary.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        replica.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        primary.Setup(x => x.DeleteHolon(holon.Id)).Returns(new OASISResult<IHolon>(holon));
        replica.Setup(x => x.DeleteHolon(holon.Id)).Returns(new OASISResult<IHolon>(holon));
        var dna = CreateDna(HyperDriveModes.Legacy);
        var providerManager = new ProviderManager(null, dna);
        providerManager.RegisterProvider(primary.Object).Should().BeTrue();
        providerManager.RegisterProvider(replica.Object).Should().BeTrue();
        providerManager.SetAndActivateCurrentStorageProvider(primary.Object).IsError.Should().BeFalse();
        providerManager.SetAutoReplicationForProviders(true,
            new[] { ProviderType.SQLLiteDBOASIS }).Should().BeTrue();
        var singletonBefore = ProviderManager.Instance.CurrentStorageProvider;

        var result = new HolonManager(null, dna, providerManager).DeleteHolon(
            holon.Id, avatarId, softDelete: false, providerType: ProviderType.MongoDBOASIS);

        result.IsError.Should().BeFalse(result.Message);
        ProviderManager.Instance.CurrentStorageProvider.Should().BeSameAs(singletonBefore);
        primary.Verify(x => x.DeleteHolon(holon.Id), Times.Once);
        replica.Verify(x => x.DeleteHolon(holon.Id), Times.Once);
    }

    [Fact]
    public async Task FilesManagerUsesItsInjectedRuntimeWithoutTouchingTheSingleton()
    {
        var avatarId = Guid.NewGuid();
        var dna = CreateDna(HyperDriveModes.V2);
        var providerManager = new ProviderManager(null, dna);
        var provider = CreateActiveProvider(ProviderType.IPFSOASIS, "isolated-files");
        provider.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        provider.Setup(x => x.SaveHolonAsync(It.IsAny<IHolon>(), true, true, 0, true, false))
            .ReturnsAsync((IHolon holon, bool _, bool _, int _, bool _, bool _) =>
                new OASISResult<IHolon>(holon) { IsSaved = true });
        providerManager.RegisterProvider(provider.Object);
        var selected = providerManager.SetAndActivateCurrentStorageProvider(provider.Object);
        selected.IsError.Should().BeFalse(selected.Message);
        var singletonBefore = ProviderManager.Instance.CurrentStorageProvider;
        var manager = new FilesManager(null, dna, providerManager);

        var result = await manager.UploadFileAsync(avatarId, "offline.dat", new byte[] { 1, 2, 3 },
            "application/octet-stream");

        result.IsError.Should().BeFalse(result.Message);
        result.Result.Should().NotBeNull();
        result.Result.AvatarId.Should().Be(avatarId);
        ProviderManager.Instance.CurrentStorageProvider.Should().BeSameAs(singletonBefore);
        provider.Verify(x => x.SaveHolonAsync(It.IsAny<IHolon>(), true, true, 0, true, false), Times.Once);
    }

    [Fact]
    public async Task SettingsManagerReadsThroughItsInjectedRuntimeWithoutTouchingTheSingleton()
    {
        var avatarId = Guid.NewGuid();
        var avatar = new Avatar
        {
            Id = avatarId,
            MetaData = new Dictionary<string, object>
            {
                ["preferences"] = new Dictionary<string, object>
                {
                    ["system"] = new Dictionary<string, object> { ["theme"] = "edge-neon" }
                }
            }
        };
        var dna = CreateDna(HyperDriveModes.V2);
        var providerManager = new ProviderManager(null, dna);
        var provider = CreateActiveProvider(ProviderType.IPFSOASIS, "isolated-settings");
        provider.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        provider.Setup(x => x.LoadAvatarAsync(avatarId, 0)).ReturnsAsync(new OASISResult<IAvatar>(avatar));
        providerManager.RegisterProvider(provider.Object);
        var selected = providerManager.SetAndActivateCurrentStorageProvider(provider.Object);
        selected.IsError.Should().BeFalse(selected.Message);
        var singletonBefore = ProviderManager.Instance.CurrentStorageProvider;
        var manager = new SettingsManager(null, dna, providerManager);

        var result = await manager.GetSystemSettingsAsync(avatarId);

        result.IsError.Should().BeFalse(result.Message);
        result.Result["theme"].Should().Be("edge-neon");
        ProviderManager.Instance.CurrentStorageProvider.Should().BeSameAs(singletonBefore);
        provider.Verify(x => x.LoadAvatarAsync(avatarId, 0), Times.Once);
    }

    [Fact]
    public async Task SettingsManagerDoesNotReportSuccessWhenItsInjectedProviderRejectsTheSave()
    {
        var avatarId = Guid.NewGuid();
        var avatar = new Avatar { Id = avatarId, MetaData = new Dictionary<string, object>() };
        var dna = CreateDna(HyperDriveModes.V2);
        var providerManager = new ProviderManager(null, dna);
        var provider = CreateActiveProvider(ProviderType.IPFSOASIS, "isolated-settings-failure");
        provider.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        provider.Setup(x => x.LoadAvatarAsync(avatarId, 0)).ReturnsAsync(new OASISResult<IAvatar>(avatar));
        provider.Setup(x => x.SaveAvatarAsync(avatar)).ReturnsAsync(new OASISResult<IAvatar>
        {
            IsError = true, ErrorCount = 1, ErrorCode = "PROVIDER_WRITE_REJECTED", Message = "write rejected"
        });
        providerManager.RegisterProvider(provider.Object);
        var selected = providerManager.SetAndActivateCurrentStorageProvider(provider.Object);
        selected.IsError.Should().BeFalse(selected.Message);
        var manager = new SettingsManager(null, dna, providerManager);

        var result = await manager.UpdateSystemSettingsAsync(avatarId,
            new Dictionary<string, object> { ["theme"] = "edge-neon" });

        result.IsError.Should().BeTrue();
        result.Result.Should().BeFalse();
        result.Message.Should().Contain("failed");
        provider.Verify(x => x.SaveAvatarAsync(avatar), Times.Once);
        provider.Verify(x => x.SaveAvatar(It.IsAny<IAvatar>()), Times.Never);
        provider.Verify(x => x.LoadAvatar(It.IsAny<Guid>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task SettingsManagerDoesNotReplaceAProviderReadFailureWithDefaultSettings()
    {
        var avatarId = Guid.NewGuid();
        var dna = CreateDna(HyperDriveModes.V2);
        var providerManager = new ProviderManager(null, dna);
        var provider = CreateActiveProvider(ProviderType.IPFSOASIS, "isolated-settings-read-failure");
        provider.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        provider.Setup(x => x.LoadAvatarAsync(avatarId, 0)).ReturnsAsync(new OASISResult<IAvatar>
        {
            IsError = true,
            ErrorCount = 1,
            ErrorCode = "PROVIDER_UNAVAILABLE",
            Message = "provider unavailable"
        });
        providerManager.RegisterProvider(provider.Object);
        var selected = providerManager.SetAndActivateCurrentStorageProvider(provider.Object);
        selected.IsError.Should().BeFalse(selected.Message);
        var manager = new SettingsManager(null, dna, providerManager);

        var result = await manager.GetSystemSettingsAsync(avatarId);

        result.IsError.Should().BeTrue();
        result.Result.Should().BeNull();
        result.Message.Should().Contain("failed");
        provider.Verify(x => x.LoadAvatarAsync(avatarId, 0), Times.Once);
        provider.Verify(x => x.LoadAvatar(It.IsAny<Guid>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task ClanManagerLoadsThroughItsInjectedRuntimeWithoutTouchingTheSingleton()
    {
        var clanId = Guid.NewGuid();
        var clan = new Clan { Id = clanId, Name = "Edge Clan", OwnerAvatarId = Guid.NewGuid() };
        var dna = CreateDna(HyperDriveModes.V2);
        var providerManager = new ProviderManager(null, dna);
        var provider = CreateActiveProvider(ProviderType.IPFSOASIS, "isolated-clans");
        provider.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        provider.Setup(x => x.LoadHolonAsync(clanId, false, false, 0, true, false, 0))
            .ReturnsAsync(new OASISResult<IHolon>(clan));
        providerManager.RegisterProvider(provider.Object);
        var selected = providerManager.SetAndActivateCurrentStorageProvider(provider.Object);
        selected.IsError.Should().BeFalse(selected.Message);
        var singletonBefore = ProviderManager.Instance.CurrentStorageProvider;
        var manager = new ClanManager(providerManager);

        var result = await manager.LoadClanAsync(clanId);

        result.IsError.Should().BeFalse(result.Message);
        result.Result.Should().BeSameAs(clan);
        ProviderManager.Instance.CurrentStorageProvider.Should().BeSameAs(singletonBefore);
        provider.Verify(x => x.LoadHolonAsync(clanId, false, false, 0, true, false, 0), Times.Once);
    }

    [Fact]
    public async Task MessagingManagerDoesNotTreatAnUnavailableProviderAsAnEmptyInbox()
    {
        var avatarId = Guid.NewGuid();
        var dna = CreateDna(HyperDriveModes.V2);
        var providerManager = new ProviderManager(null, dna);
        var provider = CreateActiveProvider(ProviderType.IPFSOASIS, "isolated-messaging-read-failure");
        provider.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        provider.Setup(x => x.LoadHolonAsync(It.IsAny<Guid>(), true, true, 0, true, false, 0))
            .ReturnsAsync(new OASISResult<IHolon> { IsError = true, Message = "provider unavailable" });
        provider.Setup(x => x.SaveHolonAsync(It.IsAny<IHolon>(), true, true, 0, true, false))
            .ReturnsAsync(new OASISResult<IHolon> { IsError = true, Message = "provider unavailable" });
        providerManager.RegisterProvider(provider.Object);
        providerManager.SetAndActivateCurrentStorageProvider(provider.Object).IsError.Should().BeFalse();
        var singletonBefore = ProviderManager.Instance.CurrentStorageProvider;
        var manager = new MessagingManager(null, dna, providerManager);

        var result = await manager.GetMessagesAsync(avatarId);

        result.IsError.Should().BeTrue();
        result.Result.Should().BeNull();
        result.Message.Should().Contain("Could not load messages");
        ProviderManager.Instance.CurrentStorageProvider.Should().BeSameAs(singletonBefore);
    }

    [Fact]
    public async Task MessagingManagerDoesNotReportASentMessageWhenPersistenceFails()
    {
        var dna = CreateDna(HyperDriveModes.V2);
        var providerManager = new ProviderManager(null, dna);
        var provider = CreateActiveProvider(ProviderType.IPFSOASIS, "isolated-messaging-write-failure");
        provider.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        provider.Setup(x => x.LoadHolonAsync(It.IsAny<Guid>(), true, true, 0, true, false, 0))
            .ReturnsAsync(() => new OASISResult<IHolon>(new Holon { MetaData = new Dictionary<string, object>() }));
        provider.Setup(x => x.SaveHolonAsync(It.IsAny<IHolon>(), true, true, 0, true, false))
            .ReturnsAsync(new OASISResult<IHolon> { IsError = true, Message = "write rejected" });
        providerManager.RegisterProvider(provider.Object);
        providerManager.SetAndActivateCurrentStorageProvider(provider.Object).IsError.Should().BeFalse();
        var manager = new MessagingManager(null, dna, providerManager);

        var result = await manager.SendMessageToAvatarAsync(Guid.NewGuid(), Guid.NewGuid(), "durable message");

        result.IsError.Should().BeTrue();
        result.Result.Should().BeFalse();
        result.Message.Should().Contain("could not be persisted");
        provider.Verify(x => x.SaveHolonAsync(It.IsAny<IHolon>(), true, true, 0, true, false), Times.Once);
    }

    [Fact]
    public async Task MessageAndReadStateSurviveManagerRestartAsOneCanonicalRecord()
    {
        var senderId = Guid.NewGuid();
        var recipientId = Guid.NewGuid();
        var stored = new Dictionary<Guid, IHolon>();
        var dna = CreateDna(HyperDriveModes.V2);
        var providerManager = new ProviderManager(null, dna);
        var provider = CreateActiveProvider(ProviderType.IPFSOASIS, "isolated-message-durable");
        provider.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        provider.Setup(x => x.LoadHolonAsync(It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<bool>(),
                It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<int>()))
            .ReturnsAsync((Guid id, bool _, bool _, int _, bool _, bool _, int _) =>
                new OASISResult<IHolon> { Result = stored.TryGetValue(id, out var holon) ? holon : null! });
        provider.Setup(x => x.SaveHolonAsync(It.IsAny<IHolon>(), true, true, 0, true, false))
            .ReturnsAsync((IHolon holon, bool _, bool _, int _, bool _, bool _) =>
            {
                stored[holon.Id] = holon;
                return new OASISResult<IHolon>(holon) { IsSaved = true };
            });
        provider.Setup(x => x.LoadHolonsByMetaDataAsync(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<HolonType>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<int>(),
                It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<int>()))
            .ReturnsAsync((string key, string value, HolonType _, bool _, bool _, int _, int _, bool _, bool _, int _) =>
                new OASISResult<IEnumerable<IHolon>>(stored.Values.Where(h => h.MetaData != null &&
                    h.MetaData.TryGetValue(key, out var indexed) && indexed?.ToString() == value).ToList()));
        providerManager.RegisterProvider(provider.Object).Should().BeTrue();
        providerManager.SetAndActivateCurrentStorageProvider(provider.Object).IsError.Should().BeFalse();

        var send = await new MessagingManager(null, dna, providerManager)
            .SendMessageToAvatarAsync(senderId, recipientId, "restart-safe message");
        var recipientMessages = await new MessagingManager(null, dna, providerManager).GetMessagesAsync(recipientId);
        var messageId = recipientMessages.Result.Single().Id;
        var markRead = await new MessagingManager(null, dna, providerManager)
            .MarkMessagesAsReadAsync(recipientId, new List<Guid> { messageId });
        var senderMessages = await new MessagingManager(null, dna, providerManager).GetMessagesAsync(senderId);

        send.IsError.Should().BeFalse(send.Message);
        recipientMessages.Result.Should().ContainSingle(x => x.Id == messageId);
        markRead.Result.Should().BeTrue(markRead.Message);
        senderMessages.Result.Should().ContainSingle(x => x.Id == messageId && x.IsRead && x.ReadAt.HasValue);
        stored.Values.Count(x => x.MetaData != null &&
            x.MetaData.TryGetValue("oasisEntityType", out var kind) && kind?.ToString() == "message").Should().Be(1);
    }

    [Fact]
    public async Task RejectedMessageReadDoesNotLeakThroughProviderObjectAlias()
    {
        var senderId = Guid.NewGuid();
        var recipientId = Guid.NewGuid();
        var stored = new Dictionary<Guid, IHolon>();
        var dna = CreateDna(HyperDriveModes.V2);
        var providerManager = new ProviderManager(null, dna);
        var provider = CreateActiveProvider(ProviderType.IPFSOASIS, "isolated-message-alias");
        provider.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        provider.Setup(x => x.LoadHolonAsync(It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<bool>(),
                It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<int>()))
            .ReturnsAsync((Guid id, bool _, bool _, int _, bool _, bool _, int _) =>
                new OASISResult<IHolon> { Result = stored.TryGetValue(id, out var holon) ? holon : null! });
        provider.Setup(x => x.SaveHolonAsync(It.IsAny<IHolon>(), true, true, 0, true, false))
            .ReturnsAsync((IHolon holon, bool _, bool _, int _, bool _, bool _) =>
            {
                stored[holon.Id] = holon;
                return new OASISResult<IHolon>(holon) { IsSaved = true };
            });
        provider.Setup(x => x.LoadHolonsByMetaDataAsync(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<HolonType>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<int>(),
                It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<int>()))
            .ReturnsAsync((string key, string value, HolonType _, bool _, bool _, int _, int _, bool _, bool _, int _) =>
                new OASISResult<IEnumerable<IHolon>>(stored.Values.Where(h => h.MetaData != null &&
                    h.MetaData.TryGetValue(key, out var indexed) && indexed?.ToString() == value).ToList()));
        providerManager.RegisterProvider(provider.Object).Should().BeTrue();
        providerManager.SetAndActivateCurrentStorageProvider(provider.Object).IsError.Should().BeFalse();
        var send = await new MessagingManager(null, dna, providerManager)
            .SendMessageToAvatarAsync(senderId, recipientId, "reject read");
        var message = (await new MessagingManager(null, dna, providerManager).GetMessagesAsync(recipientId)).Result.Single();
        provider.Setup(x => x.SaveHolonAsync(It.IsAny<IHolon>(), true, true, 0, true, false))
            .ReturnsAsync(new OASISResult<IHolon> { IsError = true, Message = "read write rejected" });

        var mark = await new MessagingManager(null, dna, providerManager)
            .MarkMessagesAsReadAsync(recipientId, new List<Guid> { message.Id });
        var reloaded = await new MessagingManager(null, dna, providerManager).GetMessagesAsync(recipientId);

        send.IsError.Should().BeFalse(send.Message);
        mark.IsError.Should().BeTrue();
        reloaded.Result.Should().ContainSingle(x => x.Id == message.Id && !x.IsRead && !x.ReadAt.HasValue);
    }

    [Fact]
    public async Task EggsManagerDoesNotTreatAnUnavailableInjectedRuntimeAsAnEmptyCollection()
    {
        var avatarId = Guid.NewGuid();
        var dna = CreateDna(HyperDriveModes.V2);
        var providerManager = new ProviderManager(null, dna);
        var provider = CreateActiveProvider(ProviderType.IPFSOASIS, "isolated-eggs-read-failure");
        provider.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        provider.Setup(x => x.LoadHolonAsync(It.IsAny<Guid>(), true, true, 0, true, false, 0))
            .ReturnsAsync(new OASISResult<IHolon> { IsError = true, Message = "egg store unavailable" });
        provider.Setup(x => x.SaveHolonAsync(It.IsAny<IHolon>(), true, true, 0, true, false))
            .ReturnsAsync(new OASISResult<IHolon> { IsError = true, Message = "egg store unavailable" });
        providerManager.RegisterProvider(provider.Object).Should().BeTrue();
        providerManager.SetAndActivateCurrentStorageProvider(provider.Object).IsError.Should().BeFalse();
        var singletonBefore = ProviderManager.Instance.CurrentStorageProvider;
        var manager = new EggsManager(null, dna, providerManager);

        var result = await manager.GetAllEggsAsync(avatarId);

        result.IsError.Should().BeTrue();
        result.Result.Should().BeNull();
        result.Message.Should().Contain("Could not load the egg collection");
        ProviderManager.Instance.CurrentStorageProvider.Should().BeSameAs(singletonBefore);
    }

    [Fact]
    public async Task EggsManagerDoesNotReportDiscoveryWhenItsDurableWriteIsRejected()
    {
        var avatarId = Guid.NewGuid();
        var dna = CreateDna(HyperDriveModes.V2);
        var providerManager = new ProviderManager(null, dna);
        var provider = CreateActiveProvider(ProviderType.IPFSOASIS, "isolated-eggs-write-failure");
        provider.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        provider.Setup(x => x.LoadHolonAsync(It.IsAny<Guid>(), true, true, 0, true, false, 0))
            .ReturnsAsync(() => new OASISResult<IHolon>(new Holon { MetaData = new Dictionary<string, object>() }));
        provider.Setup(x => x.SaveHolonAsync(It.IsAny<IHolon>(), true, true, 0, true, false))
            .ReturnsAsync(new OASISResult<IHolon> { IsError = true, Message = "egg write rejected" });
        providerManager.RegisterProvider(provider.Object).Should().BeTrue();
        providerManager.SetAndActivateCurrentStorageProvider(provider.Object).IsError.Should().BeFalse();
        var manager = new EggsManager(null, dna, providerManager);

        var result = await manager.DiscoverEggAsync(avatarId, EggType.Gold, "Golden Test Egg",
            Guid.NewGuid(), "test arena", EggDiscoveryMethod.Exploration);

        result.IsError.Should().BeTrue();
        result.Result.Should().BeNull();
        result.Message.Should().Contain("could not be committed");
        provider.Verify(x => x.SaveHolonAsync(It.IsAny<IHolon>(), true, true, 0, true, false), Times.Once);
    }

    [Fact]
    public async Task EggsManagerDiscoveryAndHatchSurviveManagerRestart()
    {
        var avatarId = Guid.NewGuid();
        IHolon stored = new Holon { MetaData = new Dictionary<string, object>() };
        var dna = CreateDna(HyperDriveModes.V2);
        var providerManager = new ProviderManager(null, dna);
        var provider = CreateActiveProvider(ProviderType.IPFSOASIS, "isolated-eggs-durable");
        provider.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        provider.Setup(x => x.LoadHolonAsync(It.IsAny<Guid>(), true, true, 0, true, false, 0))
            .ReturnsAsync(() => new OASISResult<IHolon>(stored));
        provider.Setup(x => x.SaveHolonAsync(It.IsAny<IHolon>(), true, true, 0, true, false))
            .ReturnsAsync((IHolon holon, bool _, bool _, int _, bool _, bool _) =>
            {
                stored = holon;
                return new OASISResult<IHolon>(holon) { IsSaved = true };
            });
        providerManager.RegisterProvider(provider.Object).Should().BeTrue();
        providerManager.SetAndActivateCurrentStorageProvider(provider.Object).IsError.Should().BeFalse();

        var discovery = await new EggsManager(null, dna, providerManager).DiscoverEggAsync(
            avatarId, EggType.Gold, "Durable Golden Egg", Guid.NewGuid(), "edge world",
            EggDiscoveryMethod.Exploration);
        var afterDiscoveryRestart = await new EggsManager(null, dna, providerManager).GetAllEggsAsync(avatarId);
        var hatch = await new EggsManager(null, dna, providerManager)
            .HatchEggAsync(avatarId, discovery.Result.Id);
        var afterHatchRestart = await new EggsManager(null, dna, providerManager).GetAllEggsAsync(avatarId);

        discovery.IsError.Should().BeFalse(discovery.Message);
        discovery.IsSaved.Should().BeTrue();
        afterDiscoveryRestart.Result.Should().ContainSingle(x => x.Id == discovery.Result.Id);
        hatch.IsError.Should().BeFalse(hatch.Message);
        hatch.IsSaved.Should().BeTrue();
        afterHatchRestart.Result.Should().ContainSingle(x => x.Id == discovery.Result.Id && x.IsHatched);
        provider.Verify(x => x.SaveHolonAsync(It.IsAny<IHolon>(), true, true, 0, true, false), Times.AtLeast(2));
    }

    [Fact]
    public async Task CompetitionLeaderboardSurvivesManagerRestart()
    {
        var avatarId = Guid.NewGuid();
        var stored = new Dictionary<Guid, IHolon>();
        var dna = CreateDna(HyperDriveModes.V2);
        var providerManager = new ProviderManager(null, dna);
        var provider = CreateActiveProvider(ProviderType.IPFSOASIS, "isolated-competition-durable");
        provider.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        provider.Setup(x => x.LoadHolonAsync(It.IsAny<Guid>(), true, true, 0, true, false, 0))
            .ReturnsAsync((Guid id, bool _, bool _, int _, bool _, bool _, int _) =>
                new OASISResult<IHolon> { Result = stored.TryGetValue(id, out var holon) ? holon : null! });
        provider.Setup(x => x.SaveHolonAsync(It.IsAny<IHolon>(), true, true, 0, true, false))
            .ReturnsAsync((IHolon holon, bool _, bool _, int _, bool _, bool _) =>
            {
                stored[holon.Id] = holon;
                return new OASISResult<IHolon>(holon) { IsSaved = true };
            });
        providerManager.RegisterProvider(provider.Object).Should().BeTrue();
        providerManager.SetAndActivateCurrentStorageProvider(provider.Object).IsError.Should().BeFalse();

        var update = await new CompetitionManager(null, dna, providerManager).UpdateAvatarScoreAsync(
            avatarId, CompetitionType.Karma, SeasonType.Weekly, 75);
        var leaderboard = await new CompetitionManager(null, dna, providerManager).GetLeaderboardAsync(
            CompetitionType.Karma, SeasonType.Weekly);

        update.IsError.Should().BeFalse(update.Message);
        update.Result.Should().BeTrue();
        leaderboard.IsError.Should().BeFalse(leaderboard.Message);
        leaderboard.Result.Should().ContainSingle(entry =>
            entry.AvatarId == avatarId && entry.Score == 75 && entry.Rank == 1);
    }

    [Fact]
    public async Task CompetitionRejectedWriteDoesNotLeakUncommittedScoreThroughProviderAlias()
    {
        var avatarId = Guid.NewGuid();
        var originalEntries = new List<LeaderboardEntry>
        {
            new LeaderboardEntry
            {
                AvatarId = avatarId, CompetitionType = CompetitionType.Karma,
                SeasonType = SeasonType.Daily, Score = 10, Rank = 1
            }
        };
        var stored = new Holon
        {
            MetaData = new Dictionary<string, object>
            {
                ["entries"] = Newtonsoft.Json.JsonConvert.SerializeObject(originalEntries)
            }
        };
        var dna = CreateDna(HyperDriveModes.V2);
        var providerManager = new ProviderManager(null, dna);
        var provider = CreateActiveProvider(ProviderType.IPFSOASIS, "isolated-competition-rejected");
        provider.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        provider.Setup(x => x.LoadHolonAsync(It.IsAny<Guid>(), true, true, 0, true, false, 0))
            .ReturnsAsync(new OASISResult<IHolon>(stored));
        provider.Setup(x => x.SaveHolonAsync(It.IsAny<IHolon>(), true, true, 0, true, false))
            .ReturnsAsync(new OASISResult<IHolon>
            {
                IsError = true, ErrorCount = 1, ErrorCode = "LEADERBOARD_WRITE_REJECTED",
                Message = "leaderboard write rejected"
            });
        providerManager.RegisterProvider(provider.Object).Should().BeTrue();
        providerManager.SetAndActivateCurrentStorageProvider(provider.Object).IsError.Should().BeFalse();
        var manager = new CompetitionManager(null, dna, providerManager);

        var update = await manager.UpdateAvatarScoreAsync(
            avatarId, CompetitionType.Karma, SeasonType.Daily, 50);
        var reloaded = await new CompetitionManager(null, dna, providerManager).GetLeaderboardAsync(
            CompetitionType.Karma, SeasonType.Daily);

        update.IsError.Should().BeTrue();
        update.Message.Should().Contain("not persisted");
        reloaded.Result.Should().ContainSingle(entry => entry.AvatarId == avatarId && entry.Score == 10);
    }

    [Fact]
    public async Task TournamentEnrollmentIsValidatedPersistedAndRestartSafe()
    {
        var avatarId = Guid.NewGuid();
        var tournament = new Tournament
        {
            Name = "Edge Championship", CompetitionType = CompetitionType.Karma,
            Status = TournamentStatus.Registration, RegistrationStart = DateTime.UtcNow.AddHours(-1),
            RegistrationEnd = DateTime.UtcNow.AddHours(1), StartDate = DateTime.UtcNow.AddHours(2),
            EndDate = DateTime.UtcNow.AddDays(1), MaxParticipants = 2
        };
        var stored = new Dictionary<Guid, IHolon>();
        var dna = CreateDna(HyperDriveModes.V2);
        var providerManager = new ProviderManager(null, dna);
        var provider = CreateActiveProvider(ProviderType.IPFSOASIS, "isolated-tournament-durable");
        provider.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        provider.Setup(x => x.LoadHolonAsync(It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<bool>(),
                It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<int>()))
            .ReturnsAsync((Guid id, bool _, bool _, int _, bool _, bool _, int _) =>
                new OASISResult<IHolon> { Result = stored.TryGetValue(id, out var holon) ? holon : null! });
        provider.Setup(x => x.SaveHolonAsync(It.IsAny<IHolon>(), true, true, 0, true, false))
            .ReturnsAsync((IHolon holon, bool _, bool _, int _, bool _, bool _) =>
            {
                stored[holon.Id] = holon;
                return new OASISResult<IHolon>(holon) { IsSaved = true };
            });
        providerManager.RegisterProvider(provider.Object).Should().BeTrue();
        providerManager.SetAndActivateCurrentStorageProvider(provider.Object).IsError.Should().BeFalse();
        var seed = await new CompetitionManager(null, dna, providerManager).SaveTournamentAsync(tournament);

        var join = await new CompetitionManager(null, dna, providerManager).JoinTournamentAsync(avatarId, tournament.Id);
        var duplicate = await new CompetitionManager(null, dna, providerManager).JoinTournamentAsync(avatarId, tournament.Id);
        var afterRestart = await new CompetitionManager(null, dna, providerManager)
            .GetActiveTournamentsAsync(CompetitionType.Karma);

        seed.Result.Should().BeSameAs(tournament);
        seed.IsSaved.Should().BeTrue();
        join.Result.Should().BeTrue(join.Message);
        join.IsSaved.Should().BeTrue();
        duplicate.Result.Should().BeTrue(duplicate.Message);
        duplicate.IsSaved.Should().BeFalse();
        afterRestart.Result.Should().ContainSingle(x => x.Id == tournament.Id &&
            x.CurrentParticipants == 1 && x.Participants.Single().AvatarId == avatarId);
    }

    [Fact]
    public async Task TournamentSaveRejectionIsNotReportedAsPublished()
    {
        IHolon stored = null;
        var dna = CreateDna(HyperDriveModes.V2);
        var providerManager = new ProviderManager(null, dna);
        var provider = CreateActiveProvider(ProviderType.IPFSOASIS, "isolated-tournament-rejected");
        provider.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        provider.Setup(x => x.LoadHolonAsync(It.IsAny<Guid>(), true, true, 0, true, false, 0))
            .ReturnsAsync(() => new OASISResult<IHolon> { Result = stored });
        provider.Setup(x => x.SaveHolonAsync(It.IsAny<IHolon>(), true, true, 0, true, false))
            .ReturnsAsync((IHolon holon, bool _, bool _, int _, bool _, bool _) =>
            {
                stored = holon;
                return new OASISResult<IHolon>(holon) { IsSaved = true };
            });
        providerManager.RegisterProvider(provider.Object).Should().BeTrue();
        providerManager.SetAndActivateCurrentStorageProvider(provider.Object).IsError.Should().BeFalse();
        var tournament = new Tournament
        {
            Name = "Rejected Tournament", CompetitionType = CompetitionType.Karma,
            Status = TournamentStatus.Registration, RegistrationStart = DateTime.UtcNow.AddHours(-1),
            RegistrationEnd = DateTime.UtcNow.AddHours(1), StartDate = DateTime.UtcNow.AddHours(2),
            EndDate = DateTime.UtcNow.AddDays(1), MaxParticipants = 8
        };

        var manager = new CompetitionManager(null, dna, providerManager);
        (await manager.SaveTournamentAsync(tournament)).IsError.Should().BeFalse();
        tournament.Description = "This update must be rejected.";
        provider.Setup(x => x.SaveHolonAsync(It.IsAny<IHolon>(), true, true, 0, true, false))
            .ReturnsAsync(new OASISResult<IHolon> { IsError = true, Message = "catalog write rejected" });

        var save = await manager.SaveTournamentAsync(tournament);

        save.IsError.Should().BeTrue();
        save.Result.Should().BeNull();
        save.Message.Should().Contain("not persisted");
    }

    [Fact]
    public async Task GiftsManagerDoesNotReportSendWhenCanonicalGiftWriteIsRejected()
    {
        var dna = CreateDna(HyperDriveModes.V2);
        var providerManager = new ProviderManager(null, dna);
        var provider = CreateActiveProvider(ProviderType.IPFSOASIS, "isolated-gift-rejected");
        provider.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        provider.Setup(x => x.SaveHolonAsync(It.IsAny<IHolon>(), true, true, 0, true, false))
            .ReturnsAsync(new OASISResult<IHolon>
            {
                IsError = true, ErrorCount = 1, ErrorCode = "GIFT_WRITE_REJECTED",
                Message = "gift write rejected"
            });
        providerManager.RegisterProvider(provider.Object).Should().BeTrue();
        providerManager.SetAndActivateCurrentStorageProvider(provider.Object).IsError.Should().BeFalse();

        var result = await new GiftsManager(null, dna, providerManager).SendGiftAsync(
            Guid.NewGuid(), Guid.NewGuid(), GiftType.Badge, "durability test");

        result.IsError.Should().BeTrue();
        result.Result.Should().BeNull();
        result.Message.Should().Contain("durable record could not be saved");
        provider.Verify(x => x.SaveHolonAsync(It.IsAny<IHolon>(), true, true, 0, true, false), Times.Once);
    }

    [Fact]
    public async Task GiftLifecycleAndDerivedHistorySurviveManagerRestart()
    {
        var fromAvatarId = Guid.NewGuid();
        var toAvatarId = Guid.NewGuid();
        var stored = new Dictionary<Guid, IHolon>();
        var dna = CreateDna(HyperDriveModes.V2);
        var providerManager = new ProviderManager(null, dna);
        var provider = CreateActiveProvider(ProviderType.IPFSOASIS, "isolated-gift-durable");
        provider.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        provider.Setup(x => x.LoadHolonAsync(It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<bool>(),
                It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<int>()))
            .ReturnsAsync((Guid id, bool _, bool _, int _, bool _, bool _, int _) =>
                new OASISResult<IHolon> { Result = stored.TryGetValue(id, out var holon) ? holon : null! });
        provider.Setup(x => x.SaveHolonAsync(It.IsAny<IHolon>(), true, true, 0, true, false))
            .ReturnsAsync((IHolon holon, bool _, bool _, int _, bool _, bool _) =>
            {
                stored[holon.Id] = holon;
                return new OASISResult<IHolon>(holon) { IsSaved = true };
            });
        provider.Setup(x => x.LoadHolonsByMetaDataAsync(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<HolonType>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<int>(),
                It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<int>()))
            .ReturnsAsync((string key, string value, HolonType _, bool _, bool _, int _, int _, bool _, bool _, int _) =>
                new OASISResult<IEnumerable<IHolon>>(stored.Values.Where(h => h.MetaData != null &&
                    h.MetaData.TryGetValue(key, out var indexed) && indexed?.ToString() == value).ToList()));
        providerManager.RegisterProvider(provider.Object).Should().BeTrue();
        providerManager.SetAndActivateCurrentStorageProvider(provider.Object).IsError.Should().BeFalse();

        var send = await new GiftsManager(null, dna, providerManager).SendGiftAsync(
            fromAvatarId, toAvatarId, GiftType.Egg, "restart-safe gift");
        var receive = await new GiftsManager(null, dna, providerManager).ReceiveGiftAsync(toAvatarId, send.Result.Id);
        var open = await new GiftsManager(null, dna, providerManager).OpenGiftAsync(toAvatarId, send.Result.Id);
        var gifts = await new GiftsManager(null, dna, providerManager).GetAllGiftsAsync(toAvatarId);
        var senderHistory = await new GiftsManager(null, dna, providerManager).GetGiftHistoryAsync(fromAvatarId);
        var recipientHistory = await new GiftsManager(null, dna, providerManager).GetGiftHistoryAsync(toAvatarId);

        send.IsError.Should().BeFalse(send.Message);
        receive.Result.Should().BeTrue(receive.Message);
        open.Result.Should().BeTrue(open.Message);
        gifts.Result.Should().ContainSingle(x => x.Id == send.Result.Id && x.IsReceived && x.IsOpened);
        senderHistory.Result.Should().Contain(x => x.GiftId == send.Result.Id && x.TransactionType == GiftTransactionType.Sent);
        recipientHistory.Result.Should().Contain(x => x.GiftId == send.Result.Id && x.TransactionType == GiftTransactionType.Opened);
    }

    [Fact]
    public async Task RejectedGiftTransitionDoesNotLeakThroughProviderObjectAlias()
    {
        var senderId = Guid.NewGuid();
        var recipientId = Guid.NewGuid();
        var stored = new Dictionary<Guid, IHolon>();
        var dna = CreateDna(HyperDriveModes.V2);
        var providerManager = new ProviderManager(null, dna);
        var provider = CreateActiveProvider(ProviderType.IPFSOASIS, "isolated-gift-alias");
        provider.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        provider.Setup(x => x.LoadHolonAsync(It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<bool>(),
                It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<int>()))
            .ReturnsAsync((Guid id, bool _, bool _, int _, bool _, bool _, int _) =>
                new OASISResult<IHolon> { Result = stored.TryGetValue(id, out var holon) ? holon : null! });
        provider.Setup(x => x.SaveHolonAsync(It.IsAny<IHolon>(), true, true, 0, true, false))
            .ReturnsAsync((IHolon holon, bool _, bool _, int _, bool _, bool _) =>
            {
                stored[holon.Id] = holon;
                return new OASISResult<IHolon>(holon) { IsSaved = true };
            });
        provider.Setup(x => x.LoadHolonsByMetaDataAsync(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<HolonType>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<int>(),
                It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<int>()))
            .ReturnsAsync((string key, string value, HolonType _, bool _, bool _, int _, int _, bool _, bool _, int _) =>
                new OASISResult<IEnumerable<IHolon>>(stored.Values.Where(h => h.MetaData != null &&
                    h.MetaData.TryGetValue(key, out var indexed) && indexed?.ToString() == value).ToList()));
        providerManager.RegisterProvider(provider.Object).Should().BeTrue();
        providerManager.SetAndActivateCurrentStorageProvider(provider.Object).IsError.Should().BeFalse();
        var send = await new GiftsManager(null, dna, providerManager)
            .SendGiftAsync(senderId, recipientId, GiftType.Title, "reject receive");
        provider.Setup(x => x.SaveHolonAsync(It.IsAny<IHolon>(), true, true, 0, true, false))
            .ReturnsAsync(new OASISResult<IHolon> { IsError = true, Message = "receive write rejected" });

        var receive = await new GiftsManager(null, dna, providerManager).ReceiveGiftAsync(recipientId, send.Result.Id);
        var reloaded = await new GiftsManager(null, dna, providerManager).GetAllGiftsAsync(recipientId);

        send.IsError.Should().BeFalse(send.Message);
        receive.IsError.Should().BeTrue();
        reloaded.Result.Should().ContainSingle(x => x.Id == send.Result.Id && !x.IsReceived && !x.ReceivedAt.HasValue);
    }

    [Fact]
    public void BridgeManagerBuildsItsProviderMapFromTheInjectedRuntime()
    {
        var dna = CreateDna(HyperDriveModes.V2);
        var providerManager = new ProviderManager(null, dna);
        var provider = new Mock<IOASISBlockchainStorageProvider>();
        provider.SetupAllProperties();
        provider.Object.ProviderType = new EnumValue<ProviderType>(ProviderType.EthereumOASIS);
        provider.Object.ProviderCategory = new EnumValue<ProviderCategory>(ProviderCategory.Blockchain);
        provider.Object.ProviderName = "isolated-ethereum";
        provider.Object.IsProviderActivated = true;
        providerManager.RegisterProvider(provider.Object).Should().BeTrue();
        var singletonEthereum = ProviderManager.Instance.GetStorageProvider(ProviderType.EthereumOASIS);

        var manager = new BridgeManager(null, dna, providerManager);

        manager.GetBridgeProvider("ETH").Should().BeSameAs(provider.Object);
        ProviderManager.Instance.GetStorageProvider(ProviderType.EthereumOASIS).Should().BeSameAs(singletonEthereum);
    }

    [Fact]
    public async Task BridgeManagerResolvesNftProvidersFromTheInjectedRuntime()
    {
        var dna = CreateDna(HyperDriveModes.V2);
        var providerManager = new ProviderManager(null, dna);
        var source = new Mock<IOASISBlockchainStorageProvider>();
        var sourceNft = source.As<IOASISNFTProvider>();
        source.SetupAllProperties();
        source.Object.ProviderType = new EnumValue<ProviderType>(ProviderType.EthereumOASIS);
        source.Object.ProviderCategory = new EnumValue<ProviderCategory>(ProviderCategory.Blockchain);
        source.Object.ProviderName = "isolated-nft-source";
        source.Object.IsProviderActivated = true;
        sourceNft.Setup(x => x.WithdrawNFTAsync("contract", "token", "from", string.Empty))
            .ReturnsAsync(new OASISResult<NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs.BridgeTransactionResponse>
            {
                IsError = true,
                Message = "injected source reached"
            });

        var destination = new Mock<IOASISBlockchainStorageProvider>();
        destination.As<IOASISNFTProvider>();
        destination.SetupAllProperties();
        destination.Object.ProviderType = new EnumValue<ProviderType>(ProviderType.SolanaOASIS);
        destination.Object.ProviderCategory = new EnumValue<ProviderCategory>(ProviderCategory.Blockchain);
        destination.Object.ProviderName = "isolated-nft-destination";
        destination.Object.IsProviderActivated = true;
        providerManager.RegisterProvider(source.Object).Should().BeTrue();
        providerManager.RegisterProvider(destination.Object).Should().BeTrue();

        var manager = new BridgeManager(null, dna, providerManager);
        var result = await manager.CreateNFTBridgeOrderAsync(new CreateNFTBridgeOrderRequest
        {
            FromChain = nameof(ProviderType.EthereumOASIS),
            ToChain = nameof(ProviderType.SolanaOASIS),
            NFTTokenAddress = "contract",
            TokenId = "token",
            FromAddress = "from",
            DestinationAddress = "to"
        });

        result.IsError.Should().BeTrue();
        result.Message.Should().Contain("injected source reached");
        sourceNft.Verify(x => x.WithdrawNFTAsync("contract", "token", "from", string.Empty), Times.Once);
    }

    [Fact]
    public async Task StatsManagerLoadsTheAvatarFromItsInjectedRuntime()
    {
        var dna = CreateDna(HyperDriveModes.V2);
        var providerManager = new ProviderManager(null, dna);
        var provider = CreateActiveProvider(ProviderType.IPFSOASIS, "isolated-stats-provider");
        var avatarId = Guid.NewGuid();
        provider.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        provider.Setup(x => x.LoadAvatarAsync(avatarId, 0))
            .ReturnsAsync(new OASISResult<IAvatar>
            {
                IsError = true,
                Message = "isolated stats provider reached"
            });
        providerManager.RegisterProvider(provider.Object).Should().BeTrue();
        providerManager.SetAndActivateCurrentStorageProvider(provider.Object).IsError.Should().BeFalse();

        var manager = new StatsManager(null, dna, providerManager);
        var result = await manager.GetAvatarStatsAsync(avatarId);

        result.IsError.Should().BeTrue();
        provider.Verify(x => x.LoadAvatarAsync(avatarId, 0), Times.Once);
    }

    [Fact]
    public async Task StatsManagerDoesNotReportZeroSystemCountsWhenItsProviderSearchFails()
    {
        var dna = CreateDna(HyperDriveModes.V2);
        var providerManager = new ProviderManager(null, dna);
        var provider = CreateActiveProvider(ProviderType.IPFSOASIS, "isolated-stats-search-failure");
        provider.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        provider.Setup(x => x.SearchAsync(It.IsAny<ISearchParams>(), true, true, 0, true, 0))
            .ReturnsAsync(new OASISResult<ISearchResults>
            {
                IsError = true,
                ErrorCount = 1,
                ErrorCode = "PROVIDER_UNAVAILABLE",
                Message = "provider unavailable"
            });
        providerManager.RegisterProvider(provider.Object).Should().BeTrue();
        providerManager.SetAndActivateCurrentStorageProvider(provider.Object).IsError.Should().BeFalse();

        var manager = new StatsManager(null, dna, providerManager);
        var result = await manager.GetSystemStatsAsync();

        result.IsError.Should().BeTrue();
        result.Result.Should().BeNull();
        result.Message.Should().Contain("total avatar count");
        result.Message.Should().Contain("provider unavailable");
        provider.Verify(x => x.SearchAsync(It.IsAny<ISearchParams>(), true, true, 0, true, 0), Times.Once);
    }

    [Fact]
    public async Task SettingsReadFailureNeverAttemptsToCreateOrOverwriteTheSettingsHolon()
    {
        var avatarId = Guid.NewGuid();
        var dna = CreateDna(HyperDriveModes.V2);
        var providerManager = new ProviderManager(null, dna);
        var provider = CreateActiveProvider(ProviderType.IPFSOASIS, "isolated-settings-read-only-failure");
        provider.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        provider.Setup(x => x.LoadHolonAsync(It.IsAny<Guid>(), true, true, 0, true, false, 0))
            .ReturnsAsync(new OASISResult<IHolon>
            {
                IsError = true,
                ErrorCount = 1,
                ErrorCode = "PROVIDER_UNAVAILABLE",
                Message = "provider unavailable"
            });
        provider.Setup(x => x.SaveHolonAsync(It.IsAny<IHolon>(), true, true, 0, true, false))
            .ReturnsAsync((IHolon holon, bool _, bool _, int _, bool _, bool _) =>
                new OASISResult<IHolon>(holon) { IsSaved = true });
        providerManager.RegisterProvider(provider.Object).Should().BeTrue();
        providerManager.SetAndActivateCurrentStorageProvider(provider.Object).IsError.Should().BeFalse();

        var manager = new HolonManager(null, dna, providerManager);
        var result = await manager.GetAllSettingsAsync(avatarId, "gifts");

        result.IsError.Should().BeTrue();
        result.Result.Should().BeNull();
        result.Message.Should().Contain("provider unavailable");
        provider.Verify(x => x.SaveHolonAsync(It.IsAny<IHolon>(), true, true, 0, true, false), Times.Never);
    }

    [Fact]
    public async Task AvatarStatsDoesNotReturnAPartialDashboardWhenDurableStatsCannotLoad()
    {
        var avatarId = Guid.NewGuid();
        var dna = CreateDna(HyperDriveModes.V2);
        var providerManager = new ProviderManager(null, dna);
        var provider = CreateActiveProvider(ProviderType.IPFSOASIS, "isolated-avatar-stats-failure");
        provider.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        provider.Setup(x => x.LoadAvatarAsync(avatarId, 0))
            .ReturnsAsync(new OASISResult<IAvatar>(new Avatar { Id = avatarId }));
        provider.Setup(x => x.LoadHolonAsync(It.IsAny<Guid>(), true, true, 0, true, false, 0))
            .ReturnsAsync(new OASISResult<IHolon>
            {
                IsError = true,
                ErrorCount = 1,
                ErrorCode = "PROVIDER_UNAVAILABLE",
                Message = "provider unavailable"
            });
        providerManager.RegisterProvider(provider.Object).Should().BeTrue();
        providerManager.SetAndActivateCurrentStorageProvider(provider.Object).IsError.Should().BeFalse();

        var result = await new StatsManager(null, dna, providerManager).GetAvatarStatsAsync(avatarId);

        result.IsError.Should().BeTrue();
        result.Result.Should().BeNull();
        result.Message.Should().Contain("karma statistics failed");
        result.Message.Should().Contain("provider unavailable");
        provider.Verify(x => x.SaveHolonAsync(It.IsAny<IHolon>(), true, true, 0, true, false), Times.Never);
    }

    [Fact]
    public async Task KarmaMutationDoesNotCommitMemoryOrReportSuccessWhenPersistenceFails()
    {
        var avatarId = Guid.NewGuid();
        var dna = CreateDna(HyperDriveModes.V2);
        var providerManager = new ProviderManager(null, dna);
        var provider = CreateActiveProvider(ProviderType.IPFSOASIS, "isolated-karma-write-failure");
        provider.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        provider.Setup(x => x.LoadHolonAsync(It.IsAny<Guid>(), true, true, 0, true, false, 0))
            .ReturnsAsync(new OASISResult<IHolon>(new Holon { MetaData = new Dictionary<string, object>() }));
        provider.Setup(x => x.SaveHolonAsync(It.IsAny<IHolon>(), true, true, 0, true, false))
            .ReturnsAsync(new OASISResult<IHolon>
            {
                IsError = true,
                ErrorCount = 1,
                ErrorCode = "PROVIDER_WRITE_REJECTED",
                Message = "write rejected"
            });
        providerManager.RegisterProvider(provider.Object).Should().BeTrue();
        providerManager.SetAndActivateCurrentStorageProvider(provider.Object).IsError.Should().BeFalse();
        var manager = new KarmaManager(null, dna, providerManager);

        var mutation = await manager.AddKarmaAsync(avatarId, 25, KarmaSourceType.Platform, "offline action");
        var history = await manager.GetKarmaHistoryAsync(avatarId);

        mutation.IsError.Should().BeTrue();
        mutation.Result.Should().BeFalse();
        mutation.Message.Should().Contain("durable persistence failed");
        history.Result.Should().BeEmpty();
        provider.Verify(x => x.SaveHolonAsync(It.IsAny<IHolon>(), true, true, 0, true, false), Times.Once);
    }

    [Fact]
    public async Task KarmaHistorySurvivesManagerRestartThroughDurableSettings()
    {
        var avatarId = Guid.NewGuid();
        var dna = CreateDna(HyperDriveModes.V2);
        var providerManager = new ProviderManager(null, dna);
        var provider = CreateActiveProvider(ProviderType.IPFSOASIS, "isolated-durable-karma");
        provider.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        IHolon stored = new Holon { MetaData = new Dictionary<string, object>() };
        provider.Setup(x => x.LoadHolonAsync(It.IsAny<Guid>(), true, true, 0, true, false, 0))
            .ReturnsAsync(() => new OASISResult<IHolon>(stored));
        provider.Setup(x => x.SaveHolonAsync(It.IsAny<IHolon>(), true, true, 0, true, false))
            .ReturnsAsync((IHolon holon, bool _, bool _, int _, bool _, bool _) =>
            {
                stored = holon;
                return new OASISResult<IHolon>(holon) { IsSaved = true };
            });
        providerManager.RegisterProvider(provider.Object).Should().BeTrue();
        providerManager.SetAndActivateCurrentStorageProvider(provider.Object).IsError.Should().BeFalse();

        var mutation = await new KarmaManager(null, dna, providerManager)
            .AddKarmaAsync(avatarId, 25, KarmaSourceType.Platform, "offline action");
        var restartedManager = new KarmaManager(null, dna, providerManager);
        var history = await restartedManager.GetKarmaHistoryAsync(avatarId);

        mutation.IsError.Should().BeFalse(mutation.Message);
        history.IsError.Should().BeFalse(history.Message);
        history.Result.Should().ContainSingle();
        history.Result[0].Amount.Should().Be(25);
        history.Result[0].Description.Should().Be("offline action");
    }

    [Fact]
    public async Task KarmaCommitReportsLeaderboardProjectionFailureWithoutLosingTheLedger()
    {
        var avatarId = Guid.NewGuid();
        var dna = CreateDna(HyperDriveModes.V2);
        var providerManager = new ProviderManager(null, dna);
        var provider = CreateActiveProvider(ProviderType.IPFSOASIS, "isolated-karma-projection-failure");
        provider.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        IHolon stored = new Holon { MetaData = new Dictionary<string, object>() };
        var saveCount = 0;
        provider.Setup(x => x.LoadHolonAsync(It.IsAny<Guid>(), true, true, 0, true, false, 0))
            .ReturnsAsync(() => new OASISResult<IHolon>(stored));
        provider.Setup(x => x.SaveHolonAsync(It.IsAny<IHolon>(), true, true, 0, true, false))
            .ReturnsAsync((IHolon holon, bool _, bool _, int _, bool _, bool _) =>
            {
                saveCount++;
                if (saveCount == 1)
                {
                    stored = holon;
                    return new OASISResult<IHolon>(holon) { IsSaved = true };
                }

                return new OASISResult<IHolon>(holon)
                {
                    IsError = true,
                    ErrorCount = 1,
                    ErrorCode = "LEADERBOARD_WRITE_REJECTED",
                    Message = "leaderboard write rejected"
                };
            });
        providerManager.RegisterProvider(provider.Object).Should().BeTrue();
        providerManager.SetAndActivateCurrentStorageProvider(provider.Object).IsError.Should().BeFalse();
        var manager = new KarmaManager(null, dna, providerManager);

        var mutation = await manager.AddKarmaAsync(
            avatarId, 25, KarmaSourceType.Platform, "offline action");
        var history = await manager.GetKarmaHistoryAsync(avatarId);

        mutation.IsError.Should().BeFalse(mutation.Message);
        mutation.Result.Should().BeTrue();
        mutation.WarningCount.Should().BeGreaterThan(0);
        mutation.InnerMessages.Should().Contain(message => message.Contains("leaderboard"));
        history.Result.Should().ContainSingle(transaction => transaction.Amount == 25);
        saveCount.Should().Be(6, "one durable karma commit and five seasonal projections are expected");
    }

    [Fact]
    public async Task KarmaStatsSurviveManagerRestartAndEmptyHistoryIsValid()
    {
        var avatarId = Guid.NewGuid();
        var emptyAvatarId = Guid.NewGuid();
        var dna = CreateDna(HyperDriveModes.V2);
        var providerManager = new ProviderManager(null, dna);
        var provider = CreateActiveProvider(ProviderType.IPFSOASIS, "isolated-durable-karma-stats");
        provider.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        var stored = new Dictionary<Guid, IHolon>();
        provider.Setup(x => x.LoadHolonAsync(It.IsAny<Guid>(), true, true, 0, true, false, 0))
            .ReturnsAsync((Guid id, bool _, bool _, int _, bool _, bool _, int _) =>
                new OASISResult<IHolon>(stored.TryGetValue(id, out var holon)
                    ? holon
                    : new Holon { Id = id, MetaData = new Dictionary<string, object>() }));
        provider.Setup(x => x.SaveHolonAsync(It.IsAny<IHolon>(), true, true, 0, true, false))
            .ReturnsAsync((IHolon holon, bool _, bool _, int _, bool _, bool _) =>
            {
                stored[holon.Id] = holon;
                return new OASISResult<IHolon>(holon) { IsSaved = true };
            });
        providerManager.RegisterProvider(provider.Object).Should().BeTrue();
        providerManager.SetAndActivateCurrentStorageProvider(provider.Object).IsError.Should().BeFalse();

        var mutation = await new KarmaManager(null, dna, providerManager)
            .AddKarmaAsync(avatarId, 25, KarmaSourceType.Platform, "offline action");
        var restarted = new KarmaManager(null, dna, providerManager);
        var stats = await restarted.GetKarmaStatsAsync(avatarId);
        var emptyStats = await restarted.GetKarmaStatsAsync(emptyAvatarId);

        mutation.IsError.Should().BeFalse(mutation.Message);
        stats.IsError.Should().BeFalse(stats.Message);
        stats.Result["totalKarma"].Should().Be(25L);
        stats.Result["transactionCount"].Should().Be(1);
        stats.Result["largestEarning"].Should().Be(25L);
        emptyStats.IsError.Should().BeFalse(emptyStats.Message);
        emptyStats.Result["largestEarning"].Should().Be(0L);
        emptyStats.Result["largestSpending"].Should().Be(0L);
    }

    [Fact]
    public async Task KarmaHistoryReadsProviderJsonArrayShapes()
    {
        var avatarId = Guid.NewGuid();
        var transactionId = Guid.NewGuid();
        var dna = CreateDna(HyperDriveModes.V2);
        var providerManager = new ProviderManager(null, dna);
        var provider = CreateActiveProvider(ProviderType.IPFSOASIS, "json-karma-history");
        provider.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        provider.Setup(x => x.LoadHolonAsync(It.IsAny<Guid>(), true, true, 0, true, false, 0))
            .ReturnsAsync(new OASISResult<IHolon>(new Holon
            {
                MetaData = new Dictionary<string, object>
                {
                    ["totalKarma"] = 11L,
                    ["history"] = JArray.Parse($"[{{\"id\":\"{transactionId}\",\"avatarId\":\"{avatarId}\",\"amount\":11,\"sourceType\":\"Platform\",\"description\":\"restored JSON\",\"timestamp\":\"2026-01-02T03:04:05Z\"}}]")
                }
            }));
        providerManager.RegisterProvider(provider.Object).Should().BeTrue();
        providerManager.SetAndActivateCurrentStorageProvider(provider.Object).IsError.Should().BeFalse();

        var history = await new KarmaManager(null, dna, providerManager)
            .GetKarmaHistoryAsync(avatarId);

        history.IsError.Should().BeFalse(history.Message);
        history.Result.Should().ContainSingle();
        history.Result[0].Id.Should().Be(transactionId);
        history.Result[0].Amount.Should().Be(11);
        history.Result[0].Description.Should().Be("restored JSON");
    }

    [Fact]
    public async Task KarmaReadPropagatesDurableProviderFailureInsteadOfUsingAvatarProjection()
    {
        var avatarId = Guid.NewGuid();
        var dna = CreateDna(HyperDriveModes.V2);
        var providerManager = new ProviderManager(null, dna);
        var provider = CreateActiveProvider(ProviderType.IPFSOASIS, "karma-read-failure");
        provider.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        provider.Setup(x => x.LoadHolonAsync(It.IsAny<Guid>(), true, true, 0, true, false, 0))
            .ReturnsAsync(new OASISResult<IHolon>
            {
                IsError = true,
                ErrorCount = 1,
                ErrorCode = "KARMA_STORE_UNAVAILABLE",
                Message = "karma store unavailable"
            });
        provider.Setup(x => x.LoadAvatarDetail(avatarId, 0))
            .Returns(new OASISResult<IAvatarDetail>(new AvatarDetail { Id = avatarId, Karma = 999 }));
        providerManager.RegisterProvider(provider.Object).Should().BeTrue();
        providerManager.SetAndActivateCurrentStorageProvider(provider.Object).IsError.Should().BeFalse();

        var result = await new KarmaManager(null, dna, providerManager).GetKarmaAsync(avatarId);

        result.IsError.Should().BeTrue();
        result.Message.Should().Contain("karma store unavailable");
        result.Result.Should().Be(0);
        provider.Verify(x => x.LoadAvatarDetail(avatarId, 0), Times.Never);
    }

    [Fact]
    public async Task HighLevelKarmaDoesNotMutateAvatarProjectionWhenLedgerRejectsWrite()
    {
        var avatarId = Guid.NewGuid();
        var detail = new AvatarDetail { Id = avatarId, Karma = 10 };
        var dna = CreateDna(HyperDriveModes.V2);
        var providerManager = new ProviderManager(null, dna);
        var provider = CreateActiveProvider(ProviderType.IPFSOASIS, "high-level-karma-rejection");
        provider.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        provider.Setup(x => x.LoadAvatarDetail(avatarId, 0))
            .Returns(new OASISResult<IAvatarDetail>(detail));
        provider.Setup(x => x.LoadHolonAsync(It.IsAny<Guid>(), true, true, 0, true, false, 0))
            .ReturnsAsync(new OASISResult<IHolon>(new Holon { MetaData = new Dictionary<string, object>() }));
        provider.Setup(x => x.SaveHolonAsync(It.IsAny<IHolon>(), true, true, 0, true, false))
            .ReturnsAsync(new OASISResult<IHolon>
            {
                IsError = true,
                ErrorCount = 1,
                ErrorCode = "KARMA_WRITE_REJECTED",
                Message = "karma write rejected"
            });
        providerManager.RegisterProvider(provider.Object).Should().BeTrue();
        providerManager.SetAndActivateCurrentStorageProvider(provider.Object).IsError.Should().BeFalse();

        var result = await new KarmaManager(null, dna, providerManager).AddKarmaToAvatarAsync(
            avatarId, KarmaTypePositive.CreateAvatar, KarmaSourceType.Platform,
            "registration", "registered avatar");

        result.IsError.Should().BeTrue();
        result.Message.Should().Contain("durable ledger rejected");
        detail.Karma.Should().Be(10);
        detail.KarmaAkashicRecords.Should().BeNullOrEmpty();
        provider.Verify(x => x.SaveAvatarDetail(It.IsAny<IAvatarDetail>()), Times.Never);
    }

    [Fact]
    public void AuthenticationRestoresItsRuntimeFailoverScopeWhenAvatarIsMissing()
    {
        var dna = CreateDna(HyperDriveModes.V2);
        var providerManager = new ProviderManager(null, dna);
        var provider = CreateActiveProvider(ProviderType.IPFSOASIS, "authentication-scope");
        provider.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        provider.Setup(x => x.LoadAvatarByUsername("missing", 0)).Returns(new OASISResult<IAvatar>());
        provider.Setup(x => x.LoadAvatarByEmail("missing", 0)).Returns(new OASISResult<IAvatar>());
        provider.Setup(x => x.LoadAvatarByPublicKeyAsync("missing", 0))
            .ReturnsAsync(new OASISResult<IAvatar>());
        providerManager.RegisterProvider(provider.Object).Should().BeTrue();
        providerManager.SetAndActivateCurrentStorageProvider(provider.Object).IsError.Should().BeFalse();
        providerManager.SetAndReplaceAutoFailOverListForProviders(
            new[] { new EnumValue<ProviderType>(ProviderType.IPFSOASIS) });
        providerManager.SetAndReplaceAutoFailOverListForProvidersForAvatarLogin(
            new[] { new EnumValue<ProviderType>(ProviderType.MongoDBOASIS) });
        var manager = new AvatarManager(null, dna, providerManager);

        var result = manager.Authenticate("missing", "password", "127.0.0.1");

        result.Result.Should().BeNull();
        providerManager.GetProviderAutoFailOverList().Select(x => x.Value)
            .Should().Equal(ProviderType.IPFSOASIS);
    }

    [Fact]
    public async Task SocialStatisticsUseTheManagersIsolatedRuntime()
    {
        var avatarId = Guid.NewGuid();
        var dna = CreateDna(HyperDriveModes.V2);
        var providerManager = new ProviderManager(null, dna);
        var provider = CreateActiveProvider(ProviderType.IPFSOASIS, "isolated-social-runtime");
        provider.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        provider.Setup(x => x.LoadHolonAsync(It.IsAny<Guid>(), true, true, 0, true, false, 0))
            .ReturnsAsync(new OASISResult<IHolon>(new Holon { MetaData = new Dictionary<string, object>() }));
        provider.Setup(x => x.SaveHolonAsync(It.IsAny<IHolon>(), true, true, 0, true, false))
            .ReturnsAsync((IHolon holon, bool _, bool _, int _, bool _, bool _) =>
                new OASISResult<IHolon>(holon) { IsSaved = true });
        providerManager.RegisterProvider(provider.Object).Should().BeTrue();
        providerManager.SetAndActivateCurrentStorageProvider(provider.Object).IsError.Should().BeFalse();

        var result = await new SocialManager(null, dna, providerManager)
            .ShareHolonAsync(avatarId, Guid.NewGuid(), "edge share");

        result.IsError.Should().BeFalse(result.Message);
        provider.Verify(x => x.SaveHolonAsync(
            It.Is<IHolon>(holon => holon.MetaData.ContainsKey("lastShareDate")),
            true, true, 0, true, false), Times.Once);
    }

    [Fact]
    public async Task VideoStartRollsBackTransientCallWhenItsRuntimeRejectsPersistence()
    {
        var initiatorId = Guid.NewGuid();
        var dna = CreateDna(HyperDriveModes.V2);
        var providerManager = new ProviderManager(null, dna);
        var provider = CreateActiveProvider(ProviderType.IPFSOASIS, "isolated-video-runtime");
        provider.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        provider.Setup(x => x.SaveHolonAsync(It.IsAny<IHolon>(), true, true, 0, true, false))
            .ReturnsAsync((IHolon holon, bool _, bool _, int _, bool _, bool _) => new OASISResult<IHolon>
            {
                Result = holon,
                IsError = true,
                ErrorCount = 1,
                ErrorCode = "VIDEO_WRITE_REJECTED",
                Message = "video write rejected"
            });
        providerManager.RegisterProvider(provider.Object).Should().BeTrue();
        providerManager.SetAndActivateCurrentStorageProvider(provider.Object).IsError.Should().BeFalse();
        var manager = new VideoManager(null, dna, providerManager);

        var start = await manager.StartVideoCallAsync(initiatorId, new List<Guid> { Guid.NewGuid() });
        var active = await manager.GetActiveCallsAsync(initiatorId);

        start.IsError.Should().BeTrue();
        start.Message.Should().Contain("video write rejected");
        active.Result.Should().BeEmpty();
    }

    [Fact]
    public async Task ChatStartUsesItsRuntimeIdentityAndRollsBackRejectedPersistence()
    {
        var participantId = Guid.NewGuid();
        var dna = CreateDna(HyperDriveModes.V2);
        var providerManager = new ProviderManager(null, dna);
        var provider = CreateActiveProvider(ProviderType.IPFSOASIS, "isolated-chat-runtime");
        provider.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        provider.Setup(x => x.SaveHolonAsync(It.IsAny<IHolon>(), true, true, 0, true, false))
            .ReturnsAsync((IHolon holon, bool _, bool _, int _, bool _, bool _) => new OASISResult<IHolon>
            {
                Result = holon,
                IsError = true,
                ErrorCount = 1,
                ErrorCode = "CHAT_WRITE_REJECTED",
                Message = "chat write rejected"
            });
        providerManager.RegisterProvider(provider.Object).Should().BeTrue();
        providerManager.SetAndActivateCurrentStorageProvider(provider.Object).IsError.Should().BeFalse();
        var manager = new ChatManager(null, dna, providerManager);

        var start = await manager.StartNewChatSessionAsync(
            new List<Guid> { participantId, Guid.NewGuid() });
        var active = await manager.GetActiveSessionsAsync(participantId);

        start.IsError.Should().BeTrue();
        start.Message.Should().Contain("chat write rejected");
        active.Result.Should().BeEmpty();
    }

    [Fact]
    public async Task LocalQuotaMustBeExplicitlyEnabled()
    {
        var dna = CreateDna(HyperDriveModes.V2);
        dna.OASIS.SubscriptionConfig = new SubscriptionConfig
        {
            EnforceLocalQuota = false,
            MaxRequestsPerMonth = 0,
            PayAsYouGoEnabled = false
        };
        var providerManager = new ProviderManager(null, dna);
        var provider = CreateActiveProvider(ProviderType.IPFSOASIS, "quota-disabled-provider");
        var avatarId = Guid.NewGuid();
        provider.Setup(x => x.LoadAvatarAsync(avatarId, 0))
            .ReturnsAsync(new OASISResult<IAvatar>(new Avatar { Id = avatarId }));
        providerManager.RegisterProvider(provider.Object);

        var result = await new OASISHyperDrive(providerManager).RouteRequestToProviderAsync<IAvatar>(
            new StorageOperationRequest { Operation = "LoadAvatar", AvatarId = avatarId },
            ProviderType.IPFSOASIS);

        result.IsError.Should().BeFalse(result.Message);
        result.Result.Id.Should().Be(avatarId);
        provider.Verify(x => x.LoadAvatarAsync(avatarId, 0), Times.Once);
    }

    [Fact]
    public async Task V2StorageMutationUsesReplicationQuotaAndDoesNotReachProviderWhenExhausted()
    {
        var dna = CreateDna(HyperDriveModes.V2);
        dna.OASIS.SubscriptionConfig = new SubscriptionConfig
        {
            EnforceLocalQuota = true,
            MaxRequestsPerMonth = int.MaxValue,
            MaxReplicationsPerMonth = 0,
            PayAsYouGoEnabled = false
        };
        var providerManager = new ProviderManager(null, dna);
        var provider = CreateActiveProvider(ProviderType.IPFSOASIS, "replication-quota-provider");
        providerManager.RegisterProvider(provider.Object);
        var holon = new Holon { Id = Guid.NewGuid() };

        var result = await new OASISHyperDrive(providerManager).RouteRequestToProviderAsync<IHolon>(
            new StorageOperationRequest { Operation = "SaveHolon", Payload = holon },
            ProviderType.IPFSOASIS);

        result.IsError.Should().BeTrue();
        result.ErrorCode.Should().Be("HYPERDRIVE_QUOTA_EXCEEDED");
        result.Message.Should().Contain("Quota exceeded for Replications");
        provider.Verify(x => x.SaveHolonAsync(It.IsAny<IHolon>(), It.IsAny<bool>(), It.IsAny<bool>(),
            It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task V2FailoverQuotaPreventsAnUnlicensedSecondaryProviderAttempt()
    {
        var dna = CreateDna(HyperDriveModes.V2);
        dna.OASIS.SubscriptionConfig = new SubscriptionConfig
        {
            EnforceLocalQuota = true,
            MaxRequestsPerMonth = int.MaxValue,
            MaxFailoversPerMonth = 0,
            PayAsYouGoEnabled = false
        };
        var providerManager = new ProviderManager(null, dna);
        var primary = CreateActiveProvider(ProviderType.MongoDBOASIS, "quota-primary");
        var secondary = CreateActiveProvider(ProviderType.IPFSOASIS, "quota-secondary");
        var avatarId = Guid.NewGuid();
        primary.Setup(x => x.LoadAvatarAsync(avatarId, 0)).ReturnsAsync(new OASISResult<IAvatar>
        {
            IsError = true,
            ErrorCount = 1,
            ErrorCode = "PRIMARY_UNAVAILABLE",
            Message = "primary unavailable"
        });
        secondary.Setup(x => x.LoadAvatarAsync(avatarId, 0))
            .ReturnsAsync(new OASISResult<IAvatar>(new Avatar { Id = avatarId }));
        providerManager.RegisterProvider(primary.Object);
        providerManager.RegisterProvider(secondary.Object);
        providerManager.SetAndReplaceAutoFailOverListForProviders(new[]
        {
            new EnumValue<ProviderType>(ProviderType.MongoDBOASIS),
            new EnumValue<ProviderType>(ProviderType.IPFSOASIS)
        }).IsError.Should().BeFalse();

        var result = await new OASISHyperDrive(providerManager).RouteRequestAsync<IAvatar>(
            new StorageOperationRequest
            {
                Operation = "LoadAvatar",
                AvatarId = avatarId,
                PreferredProvider = ProviderType.MongoDBOASIS
            });

        result.IsError.Should().BeTrue();
        result.ErrorCode.Should().Be("HYPERDRIVE_QUOTA_EXCEEDED");
        result.Message.Should().Contain("Quota exceeded for Failovers");
        primary.Verify(x => x.LoadAvatarAsync(avatarId, 0), Times.Once);
        secondary.Verify(x => x.LoadAvatarAsync(It.IsAny<Guid>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task ExplicitV2LoadBalancingCannotBypassRequestQuota()
    {
        var dna = CreateDna(HyperDriveModes.V2);
        dna.OASIS.SubscriptionConfig = new SubscriptionConfig
        {
            EnforceLocalQuota = true,
            MaxRequestsPerMonth = 0,
            PayAsYouGoEnabled = false
        };
        var providerManager = new ProviderManager(null, dna);
        var provider = CreateActiveProvider(ProviderType.IPFSOASIS, "load-balance-quota-provider");
        providerManager.RegisterProvider(provider.Object);
        var avatarId = Guid.NewGuid();

        var result = await new OASISHyperDrive(providerManager).LoadBalanceRequestAsync<IAvatar>(
            new StorageOperationRequest
            {
                Operation = "LoadAvatar",
                AvatarId = avatarId,
                PreferredProvider = ProviderType.IPFSOASIS
            });

        result.IsError.Should().BeTrue();
        result.Message.Should().Contain("Quota exceeded for Requests");
        provider.Verify(x => x.LoadAvatarAsync(It.IsAny<Guid>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task ExplicitV2FailoverCannotBypassFailoverQuota()
    {
        var dna = CreateDna(HyperDriveModes.V2);
        dna.OASIS.SubscriptionConfig = new SubscriptionConfig
        {
            EnforceLocalQuota = true,
            MaxFailoversPerMonth = 0,
            PayAsYouGoEnabled = false
        };
        var providerManager = new ProviderManager(null, dna);
        var provider = CreateActiveProvider(ProviderType.IPFSOASIS, "explicit-failover-quota-provider");
        providerManager.RegisterProvider(provider.Object);
        providerManager.SetAndReplaceAutoFailOverListForProviders(new[]
        {
            new EnumValue<ProviderType>(ProviderType.IPFSOASIS)
        }).IsError.Should().BeFalse();

        var result = await new OASISHyperDrive(providerManager).FailoverRequestAsync<IAvatar>(
            new StorageOperationRequest { Operation = "LoadAvatar", AvatarId = Guid.NewGuid() });

        result.IsError.Should().BeTrue();
        result.Message.Should().Contain("Quota exceeded for Failovers");
        provider.Verify(x => x.LoadAvatarAsync(It.IsAny<Guid>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task ExplicitV2ReplicationCannotBypassReplicationQuota()
    {
        var dna = CreateDna(HyperDriveModes.V2);
        dna.OASIS.SubscriptionConfig = new SubscriptionConfig
        {
            EnforceLocalQuota = true,
            MaxReplicationsPerMonth = 0,
            PayAsYouGoEnabled = false
        };
        var providerManager = new ProviderManager(null, dna);
        var provider = CreateActiveProvider(ProviderType.IPFSOASIS, "explicit-replication-quota-provider");
        providerManager.RegisterProvider(provider.Object);
        providerManager.SetAndReplaceAutoReplicationListForProviders(new[]
        {
            new EnumValue<ProviderType>(ProviderType.IPFSOASIS)
        }).IsError.Should().BeFalse();

        var result = await new OASISHyperDrive(providerManager).ReplicateRequestAsync<IHolon>(
            new StorageOperationRequest { Operation = "SaveHolon", Payload = new Holon { Id = Guid.NewGuid() } });

        result.IsError.Should().BeTrue();
        result.Message.Should().Contain("Quota exceeded for Replications");
        provider.Verify(x => x.SaveHolonAsync(It.IsAny<IHolon>(), It.IsAny<bool>(), It.IsAny<bool>(),
            It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public void KeyManagerLoadsAvatarKeysThroughItsInjectedRuntime()
    {
        var avatarId = Guid.NewGuid();
        var avatar = new Avatar { Id = avatarId };
        var dna = CreateDna(HyperDriveModes.V2);
        dna.OASIS.Security = new SecuritySettings();
        var providerManager = new ProviderManager(null, dna);
        var provider = CreateActiveProvider(ProviderType.IPFSOASIS, "isolated-keys");
        provider.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        provider.Setup(x => x.LoadAvatar(avatarId, 0))
            .Returns(new OASISResult<IAvatar>(avatar));
        providerManager.RegisterProvider(provider.Object);
        providerManager.SetAndActivateCurrentStorageProvider(provider.Object).IsError.Should().BeFalse();
        var singletonBefore = ProviderManager.Instance.CurrentStorageProvider;
        var manager = new KeyManager(null, dna, providerManager);

        var result = manager.GetAllProviderPublicKeysForAvatarById(avatarId, ProviderType.IPFSOASIS);

        result.IsError.Should().BeFalse(result.Message);
        result.Result.Should().NotBeNull().And.BeEmpty();
        ProviderManager.Instance.CurrentStorageProvider.Should().BeSameAs(singletonBefore);
        provider.Verify(x => x.LoadAvatar(avatarId, 0), Times.Once);
        provider.Verify(x => x.LoadAvatarAsync(It.IsAny<Guid>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public void V2ManagerProviderConfigurationRejectsASecondInstanceOfTheSameProviderType()
    {
        var registered = CreateActiveProvider(ProviderType.MongoDBOASIS, "registered-mongo");
        var differentInstance = CreateActiveProvider(ProviderType.MongoDBOASIS, "different-mongo");
        var providerManager = new ProviderManager(null);
        providerManager.RegisterProvider(registered.Object);

        var action = () => OASISManager.ConfigureStorageProvider(
            providerManager,
            differentInstance.Object,
            useHyperDriveV2: true);

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*different MongoDBOASIS provider instance is already registered*");
        providerManager.GetStorageProvider(ProviderType.MongoDBOASIS).Should().BeSameAs(registered.Object);
    }

    [Fact]
    public void ProviderResolutionReturnsExplicitProviderWithoutChangingCurrentProvider()
    {
        var current = CreateActiveProvider(ProviderType.MongoDBOASIS, "current-mongo");
        var selected = CreateActiveProvider(ProviderType.SQLLiteDBOASIS, "selected-sqlite");
        current.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        var providerManager = CreateProviderManager();
        providerManager.RegisterProvider(current.Object);
        providerManager.RegisterProvider(selected.Object);
        providerManager.SetAndActivateCurrentStorageProvider(current.Object).IsError.Should().BeFalse();

        var result = providerManager.ResolveStorageProvider(ProviderType.SQLLiteDBOASIS);

        result.IsError.Should().BeFalse();
        result.Result.Should().BeSameAs(selected.Object);
        providerManager.CurrentStorageProvider.Should().BeSameAs(current.Object);
    }

    [Fact]
    public async Task ProviderResolutionUsesCurrentForDefaultAndRejectsAll()
    {
        var current = CreateActiveProvider(ProviderType.MongoDBOASIS, "current-mongo");
        current.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        var providerManager = CreateProviderManager();
        providerManager.RegisterProvider(current.Object);
        providerManager.SetAndActivateCurrentStorageProvider(current.Object).IsError.Should().BeFalse();

        var defaultResult = await providerManager.ResolveStorageProviderAsync(ProviderType.Default);
        var allResult = providerManager.ResolveStorageProvider(ProviderType.All);

        defaultResult.IsError.Should().BeFalse();
        defaultResult.Result.Should().BeSameAs(current.Object);
        allResult.IsError.Should().BeTrue();
        allResult.Result.Should().BeNull();
        providerManager.CurrentStorageProvider.Should().BeSameAs(current.Object);
    }

    [Fact]
    public void WalletLoadInV2UsesTheRequestedLocalProviderWithoutChangingCurrentProvider()
    {
        var avatarId = Guid.NewGuid();
        var wallets = new Dictionary<ProviderType, List<IProviderWallet>>();
        var current = CreateActiveProvider(ProviderType.MongoDBOASIS, "current-mongo");
        current.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        var local = new Mock<IOASISStorageProvider>();
        local.SetupAllProperties();
        var localWallets = local.As<IOASISLocalStorageProvider>();
        local.Object.ProviderType = new EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS);
        local.Object.ProviderCategory = new EnumValue<ProviderCategory>(ProviderCategory.StorageLocal);
        local.Object.ProviderName = "edge-sqlite";
        local.Object.IsProviderActivated = true;
        localWallets.Setup(x => x.LoadProviderWalletsForAvatarById(avatarId))
            .Returns(new OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>(wallets));
        var dna = CreateDna(HyperDriveModes.V2);
        var providerManager = new ProviderManager(null, dna);
        providerManager.RegisterProvider(current.Object);
        providerManager.RegisterProvider(local.Object);
        providerManager.SetAndActivateCurrentStorageProvider(current.Object).IsError.Should().BeFalse();
        var walletManager = new WalletManager(null, dna, providerManager);

        var result = walletManager.LoadProviderWalletsForAvatarById(
            avatarId,
            providerTypeToLoadFrom: ProviderType.SQLLiteDBOASIS);

        result.IsError.Should().BeFalse();
        result.Result.Should().BeSameAs(wallets);
        providerManager.CurrentStorageProvider.Should().BeSameAs(current.Object);
        localWallets.Verify(x => x.LoadProviderWalletsForAvatarById(avatarId), Times.Once);
    }

    [Fact]
    public async Task WalletSaveInV2UsesTheRequestedLocalProviderWithoutChangingCurrentProvider()
    {
        var avatarId = Guid.NewGuid();
        var wallets = new Dictionary<ProviderType, List<IProviderWallet>>();
        var current = CreateActiveProvider(ProviderType.MongoDBOASIS, "current-mongo");
        current.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        var local = new Mock<IOASISStorageProvider>();
        local.SetupAllProperties();
        var localWallets = local.As<IOASISLocalStorageProvider>();
        local.Object.ProviderType = new EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS);
        local.Object.ProviderCategory = new EnumValue<ProviderCategory>(ProviderCategory.StorageLocal);
        local.Object.ProviderName = "edge-sqlite";
        local.Object.IsProviderActivated = true;
        localWallets.Setup(x => x.SaveProviderWalletsForAvatarByIdAsync(avatarId, wallets))
            .ReturnsAsync(new OASISResult<bool>(true));
        var dna = CreateDna(HyperDriveModes.V2);
        var providerManager = new ProviderManager(null, dna);
        providerManager.RegisterProvider(current.Object);
        providerManager.RegisterProvider(local.Object);
        providerManager.SetAndActivateCurrentStorageProvider(current.Object).IsError.Should().BeFalse();
        var walletManager = new WalletManager(null, dna, providerManager);

        var result = await walletManager.SaveProviderWalletsForAvatarByIdAsync(
            avatarId,
            wallets,
            ProviderType.SQLLiteDBOASIS);

        result.IsError.Should().BeFalse();
        result.Result.Should().BeTrue();
        result.IsSaved.Should().BeTrue();
        providerManager.CurrentStorageProvider.Should().BeSameAs(current.Object);
        localWallets.Verify(x => x.SaveProviderWalletsForAvatarByIdAsync(avatarId, wallets), Times.Once);
    }

    [Fact]
    public async Task AvatarProviderHelperInV2DeletesFromRequestedProviderWithoutChangingCurrentProvider()
    {
        var avatarId = Guid.NewGuid();
        var current = CreateActiveProvider(ProviderType.MongoDBOASIS, "current-mongo");
        var selected = CreateActiveProvider(ProviderType.SQLLiteDBOASIS, "selected-sqlite");
        current.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        selected.Setup(x => x.DeleteAvatarAsync(avatarId, true))
            .ReturnsAsync(new OASISResult<bool>(true));
        var dna = CreateDna(HyperDriveModes.V2);
        var providerManager = new ProviderManager(null, dna);
        providerManager.RegisterProvider(current.Object);
        providerManager.RegisterProvider(selected.Object);
        providerManager.SetAndActivateCurrentStorageProvider(current.Object).IsError.Should().BeFalse();
        var avatarManager = new AvatarManager(null, dna, providerManager);

        var result = await avatarManager.DeleteAvatarForProviderAsync(
            avatarId,
            new OASISResult<bool>(),
            SaveMode.FirstSaveAttempt,
            true,
            ProviderType.SQLLiteDBOASIS);

        result.IsError.Should().BeFalse();
        result.Result.Should().BeTrue();
        providerManager.CurrentStorageProvider.Should().BeSameAs(current.Object);
        selected.Verify(x => x.DeleteAvatarAsync(avatarId, true), Times.Once);
    }

    [Fact]
    public async Task AvatarProviderHelperSurfacesNullProviderResultWithoutThrowing()
    {
        var avatarId = Guid.NewGuid();
        var selected = CreateActiveProvider(ProviderType.SQLLiteDBOASIS, "selected-sqlite");
        selected.Setup(x => x.DeleteAvatarAsync(avatarId, true))
            .ReturnsAsync((OASISResult<bool>)null!);
        var dna = CreateDna(HyperDriveModes.V2);
        var providerManager = new ProviderManager(null, dna);
        providerManager.RegisterProvider(selected.Object);
        var avatarManager = new AvatarManager(null, dna, providerManager);

        var result = await avatarManager.DeleteAvatarForProviderAsync(
            avatarId,
            new OASISResult<bool>(),
            SaveMode.FirstSaveAttempt,
            true,
            ProviderType.SQLLiteDBOASIS);

        result.Result.Should().BeFalse();
        result.WarningCount.Should().BeGreaterThan(0);
        result.Message.Should().Contain("Unknown");
    }

    [Fact]
    public async Task AvatarSaveHelperInV2UsesRequestedProviderWithoutChangingCurrentProvider()
    {
        var avatarMock = new Mock<IAvatar>();
        avatarMock.SetupAllProperties();
        avatarMock.Object.Id = Guid.NewGuid();
        avatarMock.Object.Name = "Edge Avatar";
        avatarMock.Object.Username = "edge-avatar";
        avatarMock.Object.ProviderWallets = new Dictionary<ProviderType, List<IProviderWallet>>();
        var avatar = avatarMock.Object;
        var current = CreateActiveProvider(ProviderType.MongoDBOASIS, "current-mongo");
        var selected = CreateActiveProvider(ProviderType.SQLLiteDBOASIS, "selected-sqlite");
        current.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        selected.Setup(x => x.SaveAvatarAsync(avatar))
            .ReturnsAsync(new OASISResult<IAvatar>(avatar));
        var dna = CreateDna(HyperDriveModes.V2);
        var providerManager = new ProviderManager(null, dna);
        providerManager.RegisterProvider(current.Object);
        providerManager.RegisterProvider(selected.Object);
        providerManager.SetAndActivateCurrentStorageProvider(current.Object).IsError.Should().BeFalse();
        var avatarManager = new AvatarManager(null, dna, providerManager);

        var result = await avatarManager.SaveAvatarForProviderAsync(
            avatar,
            new OASISResult<IAvatar>(),
            SaveMode.FirstSaveAttempt,
            ProviderType.SQLLiteDBOASIS);

        result.IsSaved.Should().BeTrue();
        result.Result.Should().BeSameAs(avatar);
        providerManager.CurrentStorageProvider.Should().BeSameAs(current.Object);
        selected.Verify(x => x.SaveAvatarAsync(avatar), Times.Once);
    }

    [Fact]
    public void AvatarManagerUsesItsInjectedDnaAndProviderManagerForV2Routing()
    {
        var avatarId = Guid.NewGuid();
        var avatar = new Mock<IAvatar>().Object;
        var current = CreateActiveProvider(ProviderType.MongoDBOASIS, "current-mongo");
        var selected = CreateActiveProvider(ProviderType.SQLLiteDBOASIS, "selected-sqlite");
        current.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        selected.Setup(x => x.LoadAvatar(avatarId, 0))
            .Returns(new OASISResult<IAvatar>(avatar));
        var dna = CreateDna(HyperDriveModes.V2);
        var providerManager = new ProviderManager(null, dna);
        providerManager.RegisterProvider(current.Object);
        providerManager.RegisterProvider(selected.Object);
        providerManager.SetAndActivateCurrentStorageProvider(current.Object).IsError.Should().BeFalse();
        var avatarManager = new AvatarManager(null, dna, providerManager);

        var result = avatarManager.LoadAvatar(
            avatarId,
            loadPrivateKeys: false,
            hideAuthDetails: false,
            providerType: ProviderType.SQLLiteDBOASIS);

        result.IsError.Should().BeFalse();
        result.Result.Should().BeSameAs(avatar);
        providerManager.CurrentStorageProvider.Should().BeSameAs(current.Object);
        selected.Verify(x => x.LoadAvatar(avatarId, 0), Times.Once);
    }

    [Fact]
    public async Task AvatarDetailDeletesRouteAllKeysWithoutChangingCurrentProvider()
    {
        var avatarId = Guid.NewGuid();
        const string username = "edge-user";
        const string email = "edge@example.test";
        var current = CreateActiveProvider(ProviderType.MongoDBOASIS, "current-mongo");
        var selected = CreateActiveProvider(ProviderType.SQLLiteDBOASIS, "selected-sqlite");
        current.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        selected.Setup(x => x.DeleteAvatarDetail(avatarId, true)).Returns(new OASISResult<bool>(true));
        selected.Setup(x => x.DeleteAvatarDetailByUsername(username, true)).Returns(new OASISResult<bool>(true));
        selected.Setup(x => x.DeleteAvatarDetailByEmail(email, true)).Returns(new OASISResult<bool>(true));
        selected.Setup(x => x.DeleteAvatarDetailAsync(avatarId, false)).ReturnsAsync(new OASISResult<bool>(true));
        selected.Setup(x => x.DeleteAvatarDetailByUsernameAsync(username, false)).ReturnsAsync(new OASISResult<bool>(true));
        selected.Setup(x => x.DeleteAvatarDetailByEmailAsync(email, false)).ReturnsAsync(new OASISResult<bool>(true));
        var dna = CreateDna(HyperDriveModes.V2);
        var providerManager = new ProviderManager(null, dna);
        providerManager.RegisterProvider(current.Object);
        providerManager.RegisterProvider(selected.Object);
        providerManager.SetAndActivateCurrentStorageProvider(current.Object).IsError.Should().BeFalse();
        var manager = new AvatarManager(null, dna, providerManager);

        manager.DeleteAvatarDetail(avatarId, providerType: ProviderType.SQLLiteDBOASIS).Result.Should().BeTrue();
        manager.DeleteAvatarDetailByUsername(username, providerType: ProviderType.SQLLiteDBOASIS).Result.Should().BeTrue();
        manager.DeleteAvatarDetailByEmail(email, providerType: ProviderType.SQLLiteDBOASIS).Result.Should().BeTrue();
        (await manager.DeleteAvatarDetailAsync(avatarId, false, ProviderType.SQLLiteDBOASIS)).Result.Should().BeTrue();
        (await manager.DeleteAvatarDetailByUsernameAsync(username, false, ProviderType.SQLLiteDBOASIS)).Result.Should().BeTrue();
        (await manager.DeleteAvatarDetailByEmailAsync(email, false, ProviderType.SQLLiteDBOASIS)).Result.Should().BeTrue();

        providerManager.CurrentStorageProvider.Should().BeSameAs(current.Object);
        selected.VerifyAll();
    }

    [Fact]
    public async Task ProviderContractSoftDeletesAvatarDetailAndRejectsUnsupportedHardDelete()
    {
        var avatarId = Guid.NewGuid();
        var detail = new AvatarDetail { Id = avatarId, IsActive = true };
        var provider = new Mock<IOASISStorageProvider> { CallBase = true };
        provider.Setup(x => x.LoadAvatarDetailAsync(avatarId, 0))
            .ReturnsAsync(new OASISResult<IAvatarDetail>(detail));
        provider.Setup(x => x.SaveAvatarDetailAsync(detail))
            .ReturnsAsync(new OASISResult<IAvatarDetail>(detail));

        var softDelete = await provider.Object.DeleteAvatarDetailAsync(avatarId, true);
        var hardDelete = await provider.Object.DeleteAvatarDetailAsync(avatarId, false);

        softDelete.IsError.Should().BeFalse();
        softDelete.Result.Should().BeTrue();
        detail.IsActive.Should().BeFalse();
        detail.DeletedDate.Should().NotBe(default);
        hardDelete.IsError.Should().BeTrue();
        hardDelete.Message.Should().Contain("does not implement hard deletion");
        provider.Verify(x => x.SaveAvatarDetailAsync(detail), Times.Once);
    }

    [Fact]
    public void HolonManagerUsesItsInjectedDnaAndProviderManagerForV2Routing()
    {
        var holonId = Guid.NewGuid();
        var holon = new Mock<IHolon>().Object;
        var current = CreateActiveProvider(ProviderType.MongoDBOASIS, "current-mongo");
        var selected = CreateActiveProvider(ProviderType.SQLLiteDBOASIS, "selected-sqlite");
        current.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        selected.Setup(x => x.LoadHolon(holonId, false, false, 0, true, false, 0))
            .Returns(new OASISResult<IHolon>(holon));
        var dna = CreateDna(HyperDriveModes.V2);
        var providerManager = new ProviderManager(null, dna);
        providerManager.RegisterProvider(current.Object);
        providerManager.RegisterProvider(selected.Object);
        providerManager.SetAndActivateCurrentStorageProvider(current.Object).IsError.Should().BeFalse();
        var holonManager = new HolonManager(null, dna, providerManager);

        var result = holonManager.LoadHolon(
            holonId,
            loadChildren: false,
            recursive: false,
            providerType: ProviderType.SQLLiteDBOASIS);

        result.IsError.Should().BeFalse();
        result.Result.Should().BeSameAs(holon);
        providerManager.CurrentStorageProvider.Should().BeSameAs(current.Object);
        selected.Verify(x => x.LoadHolon(holonId, false, false, 0, true, false, 0), Times.Once);
    }

    [Fact]
    public async Task HyperDriveUsesTheInjectedRuntimeSubscriptionPolicy()
    {
        var holonId = Guid.NewGuid();
        var selected = CreateActiveProvider(ProviderType.SQLLiteDBOASIS, "selected-sqlite");
        var dna = CreateDna(HyperDriveModes.V2);
        dna.OASIS.SubscriptionConfig = new SubscriptionConfig
        {
            EnforceLocalQuota = true,
            MaxRequestsPerMonth = 0,
            PayAsYouGoEnabled = false
        };
        var providerManager = new ProviderManager(null, dna);
        providerManager.RegisterProvider(selected.Object);
        var holonManager = new HolonManager(null, dna, providerManager);

        var result = await holonManager.LoadHolonAsync(
            holonId,
            loadChildren: false,
            recursive: false,
            providerType: ProviderType.SQLLiteDBOASIS);

        result.IsError.Should().BeTrue();
        result.Message.Should().Contain("Quota exceeded for Requests");
        selected.Verify(x => x.LoadHolonAsync(
            It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<int>(),
            It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public void SearchManagerUsesItsInjectedDnaAndProviderManagerForV2Routing()
    {
        ISearchParams search = new SearchParams();
        ISearchResults searchResults = new SearchResults();
        var current = CreateActiveProvider(ProviderType.MongoDBOASIS, "current-mongo");
        var selected = CreateActiveProvider(ProviderType.SQLLiteDBOASIS, "selected-sqlite");
        current.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        selected.Setup(x => x.Search(search, false, false, 0, true, 0))
            .Returns(new OASISResult<ISearchResults>(searchResults));
        var dna = CreateDna(HyperDriveModes.V2);
        var providerManager = new ProviderManager(null, dna);
        providerManager.RegisterProvider(current.Object);
        providerManager.RegisterProvider(selected.Object);
        providerManager.SetAndActivateCurrentStorageProvider(current.Object).IsError.Should().BeFalse();
        var searchManager = new SearchManager(null, dna, providerManager);

        var result = searchManager.Search(
            search,
            ProviderType.SQLLiteDBOASIS,
            loadChildren: false,
            recursive: false);

        result.IsError.Should().BeFalse();
        result.Result.Should().NotBeNull();
        providerManager.CurrentStorageProvider.Should().BeSameAs(current.Object);
        selected.Verify(x => x.Search(search, false, false, 0, true, 0), Times.Once);
    }

    [Fact]
    public void SearchAllEnumeratesOnlyTheManagersInjectedRuntimeProviders()
    {
        ISearchParams search = new SearchParams();
        var mongo = CreateActiveProvider(ProviderType.MongoDBOASIS, "isolated-search-mongo");
        var sqlite = CreateActiveProvider(ProviderType.SQLLiteDBOASIS, "isolated-search-sqlite");
        mongo.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        mongo.Setup(x => x.Search(search, true, true, 0, true, 0))
            .Returns(new OASISResult<ISearchResults>(new SearchResults()));
        sqlite.Setup(x => x.Search(search, true, true, 0, true, 0))
            .Returns(new OASISResult<ISearchResults>(new SearchResults()));
        var dna = CreateDna(HyperDriveModes.V2);
        var providerManager = new ProviderManager(null, dna);
        providerManager.RegisterProvider(mongo.Object).Should().BeTrue();
        providerManager.RegisterProvider(sqlite.Object).Should().BeTrue();
        providerManager.SetAndActivateCurrentStorageProvider(mongo.Object).IsError.Should().BeFalse();

        var result = new SearchManager(null, dna, providerManager)
            .Search(search, ProviderType.All);

        result.IsError.Should().BeFalse(result.Message);
        result.Result.Should().NotBeNull();
        providerManager.CurrentStorageProvider.Should().BeSameAs(mongo.Object);
        mongo.Verify(x => x.Search(search, true, true, 0, true, 0), Times.Once);
        sqlite.Verify(x => x.Search(search, true, true, 0, true, 0), Times.Once);
    }

    [Fact]
    public void SynchronousStorageOperationUsesSynchronousProviderWithoutChangingGlobalProvider()
    {
        var holonId = Guid.NewGuid();
        var holon = new Mock<IHolon>().Object;
        var provider = CreateActiveProvider();
        provider.Setup(x => x.LoadHolon(holonId, false, false, 3, false, true, 7))
            .Returns(new OASISResult<IHolon> { Result = holon });
        var manager = new ProviderManager(null);
        manager.RegisterProvider(provider.Object).Should().BeTrue();
        var original = manager.CurrentStorageProviderType.Value;

        var result = new OASISHyperDrive(manager).RouteRequestToProvider<IHolon>(new StorageOperationRequest
        {
            Operation = "LoadHolon", HolonId = holonId, LoadChildren = false, Recursive = false,
            MaxChildDepth = 3, ContinueOnError = false, ChildrenFromProvider = true, Version = 7
        }, ProviderType.MongoDBOASIS);

        result.IsError.Should().BeFalse(result.Message);
        result.Result.Should().BeSameAs(holon);
        manager.CurrentStorageProviderType.Value.Should().Be(original);
        provider.Verify(x => x.LoadHolon(holonId, false, false, 3, false, true, 7), Times.Once);
        provider.Verify(x => x.LoadHolonAsync(It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<bool>(),
            It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public void SynchronousUnsupportedOperationReturnsStructuredErrorWithoutLegacyFallback()
    {
        var provider = CreateActiveProvider();
        var manager = new ProviderManager(null);
        manager.RegisterProvider(provider.Object);

        var result = new OASISHyperDrive(manager).RouteRequestToProvider<object>(new StorageOperationRequest
        { Operation = "NotARealOperation" }, ProviderType.MongoDBOASIS);

        result.IsError.Should().BeTrue();
        result.ErrorCode.Should().Be("HYPERDRIVE_OPERATION_UNSUPPORTED");
    }

    [Fact]
    public void SynchronousBatchSavePreservesEveryProviderOption()
    {
        var holons = new[] { new Mock<IHolon>().Object, new Mock<IHolon>().Object };
        var provider = CreateActiveProvider();
        provider.Setup(x => x.SaveHolons(holons, false, false, 5, 2, false, true))
            .Returns(new OASISResult<IEnumerable<IHolon>> { Result = holons });
        var manager = new ProviderManager(null);
        manager.RegisterProvider(provider.Object);

        var result = new OASISHyperDrive(manager).RouteRequestToProvider<IEnumerable<IHolon>>(
            new StorageOperationRequest
            {
                Operation = "SaveHolons", Payload = holons, SaveChildren = false, Recursive = false,
                MaxChildDepth = 5, CurrentChildDepth = 2, ContinueOnError = false,
                ChildrenFromProvider = true
            }, ProviderType.MongoDBOASIS);

        result.IsError.Should().BeFalse(result.Message);
        result.Result.Should().BeSameAs(holons);
        provider.Verify(x => x.SaveHolons(holons, false, false, 5, 2, false, true), Times.Once);
    }

    [Fact]
    public void SynchronousMetadataLoadPreservesScopeAndTraversalOptions()
    {
        var avatarId = Guid.NewGuid();
        var holons = new[] { new Mock<IHolon>().Object };
        var provider = CreateActiveProvider();
        provider.Setup(x => x.LoadHolonsByMetaData("kind", "quest", avatarId, true,
                HolonType.Quest, false, false, 4, 1, false, true, 3))
            .Returns(new OASISResult<IEnumerable<IHolon>> { Result = holons });
        var manager = new ProviderManager(null);
        manager.RegisterProvider(provider.Object);

        var result = new OASISHyperDrive(manager).RouteRequestToProvider<IEnumerable<IHolon>>(
            new StorageOperationRequest
            {
                Operation = "LoadHolonsByMetaData", MetaKey = "kind", MetaValue = "quest",
                AvatarId = avatarId, IncludePublic = true, HolonType = HolonType.Quest,
                LoadChildren = false, Recursive = false, MaxChildDepth = 4, CurrentChildDepth = 1,
                ContinueOnError = false, ChildrenFromProvider = true, Version = 3
            }, ProviderType.MongoDBOASIS);

        result.IsError.Should().BeFalse(result.Message);
        result.Result.Should().BeSameAs(holons);
        provider.VerifyAll();
    }

    [Fact]
    public void SynchronousAvatarOperationsUseOnlySynchronousProviderContracts()
    {
        var avatarId = Guid.NewGuid();
        var avatar = new Mock<IAvatar>().Object;
        var detail = new Mock<IAvatarDetail>().Object;
        var provider = CreateActiveProvider();
        provider.Setup(x => x.LoadAvatar(avatarId, 4)).Returns(new OASISResult<IAvatar>(avatar));
        provider.Setup(x => x.SaveAvatar(avatar)).Returns(new OASISResult<IAvatar>(avatar));
        provider.Setup(x => x.LoadAvatarDetailByEmail("edge@example.com", 4))
            .Returns(new OASISResult<IAvatarDetail>(detail));
        provider.Setup(x => x.DeleteAvatarByUsername("edge", true))
            .Returns(new OASISResult<bool>(true));
        var manager = new ProviderManager(null);
        manager.RegisterProvider(provider.Object);
        var hyperDrive = new OASISHyperDrive(manager);

        hyperDrive.RouteRequestToProvider<IAvatar>(new StorageOperationRequest
            { Operation = "LoadAvatar", AvatarId = avatarId, Version = 4 }, ProviderType.MongoDBOASIS)
            .Result.Should().BeSameAs(avatar);
        hyperDrive.RouteRequestToProvider<IAvatar>(new StorageOperationRequest
            { Operation = "SaveAvatar", Payload = avatar }, ProviderType.MongoDBOASIS)
            .Result.Should().BeSameAs(avatar);
        hyperDrive.RouteRequestToProvider<IAvatarDetail>(new StorageOperationRequest
            { Operation = "LoadAvatarDetailByEmail", Email = "edge@example.com", Version = 4 }, ProviderType.MongoDBOASIS)
            .Result.Should().BeSameAs(detail);
        hyperDrive.RouteRequestToProvider<bool>(new StorageOperationRequest
            { Operation = "DeleteAvatarByUsername", Username = "edge", SoftDelete = true }, ProviderType.MongoDBOASIS)
            .Result.Should().BeTrue();

        provider.VerifyAll();
        provider.Verify(x => x.LoadAvatarAsync(It.IsAny<Guid>(), It.IsAny<int>()), Times.Never);
        provider.Verify(x => x.SaveAvatarAsync(It.IsAny<IAvatar>()), Times.Never);
    }

    [Fact]
    public void SynchronousSearchPreservesQueryAndNeverInvokesAsyncProvider()
    {
        ISearchParams search = new SearchParams { AvatarId = Guid.NewGuid(), SearchOnlyForCurrentAvatar = true };
        ISearchResults searchResults = new SearchResults();
        var provider = CreateActiveProvider();
        provider.Setup(x => x.Search(search, false, false, 4, false, 9))
            .Returns(new OASISResult<ISearchResults>(searchResults));
        var manager = new ProviderManager(null);
        manager.RegisterProvider(provider.Object);

        var result = new OASISHyperDrive(manager).RouteRequestToProvider<ISearchResults>(
            new StorageOperationRequest
            {
                Operation = "Search", SearchParams = search, LoadChildren = false, Recursive = false,
                MaxChildDepth = 4, ContinueOnError = false, Version = 9
            }, ProviderType.MongoDBOASIS);

        result.IsError.Should().BeFalse(result.Message);
        result.Result.Should().BeSameAs(searchResults);
        provider.Verify(x => x.Search(search, false, false, 4, false, 9), Times.Once);
        provider.Verify(x => x.SearchAsync(It.IsAny<ISearchParams>(), It.IsAny<bool>(), It.IsAny<bool>(),
            It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task StorageOperationExecutesDirectlyOnPreferredProviderWithoutChangingGlobalProvider()
    {
        var holonId = Guid.NewGuid();
        var holon = new Mock<IHolon>().Object;
        var provider = new Mock<IOASISStorageProvider>();
        provider.SetupAllProperties();
        provider.Object.ProviderType = new EnumValue<ProviderType>(ProviderType.MongoDBOASIS);
        provider.Object.ProviderCategory = new EnumValue<ProviderCategory>(ProviderCategory.Storage);
        provider.Object.ProviderName = "test-mongo";
        provider.Object.IsProviderActivated = true;
        provider.Setup(x => x.LoadHolonAsync(holonId, true, true, 0, true, false, 0))
            .ReturnsAsync(new OASISResult<IHolon> { Result = holon });
        var manager = new ProviderManager(null);
        manager.RegisterProvider(provider.Object).Should().BeTrue();
        var originalGlobalProvider = manager.CurrentStorageProviderType.Value;
        var hyperDrive = new OASISHyperDrive(manager);

        var result = await hyperDrive.LoadBalanceRequestAsync<IHolon>(new StorageOperationRequest
        {
            Operation = "LoadHolon", HolonId = holonId, PreferredProvider = ProviderType.MongoDBOASIS
        });

        result.IsError.Should().BeFalse(result.Message);
        result.Result.Should().BeSameAs(holon);
        manager.CurrentStorageProviderType.Value.Should().Be(originalGlobalProvider);
        provider.Verify(x => x.LoadHolonAsync(holonId, true, true, 0, true, false, 0), Times.Once);
    }

    [Fact]
    public async Task UnsupportedStorageOperationReturnsStructuredErrorWithoutCallingProvider()
    {
        var provider = new Mock<IOASISStorageProvider>();
        provider.SetupAllProperties();
        provider.Object.ProviderType = new EnumValue<ProviderType>(ProviderType.MongoDBOASIS);
        provider.Object.ProviderCategory = new EnumValue<ProviderCategory>(ProviderCategory.Storage);
        provider.Object.ProviderName = "test-mongo";
        provider.Object.IsProviderActivated = true;
        var manager = new ProviderManager(null);
        manager.RegisterProvider(provider.Object);

        var result = await new OASISHyperDrive(manager).LoadBalanceRequestAsync<object>(new StorageOperationRequest
        { Operation = "NotARealOperation", PreferredProvider = ProviderType.MongoDBOASIS });

        result.IsError.Should().BeTrue();
        result.ErrorCode.Should().Be("HYPERDRIVE_OPERATION_UNSUPPORTED");
    }

    [Fact]
    public async Task HolonLoadPreservesEveryManagerOptionAtTheProviderBoundary()
    {
        var holonId = Guid.NewGuid();
        var holon = new Mock<IHolon>().Object;
        var provider = CreateActiveProvider();
        provider.Setup(x => x.LoadHolonAsync(holonId, false, false, 3, false, true, 7))
            .ReturnsAsync(new OASISResult<IHolon> { Result = holon });
        var manager = new ProviderManager(null);
        manager.RegisterProvider(provider.Object);

        var result = await new OASISHyperDrive(manager).LoadBalanceRequestAsync<IHolon>(new StorageOperationRequest
        {
            Operation = "LoadHolon",
            HolonId = holonId,
            PreferredProvider = ProviderType.MongoDBOASIS,
            LoadChildren = false,
            Recursive = false,
            MaxChildDepth = 3,
            ContinueOnError = false,
            ChildrenFromProvider = true,
            Version = 7
        });

        result.IsError.Should().BeFalse(result.Message);
        result.Result.Should().BeSameAs(holon);
        provider.Verify(x => x.LoadHolonAsync(holonId, false, false, 3, false, true, 7), Times.Once);
    }

    [Fact]
    public async Task ProviderKeyLoadPreservesKeyAndEveryOptionAtTheProviderBoundary()
    {
        const string providerKey = "provider-native-key";
        var holon = new Mock<IHolon>().Object;
        var provider = CreateActiveProvider();
        provider.Setup(x => x.LoadHolonAsync(providerKey, false, false, 4, false, true, 9))
            .ReturnsAsync(new OASISResult<IHolon> { Result = holon });
        var manager = new ProviderManager(null);
        manager.RegisterProvider(provider.Object);

        var result = await new OASISHyperDrive(manager).LoadBalanceRequestAsync<IHolon>(new StorageOperationRequest
        {
            Operation = "LoadHolonByProviderKey",
            ProviderKey = providerKey,
            PreferredProvider = ProviderType.MongoDBOASIS,
            LoadChildren = false,
            Recursive = false,
            MaxChildDepth = 4,
            ContinueOnError = false,
            ChildrenFromProvider = true,
            Version = 9
        });

        result.IsError.Should().BeFalse(result.Message);
        result.Result.Should().BeSameAs(holon);
        provider.Verify(x => x.LoadHolonAsync(providerKey, false, false, 4, false, true, 9), Times.Once);
    }

    [Fact]
    public async Task ProviderKeyLoadRejectsAnEmptyKeyBeforeCallingProvider()
    {
        var provider = CreateActiveProvider();
        var manager = new ProviderManager(null);
        manager.RegisterProvider(provider.Object);

        var result = await new OASISHyperDrive(manager).LoadBalanceRequestAsync<IHolon>(new StorageOperationRequest
        {
            Operation = "LoadHolonByProviderKey",
            ProviderKey = " ",
            PreferredProvider = ProviderType.MongoDBOASIS
        });

        result.IsError.Should().BeTrue();
        result.ErrorCode.Should().Be("HYPERDRIVE_PROVIDER_KEY_REQUIRED");
        provider.Verify(x => x.LoadHolonAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(),
            It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task BatchSavePreservesEveryOptionAtTheProviderBoundary()
    {
        var holons = new[] { new Mock<IHolon>().Object, new Mock<IHolon>().Object };
        var provider = CreateActiveProvider();
        provider.Setup(x => x.SaveHolonsAsync(holons, false, false, 5, 2, false, true))
            .ReturnsAsync(new OASISResult<IEnumerable<IHolon>> { Result = holons });
        var manager = new ProviderManager(null);
        manager.RegisterProvider(provider.Object);

        var result = await new OASISHyperDrive(manager).LoadBalanceRequestAsync<IEnumerable<IHolon>>(new StorageOperationRequest
        {
            Operation = "SaveHolons",
            Payload = holons,
            PreferredProvider = ProviderType.MongoDBOASIS,
            SaveChildren = false,
            Recursive = false,
            MaxChildDepth = 5,
            CurrentChildDepth = 2,
            ContinueOnError = false,
            ChildrenFromProvider = true
        });

        result.IsError.Should().BeFalse(result.Message);
        result.Result.Should().BeSameAs(holons);
        provider.Verify(x => x.SaveHolonsAsync(holons, false, false, 5, 2, false, true), Times.Once);
    }

    [Fact]
    public async Task HardDeleteExecutesDirectlyOnPreferredProvider()
    {
        var holonId = Guid.NewGuid();
        var provider = CreateActiveProvider();
        var deleted = new Mock<IHolon>().Object;
        provider.Setup(x => x.DeleteHolonAsync(holonId))
            .ReturnsAsync(new OASISResult<IHolon> { Result = deleted });
        var manager = new ProviderManager(null);
        manager.RegisterProvider(provider.Object);

        var result = await new OASISHyperDrive(manager).LoadBalanceRequestAsync<IHolon>(new StorageOperationRequest
        {
            Operation = "DeleteHolon",
            HolonId = holonId,
            PreferredProvider = ProviderType.MongoDBOASIS,
            SoftDelete = false
        });

        result.IsError.Should().BeFalse(result.Message);
        result.Result.Should().BeSameAs(deleted);
        provider.Verify(x => x.DeleteHolonAsync(holonId), Times.Once);
    }

    [Fact]
    public async Task ParentLoadPreservesVisibilityAndTraversalOptionsAtProviderBoundary()
    {
        var parentId = Guid.NewGuid();
        var avatarId = Guid.NewGuid();
        var holons = new[] { new Mock<IHolon>().Object };
        var provider = CreateActiveProvider();
        provider.Setup(x => x.LoadHolonsForParentAsync(parentId, avatarId, true, HolonType.Quest,
                false, false, 6, 2, false, true, 4))
            .ReturnsAsync(new OASISResult<IEnumerable<IHolon>> { Result = holons });
        var manager = new ProviderManager(null);
        manager.RegisterProvider(provider.Object);

        var result = await new OASISHyperDrive(manager).LoadBalanceRequestAsync<IEnumerable<IHolon>>(new StorageOperationRequest
        {
            Operation = "LoadHolonsForParent",
            HolonId = parentId,
            AvatarId = avatarId,
            IncludePublic = true,
            HolonType = HolonType.Quest,
            PreferredProvider = ProviderType.MongoDBOASIS,
            LoadChildren = false,
            Recursive = false,
            MaxChildDepth = 6,
            CurrentChildDepth = 2,
            ContinueOnError = false,
            ChildrenFromProvider = true,
            Version = 4
        });

        result.IsError.Should().BeFalse(result.Message);
        result.Result.Should().BeSameAs(holons);
        provider.VerifyAll();
    }

    [Fact]
    public async Task MetadataLoadPreservesVisibilityAndTraversalOptionsAtProviderBoundary()
    {
        var avatarId = Guid.NewGuid();
        var holons = new[] { new Mock<IHolon>().Object };
        var provider = CreateActiveProvider();
        provider.Setup(x => x.LoadHolonsByMetaDataAsync("kind", "quest", avatarId, true, HolonType.Quest,
                false, false, 4, 1, false, true, 3))
            .ReturnsAsync(new OASISResult<IEnumerable<IHolon>> { Result = holons });
        var manager = new ProviderManager(null);
        manager.RegisterProvider(provider.Object);

        var result = await new OASISHyperDrive(manager).LoadBalanceRequestAsync<IEnumerable<IHolon>>(new StorageOperationRequest
        {
            Operation = "LoadHolonsByMetaData",
            MetaKey = "kind",
            MetaValue = "quest",
            AvatarId = avatarId,
            IncludePublic = true,
            HolonType = HolonType.Quest,
            PreferredProvider = ProviderType.MongoDBOASIS,
            LoadChildren = false,
            Recursive = false,
            MaxChildDepth = 4,
            CurrentChildDepth = 1,
            ContinueOnError = false,
            ChildrenFromProvider = true,
            Version = 3
        });

        result.IsError.Should().BeFalse(result.Message);
        result.Result.Should().BeSameAs(holons);
        provider.VerifyAll();
    }

    [Fact]
    public async Task MetadataPairLoadPreservesMatchModeAtProviderBoundary()
    {
        var pairs = new Dictionary<string, string> { ["kind"] = "quest", ["state"] = "active" };
        var holons = new[] { new Mock<IHolon>().Object };
        var provider = CreateActiveProvider();
        provider.Setup(x => x.LoadHolonsByMetaDataAsync(pairs, MetaKeyValuePairMatchMode.All,
                HolonType.Quest, true, true, 2, 0, true, false, 5))
            .ReturnsAsync(new OASISResult<IEnumerable<IHolon>> { Result = holons });
        var manager = new ProviderManager(null);
        manager.RegisterProvider(provider.Object);

        var result = await new OASISHyperDrive(manager).LoadBalanceRequestAsync<IEnumerable<IHolon>>(new StorageOperationRequest
        {
            Operation = "LoadHolonsByMetaDataPairs",
            MetaKeyValuePairs = pairs,
            MetaKeyValuePairMatchMode = MetaKeyValuePairMatchMode.All,
            HolonType = HolonType.Quest,
            PreferredProvider = ProviderType.MongoDBOASIS,
            MaxChildDepth = 2,
            Version = 5
        });

        result.IsError.Should().BeFalse(result.Message);
        result.Result.Should().BeSameAs(holons);
        provider.VerifyAll();
    }

    [Fact]
    public async Task AvatarDetailOperationsExecuteDirectlyOnPreferredProvider()
    {
        var avatarId = Guid.NewGuid();
        var detail = new Mock<IAvatarDetail>().Object;
        var provider = CreateActiveProvider();
        provider.Setup(x => x.LoadAvatarDetailAsync(avatarId, 8))
            .ReturnsAsync(new OASISResult<IAvatarDetail> { Result = detail });
        provider.Setup(x => x.SaveAvatarDetailAsync(detail))
            .ReturnsAsync(new OASISResult<IAvatarDetail> { Result = detail });
        var manager = new ProviderManager(null);
        manager.RegisterProvider(provider.Object);
        var hyperDrive = new OASISHyperDrive(manager);

        var loaded = await hyperDrive.LoadBalanceRequestAsync<IAvatarDetail>(new StorageOperationRequest
        { Operation = "LoadAvatarDetail", AvatarId = avatarId, Version = 8, PreferredProvider = ProviderType.MongoDBOASIS });
        var saved = await hyperDrive.LoadBalanceRequestAsync<IAvatarDetail>(new StorageOperationRequest
        { Operation = "SaveAvatarDetail", Payload = detail, PreferredProvider = ProviderType.MongoDBOASIS });

        loaded.IsError.Should().BeFalse(loaded.Message);
        saved.IsError.Should().BeFalse(saved.Message);
        loaded.Result.Should().BeSameAs(detail);
        saved.Result.Should().BeSameAs(detail);
        provider.VerifyAll();
    }

    [Fact]
    public async Task AvatarUsernameLookupPreservesVersionAtProviderBoundary()
    {
        var avatar = new Mock<IAvatar>().Object;
        var provider = CreateActiveProvider();
        provider.Setup(x => x.LoadAvatarByUsernameAsync("edge-user", 6))
            .ReturnsAsync(new OASISResult<IAvatar> { Result = avatar });
        var manager = new ProviderManager(null);
        manager.RegisterProvider(provider.Object);

        var result = await new OASISHyperDrive(manager).LoadBalanceRequestAsync<IAvatar>(new StorageOperationRequest
        { Operation = "LoadAvatarByUsername", Username = "edge-user", Version = 6, PreferredProvider = ProviderType.MongoDBOASIS });

        result.IsError.Should().BeFalse(result.Message);
        result.Result.Should().BeSameAs(avatar);
        provider.VerifyAll();
    }

    [Fact]
    public async Task AvatarDeletePreservesSoftDeleteAtProviderBoundary()
    {
        var avatarId = Guid.NewGuid();
        var provider = CreateActiveProvider();
        provider.Setup(x => x.DeleteAvatarAsync(avatarId, true))
            .ReturnsAsync(new OASISResult<bool> { Result = true });
        var manager = new ProviderManager(null);
        manager.RegisterProvider(provider.Object);

        var result = await new OASISHyperDrive(manager).LoadBalanceRequestAsync<bool>(new StorageOperationRequest
        { Operation = "DeleteAvatar", AvatarId = avatarId, SoftDelete = true, PreferredProvider = ProviderType.MongoDBOASIS });

        result.IsError.Should().BeFalse(result.Message);
        result.Result.Should().BeTrue();
        provider.VerifyAll();
    }

    [Fact]
    public async Task LoadAllAvatarsPreservesVersionAtProviderBoundary()
    {
        var avatars = new[] { new Mock<IAvatar>().Object };
        var provider = CreateActiveProvider();
        provider.Setup(x => x.LoadAllAvatarsAsync(12))
            .ReturnsAsync(new OASISResult<IEnumerable<IAvatar>> { Result = avatars });
        var manager = new ProviderManager(null);
        manager.RegisterProvider(provider.Object);

        var result = await new OASISHyperDrive(manager).LoadBalanceRequestAsync<IEnumerable<IAvatar>>(new StorageOperationRequest
        { Operation = "LoadAllAvatars", Version = 12, PreferredProvider = ProviderType.MongoDBOASIS });

        result.IsError.Should().BeFalse(result.Message);
        result.Result.Should().BeSameAs(avatars);
        provider.VerifyAll();
    }

    [Fact]
    public async Task LoadAllHolonsPreservesVisibilityAndTraversalOptionsAtProviderBoundary()
    {
        var avatarId = Guid.NewGuid();
        var holons = new[] { new Mock<IHolon>().Object };
        var provider = CreateActiveProvider();
        provider.Setup(x => x.LoadAllHolonsAsync(avatarId, true, HolonType.Quest, false, false,
                7, 3, false, true, 11))
            .ReturnsAsync(new OASISResult<IEnumerable<IHolon>> { Result = holons });
        var manager = new ProviderManager(null);
        manager.RegisterProvider(provider.Object);

        var result = await new OASISHyperDrive(manager).LoadBalanceRequestAsync<IEnumerable<IHolon>>(
            new StorageOperationRequest
            {
                Operation = "LoadAllHolons", AvatarId = avatarId, IncludePublic = true,
                HolonType = HolonType.Quest, LoadChildren = false, Recursive = false,
                MaxChildDepth = 7, CurrentChildDepth = 3, ContinueOnError = false,
                ChildrenFromProvider = true, Version = 11,
                PreferredProvider = ProviderType.MongoDBOASIS
            });

        result.IsError.Should().BeFalse(result.Message);
        result.Result.Should().BeSameAs(holons);
        provider.VerifyAll();
    }

    [Fact]
    public async Task SearchPreservesQueryAndTraversalOptionsAtProviderBoundary()
    {
        ISearchParams search = new SearchParams { AvatarId = Guid.NewGuid(), SearchOnlyForCurrentAvatar = true };
        ISearchResults searchResults = new SearchResults();
        var provider = CreateActiveProvider();
        provider.Setup(x => x.SearchAsync(search, false, false, 4, false, 9))
            .ReturnsAsync(new OASISResult<ISearchResults> { Result = searchResults });
        var manager = new ProviderManager(null);
        manager.RegisterProvider(provider.Object);

        var result = await new OASISHyperDrive(manager).LoadBalanceRequestAsync<ISearchResults>(
            new StorageOperationRequest
            {
                Operation = "Search", SearchParams = search, LoadChildren = false, Recursive = false,
                MaxChildDepth = 4, ContinueOnError = false, Version = 9,
                PreferredProvider = ProviderType.MongoDBOASIS
            });

        result.IsError.Should().BeFalse(result.Message);
        result.Result.Should().BeSameAs(searchResults);
        provider.VerifyAll();
    }

    [Fact]
    public async Task ExplicitProviderRouteDoesNotFailOverOrMutateGlobalProvider()
    {
        ISearchParams search = new SearchParams();
        ISearchResults searchResults = new SearchResults();
        var selected = CreateActiveProvider();
        selected.Setup(x => x.SearchAsync(search, true, true, 0, true, 0))
            .ReturnsAsync(new OASISResult<ISearchResults> { Result = searchResults });
        var other = new Mock<IOASISStorageProvider>();
        other.SetupAllProperties();
        other.Object.ProviderType = new EnumValue<ProviderType>(ProviderType.Neo4jOASIS);
        other.Object.ProviderCategory = new EnumValue<ProviderCategory>(ProviderCategory.Storage);
        other.Object.ProviderName = "other";
        other.Object.IsProviderActivated = true;
        var manager = new ProviderManager(null);
        manager.RegisterProvider(selected.Object);
        manager.RegisterProvider(other.Object);
        var original = manager.CurrentStorageProviderType.Value;

        var result = await new OASISHyperDrive(manager).RouteRequestToProviderAsync<ISearchResults>(
            new StorageOperationRequest { Operation = "Search", SearchParams = search,
                PreferredProvider = ProviderType.MongoDBOASIS }, ProviderType.MongoDBOASIS);

        result.IsError.Should().BeFalse(result.Message);
        result.Result.Should().BeSameAs(searchResults);
        manager.CurrentStorageProviderType.Value.Should().Be(original);
        other.Verify(x => x.SearchAsync(It.IsAny<ISearchParams>(), It.IsAny<bool>(), It.IsAny<bool>(),
            It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task IndexedAvatarLookupsExecuteDirectlyAndPreserveVersion()
    {
        var avatar = new Mock<IAvatar>().Object;
        var provider = CreateActiveProvider();
        provider.Setup(x => x.LoadAvatarByProviderKeyAsync("provider-key", 6)).ReturnsAsync(new OASISResult<IAvatar>(avatar));
        provider.Setup(x => x.LoadAvatarByVerificationTokenAsync("verify", 6)).ReturnsAsync(new OASISResult<IAvatar>(avatar));
        provider.Setup(x => x.LoadAvatarByResetTokenAsync("reset", 6)).ReturnsAsync(new OASISResult<IAvatar>(avatar));
        provider.Setup(x => x.LoadAvatarByRefreshTokenAsync("refresh", 6)).ReturnsAsync(new OASISResult<IAvatar>(avatar));
        provider.Setup(x => x.LoadAvatarByPublicKeyAsync("public", 6)).ReturnsAsync(new OASISResult<IAvatar>(avatar));
        provider.Setup(x => x.LoadAvatarByPrivateKeyAsync("private", 6)).ReturnsAsync(new OASISResult<IAvatar>(avatar));
        var manager = new ProviderManager(null);
        manager.RegisterProvider(provider.Object);
        var hyperDrive = new OASISHyperDrive(manager);

        var requests = new[]
        {
            new StorageOperationRequest { Operation = "LoadAvatarByProviderKey", ProviderKey = "provider-key" },
            new StorageOperationRequest { Operation = "LoadAvatarByVerificationToken", VerificationToken = "verify" },
            new StorageOperationRequest { Operation = "LoadAvatarByResetToken", ResetToken = "reset" },
            new StorageOperationRequest { Operation = "LoadAvatarByRefreshToken", RefreshToken = "refresh" },
            new StorageOperationRequest { Operation = "LoadAvatarByPublicKey", PublicKey = "public" },
            new StorageOperationRequest { Operation = "LoadAvatarByPrivateKey", PrivateKey = "private" }
        };
        foreach (var request in requests)
        {
            request.Version = 6;
            request.PreferredProvider = ProviderType.MongoDBOASIS;
            var result = await hyperDrive.RouteRequestToProviderAsync<IAvatar>(request, ProviderType.MongoDBOASIS);
            result.IsError.Should().BeFalse(result.Message);
            result.Result.Should().BeSameAs(avatar);
        }
        provider.VerifyAll();
    }

    [Fact]
    public async Task IndexedAvatarLookupRejectsEmptySecretBeforeProviderCall()
    {
        var provider = CreateActiveProvider();
        var manager = new ProviderManager(null);
        manager.RegisterProvider(provider.Object);

        var result = await new OASISHyperDrive(manager).RouteRequestToProviderAsync<IAvatar>(
            new StorageOperationRequest { Operation = "LoadAvatarByRefreshToken", RefreshToken = " " },
            ProviderType.MongoDBOASIS);

        result.IsError.Should().BeTrue();
        result.ErrorCode.Should().Be("HYPERDRIVE_REFRESH_TOKEN_REQUIRED");
        provider.Verify(x => x.LoadAvatarByRefreshTokenAsync(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task RemainingStorageContractOperationsRouteDirectlyInAsyncMode()
    {
        var avatarId = Guid.NewGuid();
        var detail = new Mock<IAvatarDetail>().Object;
        var holons = new[] { new Mock<IHolon>().Object };
        var karma = new KarmaAkashicRecord();
        var provider = CreateActiveProvider();
        provider.Setup(x => x.DeleteAvatarAsync("avatar-provider-key", false)).ReturnsAsync(new OASISResult<bool>(true));
        provider.Setup(x => x.AddKarmaToAvatarAsync(detail, default, default, "source", "description", "https://source"))
            .ReturnsAsync(new OASISResult<KarmaAkashicRecord>(karma));
        provider.Setup(x => x.RemoveKarmaFromAvatarAsync(detail, default, default, "source", "description", "https://source"))
            .ReturnsAsync(new OASISResult<KarmaAkashicRecord>(karma));
        provider.Setup(x => x.ImportAsync(holons)).ReturnsAsync(new OASISResult<bool>(true));
        provider.Setup(x => x.ExportAllDataForAvatarByIdAsync(avatarId, 7)).ReturnsAsync(new OASISResult<IEnumerable<IHolon>>(holons));
        provider.Setup(x => x.ExportAllDataForAvatarByUsernameAsync("edge", 7)).ReturnsAsync(new OASISResult<IEnumerable<IHolon>>(holons));
        provider.Setup(x => x.ExportAllDataForAvatarByEmailAsync("edge@example.com", 7)).ReturnsAsync(new OASISResult<IEnumerable<IHolon>>(holons));
        provider.Setup(x => x.ExportAllAsync(7)).ReturnsAsync(new OASISResult<IEnumerable<IHolon>>(holons));
        var manager = new ProviderManager(null);
        manager.RegisterProvider(provider.Object);
        var hyperDrive = new OASISHyperDrive(manager);
        var common = new StorageOperationRequest
        {
            Payload = detail, KarmaSourceTitle = "source", KarmaSourceDescription = "description",
            KarmaSourceWebLink = "https://source", PreferredProvider = ProviderType.MongoDBOASIS
        };

        (await hyperDrive.RouteRequestToProviderAsync<bool>(new StorageOperationRequest
            { Operation = "DeleteAvatarByProviderKey", ProviderKey = "avatar-provider-key", SoftDelete = false }, ProviderType.MongoDBOASIS)).Result.Should().BeTrue();
        common.Operation = "AddKarmaToAvatar";
        (await hyperDrive.RouteRequestToProviderAsync<KarmaAkashicRecord>(common, ProviderType.MongoDBOASIS)).Result.Should().BeSameAs(karma);
        common.Operation = "RemoveKarmaFromAvatar";
        (await hyperDrive.RouteRequestToProviderAsync<KarmaAkashicRecord>(common, ProviderType.MongoDBOASIS)).Result.Should().BeSameAs(karma);
        (await hyperDrive.RouteRequestToProviderAsync<bool>(new StorageOperationRequest
            { Operation = "Import", Payload = holons }, ProviderType.MongoDBOASIS)).Result.Should().BeTrue();
        foreach (var request in new[]
        {
            new StorageOperationRequest { Operation = "ExportAllDataForAvatarById", AvatarId = avatarId, Version = 7 },
            new StorageOperationRequest { Operation = "ExportAllDataForAvatarByUsername", Username = "edge", Version = 7 },
            new StorageOperationRequest { Operation = "ExportAllDataForAvatarByEmail", Email = "edge@example.com", Version = 7 },
            new StorageOperationRequest { Operation = "ExportAll", Version = 7 }
        })
            (await hyperDrive.RouteRequestToProviderAsync<IEnumerable<IHolon>>(request, ProviderType.MongoDBOASIS)).Result.Should().BeSameAs(holons);
        provider.VerifyAll();
    }

    [Fact]
    public void RemainingStorageContractOperationsRouteDirectlyInSynchronousMode()
    {
        var avatarId = Guid.NewGuid();
        var detail = new Mock<IAvatarDetail>().Object;
        var holons = new[] { new Mock<IHolon>().Object };
        var karma = new KarmaAkashicRecord();
        var provider = CreateActiveProvider();
        provider.Setup(x => x.DeleteAvatar("avatar-provider-key", false)).Returns(new OASISResult<bool>(true));
        provider.Setup(x => x.AddKarmaToAvatar(detail, default, default, "source", "description", null))
            .Returns(new OASISResult<KarmaAkashicRecord>(karma));
        provider.Setup(x => x.Import(holons)).Returns(new OASISResult<bool>(true));
        provider.Setup(x => x.ExportAllDataForAvatarById(avatarId, 3)).Returns(new OASISResult<IEnumerable<IHolon>>(holons));
        provider.Setup(x => x.ExportAll(3)).Returns(new OASISResult<IEnumerable<IHolon>>(holons));
        var manager = new ProviderManager(null);
        manager.RegisterProvider(provider.Object);
        var hyperDrive = new OASISHyperDrive(manager);

        hyperDrive.RouteRequestToProvider<bool>(new StorageOperationRequest
            { Operation = "DeleteAvatarByProviderKey", ProviderKey = "avatar-provider-key", SoftDelete = false }, ProviderType.MongoDBOASIS).Result.Should().BeTrue();
        hyperDrive.RouteRequestToProvider<KarmaAkashicRecord>(new StorageOperationRequest
            { Operation = "AddKarmaToAvatar", Payload = detail, KarmaSourceTitle = "source", KarmaSourceDescription = "description" }, ProviderType.MongoDBOASIS).Result.Should().BeSameAs(karma);
        hyperDrive.RouteRequestToProvider<bool>(new StorageOperationRequest
            { Operation = "Import", Payload = holons }, ProviderType.MongoDBOASIS).Result.Should().BeTrue();
        hyperDrive.RouteRequestToProvider<IEnumerable<IHolon>>(new StorageOperationRequest
            { Operation = "ExportAllDataForAvatarById", AvatarId = avatarId, Version = 3 }, ProviderType.MongoDBOASIS).Result.Should().BeSameAs(holons);
        hyperDrive.RouteRequestToProvider<IEnumerable<IHolon>>(new StorageOperationRequest
            { Operation = "ExportAll", Version = 3 }, ProviderType.MongoDBOASIS).Result.Should().BeSameAs(holons);
        provider.VerifyAll();
    }

    [Fact]
    public async Task ProviderRecommendationsAreDeterministicForIdenticalInputs()
    {
        var engine = new AIOptimizationEngine();
        var providers = new List<ProviderType>
        {
            ProviderType.Neo4jOASIS,
            ProviderType.MongoDBOASIS,
            ProviderType.SQLLiteDBOASIS
        };
        var request = new StorageOperationRequest { Operation = "SaveHolon" };

        var first = await engine.GetProviderRecommendationsAsync(request, providers);
        var second = await engine.GetProviderRecommendationsAsync(request, providers);

        second.Select(x => x.ProviderType).Should().Equal(first.Select(x => x.ProviderType));
        second.Select(x => x.Score).Should().Equal(first.Select(x => x.Score));
    }

    [Fact]
    public async Task V2DoesNotFailOverWhenAutoFailoverIsDisabled()
    {
        var manager = new ProviderManager(null, CreateDna(HyperDriveModes.V2))
            { IsAutoFailOverEnabled = false, IsAutoLoadBalanceEnabled = false };
        var primary = CreateActiveProvider(ProviderType.MongoDBOASIS, "primary");
        var secondary = CreateActiveProvider(ProviderType.IPFSOASIS, "secondary");
        primary.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        var avatarId = Guid.NewGuid();
        primary.Setup(x => x.LoadAvatarAsync(avatarId, 0)).ReturnsAsync(new OASISResult<IAvatar>
            { IsError = true, ErrorCount = 1, Message = "primary failed" });
        manager.RegisterProvider(primary.Object);
        manager.RegisterProvider(secondary.Object);
        manager.SetAndActivateCurrentStorageProvider(primary.Object).IsError.Should().BeFalse();
        manager.SetAndReplaceAutoFailOverListForProviders(new[]
            { new EnumValue<ProviderType>(ProviderType.IPFSOASIS) });

        var result = await new OASISHyperDrive(manager).RouteRequestAsync<IAvatar>(
            new StorageOperationRequest { Operation = "LoadAvatar", AvatarId = avatarId });

        result.IsError.Should().BeTrue();
        secondary.Verify(x => x.LoadAvatarAsync(It.IsAny<Guid>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task V2UsesCurrentProviderWhenAutoLoadBalancingIsDisabled()
    {
        var manager = new ProviderManager(null, CreateDna(HyperDriveModes.V2))
            { IsAutoLoadBalanceEnabled = false };
        var current = CreateActiveProvider(ProviderType.MongoDBOASIS, "current");
        var candidate = CreateActiveProvider(ProviderType.IPFSOASIS, "candidate");
        current.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        var avatarId = Guid.NewGuid();
        current.Setup(x => x.LoadAvatarAsync(avatarId, 0))
            .ReturnsAsync(new OASISResult<IAvatar>(new Avatar { Id = avatarId }));
        manager.RegisterProvider(current.Object);
        manager.RegisterProvider(candidate.Object);
        manager.SetAndActivateCurrentStorageProvider(current.Object).IsError.Should().BeFalse();
        manager.SetAndReplaceAutoLoadBalanceListForProviders(new[]
            { new EnumValue<ProviderType>(ProviderType.IPFSOASIS) });

        var result = await new OASISHyperDrive(manager).RouteRequestAsync<IAvatar>(
            new StorageOperationRequest { Operation = "LoadAvatar", AvatarId = avatarId });

        result.IsError.Should().BeFalse(result.Message);
        current.Verify(x => x.LoadAvatarAsync(avatarId, 0), Times.Once);
        candidate.Verify(x => x.LoadAvatarAsync(It.IsAny<Guid>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task V2AutomaticallyReplicatesSuccessfulMutationsInConfiguredOrder()
    {
        var manager = new ProviderManager(null, CreateDna(HyperDriveModes.V2))
            { IsAutoReplicationEnabled = true };
        var calls = new List<string>();
        var primary = CreateActiveProvider(ProviderType.MongoDBOASIS, "primary");
        var first = CreateActiveProvider(ProviderType.IPFSOASIS, "first");
        var second = CreateActiveProvider(ProviderType.Neo4jOASIS, "second");
        var holon = new Holon { Id = Guid.NewGuid() };
        primary.Setup(x => x.SaveHolonAsync(holon, true, true, 0, true, false))
            .Callback(() => calls.Add("primary")).ReturnsAsync(new OASISResult<IHolon>(holon));
        first.Setup(x => x.SaveHolonAsync(holon, true, true, 0, true, false))
            .Callback(() => calls.Add("first")).ReturnsAsync(new OASISResult<IHolon>(holon));
        second.Setup(x => x.SaveHolonAsync(holon, true, true, 0, true, false))
            .Callback(() => calls.Add("second")).ReturnsAsync(new OASISResult<IHolon>(holon));
        manager.RegisterProvider(primary.Object);
        manager.RegisterProvider(first.Object);
        manager.RegisterProvider(second.Object);
        manager.SetAndReplaceAutoReplicationListForProviders(new[]
        {
            new EnumValue<ProviderType>(ProviderType.MongoDBOASIS),
            new EnumValue<ProviderType>(ProviderType.IPFSOASIS),
            new EnumValue<ProviderType>(ProviderType.Neo4jOASIS)
        });

        var result = await new OASISHyperDrive(manager).RouteRequestAsync<IHolon>(new StorageOperationRequest
            { Operation = "SaveHolon", Payload = holon, PreferredProvider = ProviderType.MongoDBOASIS });

        result.IsError.Should().BeFalse(result.Message);
        calls.Should().Equal("primary", "first", "second");
    }

    [Fact]
    public async Task V2DoesNotReplicateMutationsWhenAutoReplicationIsDisabled()
    {
        var manager = new ProviderManager(null, CreateDna(HyperDriveModes.V2))
            { IsAutoReplicationEnabled = false };
        var primary = CreateActiveProvider(ProviderType.MongoDBOASIS, "primary");
        var secondary = CreateActiveProvider(ProviderType.IPFSOASIS, "secondary");
        var holon = new Holon { Id = Guid.NewGuid() };
        primary.Setup(x => x.SaveHolonAsync(holon, true, true, 0, true, false))
            .ReturnsAsync(new OASISResult<IHolon>(holon));
        manager.RegisterProvider(primary.Object);
        manager.RegisterProvider(secondary.Object);
        manager.SetAndReplaceAutoReplicationListForProviders(new[]
            { new EnumValue<ProviderType>(ProviderType.IPFSOASIS) });

        var result = await new OASISHyperDrive(manager).RouteRequestAsync<IHolon>(new StorageOperationRequest
            { Operation = "SaveHolon", Payload = holon, PreferredProvider = ProviderType.MongoDBOASIS });

        result.IsError.Should().BeFalse(result.Message);
        secondary.Verify(x => x.SaveHolonAsync(It.IsAny<IHolon>(), It.IsAny<bool>(), It.IsAny<bool>(),
            It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task V2DefersMutationReplicationToDurableHostedPipelineWhenEnabled()
    {
        var dna = CreateDna(HyperDriveModes.V2);
        dna.OASIS.OASISHyperDriveConfig = new NextGenSoftware.OASIS.API.Core.Configuration.OASISHyperDriveConfig
            { EnableHostedSync = true };
        var manager = new ProviderManager(null, dna) { IsAutoReplicationEnabled = true };
        var primary = CreateActiveProvider(ProviderType.MongoDBOASIS, "primary");
        var secondary = CreateActiveProvider(ProviderType.IPFSOASIS, "secondary");
        var holon = new Holon { Id = Guid.NewGuid() };
        primary.Setup(x => x.SaveHolonAsync(holon, true, true, 0, true, false))
            .ReturnsAsync(new OASISResult<IHolon>(holon));
        manager.RegisterProvider(primary.Object);
        manager.RegisterProvider(secondary.Object);
        manager.SetAndReplaceAutoReplicationListForProviders(new[]
            { new EnumValue<ProviderType>(ProviderType.IPFSOASIS) });

        var result = await new OASISHyperDrive(manager).RouteRequestAsync<IHolon>(new StorageOperationRequest
            { Operation = "SaveHolon", Payload = holon, PreferredProvider = ProviderType.MongoDBOASIS });

        result.IsError.Should().BeFalse(result.Message);
        secondary.Verify(x => x.SaveHolonAsync(It.IsAny<IHolon>(), It.IsAny<bool>(), It.IsAny<bool>(),
            It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Never);
    }

    private static Mock<IOASISStorageProvider> CreateActiveProvider(
        ProviderType providerType = ProviderType.MongoDBOASIS,
        string providerName = "test-mongo")
    {
        var provider = new Mock<IOASISStorageProvider>();
        provider.SetupAllProperties();
        provider.Object.ProviderType = new EnumValue<ProviderType>(providerType);
        provider.Object.ProviderCategory = new EnumValue<ProviderCategory>(ProviderCategory.Storage);
        provider.Object.ProviderName = providerName;
        provider.Object.IsProviderActivated = true;
        return provider;
    }

    private static ProviderManager CreateProviderManager()
    {
        return new ProviderManager(null, CreateDna());
    }

    private static OASISDNA CreateDna(string hyperDriveMode = HyperDriveModes.Legacy)
    {
        return new OASISDNA
        {
            OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS
            {
                HyperDriveMode = hyperDriveMode,
                StorageProviders = new StorageProviderSettings { LogSwitchingProviders = false }
            }
        };
    }
}
