using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.NEAROASIS.UnitTests;

[TestClass][DoNotParallelize]
public class NearNativeFollowupTests
{
    [DataTestMethod][DataRow("send")][DataRow("deposit")][DataRow("withdraw")][DataRow("lock")][DataRow("lock-mismatch")][DataRow("withdraw-nft")][DataRow("deposit-nft")][DataRow("deposit-transfer-failed")][DataRow("lock-success")]
    public async Task EveryAcknowledgedNativeFollowupFailureKeepsOriginalAndReconciliationMarker(string operation)
    {
        var names=new[]{"OASIS_NEAR_SDK_BRIDGE","OASIS_NEAR_TOKEN_TEST_ACCOUNT","OASIS_NEAR_NFT_TEST_MODE","OASIS_NEAR_STORAGE_ENCRYPTION_KEY"};
        var prior=names.Select(Environment.GetEnvironmentVariable).ToArray();
        try{
            Environment.SetEnvironmentVariable(names[0],Path.Combine(AppContext.BaseDirectory,"Fixtures","nft-followup.mjs"));
            Environment.SetEnvironmentVariable(names[1],"service.near");Environment.SetEnvironmentVariable(names[2],operation=="lock-mismatch"?"present":operation=="lock-success"?"locked":operation=="deposit-transfer-failed"?"transfer-failed":"missing");
            Environment.SetEnvironmentVariable(names[3],null);
            var provider=new NEAROASIS("http://127.0.0.1:1","fixture","fixture","contract.near","service.near","controlled-owner-key");
            Exception? error;bool isError;
            switch(operation){
                case "send":var sent=await provider.SendTransactionAsync("service.near","receiver.near",1m,"controlled followup");error=sent.Exception;isError=sent.IsError;break;
                case "deposit":var deposit=await provider.DepositAsync(1m,"receiver.near");error=deposit.Exception;isError=deposit.IsError;break;
                case "withdraw":var withdraw=await provider.WithdrawAsync(1m,"service.near","controlled-owner-key");error=withdraw.Exception;isError=withdraw.IsError;break;
                case "lock":case "lock-mismatch":case "lock-success":var locked=await provider.LockNFTAsync(new LockWeb3NFTRequest{NFTTokenAddress="owned-test-token"});error=locked.Exception;isError=locked.IsError;break;
                case "withdraw-nft":var withdrawNft=await provider.WithdrawNFTAsync("contract.near","owned-test-token","service.near","controlled-owner-key");error=withdrawNft.Exception;isError=withdrawNft.IsError;Assert.AreEqual(NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums.BridgeTransactionStatus.Pending,withdrawNft.Result.Status);Assert.IsFalse(withdrawNft.Result.IsSuccessful);break;
                default:var depositNft=await provider.DepositNFTAsync("contract.near","owned-test-token","receiver.near");error=depositNft.Exception;isError=depositNft.IsError;Assert.AreEqual(NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums.BridgeTransactionStatus.Pending,depositNft.Result.Status);Assert.IsFalse(depositNft.Result.IsSuccessful);break;
            }
            if(operation=="lock-success"){Assert.IsFalse(isError,error?.ToString());return;}
            Assert.IsTrue(isError);Assert.IsInstanceOfType(error,typeof(NativeTransactionFollowupException));
            Assert.IsTrue(NativeMutationReconciliation.IsRequired(error));
            Assert.AreEqual(operation is "send" or "deposit" or "withdraw"?"controlled-original-send-transaction":operation=="deposit-transfer-failed"?"controlled-original-unlock":"controlled-original-nft-transaction",((NativeTransactionFollowupException)error).OriginalTransactionId);
            if(operation=="deposit-transfer-failed"){Assert.IsInstanceOfType(error.InnerException,typeof(NativeTransactionFollowupException));Assert.AreEqual("controlled-original-nft-transaction",((NativeTransactionFollowupException)error.InnerException).OriginalTransactionId);}
        }finally{for(var i=0;i<names.Length;i++)Environment.SetEnvironmentVariable(names[i],prior[i]);}
    }
}
