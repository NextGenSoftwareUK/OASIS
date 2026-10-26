using System.Reflection;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers.NFT;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.Common;
using AttemptRecord=NextGenSoftware.OASIS.API.Providers.PolkadotOASIS.IntegrationTests.PolkadotNftPublicMutationNativeTests.AttemptRecord;
using FlushedOwner=NextGenSoftware.OASIS.API.Providers.PolkadotOASIS.IntegrationTests.PolkadotNftPublicMutationNativeTests.FlushedNftOwner;

namespace NextGenSoftware.OASIS.API.Providers.PolkadotOASIS.IntegrationTests;

[TestClass][DoNotParallelize]
public class PolkadotNftCustodyNativeTests
{
    private const string Genesis="0xe8cf5d7d636a08721efaa17acc52ed3d895fe2468dccb2520cc8e249ddb6b208";
    private const string Holder="0x1a4fee48c1ba1a48e8cd43782a8485d635aa91cfb82cbb477f0c1c576bc4031c";
    private const string Pool="0x8ee504148e75c34e8f051899b3c6e4241ff18dc1c9211260b6a6a434bedb485f";
    private const string NativeId="{\"U128\":\"18446744073709551619\"}";
    private static readonly Guid Avatar=Guid.Parse("da06227d-87d7-4c71-a6ef-c88ded5e7201");
    private static readonly Guid StoredId=Guid.Parse("b439cdfb-c50f-4f2e-b6bb-5158c3385559");

    [TestMethod][TestCategory("LocalNative")]
    public async Task StoredLockAndSourceProvenReleaseUseOriginalOwnedSdkTransactions()
    {
        Assert.AreEqual("1",Environment.GetEnvironmentVariable("OASIS_POLKADOT_LOCAL_NATIVE"));
        var bridge=Environment.GetEnvironmentVariable("OASIS_POLKADOT_SDK_BRIDGE")!;
        var folder=Path.GetFullPath(Path.Combine(Path.GetDirectoryName(bridge)!,"../../../../Docs/WIP/HyperDrive-Providers/NativeEvidence/Polkadot"));
        var bridgeMode=Environment.GetEnvironmentVariable("OASIS_POLKADOT_NFT_BRIDGE_NATIVE")=="1";
        var prefix=bridgeMode?"psp34-bridge":"psp34-custody";
        var nativeId=bridgeMode?"{\"U128\":\"18446744073709551622\"}":NativeId;
        var pending=Path.Combine(folder,prefix+"-pending.jsonl");
        var completed=Path.Combine(folder,prefix+".completed.json");
        using var deployment=JsonDocument.Parse(File.ReadAllText(Path.Combine(folder,"psp34-deployment.completed.json")));
        var collection=deployment.RootElement.GetProperty("address").GetString()!;
        var code=deployment.RootElement.GetProperty("codeHash").GetString()!;
        var records=new List<AttemptRecord>();
        var recovering=File.Exists(completed)||File.Exists(pending);
        Assert.IsTrue(bridgeMode||recovering,"The v0.1 service-pool fixture is now recovery-only; use the native v0.2 escrow fixture for new lock/unlock operations.");
        if(Environment.GetEnvironmentVariable("OASIS_POLKADOT_NFT_CUSTODY_RECOVERY")=="1")
            Assert.IsTrue(recovering,"Explicit recovery cannot start writes.");
        if(File.Exists(completed)) {
            using var file=JsonDocument.Parse(File.ReadAllText(completed));
            records.AddRange(file.RootElement.GetProperty("records").Deserialize<List<AttemptRecord>>()!);
        } else if(File.Exists(pending))records.AddRange(File.ReadAllLines(pending).Select(line=>JsonSerializer.Deserialize<AttemptRecord>(line)!));
        using var provider=new LocalProvider(code,collection);
        var activated=await provider.ActivateProviderAsync();Assert.IsFalse(activated.IsError,activated.Message);
        if(!recovering)for(var step=0;step<4;step++) {
            var record=new AttemptRecord { Action=step==0?"mint":step==3?"burn":"transfer",TokenId=nativeId,
                From=step==0?null:step==2?Pool:Holder,To=step==3?null:step==1?Pool:Holder };
            var store=DispatchProxy.Create<IUsageOutboxStore,FlushedOwner>();
            var owner=(FlushedOwner)(object)store;owner.Journal=pending;owner.Record=record;
            var constructor=typeof(UsageExecutionHandle).GetConstructors(BindingFlags.Instance|BindingFlags.NonPublic)
                .Single(value=>value.GetParameters().Length==5);
            var execution=(UsageExecutionHandle)constructor.Invoke(new object?[]{
                new UsageOutboxOperation { Id=Guid.NewGuid().ToString("D"),ExecutionOwner="native-nft-custody-owner",Policy=new UsageEndpointPolicy() },
                store,new Web4UsageLedgerOptions(),null,null});
            async Task<string> Invoke() {
                if(step==2) {
                    var released=await provider.DepositNFTAsync(collection,nativeId,Holder,records[1].Attempt.ProviderRequestId);
                    Assert.IsFalse(released.IsError,released.Exception?.ToString()??released.Message);
                    Assert.AreEqual(Core.Managers.Bridge.Enums.BridgeTransactionStatus.Completed,released.Result.Status);
                    return released.Result.TransactionId;
                }
                if(step==1&&bridgeMode) {
                    var withdrawn=await provider.WithdrawNFTAsync(collection,nativeId,Holder,"0x"+string.Concat(Enumerable.Repeat("02",32)));
                    Assert.IsFalse(withdrawn.IsError,withdrawn.Exception?.ToString()??withdrawn.Message);
                    Assert.IsTrue(withdrawn.Result.IsSuccessful);
                    Assert.AreEqual(Core.Managers.Bridge.Enums.BridgeTransactionStatus.Completed,withdrawn.Result.Status);
                    return withdrawn.Result.TransactionId;
                }
                var result=step==0?await provider.MintNFTAsync(new MintWeb3NFTRequest {
                    MintedByAvatarId=Avatar,CollectionPublicKey=collection,NumberToMint=1,Amount=1,NFTStandardType=NFTStandardType.PSP34,
                    Title="Retained custody fixture",MetaData=new(){{"polkadot:token-id",nativeId}}
                }):step==1?await provider.LockNFTAsync(new LockWeb3NFTRequest {
                    LockedByAvatarId=Avatar,Web3NFTId=StoredId,NFTTokenAddress=collection
                }):await provider.BurnNFTAsync(new BurnWeb3NFTRequest {
                    BurntByAvatarId=Avatar,NFTTokenAddress=collection,TokenId=nativeId,OwnerPublicKey=Holder,
                    OwnerPrivateKey="0x"+string.Concat(Enumerable.Repeat("02",32)),OwnerSeedPhrase=null!
                });
                Assert.IsFalse(result.IsError,result.Exception?.ToString()??result.Message);
                Assert.IsTrue(result.IsSaved);Assert.AreEqual(nativeId,result.Result.Web3NFT.TokenId);
                return result.Result.TransactionResult;
            }
            using var timeout=new CancellationTokenSource(TimeSpan.FromMinutes(3));
            var hash=await provider.ExecutePreparedNativeOperationAsync(Invoke,execution,$"{prefix}-v1-{step}",step==2?Pool:Holder,timeout.Token);
            Assert.AreEqual(1,owner.Calls);Assert.AreEqual(record.Attempt.ProviderRequestId,hash);
            records.Add(record);
        }
        Assert.IsTrue(records.Count>0&&records.Count<=4);
        var receipts=new List<JsonElement>();
        foreach(var record in records) {
            var recovered=await provider.RecoverNftPreparationAsync(record.Attempt);Assert.IsFalse(recovered.IsError,recovered.Message);
            var original=recovered.Result;
            Assert.AreEqual(record.Action,original.GetProperty("action").GetString());
            using var id=JsonDocument.Parse(nativeId);Assert.IsTrue(JsonElement.DeepEquals(id.RootElement,original.GetProperty("tokenId")));
            var outcome=await provider.ReadOriginalNftOutcomeAsync(original,maximumBlocks:32);
            Assert.IsFalse(outcome.IsError,outcome.Message);Assert.IsTrue(outcome.Result.GetProperty("found").GetBoolean());
            var receipt=outcome.Result.GetProperty("receipt");
            Assert.IsTrue(receipt.GetProperty("dispatchSucceeded").GetBoolean());Assert.IsTrue(receipt.GetProperty("nftEffectMatchesRequest").GetBoolean());
            Assert.AreEqual(record.From,receipt.GetProperty("from").GetString());Assert.AreEqual(record.To,receipt.GetProperty("to").GetString());
            receipts.Add(receipt.Clone());
        }
        Assert.AreEqual(4,records.Count,"Partial original sequence remains preserved; never replay or automatically finish it.");
        if(!File.Exists(completed)) {
            using var file=new FileStream(completed,FileMode.CreateNew,FileAccess.Write,FileShare.None);
            file.Write(JsonSerializer.SerializeToUtf8Bytes(new { records,receipts,syntheticStoredNftLookup=true,
                syntheticWalletLookup=true,fileBackedAdmissionOnly=true,hostedAdmissionVerified=false,unlockVerified=false,publicWithdrawVerified=bridgeMode,localSourceProvenReleaseVerified=bridgeMode,crossChainSettlementVerified=false }));file.Flush(true);
        }
        if(File.Exists(pending)) {
            CollectionAssert.AreEqual(records.Select(row=>row.Attempt.ProviderRequestId).ToArray(),
                File.ReadAllLines(pending).Select(line=>JsonSerializer.Deserialize<AttemptRecord>(line)!.Attempt.ProviderRequestId).ToArray());
            File.Delete(pending);
        }
    }

    private sealed class LocalProvider(string code,string collection):PolkadotOASIS("http://127.0.0.1:19944",Genesis,
        "0x"+string.Concat(Enumerable.Repeat("03",32)),Pool,null,new PolkadotNftConfiguration(code,Holder))
    {
        protected override Task<OASISResult<IWeb3NFT>> ResolveStoredWeb3NftAsync(Guid id) {
            Assert.AreEqual(StoredId,id);
            var holon=new Holon(HolonType.Web3NFT) { Id=id };
            holon.MetaData[Web3NftMetadata.MetadataKey]=JsonSerializer.Serialize(new Web3NFT {
                Id=id,TokenId=NativeId,NFTTokenAddress=collection,OnChainProvider=new(Core.Enums.ProviderType.PolkadotOASIS),
                NFTStandardType=new(NFTStandardType.PSP34) });
            return Task.FromResult(new OASISResult<IWeb3NFT>{Result=Web3NftMetadata.Deserialize(holon)});
        }
        protected override Task<OASISResult<IProviderWallet>> ResolveAssetMintWalletAsync(Guid avatarId)=>Task.FromResult(
            new OASISResult<IProviderWallet>{Result=new ProviderWallet{AvatarId=avatarId,ProviderType=Core.Enums.ProviderType.PolkadotOASIS,
                WalletAddress=Holder,PublicKey=Holder,PrivateKey="0x"+string.Concat(Enumerable.Repeat("02",32))}});
    }
}
