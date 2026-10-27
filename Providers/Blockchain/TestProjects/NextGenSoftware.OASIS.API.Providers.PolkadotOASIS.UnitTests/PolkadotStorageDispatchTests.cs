using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.PolkadotOASIS.UnitTests;

[TestClass]
public class PolkadotStorageDispatchTests
{
    private const string Identity="0xaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
    [DataTestMethod]
    [DataRow("LoadAvatarAsync")][DataRow("LoadAvatarByUsernameAsync")][DataRow("LoadAvatarByEmailAsync")]
    [DataRow("LoadAvatarDetailAsync")][DataRow("LoadHolonAsync")][DataRow("LoadHolonsForParentAsync")]
    [DataRow("LoadHolonsByMetaDataAsync")][DataRow("SaveAvatarAsync")][DataRow("SaveAvatarDetailAsync")]
    [DataRow("SaveHolonAsync")][DataRow("SaveHolonsAsync")][DataRow("DeleteHolonAsync")]
    [DataRow("SearchAsync")][DataRow("ExportAllAsync")][DataRow("ImportAsync")]
    public void PublicStorageUsesCanonicalSharedEngine(string method)
    {
        var matches=typeof(PolkadotOASIS).GetMethods(BindingFlags.Public|BindingFlags.Instance).Where(m=>m.Name==method).ToArray();
        Assert.IsTrue(matches.Any(m=>m.DeclaringType==typeof(KeyValueStorageProviderBase)));
        Assert.IsFalse(matches.Any(m=>m.DeclaringType==typeof(PolkadotOASIS)),"A provider-specific storage override bypasses the canonical engine.");
    }
    [TestMethod]
    public void ConfigurationNormalizesIdentityButDoesNotResolveSecretsDuringWalletConstruction()
    {
        var reads=0;
        var configuration=new PolkadotStorageConfiguration("0x"+new string('A',64),Identity,Identity,()=>{reads++;throw new AssertFailedException("Unexpected secret resolution");});
        using var provider=new PolkadotOASIS("http://127.0.0.1:1","fixture","",null,configuration);
        Assert.AreEqual(Identity,configuration.ContractAccountId);
        Assert.AreEqual(0,reads);
    }
    [DataTestMethod][DataRow("ss58-not-canonical")][DataRow("")][DataRow("0x01")]
    public void InvalidConfigurationFailsBeforeAnyClientWork(string value)
        =>Assert.ThrowsException<ArgumentException>(()=>new PolkadotStorageConfiguration(value,Identity,Identity));
    [TestMethod]
    public async Task UnactivatedScopeCannotResolveKeyOrClaimDurableOwner()
    {
        var reads=0;
        var configuration=new PolkadotStorageConfiguration(Identity,Identity,Identity,()=>{reads++;return new byte[32];});
        using var provider=new PolkadotOASIS("http://127.0.0.1:1","fixture","",null,configuration);
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(()=>provider.WithRecordStoragePreparationAsync(
            ()=>Task.FromResult(true),(_,_)=>throw new AssertFailedException("Unexpected owner")));
        Assert.AreEqual(0,reads);
    }
    [TestMethod]
    public async Task UnconfiguredStorageCannotBecomeFallbackSuccess()
    {
        using var provider=new PolkadotOASIS("http://127.0.0.1:1","fixture","",null,null);
        var result=await provider.SaveHolonAsync(new Holon{Id=Guid.NewGuid(),Name="synthetic"},saveChildren:false);
        Assert.IsTrue(result.IsError);Assert.IsNull(result.Result);
    }
}
