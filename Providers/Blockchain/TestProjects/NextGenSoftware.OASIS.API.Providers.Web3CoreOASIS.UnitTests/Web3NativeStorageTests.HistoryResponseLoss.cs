using System.Reflection;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS.UnitTests;

public partial class Web3NativeStorageTests
{
    private static async Task VerifyHistoryResponseLossAsync(Web3CoreOASISBaseProvider provider)
    {
        var flags=BindingFlags.Instance|BindingFlags.NonPublic;
        var field=typeof(Web3CoreOASISBaseProvider).GetField("_web3CoreOASIS",flags)!;
        var healthy=(Web3CoreOASIS)field.GetValue(provider)!;
        var originalOption=provider.IsVersionControlEnabled;
        var contract=(string)typeof(Web3CoreOASISBaseProvider).GetField("_contractAddress",flags)!.GetValue(provider)!;
        var work=Path.GetDirectoryName(Path.GetFullPath(Environment.GetEnvironmentVariable("WEB3CORE_NATIVE_FIXTURE")!))!;
        var journal=Path.Combine(work,"history-response-loss.pending.json");
        var id=Guid.NewGuid();var numeric=(uint)HashUtility.GetNumericHash(id.ToString());
        using(var created=new FileStream(journal,FileMode.CreateNew,FileAccess.Write,FileShare.None)){
            var bytes=Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new{record=id,contract,chainId=31337,phase="before-seed"})+"\n");created.Write(bytes);created.Flush(true);
        }
        var phase="seed";var intents=new List<JsonElement>();
        Task Persist(JsonElement evidence){
            var bytes=Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new{phase,evidence})+"\n");
            using var stream=new FileStream(journal,FileMode.Append,FileAccess.Write,FileShare.None);stream.Write(bytes);stream.Flush(true);
            intents.Add(evidence.Clone());return Task.CompletedTask;
        }
        try{
            provider.IsVersionControlEnabled=true;
            var source=new AvatarDetail{Id=id,IsActive=true,MetaData=new(){{"history-marker","native-history-first"}}};
            var seed=await healthy.WithNativePreparationAsync(()=>provider.SaveAvatarDetailAsync(source),Persist);
            Assert.IsFalse(seed.IsError,seed.Message);Assert.AreEqual(1,source.Version);
            var versionId=source.VersionId;var previousVersionId=source.PreviousVersionId;
            var storageKey=source.ProviderUniqueStorageKey[provider.ProviderType.Value];
            source.MetaData["history-marker"]="native-history-second";
            phase="lost-response";
            string accepted;
            await using(var proxy=new Web3AcceptedWriteProxy()){
                var fault=new Web3CoreOASIS(Environment.GetEnvironmentVariable("ETHEREUMOASIS_TEST_PRIVATE_KEY")!,"http://127.0.0.1:18546",contract,Web3CoreOASISBaseProviderHelper.Abi,31337);
                field.SetValue(provider,fault);
                var failed=await fault.WithNativePreparationAsync(()=>provider.SaveAvatarDetailAsync(source),Persist);
                Assert.IsTrue(failed.IsError);Assert.IsFalse(failed.IsSaved);Assert.IsNull(failed.Result);
                Assert.IsInstanceOfType(failed.Exception,typeof(Web3MutationReconciliationException));
                accepted=proxy.AcceptedHashes.Single();
                Assert.AreEqual(accepted,((Web3MutationReconciliationException)failed.Exception).OriginalTransactionId);
                Assert.AreEqual(1,proxy.Methods.Count(method=>method=="eth_sendRawTransaction"));
                Assert.AreEqual(accepted,intents.Single(item=>item.GetProperty("transactionHash").GetString()==accepted).GetProperty("transactionHash").GetString());
                Assert.AreEqual(1,source.Version);Assert.AreEqual(versionId,source.VersionId);Assert.AreEqual(previousVersionId,source.PreviousVersionId);
                Assert.AreEqual(storageKey,source.ProviderUniqueStorageKey[provider.ProviderType.Value]);
            }
            field.SetValue(provider,healthy);
            var receipt=await healthy.ReadOriginalReceiptAsync(accepted);Assert.IsNotNull(receipt);Assert.AreEqual(System.Numerics.BigInteger.One,receipt.Status.Value);
            Assert.AreEqual(accepted,receipt.TransactionHash);
            var native=await healthy.GetAvatarDetailByIdAsync(numeric);
            await VerifyNativeHistory<AvatarDetail>(provider,"detail",id,native,2,false);
            // Bounded high-entropy payload exceeds the existing native gas allowance without relying on compression luck.
            phase="capacity-rejection";
            source.Description=Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(24576));
            var rejected=await healthy.WithNativePreparationAsync(()=>provider.SaveAvatarDetailAsync(source),Persist);
            Assert.IsTrue(rejected.IsError);Assert.IsFalse(rejected.IsSaved);Assert.IsNull(rejected.Result);
            Assert.IsInstanceOfType(rejected.Exception,typeof(Web3MutationReconciliationException));
            var capacityHash=intents.Last().GetProperty("transactionHash").GetString()!;
            Assert.AreEqual(capacityHash,((Web3MutationReconciliationException)rejected.Exception).OriginalTransactionId);
            var capacityReceipt=await healthy.ReadOriginalReceiptAsync(capacityHash);
            Assert.IsNotNull(capacityReceipt);Assert.AreEqual(System.Numerics.BigInteger.Zero,capacityReceipt.Status.Value);
            Assert.AreEqual(capacityHash,capacityReceipt.TransactionHash);
            CollectionAssert.AreEqual(native.Info,(await healthy.GetAvatarDetailByIdAsync(numeric)).Info);
            Assert.AreEqual(1,source.Version);Assert.AreEqual(versionId,source.VersionId);
            var missing=await provider.LoadAvatarDetailAsync(id,version:3);Assert.IsTrue(missing.IsError);
            Assert.AreEqual(Common.OASISResultErrorCodes.RecordNotFound,missing.ErrorCode);
            phase="cleanup";Assert.IsTrue(await healthy.WithNativePreparationAsync(()=>healthy.DeleteAvatarDetailAsync(numeric),Persist));
            Assert.IsTrue(Web3CoreOASISBaseProvider.IsPrivateRecordAbsent(await healthy.GetAvatarDetailByIdAsync(numeric)));
            File.Delete(journal); // Only after original receipt/history and exact fixture absence are verified.
            Console.WriteLine(JsonSerializer.Serialize(new{proof="WEB3_HISTORY_LOSS_AND_CAPACITY_VERIFIED",record=id,accepted,capacityHash,
                capacityGasUsed=capacityReceipt.GasUsed.Value.ToString(),capacityGasPrice=capacityReceipt.EffectiveGasPrice?.Value.ToString(),
                capacityFeeWei=capacityReceipt.EffectiveGasPrice==null?null:(capacityReceipt.GasUsed.Value*capacityReceipt.EffectiveGasPrice.Value).ToString(),
                originalArchiveUnchanged=true,fixtureAbsent=true}));
        }finally{field.SetValue(provider,healthy);provider.IsVersionControlEnabled=originalOption;}
    }
}
