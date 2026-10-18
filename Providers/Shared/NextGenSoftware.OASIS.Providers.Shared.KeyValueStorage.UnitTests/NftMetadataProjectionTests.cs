using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.UnitTests;

[TestClass]
public class NftMetadataProjectionTests
{
    [DataTestMethod]
    [DataRow(null)] [DataRow("")] [DataRow("relative.json")]
    [DataRow("file:///secret.json")] [DataRow("https://user:password@example.com/nft.json")]
    public void OffChainProjectionRejectsMissingOrUnsafeUri(string uri)
        => Assert.ThrowsException<ArgumentException>(() => NftMetadataProjection.Validate(false, uri));

    [DataTestMethod]
    [DataRow("https://example.com/nft.json")] [DataRow("http://localhost/nft.json")]
    [DataRow("ipfs://bafyexample/nft.json")] [DataRow("ar://example-transaction")]
    public void SuppliedUriIsRetainedWithoutRetrieval(string uri)
    {
        var original = new Web3NFT { Id = Guid.NewGuid(), NFTTokenAddress = "token", StoreNFTMetaDataOnChain = false,
            JSONMetaDataURL = uri, Title = "private-canary", Description = "private-canary", JSONMetaData = "private-canary",
            Image = new byte[] { 1, 2, 3 }, Thumbnail = new byte[] { 1, 2, 3 },
            MintedByAvatarId = Guid.NewGuid(), CurrentOwnerAvatarId = Guid.NewGuid(),
            MetaData = new Dictionary<string,string> { ["Email"] = "private-canary" }, Tags = new List<string> { "private-canary" } };
        var projection = NftMetadataProjection.ForOnChainStorage(original);
        Assert.AreNotSame(original, projection);
        Assert.AreEqual(original.Id, projection.Id);
        Assert.AreEqual("token", projection.NFTTokenAddress);
        Assert.AreEqual(uri, projection.JSONMetaDataURL);
        Assert.IsFalse(projection.StoreNFTMetaDataOnChain);
        Assert.IsFalse(OasisJson.Serialize(projection).Contains("private-canary"));
        Assert.AreEqual(Guid.Empty, projection.MintedByAvatarId);
        Assert.AreEqual(Guid.Empty, projection.CurrentOwnerAvatarId);
        Assert.IsNull(projection.Image); Assert.IsNull(projection.Thumbnail);
        Assert.AreEqual("private-canary", original.MetaData["Email"]);
    }

    [TestMethod]
    public void ExplicitOnChainModePreservesExistingEnvelope()
    {
        var nft = new Web3NFT { StoreNFTMetaDataOnChain = true, Title = "public" };
        Assert.AreSame(nft, NftMetadataProjection.ForOnChainStorage(nft));
    }
}
