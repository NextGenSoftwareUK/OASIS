using System.Reflection;
using System.Security.Cryptography;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nethereum.RPC.Eth.DTOs;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Utilities;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS.UnitTests;

[TestClass,DoNotParallelize]
public class Web3GraphDispatchControlledTests
{
    private sealed class Provider(Action edit):Web3CoreOASISBaseProvider("http://127.0.0.1:1","0x"+new string('1',64),"0x0000000000000000000000000000000000000001",31337)
    {
        public int Writes;
        public void Configure()
        {
            ProviderType=new NextGenSoftware.Utilities.EnumValue<Core.Enums.ProviderType>(Core.Enums.ProviderType.PolygonOASIS);
            typeof(Web3CoreOASISBaseProvider).GetField("_web3CoreOASIS",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(this,
                new Web3CoreOASIS("0x"+new string('1',64),"http://127.0.0.1:1","0x0000000000000000000000000000000000000001",Web3CoreOASISBaseProviderHelper.Abi,31337));
        }
        protected override async Task<(TransactionReceipt Receipt,Holon Record)> SavePlannedPrivateHolonAsync(PlannedHolonWrite write,bool history)
        {
            Writes++;await Task.Yield();if(Writes==1)edit();
            var stored=OasisJson.DeserializeStored<Holon>(OasisJson.Serialize(write.Snapshot));stored.Version=1;stored.VersionId=Guid.NewGuid();
            return(new(){TransactionHash="0x"+new string('1',64)},stored);
        }
    }
    private static async Task WithKey(Func<Task> test)
    {
        const string name="OASIS_WEB3CORE_STORAGE_ENCRYPTION_KEY";var previous=Environment.GetEnvironmentVariable(name);var key=RandomNumberGenerator.GetBytes(32);
        try{Environment.SetEnvironmentVariable(name,Convert.ToBase64String(key));await test();}
        finally{Environment.SetEnvironmentVariable(name,previous);CryptographicOperations.ZeroMemory(key);}
    }
    [DataTestMethod][DataRow(false,0)][DataRow(true,0)][DataRow(false,1)][DataRow(true,1)][DataRow(false,2)][DataRow(true,2)]
    public Task GraphCallerEditsCannotDispatchAnotherStaleNode(bool synchronous,int edit)=>WithKey(async()=>{
        var root=new Holon{Id=Guid.NewGuid()};var child=new Holon{Id=Guid.NewGuid()};root.Children.Add(child);
        var provider=new Provider(()=>{if(edit==1)root.Description="newer root";if(edit==2)child.Description="newer child";});provider.Configure();
        var result=synchronous?provider.SaveHolons(new[]{root}):await provider.SaveHolonsAsync(new[]{root});
        Assert.AreEqual(edit!=0,result.IsError);Assert.AreEqual(edit==0,result.IsSaved);
        Assert.AreEqual(edit==0?2:1,provider.Writes,"An edited later source must be rejected BEFORE its native submission.");
        Assert.AreEqual(edit==1?0:1,root.Version);Assert.AreEqual(edit==0?1:0,child.Version);
        if(edit!=0){Assert.IsInstanceOfType(result.Exception,typeof(Web3PartialMutationException));Assert.IsTrue(Core.Helpers.NativeMutationReconciliation.IsRequired(result.Exception));}
    });
    [TestMethod]
    public Task NumericGraphKeyCollisionRejectsBeforeAnySubmission()=>WithKey(async()=>{
        var first=Guid.Parse("00005f42-0000-0000-0000-000000000000");var second=Guid.Parse("00017b6d-0000-0000-0000-000000000000");
        Assert.AreEqual(HashUtility.GetNumericHash(first.ToString()),HashUtility.GetNumericHash(second.ToString()));
        var provider=new Provider(()=>{});provider.Configure();
        var result=await provider.SaveHolonsAsync(new[]{new Holon{Id=first},new Holon{Id=second}},saveChildren:false);
        Assert.IsTrue(result.IsError);Assert.IsInstanceOfType(result.Exception,typeof(ArgumentException));Assert.AreEqual(0,provider.Writes);
    });
}
