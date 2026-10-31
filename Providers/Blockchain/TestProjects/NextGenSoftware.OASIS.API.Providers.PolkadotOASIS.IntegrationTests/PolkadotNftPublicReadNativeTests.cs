using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.API.Providers.PolkadotOASIS.IntegrationTests;

[TestClass][DoNotParallelize]
public class PolkadotNftPublicReadNativeTests
{
    [DataTestMethod][DataRow(false)][DataRow(true)][TestCategory("LocalNative")]
    public async Task PublicCollectionReadUsesDeployedCodeAndShippingBridge(bool synchronous)
    {
        var fixture=ReadFixture();
        using var provider=CreateProvider(fixture);
        var address=fixture.GetProperty("address").GetString()!;
        var result=synchronous?provider.LoadOnChainNFTData(address):await provider.LoadOnChainNFTDataAsync(address);
        Assert.IsFalse(result.IsError,result.Message);Assert.IsTrue(result.IsLoaded);Assert.IsNotNull(result.Result);
        var nft=result.Result;var attributes=fixture.GetProperty("collection").GetProperty("attributes");
        Assert.AreEqual(address,nft.NFTTokenAddress);
        Assert.AreEqual(attributes.GetProperty("name").GetString(),nft.Title);
        Assert.AreEqual(attributes.GetProperty("symbol").GetString(),nft.Symbol);
        Assert.AreEqual(attributes.GetProperty("description").GetString(),nft.Description);
        Assert.AreEqual(attributes.GetProperty("image").GetString(),nft.ImageUrl);
        Assert.IsNull(nft.TokenId);Assert.IsNull(nft.Image);
        Assert.AreEqual(fixture.GetProperty("codeHash").GetString(),nft.MetaData["polkadot:code-hash"]);
        using var identity=JsonDocument.Parse(nft.MetaData["polkadot:collection-id"]);
        Assert.AreEqual(address,identity.RootElement.GetProperty("bytes").GetString());
        StringAssert.Matches(nft.MetaData["polkadot:block-hash"],new System.Text.RegularExpressions.Regex("^0x[0-9a-f]{64}$"));
        Assert.AreEqual("0xe8cf5d7d636a08721efaa17acc52ed3d895fe2468dccb2520cc8e249ddb6b208",provider.ReportedGenesisHash);
    }

    [TestMethod][TestCategory("LocalNative")]
    public async Task PublicCollectionReadRejectsDifferentCodeOnTheRealRuntime()
    {
        var fixture=ReadFixture();
        using var provider=CreateProvider(fixture,"0x"+new string('0',64));
        var result=await provider.LoadOnChainNFTDataAsync(fixture.GetProperty("address").GetString()!);
        Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsLoaded);Assert.IsNull(result.Result);
        StringAssert.Contains(result.Message,"Contract code differs");
    }

    private static JsonElement ReadFixture()
    {
        Assert.AreEqual("1",Environment.GetEnvironmentVariable("OASIS_POLKADOT_LOCAL_NATIVE"));
        var bridge=Environment.GetEnvironmentVariable("OASIS_POLKADOT_SDK_BRIDGE")!;
        var directory=Path.GetFullPath(Path.Combine(Path.GetDirectoryName(bridge)!,"../../../../Docs/WIP/HyperDrive-Providers/NativeEvidence/Polkadot"));
        Assert.IsFalse(File.Exists(Path.Combine(directory,"psp34-deployment-pending.json")),"Unresolved original must be preserved.");
        using var file=JsonDocument.Parse(File.ReadAllText(Path.Combine(directory,"psp34-deployment.completed.json")));
        var result=file.RootElement.Clone();
        Assert.IsTrue(result.GetProperty("nativeDeploymentVerified").GetBoolean());
        Assert.IsTrue(result.GetProperty("receipt").GetProperty("dispatchSucceeded").GetBoolean());
        return result;
    }

    private static PolkadotOASIS CreateProvider(JsonElement fixture,string? code=null)=>new(
        "http://127.0.0.1:19944","0xe8cf5d7d636a08721efaa17acc52ed3d895fe2468dccb2520cc8e249ddb6b208",
        "",null,null,new PolkadotNftConfiguration(code??fixture.GetProperty("codeHash").GetString()!,fixture.GetProperty("sender").GetString()!));
}
