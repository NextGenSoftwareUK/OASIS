using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;
using Xunit;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

[Collection("COSMIC hosted global state")]
public sealed class NftAvatarStorageTests
{
    [Fact]
    public async Task NativeAvatarNftReadsMatchAcrossSyncAndAsync()
    {
        var folder = Directory.CreateTempSubdirectory("oasis-avatar-nfts-");
        try
        {
            using var provider = new SQLLiteDBOASIS($"Data Source={Path.Combine(folder.FullName, "nfts.db")};Pooling=False");
            try
            {
                var activation = await provider.ActivateProviderAsync();
                Assert.False(activation.IsError, activation.Message);

                var minter = Guid.NewGuid();
                var placer = Guid.NewGuid();
                var nft = new Web4NFT { Id = Guid.NewGuid(), MintedByAvatarId = minter, Title = "Original NFT" };
                var geo = new Web4OASISGeoSpatialNFT
                {
                    Id = Guid.NewGuid(), MintedByAvatarId = minter, PlacedByAvatarId = placer,
                    OriginalWeb4OASISNFTId = nft.Id, Title = "Placed NFT"
                };
                await SaveAsync(HolonType.Web4NFT, nft.Id, "NFT.MintedByAvatarId", minter, "NFT.WEB4NFT", JsonSerializer.Serialize(nft));
                await SaveAsync(HolonType.Web4GeoNFT, geo.Id, "GEONFT.PlacedByAvatarId", placer, "GEONFT.WEB4GEONFT", JsonSerializer.Serialize(geo));

                var dna = new OASISDNA
                {
                    OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS
                    {
                        HyperDriveMode = HyperDriveModes.V2, StorageProviders = new StorageProviderSettings()
                    }
                };
                var providers = new ProviderManager(null, dna)
                {
                    IsAutoFailOverEnabled = false, IsAutoLoadBalanceEnabled = false, IsAutoReplicationEnabled = false
                };
                providers.RegisterProvider(provider);
                var manager = new NFTManager(provider, placer, dna) { Data = new HolonManager(provider, dna, providers) };

                var asyncNfts = await manager.LoadAllWeb4NFTsForAvatarAsync(minter, ProviderType.SQLLiteDBOASIS);
                var syncNfts = manager.LoadAllWeb4NFTsForAvatar(minter, ProviderType.SQLLiteDBOASIS);
                Assert.False(asyncNfts.IsError, asyncNfts.Message);
                Assert.False(syncNfts.IsError, syncNfts.Message);
                Assert.Equal(nft.Id, Assert.Single(asyncNfts.Result).Id);
                Assert.Equal(nft.Id, Assert.Single(syncNfts.Result).Id);

                var asyncGeo = await manager.LoadAllWeb4GeoNFTsForAvatarAsync(placer, ProviderType.SQLLiteDBOASIS);
                var syncGeo = manager.LoadAllWeb4GeoNFTsForAvatar(placer, ProviderType.SQLLiteDBOASIS);
                Assert.False(asyncGeo.IsError, asyncGeo.Message);
                Assert.False(syncGeo.IsError, syncGeo.Message);
                Assert.Equal(geo.Id, Assert.Single(asyncGeo.Result).Id);
                Assert.Equal(geo.Id, Assert.Single(syncGeo.Result).Id);
                Assert.Equal(placer, Assert.Single(syncGeo.Result).PlacedByAvatarId);

                async Task SaveAsync(HolonType type, Guid id, string ownerKey, Guid owner, string payloadKey, string payload)
                {
                    var saved = await provider.SaveHolonAsync(new Holon
                    {
                        Id = id,
                        HolonType = type,
                        IsActive = true,
                        MetaData = new Dictionary<string, object> { [ownerKey] = owner.ToString(), [payloadKey] = payload }
                    }, saveChildren: false);
                    Assert.False(saved.IsError, saved.Message);
                }
            }
            finally
            {
                var shared = ProviderManager.Instance;
                if (ReferenceEquals(shared.GetStorageProvider(ProviderType.SQLLiteDBOASIS), provider))
                    shared.UnRegisterProvider(provider);
            }
        }
        finally
        {
            folder.Delete(recursive: true);
        }
    }
}
