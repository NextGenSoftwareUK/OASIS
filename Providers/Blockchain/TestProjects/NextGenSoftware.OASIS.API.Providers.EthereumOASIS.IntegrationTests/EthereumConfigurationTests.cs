using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.OASISBootLoader;

namespace NextGenSoftware.OASIS.API.Providers.EthereumOASIS.IntegrationTests;

[TestClass]
public class EthereumConfigurationTests
{
    [DataTestMethod][DataRow(false)][DataRow(true)]
    public void DnaHistoryRoundtripAndFactoryBindingDoNotActivate(bool enabled)
    {
        Assert.IsFalse(JsonConvert.DeserializeObject<EthereumOASISProviderSettings>("{}")!.IsVersionControlEnabled);
        var settings=JsonConvert.DeserializeObject<EthereumOASISProviderSettings>(JsonConvert.SerializeObject(new EthereumOASISProviderSettings{
            IsVersionControlEnabled=enabled,ConnectionString="http://127.0.0.1:1",ChainPrivateKey=new string('1',64),ChainId=31337,
            ContractAddress="0x"+new string('2',40),TokenEscrowContractAddress="0x"+new string('3',40)}))!;
        var dna=JsonConvert.DeserializeObject<OASISDNA>(JsonConvert.SerializeObject(new OASISDNA{OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS{
            HyperDriveMode=HyperDriveModes.V2,StorageProviders=new StorageProviderSettings{EthereumOASIS=settings}}}))!;
        settings=dna.OASIS.StorageProviders.EthereumOASIS;
        Assert.AreEqual(enabled,settings.IsVersionControlEnabled);
        foreach(var endpoint in new string?[]{null,"http://127.0.0.1:2"}){
            var provider=EthereumProviderFactory.Create(settings,endpoint!);
            Assert.AreEqual(enabled,provider.IsVersionControlEnabled);Assert.AreEqual(settings.ContractAddress,provider.ContractAddress);
            Assert.AreEqual(endpoint??settings.ConnectionString,provider.HostURI);Assert.AreEqual(settings.ChainPrivateKey,provider.ChainPrivateKey);
            Assert.AreEqual(settings.ChainId,(long)provider.ChainId);Assert.AreEqual(settings.TokenEscrowContractAddress,provider.TokenEscrowContractAddress);
            Assert.IsFalse(provider.IsProviderActivated);Assert.IsNull(provider.Web3Client);
        }
    }
}
