using FluentAssertions;
using Moq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Core.UnitTests.HyperDrive;

public sealed class AvatarInventoryIdempotencyTests
{
    [Fact]
    public async Task ReplayingStableOperationReturnsOriginalInventoryGrantWithoutSavingOrIncrementingAgain()
    {
        var avatarId = Guid.NewGuid();
        var operationId = Guid.NewGuid();
        var detail = new AvatarDetail
        {
            Id = avatarId,
            Inventory = new List<IInventoryItem>(),
            MetaData = new Dictionary<string, object>()
        };
        var provider = new Mock<IOASISStorageProvider>();
        provider.SetupAllProperties();
        provider.Object.ProviderType = new EnumValue<ProviderType>(ProviderType.MongoDBOASIS);
        provider.Object.ProviderCategory = new EnumValue<ProviderCategory>(ProviderCategory.Storage);
        provider.Object.ProviderName = "inventory-idempotency-test";
        provider.Object.IsProviderActivated = true;
        provider.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        provider.Setup(x => x.ActivateProviderAsync()).ReturnsAsync(new OASISResult<bool>(true));
        provider.Setup(x => x.LoadAvatarDetailAsync(avatarId, 0))
            .ReturnsAsync(() => new OASISResult<IAvatarDetail>(detail));
        provider.Setup(x => x.SaveAvatarDetailAsync(It.IsAny<IAvatarDetail>()))
            .ReturnsAsync((IAvatarDetail saved) => new OASISResult<IAvatarDetail>(saved));
        var dna = new OASISDNA
        {
            OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS
            {
                StorageProviders = new StorageProviderSettings
                {
                    LogSwitchingProviders = false,
                    ActivateProviderTimeOutSeconds = 5
                }
            }
        };
        var manager = new AvatarManager(provider.Object, dna, new ProviderManager(null, dna));
        var item = new InventoryItem
        {
            Id = Guid.NewGuid(), Name = "Offline reward", Quantity = 2, Stack = true,
            GameSource = "Our World", ItemType = InventoryItemType.Nature
        };

        var first = await manager.AddItemToAvatarInventoryAsync(avatarId, item,
            operationId: operationId);
        var replay = await manager.AddItemToAvatarInventoryAsync(avatarId,
            new InventoryItem
            {
                Id = Guid.NewGuid(), Name = item.Name, Quantity = 2, Stack = true,
                GameSource = item.GameSource, ItemType = item.ItemType
            }, operationId: operationId);

        first.IsError.Should().BeFalse(first.Message);
        replay.IsError.Should().BeFalse(replay.Message);
        replay.Message.Should().Contain("already applied");
        replay.Result.Should().BeSameAs(first.Result);
        detail.Inventory.Should().ContainSingle();
        detail.Inventory.Single().Quantity.Should().Be(2);
        provider.Verify(x => x.SaveAvatarDetailAsync(It.IsAny<IAvatarDetail>()), Times.Once);
    }

    [Fact]
    public async Task ReplayingStableOperationReturnsOriginalGeoNftCollectionWithoutSavingOrCollectingAgain()
    {
        var avatarId = Guid.NewGuid();
        var geoNftId = Guid.NewGuid();
        var operationId = Guid.NewGuid();
        var detail = new AvatarDetail
        {
            Id = avatarId,
            Inventory = new List<IInventoryItem>(),
            MetaData = new Dictionary<string, object>()
        };
        var provider = CreateProvider(avatarId, detail);
        provider.Setup(x => x.LoadAllAvatarDetailsAsync(0))
            .ReturnsAsync(() => new OASISResult<IEnumerable<IAvatarDetail>>(new[] { detail }));
        var manager = CreateManager(provider.Object);
        var nft = new Mock<IWeb4GeoSpatialNFT>();
        nft.SetupGet(x => x.Id).Returns(geoNftId);
        nft.SetupGet(x => x.PermSpawn).Returns(true);
        nft.SetupGet(x => x.AllowOtherPlayersToAlsoCollect).Returns(true);
        nft.SetupGet(x => x.GlobalSpawnQuantity).Returns(10);
        nft.SetupGet(x => x.PlayerSpawnQuantity).Returns(10);

        var first = await manager.CollectGeoNFTInventoryAsync(avatarId, nft.Object,
            new InventoryItem { GeoNFTId = geoNftId, Quantity = 1, Stack = true },
            operationId: operationId);
        var replay = await manager.CollectGeoNFTInventoryAsync(avatarId, nft.Object,
            new InventoryItem { GeoNFTId = geoNftId, Quantity = 1, Stack = true },
            operationId: operationId);

        first.IsError.Should().BeFalse(first.Message);
        replay.IsError.Should().BeFalse(replay.Message);
        replay.Message.Should().Contain("already applied");
        replay.Result.Should().BeSameAs(first.Result);
        detail.Inventory.Should().ContainSingle();
        detail.Inventory.Single().Quantity.Should().Be(1);
        provider.Verify(x => x.SaveAvatarDetailAsync(It.IsAny<IAvatarDetail>()), Times.Once);
    }

    private static Mock<IOASISStorageProvider> CreateProvider(Guid avatarId, AvatarDetail detail)
    {
        var provider = new Mock<IOASISStorageProvider>();
        provider.SetupAllProperties();
        provider.Object.ProviderType = new EnumValue<ProviderType>(ProviderType.MongoDBOASIS);
        provider.Object.ProviderCategory = new EnumValue<ProviderCategory>(ProviderCategory.Storage);
        provider.Object.ProviderName = "idempotency-test";
        provider.Object.IsProviderActivated = true;
        provider.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        provider.Setup(x => x.ActivateProviderAsync()).ReturnsAsync(new OASISResult<bool>(true));
        provider.Setup(x => x.LoadAvatarDetailAsync(avatarId, 0))
            .ReturnsAsync(() => new OASISResult<IAvatarDetail>(detail));
        provider.Setup(x => x.SaveAvatarDetailAsync(It.IsAny<IAvatarDetail>()))
            .ReturnsAsync((IAvatarDetail saved) => new OASISResult<IAvatarDetail>(saved));
        return provider;
    }

    private static AvatarManager CreateManager(IOASISStorageProvider provider)
    {
        var dna = new OASISDNA
        {
            OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS
            {
                StorageProviders = new StorageProviderSettings
                {
                    LogSwitchingProviders = false,
                    ActivateProviderTimeOutSeconds = 5
                }
            }
        };
        return new AvatarManager(provider, dna, new ProviderManager(null, dna));
    }
}
