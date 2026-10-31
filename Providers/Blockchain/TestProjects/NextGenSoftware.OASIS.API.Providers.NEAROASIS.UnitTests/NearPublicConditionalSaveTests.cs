using System.Reflection;
using System.Text.Json;
using System.Security.Cryptography;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
namespace NextGenSoftware.OASIS.API.Providers.NEAROASIS.UnitTests;

[TestClass][DoNotParallelize]
public class NearPublicConditionalSaveTests
{
    [DataTestMethod]
    [DataRow(0,"OASIS_RECORD_CONFLICT")][DataRow(1,"OASIS_RECORD_CONFLICT")][DataRow(2,"OASIS_RECORD_CONFLICT")]
    [DataRow(0,"OASIS_ARCHIVE_CONFLICT")][DataRow(1,"OASIS_ARCHIVE_CONFLICT")][DataRow(2,"OASIS_ARCHIVE_CONFLICT")]
    [DataRow(0,"unknown")][DataRow(1,"unknown")][DataRow(2,"unknown")]
    [DataRow(0,"success")][DataRow(1,"success")][DataRow(2,"success")]
    public async Task PublicSavesUseConditionalReceiptAndActualV2Policy(int kind,string mode)
    {
        var names=new[]{"OASIS_NEAR_SDK_BRIDGE","OASIS_NEAR_STORAGE_ENCRYPTION_KEY","OASIS_NEAR_SNAPSHOT_FIXTURE_RESPONSE","OASIS_NEAR_CHECKED_FIXTURE_ENVELOPE"};
        var previous=names.ToDictionary(name=>name,Environment.GetEnvironmentVariable);
        try{
            Environment.SetEnvironmentVariable(names[0],Path.Combine(AppContext.BaseDirectory,"Fixtures","storage-snapshot.mjs"));
            Environment.SetEnvironmentVariable(names[1],Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)));
            Environment.SetEnvironmentVariable(names[2],"null");
            object status=mode=="success"?new{SuccessValue=""}:new{Failure=new{ActionError=new{index=0,kind=new{FunctionCallError=new{ExecutionError="Smart contract panicked: "+mode+"\ncontrolled native-format rejection"}}}}};
            Environment.SetEnvironmentVariable(names[3],mode=="unknown"?"""{"ok":false,"error":"controlled response loss"}""":JsonSerializer.Serialize(new{ok=true,result=new{transactionHash="controlled-original",finalExecutionStatus="FINAL",nativeStatus=status}}));
            var provider=new NEAROASIS("http://127.0.0.1:1","fixture","fixture","contract.near","sender.near","unused");
            IHolonBase source=kind==0?new Avatar():kind==1?new AvatarDetail():new Holon();
            if(kind==0)Check(await provider.SaveAvatarAsync((IAvatar)source));
            else if(kind==1)Check(await provider.SaveAvatarDetailAsync((IAvatarDetail)source));
            else Check(await provider.SaveHolonAsync((IHolon)source,saveChildren:false));
            void Check<T>(OASISResult<T> result)
            {
                if(mode=="success"){Assert.IsFalse(result.IsError,result.Message);Assert.IsTrue(result.IsSaved);Assert.IsNotNull(result.Result);Assert.AreNotEqual(Guid.Empty,source.Id);Assert.AreEqual(1,source.Version);return;}
                Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsSaved);Assert.IsNull(result.Result);Assert.AreEqual(Guid.Empty,source.Id);Assert.AreEqual(0,source.Version);
                if(mode=="unknown")Assert.IsTrue(NativeMutationReconciliation.IsRequired(result.Exception));
                else{Assert.IsInstanceOfType<NearRecordWriteRejectedException>(result.Exception);Assert.AreEqual("controlled-original",((NearRecordWriteRejectedException)result.Exception).OriginalTransactionId);Assert.AreEqual(StorageLifecycleConflictException.ErrorCode,result.ErrorCode);}
                var decision=typeof(NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.OASISHyperDrive).GetMethod("ShouldFailOver",BindingFlags.Static|BindingFlags.NonPublic)!.MakeGenericMethod(typeof(T));
                Assert.AreEqual(false,decision.Invoke(null,new object?[]{null,result}));
            }
        }finally{foreach(var name in names)Environment.SetEnvironmentVariable(name,previous[name]);}
    }
}
