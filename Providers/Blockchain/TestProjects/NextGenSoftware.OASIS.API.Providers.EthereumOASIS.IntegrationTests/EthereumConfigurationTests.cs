using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.OASISBootLoader;

namespace NextGenSoftware.OASIS.API.Providers.EthereumOASIS.IntegrationTests;

[TestClass,DoNotParallelize]
public class EthereumConfigurationTests
{
    [TestMethod]
    public async Task PendingFaultJournalRejectsBeforeAllocatingNativeFixture()
    {
        var folder=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../../../../TestResults/EthereumCurrent/admission-guard"));
        Directory.CreateDirectory(folder);
        var journal=Path.Combine(folder,"ethereum-accepted-loss.pending.json");
        var prior=Environment.GetEnvironmentVariable("ETHEREUMOASIS_TEST_FIXTURE_JSON");
        var owned=false;
        try {
            using(var stream=new FileStream(journal,FileMode.CreateNew,FileAccess.Write,FileShare.None)){
                var bytes=System.Text.Encoding.UTF8.GetBytes("owned admission guard");stream.Write(bytes);stream.Flush(true);owned=true;
            }
            Environment.SetEnvironmentVariable("ETHEREUMOASIS_TEST_FIXTURE_JSON",Path.Combine(folder,"unused-fixture.json"));
            var refused=await Assert.ThrowsExceptionAsync<InvalidOperationException>(()=>new EthereumOASISIntegrationTests().AcceptedStorageResponseLossRetainsOriginalSignedHashAndArchive("avatar"));
            StringAssert.Contains(refused.Message,"Reconcile the original Ethereum fault");
            Assert.AreEqual("owned admission guard",await File.ReadAllTextAsync(journal));
        }finally{
            Environment.SetEnvironmentVariable("ETHEREUMOASIS_TEST_FIXTURE_JSON",prior);
            if(owned)File.Delete(journal);
            if(Directory.GetFileSystemEntries(folder).Length==0)Directory.Delete(folder);
        }
    }

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
