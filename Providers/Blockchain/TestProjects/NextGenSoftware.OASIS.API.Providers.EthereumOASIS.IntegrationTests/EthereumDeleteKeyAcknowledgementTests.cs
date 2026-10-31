using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.EthereumOASIS.IntegrationTests;

public partial class EthereumOASISIntegrationTests
{
    [DataTestMethod]
    [DataRow(0,false)][DataRow(1,false)][DataRow(2,false)]
    [DataRow(0,true)][DataRow(1,true)][DataRow(2,true)]
    public async Task NativeProviderKeyDeleteKeepsReceiptOutcome(int route,bool lost)
    {
        var fixture=Environment.GetEnvironmentVariable("ETHEREUMOASIS_TEST_FIXTURE_JSON")??throw new InvalidOperationException("Retained fixture required.");
        var journal=Path.Combine(Path.GetDirectoryName(fixture)!,"delete-key-native.pending.json");
        Assert.IsFalse(File.Exists(journal),"Reconcile original key-deletion fixture; never replay/reset.");
        var (reader,contract)=await CreateActivatedProviderAsync();var faulty=lost?new LostStorageReceiptProvider(contract):null;
        EthereumOASIS writer=faulty??reader;var id=Guid.NewGuid();
        try{
            if(lost)Assert.IsFalse((await writer.ActivateProviderAsync()).IsError);
            using(var intent=new FileStream(journal,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)){
                var bytes=System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(new{contract,id,route,lost});await intent.WriteAsync(bytes);intent.Flush(true);
            }
            Assert.IsFalse((await reader.SaveHolonAsync(new Holon{Id=id,Name="owned-key-delete"},saveChildren:false)).IsError);
            var result=route==0?writer.DeleteHolon(id.ToString()):route==1?await writer.DeleteHolonAsync(id.ToString()):await writer.DeleteHolonByProviderKeyAsync(id.ToString());
            if(lost){
                Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsDeleted);Assert.IsNull(result.Result);
                var marker=result.Exception as EthereumStorageReceiptReconciliationException;Assert.IsNotNull(marker,result.Exception?.ToString());
                Assert.AreSame(faulty!.Cause,marker.InnerException);Assert.AreEqual(faulty.SubmittedHash,marker.OriginalTransactionId);Assert.AreEqual(1,faulty.Polls);
                Assert.IsTrue(NextGenSoftware.OASIS.API.Core.Helpers.NativeMutationReconciliation.IsRequired(marker));
                var receipt=await reader.Web3Client.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(marker.OriginalTransactionId);
                Assert.AreEqual(System.Numerics.BigInteger.One,receipt.Status.Value);
            }else{Assert.IsFalse(result.IsError,result.Exception?.ToString());Assert.IsTrue(result.IsDeleted);Assert.AreEqual(1,result.DeletedCount);Assert.AreEqual(id,result.Result.Id);}
            var native=new NextGenSoftwareOASISService(reader.Web3Client,contract);
            Assert.AreEqual(System.Numerics.BigInteger.Zero,(await native.GetHolonByIdQueryAsync(NextGenSoftware.OASIS.API.Core.Utilities.HashUtility.GetNumericHash(id.ToString()))).ReturnValue1.EntityId);
            File.Delete(journal);
        }finally{if(faulty!=null)await faulty.DeActivateProviderAsync();await reader.DeActivateProviderAsync();}
    }

    private sealed class DeleteKeyOwner:EthereumOASIS
    {
        public readonly IHolon Row=new Holon{Id=Guid.NewGuid()};
        public readonly OASISResult<IHolon> Loaded,Acknowledgement;
        public int Reads,Deletes;
        public DeleteKeyOwner(int outcome):base("http://127.0.0.1:18545","0x0000000000000000000000000000000000000000000000000000000000000001",31337,"0x0000000000000000000000000000000000000000")
        {
            Loaded=outcome==0?new(){IsError=true,ErrorCode="owned-read-failure",Exception=new IOException("owned read failure")}:new(){Result=Row,IsLoaded=true};
            Acknowledgement=outcome==1?new(){IsError=true,ErrorCode="owned-uncertain-delete",Exception=new EthereumStorageReceiptReconciliationException("owned-transaction",new IOException("owned lost receipt"))}:new(){IsDeleted=true,DeletedCount=1};
        }
        public override OASISResult<bool> ActivateProvider()=>new(){Result=true};
        public override Task<OASISResult<IHolon>> LoadHolonAsync(string key,bool loadChildren=true,bool recursive=true,int maxChildDepth=0,bool continueOnError=true,bool loadChildrenFromProvider=false,int version=0)
        {Reads++;return Task.FromResult(Loaded);}
        public override Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id)
        {Assert.AreEqual(Row.Id,id);Deletes++;return Task.FromResult(Acknowledgement);}
    }

    [DataTestMethod]
    [DataRow(0,0)][DataRow(0,1)][DataRow(0,2)]
    [DataRow(1,0)][DataRow(1,1)][DataRow(1,2)]
    [DataRow(2,0)][DataRow(2,1)][DataRow(2,2)]
    public async Task ProviderKeyDeletePreservesOriginalAcknowledgement(int route,int outcome)
    {
        var provider=new DeleteKeyOwner(outcome);
        var result=route==0?provider.DeleteHolon(provider.Row.Id.ToString()):route==1?await provider.DeleteHolonAsync(provider.Row.Id.ToString()):await provider.DeleteHolonByProviderKeyAsync(provider.Row.Id.ToString());
        Assert.AreEqual(1,provider.Reads);Assert.AreEqual(outcome==0?0:1,provider.Deletes);
        if(outcome==0){Assert.AreSame(provider.Loaded,result);Assert.IsTrue(result.IsError);}
        else{
            Assert.AreSame(provider.Acknowledgement,result);
            if(outcome==1){Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsDeleted);Assert.IsNull(result.Result);Assert.IsInstanceOfType(result.Exception,typeof(EthereumStorageReceiptReconciliationException));}
            else{Assert.IsFalse(result.IsError);Assert.IsTrue(result.IsDeleted);Assert.AreEqual(1,result.DeletedCount);Assert.AreSame(provider.Row,result.Result);}
        }
    }
}
