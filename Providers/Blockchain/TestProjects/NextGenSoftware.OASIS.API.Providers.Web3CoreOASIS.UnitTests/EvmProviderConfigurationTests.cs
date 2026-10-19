using System.Numerics;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.ArbitrumOASIS;
using NextGenSoftware.OASIS.OASISBootLoader;

namespace NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS.UnitTests;

[TestClass]
public sealed class EvmProviderConfigurationTests
{
    [DataTestMethod][DataRow(false)][DataRow(true)]
    public void HistoryOptionRoundtripsAndBindsOnlyThroughExistingV2Factories(bool enabled)
    {
        Assert.IsFalse(JsonConvert.DeserializeObject<ArbitrumOASISProviderSettings>("{}")!.IsVersionControlEnabled);
        Assert.IsFalse(JsonConvert.DeserializeObject<PolygonOASISProviderSettings>("{}")!.IsVersionControlEnabled);
        var arbitrum=JsonConvert.DeserializeObject<ArbitrumOASISProviderSettings>(JsonConvert.SerializeObject(new ArbitrumOASISProviderSettings{
            IsVersionControlEnabled=enabled,ConnectionString="http://127.0.0.1:1",ChainId=421614,
            ChainPrivateKey="0x"+new string('1',64),ContractAddress="0x"+new string('2',40)}))!;
        var polygon=JsonConvert.DeserializeObject<PolygonOASISProviderSettings>(JsonConvert.SerializeObject(new PolygonOASISProviderSettings{
            IsVersionControlEnabled=enabled,ConnectionString="http://127.0.0.1:1",ChainId=80002,
            ChainPrivateKey="0x"+new string('1',64),ContractAddress="0x"+new string('2',40)}))!;
        var a=(Web3CoreOASISBaseProvider)EvmProviderFactory.CreateArbitrum(arbitrum,HyperDriveModes.V2,null!);
        var p=EvmProviderFactory.CreatePolygon(polygon,null!);
        Assert.AreEqual(enabled,a.IsVersionControlEnabled);Assert.AreEqual(enabled,p.IsVersionControlEnabled);
        Assert.IsFalse(a.IsProviderActivated);Assert.IsFalse(p.IsProviderActivated);
    }
    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public void ArbitrumFactoryHonorsModeAndExplicitDnaConfigurationWithoutActivation(bool v2)
    {
        var settings=new ArbitrumOASISProviderSettings{ConnectionString="http://127.0.0.1:1",ChainId=421614,
            ChainPrivateKey="0x"+new string('1',64),ContractAddress="0x"+new string('2',40),TokenEscrowContractAddress="0x"+new string('3',40)};
        var restored=JsonConvert.DeserializeObject<ArbitrumOASISProviderSettings>(JsonConvert.SerializeObject(settings))!;
        var provider=EvmProviderFactory.CreateArbitrum(restored,v2 ? HyperDriveModes.V2:HyperDriveModes.Legacy,"http://127.0.0.1:2");
        Assert.IsFalse(provider.IsProviderActivated);
        Assert.AreEqual(v2 ? typeof(ArbitrumOASIS_Web3Core):typeof(NextGenSoftware.OASIS.API.Providers.ArbitrumOASIS.ArbitrumOASIS),provider.GetType());
        Assert.AreEqual(!v2,provider is IOASISLegacyOnlyStorageProvider);
        if(v2)
        {
            var shared=(Web3CoreOASISBaseProvider)provider;
            Assert.AreEqual(settings.TokenEscrowContractAddress,shared.TokenEscrowContractAddress);
            Assert.AreEqual(new BigInteger(settings.ChainId),Field(shared,"_expectedChainId"));
            Assert.AreEqual("http://127.0.0.1:2",Field(shared,"_hostURI"));
            Assert.AreEqual(settings.ContractAddress,Field(shared,"_contractAddress"));
        }
    }

    [DataTestMethod]
    [DataRow(0L)] [DataRow(-1L)]
    public void InvalidV2ArbitrumChainCannotSelectLegacyAsFallback(long chain)
        =>Assert.ThrowsException<ArgumentOutOfRangeException>(()=>EvmProviderFactory.CreateArbitrum(new(){ChainId=chain},HyperDriveModes.V2,null!));

    [TestMethod]
    public void PolygonConfigurationPreservesOldDefaultAndSeparateEscrowAddress()
    {
        Assert.AreEqual(137L,JsonConvert.DeserializeObject<PolygonOASISProviderSettings>("{}")!.ChainId);
        var settings=new PolygonOASISProviderSettings{ConnectionString="http://127.0.0.1:1",ChainId=80002,
            ContractAddress="0x"+new string('2',40),TokenEscrowContractAddress="0x"+new string('3',40),ChainPrivateKey="0x"+new string('1',64)};
        var restored=JsonConvert.DeserializeObject<PolygonOASISProviderSettings>(JsonConvert.SerializeObject(settings))!;
        var provider=EvmProviderFactory.CreatePolygon(restored,null!);
        Assert.IsFalse(provider.IsProviderActivated);Assert.AreEqual(settings.TokenEscrowContractAddress,provider.TokenEscrowContractAddress);
        Assert.AreEqual(new BigInteger(80002),Field(provider,"_expectedChainId"));
        Assert.AreEqual(settings.ContractAddress,Field(provider,"_contractAddress"));
        Assert.IsNull(new ArbitrumOASISProviderSettings().TokenEscrowContractAddress);Assert.IsNull(new PolygonOASISProviderSettings().TokenEscrowContractAddress);
    }

    private static object Field(Web3CoreOASISBaseProvider provider,string name)
        =>typeof(Web3CoreOASISBaseProvider).GetField(name,BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(provider)!;
}
