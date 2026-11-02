using Moq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.UnitTests.HyperDrive;
public sealed partial class HyperDriveProviderExecutionTests
{
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task GeoCollectionAvailabilityKeepsItsConfiguredAccountProvider(bool autoBalance)
    {
        var primary=CreateActiveProvider();var unrelated=CreateActiveProvider(ProviderType.AWSOASIS,"unrelated-global-provider");
        var actor=Guid.NewGuid();var nftId=Guid.NewGuid();
        primary.Setup(p=>p.LoadAllAvatarDetailsAsync(0)).ReturnsAsync(new OASISResult<IEnumerable<IAvatarDetail>>(new[]{new AvatarDetail{Id=actor}}));
        unrelated.Setup(p=>p.LoadAllAvatarDetailsAsync(0)).ReturnsAsync(new OASISResult<IEnumerable<IAvatarDetail>>{IsError=true,Message="Wrong authority selected"});
        var dna=CreateDna(HyperDriveModes.V2);var providers=new ProviderManager(null,dna){IsAutoFailOverEnabled=false,IsAutoLoadBalanceEnabled=autoBalance,IsAutoReplicationEnabled=false};
        providers.RegisterProvider(primary.Object);providers.RegisterProvider(unrelated.Object);
        var accounts=new AvatarManager(primary.Object,dna,providers);
        providers.SetAndActivateCurrentStorageProvider(unrelated.Object);
        var nft=new Mock<IWeb4GeoSpatialNFT>();nft.SetupGet(n=>n.Id).Returns(nftId);nft.SetupGet(n=>n.GlobalSpawnQuantity).Returns(-1);nft.SetupGet(n=>n.PlayerSpawnQuantity).Returns(1);nft.SetupGet(n=>n.AllowOtherPlayersToAlsoCollect).Returns(true);
        var result=await accounts.GetGeoNFTCollectionStatusesAsync(actor,new[]{nft.Object});
        Assert.False(result.IsError,result.Message);Assert.Single(result.Result);
        primary.Verify(p=>p.LoadAllAvatarDetailsAsync(0),Times.Once);unrelated.Verify(p=>p.LoadAllAvatarDetailsAsync(0),Times.Never);
        Assert.Same(unrelated.Object,providers.CurrentStorageProvider);
    }
}
