using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.PolkadotOASIS.UnitTests;

[TestClass]
public class PolkadotPsp34CollectionTests
{
    private static readonly string Address="0x"+new string('1',64),Code="0x"+new string('2',64),Origin="0x"+new string('3',64);
    private sealed class Provider(PolkadotNftConfiguration? nft):PolkadotOASIS("http://127.0.0.1:1","fixture","",null,null,nft)
    {
        public int Activations;
        public override Task<OASISResult<bool>> ActivateProviderAsync(){Activations++;throw new AssertFailedException("Unexpected network activation");}
    }
    private static JsonObject Reply()=>new(){["contractAddress"]=Address,["codeHash"]=Code,["blockHash"]=Origin,
        ["collectionId"]=new JsonObject{["bytes"]="0x010203"},
        ["attributes"]=new JsonObject{["name"]="Living tree",["symbol"]="TREE",["description"]="",["image"]="https://example.invalid/tree.png"}};
    private static IWeb3NFT Parse(Provider provider,JsonObject reply)=>(IWeb3NFT)typeof(PolkadotOASIS)
        .GetMethod("ParsePsp34Collection",BindingFlags.NonPublic|BindingFlags.Instance)!
        .Invoke(provider,new object[]{JsonSerializer.SerializeToElement(reply),Address})!;
    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task MissingReviewedConfigurationFailsBeforeNetwork(bool synchronous)
    {
        using var provider=new Provider(null);
        var result=synchronous?provider.LoadOnChainNFTData(Address):await provider.LoadOnChainNFTDataAsync(Address);
        Assert.IsTrue(result.IsError);Assert.IsNull(result.Result);Assert.IsFalse(result.IsLoaded);
        Assert.IsInstanceOfType(result.Exception,typeof(InvalidOperationException));Assert.AreEqual(0,provider.Activations);
    }
    [DataTestMethod][DataRow(null)][DataRow("")][DataRow("0x1234")]
    public async Task InvalidCollectionIdentityFailsBeforeNetwork(string address)
    {
        using var provider=new Provider(new(Code,Origin));var result=await provider.LoadOnChainNFTDataAsync(address);
        Assert.IsTrue(result.IsError);Assert.IsInstanceOfType(result.Exception,typeof(ArgumentException));Assert.AreEqual(0,provider.Activations);
    }
    [TestMethod]
    public void CollectionMetadataKeepsNativeEvidenceWithoutInventingTokenIdentityOrImageBytes()
    {
        using var provider=new Provider(new(Code,Origin));var nft=Parse(provider,Reply());
        Assert.AreEqual("Living tree",nft.Title);Assert.AreEqual("TREE",nft.Symbol);Assert.AreEqual("",nft.Description);
        Assert.AreEqual("https://example.invalid/tree.png",nft.ImageUrl);Assert.IsNull(nft.Image);Assert.IsNull(nft.TokenId);
        Assert.AreEqual(Address,nft.NFTTokenAddress);Assert.AreEqual(Origin,nft.MetaData["polkadot:block-hash"]);
        Assert.AreEqual(Code,nft.MetaData["polkadot:code-hash"]);
    }
    [DataTestMethod][DataRow("contractAddress")][DataRow("codeHash")][DataRow("blockHash")][DataRow("collectionId")][DataRow("attribute")]
    public void ForeignOrMalformedNativeResponseNeverBecomesNftSuccess(string field)
    {
        using var provider=new Provider(new(Code,Origin));var reply=Reply();
        if(field=="attribute")reply["attributes"]!["name"]=123;
        else reply[field]=field=="collectionId"?JsonValue.Create("invented-id"):JsonValue.Create("0x00");
        var failure=Assert.ThrowsException<TargetInvocationException>(()=>Parse(provider,reply));
        Assert.IsInstanceOfType(failure.InnerException,typeof(InvalidDataException));
    }
}
