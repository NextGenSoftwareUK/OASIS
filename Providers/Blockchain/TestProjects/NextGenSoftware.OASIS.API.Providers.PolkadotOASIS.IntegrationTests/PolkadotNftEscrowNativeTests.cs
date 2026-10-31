using System.Reflection;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.Common;
using AttemptRecord=NextGenSoftware.OASIS.API.Providers.PolkadotOASIS.IntegrationTests.PolkadotNftPublicMutationNativeTests.AttemptRecord;
using FlushedOwner=NextGenSoftware.OASIS.API.Providers.PolkadotOASIS.IntegrationTests.PolkadotNftPublicMutationNativeTests.FlushedNftOwner;

namespace NextGenSoftware.OASIS.API.Providers.PolkadotOASIS.IntegrationTests;

[TestClass][DoNotParallelize]
public class PolkadotNftEscrowNativeTests
{
    private const string Genesis="0xe8cf5d7d636a08721efaa17acc52ed3d895fe2468dccb2520cc8e249ddb6b208";
    private const string Alice="0x1a4fee48c1ba1a48e8cd43782a8485d635aa91cfb82cbb477f0c1c576bc4031c";
    private const string Bob="0x8ee504148e75c34e8f051899b3c6e4241ff18dc1c9211260b6a6a434bedb485f";
    private const string NativeId="{\"U128\":\"18446744073709551621\"}";
    private static readonly Guid AvatarA=Guid.Parse("da06227d-87d7-4c71-a6ef-c88ded5e7201");
    private static readonly Guid AvatarB=Guid.Parse("da06227d-87d7-4c71-a6ef-c88ded5e7202");
    private static readonly Guid StoredId=Guid.Parse("b439cdfb-c50f-4f2e-b6bb-5158c3385560");

    [TestMethod][TestCategory("LocalNative")]
    public async Task PublicStoredEscrowChangesBeneficiaryWithOwnershipAndRecoversCommonSignedFields()
    {
        Assert.AreEqual("1",Environment.GetEnvironmentVariable("OASIS_POLKADOT_LOCAL_NATIVE"));
        var bridge=Environment.GetEnvironmentVariable("OASIS_POLKADOT_SDK_BRIDGE")!;
        var folder=Path.GetFullPath(Path.Combine(Path.GetDirectoryName(bridge)!,"../../../../Docs/WIP/HyperDrive-Providers/NativeEvidence/Polkadot"));
        var pending=Path.Combine(folder,"psp34-escrow-public-pending.jsonl");
        var completed=Path.Combine(folder,"psp34-escrow-public.completed.json");
        using var deployment=JsonDocument.Parse(File.ReadAllText(Path.Combine(folder,"psp34-escrow-deployment.completed.json")));
        var collection=deployment.RootElement.GetProperty("address").GetString()!;
        var code=deployment.RootElement.GetProperty("codeHash").GetString()!;
        var records=new List<AttemptRecord>();
        var recovering=File.Exists(completed)||File.Exists(pending);
        if(Environment.GetEnvironmentVariable("OASIS_POLKADOT_NFT_ESCROW_RECOVERY")=="1")
            Assert.IsTrue(recovering,"Explicit recovery cannot start writes.");
        if(File.Exists(completed)) {
            using var file=JsonDocument.Parse(File.ReadAllText(completed));
            records.AddRange(file.RootElement.GetProperty("records").Deserialize<List<AttemptRecord>>()!);
        } else if(File.Exists(pending))records.AddRange(File.ReadAllLines(pending).Select(line=>JsonSerializer.Deserialize<AttemptRecord>(line)!));
        using var provider=new LocalProvider(code,collection);
        var activated=await provider.ActivateProviderAsync();Assert.IsFalse(activated.IsError,activated.Message);
        if(!recovering)for(var step=0;step<7;step++) {
            var action=step==0?"mint":step==6?"burn":step==3?"transfer":step is 1 or 4?"lock":"unlock";
            var avatar=step>=4?AvatarB:AvatarA;
            var signer=step>=4?Bob:Alice;
            var from=step==0?null:action=="unlock"?collection:signer;
            var to=step==6?null:action=="lock"?collection:step==3?Bob:signer;
            var record=new AttemptRecord{Action=action,TokenId=NativeId,From=from,To=to};
            var store=DispatchProxy.Create<IUsageOutboxStore,FlushedOwner>();
            var owner=(FlushedOwner)(object)store;owner.Journal=pending;owner.Record=record;
            var execution=CreateExecution(store);
            Task<OASISResult<IWeb3NFTTransactionResponse>> Invoke()=>action switch {
                "mint"=>provider.MintNFTAsync(new MintWeb3NFTRequest {
                    MintedByAvatarId=avatar,CollectionPublicKey=collection,NumberToMint=1,Amount=1,NFTStandardType=NFTStandardType.PSP34,
                    Title="Retained public escrow fixture",MetaData=new(){{"polkadot:token-id",NativeId}} }),
                "lock"=>provider.LockNFTAsync(new LockWeb3NFTRequest { LockedByAvatarId=avatar,Web3NFTId=StoredId,NFTTokenAddress=collection }),
                "unlock"=>provider.UnlockNFTAsync(new UnlockWeb3NFTRequest { UnlockedByAvatarId=avatar,Web3NFTId=StoredId,NFTTokenAddress=collection }),
                "transfer"=>provider.SendNFTAsync(new SendWeb3NFTRequest { FromNFTTokenAddress=collection,FromWalletAddress=Alice,
                    ToWalletAddress=Bob,TokenId=NativeId,Amount=1,OwnerPublicKey=Alice,OwnerPrivateKey=Key("02") }),
                _=>provider.BurnNFTAsync(new BurnWeb3NFTRequest { BurntByAvatarId=avatar,NFTTokenAddress=collection,TokenId=NativeId,
                    OwnerPublicKey=Bob,OwnerPrivateKey=Key("03"),OwnerSeedPhrase=null! })
            };
            using var timeout=new CancellationTokenSource(TimeSpan.FromMinutes(3));
            var result=await provider.ExecutePreparedNativeOperationAsync(Invoke,execution,$"public-escrow-v2-{step}",signer,timeout.Token);
            Assert.IsFalse(result.IsError,result.Exception?.ToString()??result.Message);
            Assert.IsTrue(result.IsSaved);Assert.AreEqual(1,owner.Calls);
            Assert.AreEqual(NativeId,result.Result.Web3NFT.TokenId);
            records.Add(record);
            if(action=="lock") {
                var foreign=avatar==AvatarA?AvatarB:AvatarA;
                var deniedStore=DispatchProxy.Create<IUsageOutboxStore,FlushedOwner>();
                var deniedOwner=(FlushedOwner)(object)deniedStore;
                deniedOwner.Journal=pending;deniedOwner.Record=new AttemptRecord();
                var rejected=await provider.ExecutePreparedNativeOperationAsync(()=>provider.UnlockNFTAsync(new UnlockWeb3NFTRequest {
                    UnlockedByAvatarId=foreign,Web3NFTId=StoredId,NFTTokenAddress=collection }),
                    CreateExecution(deniedStore),$"public-escrow-denied-{step}",foreign==AvatarA?Alice:Bob,timeout.Token);
                Assert.IsTrue(rejected.IsError,"Foreign/stale beneficiary must be rejected.");
                StringAssert.Contains(rejected.Exception?.ToString()??rejected.Message,"Native escrow beneficiary does not authorize");
                Assert.AreEqual(0,deniedOwner.Calls,"Rejected unlock must never reach the durable submission owner.");
            }
        }
        Assert.IsTrue(records.Count>0&&records.Count<=7);
        var receipts=new List<JsonElement>();
        foreach(var record in records) {
            var recovered=await provider.RecoverNftPreparationAsync(record.Attempt);Assert.IsFalse(recovered.IsError,recovered.Message);
            var original=recovered.Result;
            Assert.AreEqual(record.Action,original.GetProperty("action").GetString());
            using var id=JsonDocument.Parse(NativeId);Assert.IsTrue(JsonElement.DeepEquals(id.RootElement,original.GetProperty("tokenId")));
            if(record.From!=null)Assert.AreEqual(record.From,original.GetProperty("expectedFrom").GetString());
            var outcome=await provider.ReadOriginalNftOutcomeAsync(original,maximumBlocks:32);
            Assert.IsFalse(outcome.IsError,outcome.Message);Assert.IsTrue(outcome.Result.GetProperty("found").GetBoolean());
            var receipt=outcome.Result.GetProperty("receipt");
            Assert.IsTrue(receipt.GetProperty("dispatchSucceeded").GetBoolean());Assert.IsTrue(receipt.GetProperty("nftEffectMatchesRequest").GetBoolean());
            Assert.AreEqual(record.From,receipt.GetProperty("from").GetString());Assert.AreEqual(record.To,receipt.GetProperty("to").GetString());
            receipts.Add(receipt.Clone());
        }
        Assert.AreEqual(7,records.Count,"Partial original sequence remains preserved; never replay or automatically finish it.");
        if(!File.Exists(completed)) {
            using var file=new FileStream(completed,FileMode.CreateNew,FileAccess.Write,FileShare.None);
            file.Write(JsonSerializer.SerializeToUtf8Bytes(new { records,receipts,publicCSharpEscrowMethodsVerified=true,
                syntheticStoredNftLookup=true,syntheticWalletLookup=true,fileBackedAdmissionOnly=true,hostedAdmissionVerified=false }));file.Flush(true);
        }
        if(File.Exists(pending)) {
            CollectionAssert.AreEqual(records.Select(row=>row.Attempt.ProviderRequestId).ToArray(),
                File.ReadAllLines(pending).Select(line=>JsonSerializer.Deserialize<AttemptRecord>(line)!.Attempt.ProviderRequestId).ToArray());
            File.Delete(pending);
        }
    }

    private static string Key(string octet)=>"0x"+string.Concat(Enumerable.Repeat(octet,32));
    private static UsageExecutionHandle CreateExecution(IUsageOutboxStore store)=>(UsageExecutionHandle)
        typeof(UsageExecutionHandle).GetConstructors(BindingFlags.Instance|BindingFlags.NonPublic)
        .Single(value=>value.GetParameters().Length==5).Invoke(new object?[]{
            new UsageOutboxOperation { Id=Guid.NewGuid().ToString("D"),ExecutionOwner="public-native-escrow-owner",Policy=new UsageEndpointPolicy() },
            store,new Web4UsageLedgerOptions(),null,null});

    private sealed class LocalProvider(string code,string collection):PolkadotOASIS("http://127.0.0.1:19944",Genesis,
        "",null,null,new PolkadotNftConfiguration(code,Alice))
    {
        protected override Task<OASISResult<IWeb3NFT>> ResolveStoredWeb3NftAsync(Guid id) {
            Assert.AreEqual(StoredId,id);
            return Task.FromResult(new OASISResult<IWeb3NFT>{Result=new Web3NFT {
                Id=id,TokenId=NativeId,NFTTokenAddress=collection,OnChainProvider=new(Core.Enums.ProviderType.PolkadotOASIS),
                NFTStandardType=new(NFTStandardType.PSP34),MetaData=new(){{"polkadot:genesis-hash",Genesis}} }});
        }
        protected override Task<OASISResult<IProviderWallet>> ResolveAssetMintWalletAsync(Guid avatarId) {
            Assert.IsTrue(avatarId==AvatarA||avatarId==AvatarB);
            var address=avatarId==AvatarA?Alice:Bob;
            return Task.FromResult(new OASISResult<IProviderWallet>{Result=new ProviderWallet{AvatarId=avatarId,
                ProviderType=Core.Enums.ProviderType.PolkadotOASIS,WalletAddress=address,PublicKey=address,
                PrivateKey=Key(avatarId==AvatarA?"02":"03")}});
        }
    }
}
