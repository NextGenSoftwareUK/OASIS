using System.Reflection;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers;

namespace NextGenSoftware.OASIS.API.ONODE.Core.UnitTests;

public sealed class NFTBatchResultTests
{
    [Fact]
    public void MetadataProjectionPreservesProviderIdentityWithoutSharingRequestCollections()
    {
        var manager = new NFTManager((IOASISStorageProvider)null!, Guid.NewGuid(), new OASISDNA());
        var asset = new Web3NFT { MetaData = new Dictionary<string, string> { ["HederaSerials"] = "[7]" } };
        var request = new MintWeb3NFTRequest
        {
            Price = 0, Discount = 0, OnChainProvider = ProviderType.HashgraphOASIS,
            OffChainProvider = ProviderType.LocalFileOASIS,
            NFTOffChainMetaType = NFTOffChainMetaType.ExternalJSONURL,
            NFTStandardType = NFTStandardType.ERC721,
            MetaData = new Dictionary<string, string> { ["HederaSerials"] = "wrong", ["custom"] = "value" },
            Tags = new List<string> { "original" }
        };

        typeof(NFTManager).GetMethod("UpdateWeb3NFT", BindingFlags.Instance | BindingFlags.NonPublic,
            null, new[] { typeof(IWeb3NFT), typeof(NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests.IMintWeb3NFTRequest) }, null)!
            .Invoke(manager, new object[] { asset, request });

        Assert.Equal("[7]", asset.MetaData["HederaSerials"]);
        Assert.Equal("value", asset.MetaData["custom"]);
        asset.MetaData["{{{newnft}}}"] = "true";
        asset.Tags.Add("new");
        Assert.False(request.MetaData.ContainsKey("{{{newnft}}}"));
        Assert.Single(request.Tags);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    public void EachRecordedAssetRemainsInBothResponseLists(int count)
    {
        var parent = new Web4NFT { Id = Guid.NewGuid() };
        var assets = Enumerable.Range(1, count).Select(i => new Web3NFT
            { Id = Guid.NewGuid(), NFTTokenAddress = $"0.0.123/{i}", MintTransactionHash = $"mint-{i}" }).ToArray();

        foreach (var asset in assets)
            Record(parent, asset);

        Assert.Equal(assets, parent.Web3NFTs);
        Assert.Equal(assets.Cast<IWeb3NFT>(), parent.NewlyMintedWeb3NFTs);
        Assert.Equal(count, parent.Web3NFTs.Select(x => x.NFTTokenAddress).Distinct().Count());
    }

    [Fact]
    public void RecordingPartialAssetDoesNotDiscardEarlierConfirmedAsset()
    {
        var parent = new Web4NFT();
        var confirmed = new Web3NFT { NFTTokenAddress = "0.0.123/1", MintTransactionHash = "confirmed-mint" };
        var partial = new Web3NFT { NFTTokenAddress = "0.0.123/2", MintTransactionHash = "mint-with-failed-delivery" };

        Record(parent, confirmed);
        Record(parent, partial);

        Assert.Same(confirmed, parent.Web3NFTs[0]);
        Assert.Same(partial, parent.Web3NFTs[1]);
        Assert.Equal("mint-with-failed-delivery", parent.NewlyMintedWeb3NFTs[1].MintTransactionHash);
    }

    private static void Record(IWeb4NFT parent, Web3NFT asset) =>
        typeof(NFTManager).GetMethod("RecordMintedNFT", BindingFlags.Static | BindingFlags.NonPublic)!
            .Invoke(null, new object[] { parent, asset });
}
