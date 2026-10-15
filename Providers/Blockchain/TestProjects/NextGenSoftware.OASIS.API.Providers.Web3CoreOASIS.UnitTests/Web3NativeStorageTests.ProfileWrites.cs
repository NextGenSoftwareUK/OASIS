using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using Nethereum.RPC.Eth.DTOs;

namespace NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS.UnitTests;

public partial class Web3NativeStorageTests
{
    private static async Task VerifyProfileAcknowledgementAsync(PolygonOASIS.PolygonOASIS provider,Web3CoreOASIS native)
    {
        var generated=new Avatar {IsActive=true,IsNewHolon=true,Username="generated profile fixture"};
        var saved=await provider.SaveAvatarAsync(generated);
        Assert.IsFalse(saved.IsError,saved.Message);Assert.AreNotEqual(Guid.Empty,generated.Id);Assert.IsTrue(saved.IsSaved);
        Assert.AreEqual(generated.Id,(await provider.LoadAvatarAsync(generated.Id)).Result.Id);
        Assert.AreEqual(generated.Id.ToString(),generated.ProviderUniqueStorageKey[Core.Enums.ProviderType.PolygonOASIS]);
        Assert.IsFalse((await provider.DeleteAvatarAsync(generated.Id,softDelete:false)).IsError);

        var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
        var field=typeof(Web3CoreOASISBaseProvider).GetField("_web3CoreOASIS",flags)!;
        var healthy=field.GetValue(provider);
        field.SetValue(provider,new Web3CoreOASIS(Environment.GetEnvironmentVariable("ETHEREUMOASIS_TEST_PRIVATE_KEY")!,"http://127.0.0.1:18546",
            (string)typeof(Web3CoreOASISBaseProvider).GetField("_contractAddress",flags)!.GetValue(provider)!,Web3CoreOASISBaseProviderHelper.Abi,31337));
        try
        {
            var pending=new AvatarDetail {IsNewHolon=true,IsActive=true,XP=123};
            var failed=await provider.SaveAvatarDetailAsync(pending);
            Assert.IsTrue(failed.IsError);Assert.IsTrue(Core.Helpers.NativeMutationReconciliation.IsRequired(failed.Exception));
            Assert.AreEqual(Guid.Empty,pending.Id);Assert.IsTrue(pending.IsNewHolon);
            Assert.IsFalse(pending.ProviderUniqueStorageKey?.ContainsKey(Core.Enums.ProviderType.PolygonOASIS)==true);
        }
        finally {field.SetValue(provider,healthy);}

        var originalId=Guid.NewGuid();var changedId=Guid.NewGuid();
        var source=new AvatarDetail {Id=originalId,IsActive=true,IsNewHolon=true,XP=642};
        var acknowledged=new TaskCompletionSource<TransactionReceipt>(TaskCreationOptions.RunContinuationsAsynchronously);
        var release=new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        Func<uint,byte[],byte[],Task<TransactionReceipt>> submit=async (id,key,payload)=>
        {
            var receipt=await native.UpsertAvatarDetailAsync(id,key,payload);
            acknowledged.SetResult(receipt);
            await release.Task;
            return receipt;
        };
        var method=typeof(Web3CoreOASISBaseProvider).GetMethod("SavePrivateProfileAsync",flags)!.MakeGenericMethod(typeof(IAvatarDetail));
        Func<IAvatarDetail,IAvatarDetail> clone=row=>OasisJson.Deserialize<AvatarDetail>(OasisJson.Serialize(row));
        var operation=(Task<OASISResult<IAvatarDetail>>)method.Invoke(provider,new object[] {source,"detail",clone,submit})!;
        TransactionReceipt receipt;
        try
        {
            var first=await Task.WhenAny(acknowledged.Task,operation).WaitAsync(TimeSpan.FromSeconds(30));
            if(first==operation) Assert.Fail("Profile write ended before acknowledgement barrier: "+(await operation).Message);
            receipt=await acknowledged.Task;
            source.Id=changedId;source.XP=999;
        }
        finally {release.TrySetResult(true);}
        var result=await operation;
        Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsSaved);Assert.IsNull(result.Result);
        var evidence=result.Exception as Web3PartialMutationException;
        Assert.IsNotNull(evidence);CollectionAssert.AreEqual(new[] {originalId},evidence.AcknowledgedRecordIds.ToArray());
        CollectionAssert.AreEqual(new[] {receipt.TransactionHash},evidence.AcknowledgedTransactionIds.ToArray());
        Assert.AreEqual(changedId,source.Id);Assert.IsTrue(source.IsNewHolon);
        Assert.IsFalse(source.ProviderUniqueStorageKey?.ContainsKey(Core.Enums.ProviderType.PolygonOASIS)==true);
        var stored=await provider.LoadAvatarDetailAsync(originalId);Assert.IsFalse(stored.IsError,stored.Message);Assert.AreEqual(642L,stored.Result.XP);
        Assert.AreEqual(NextGenSoftware.OASIS.Common.OASISResultErrorCodes.RecordNotFound,(await provider.LoadAvatarDetailAsync(changedId)).ErrorCode);
    }
}
