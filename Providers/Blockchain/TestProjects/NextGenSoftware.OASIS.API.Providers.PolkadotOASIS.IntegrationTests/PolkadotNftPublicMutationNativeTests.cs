using System.Reflection;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.PolkadotOASIS.IntegrationTests;

[TestClass][DoNotParallelize]
public class PolkadotNftPublicMutationNativeTests
{
    private const string Genesis="0xe8cf5d7d636a08721efaa17acc52ed3d895fe2468dccb2520cc8e249ddb6b208";
    private const string Alice="0x1a4fee48c1ba1a48e8cd43782a8485d635aa91cfb82cbb477f0c1c576bc4031c";
    private const string Bob="0x8ee504148e75c34e8f051899b3c6e4241ff18dc1c9211260b6a6a434bedb485f";
    private const string ExplicitId="{\"U128\":\"18446744073709551618\"}";
    private static readonly Guid Avatar=Guid.Parse("da06227d-87d7-4c71-a6ef-c88ded5e7201");

    [TestMethod][TestCategory("LocalNative")]
    public async Task PublicNftLifecycleAndCommonSignedFieldRecoveryUseActualSdkAndRetainedLedger()
    {
        Assert.AreEqual("1",Environment.GetEnvironmentVariable("OASIS_POLKADOT_LOCAL_NATIVE"));
        var bridge=Environment.GetEnvironmentVariable("OASIS_POLKADOT_SDK_BRIDGE")!;
        var folder=Path.GetFullPath(Path.Combine(Path.GetDirectoryName(bridge)!,"../../../../Docs/WIP/HyperDrive-Providers/NativeEvidence/Polkadot"));
        var pending=Path.Combine(folder,"psp34-public-mutation-pending.jsonl");
        var completed=Path.Combine(folder,"psp34-public-mutation.completed.json");
        using var fixture=JsonDocument.Parse(File.ReadAllText(Path.Combine(folder,"psp34-deployment.completed.json")));
        var address=fixture.RootElement.GetProperty("address").GetString()!;
        var code=fixture.RootElement.GetProperty("codeHash").GetString()!;
        var records=new List<AttemptRecord>();
        var recovering=File.Exists(completed)||File.Exists(pending);
        if(Environment.GetEnvironmentVariable("OASIS_POLKADOT_NFT_PUBLIC_RECOVERY")=="1")
            Assert.IsTrue(recovering,"Read-only recovery must never enter the write branch.");
        if(File.Exists(completed)) {
            using var file=JsonDocument.Parse(File.ReadAllText(completed));
            records.AddRange(file.RootElement.GetProperty("records").Deserialize<List<AttemptRecord>>()!);
        } else if(File.Exists(pending)) {
            records.AddRange(File.ReadAllLines(pending).Select(line=>JsonSerializer.Deserialize<AttemptRecord>(line)!));
        }
        using var provider=new LocalProvider(code);
        var active=await provider.ActivateProviderAsync();Assert.IsFalse(active.IsError,active.Message);
        if(!recovering) {
            string? generatedId=null;
            for(var i=0;i<5;i++) {
                var action=i is 0 or 3?"mint":i==1?"transfer":"burn";
                var signer=action=="burn"?Bob:Alice;
                var expectedFrom=action=="mint"?null:i==1?Alice:Bob;
                var to=action=="burn"?null:i==0?Alice:Bob;
                var id=i==3?null:i==4?generatedId:ExplicitId;
                var record=new AttemptRecord{Action=action,TokenId=id,From=expectedFrom,To=to};
                var store=DispatchProxy.Create<IUsageOutboxStore,FlushedNftOwner>();
                var owner=(FlushedNftOwner)(object)store;owner.Journal=pending;owner.Record=record;
                var constructor=typeof(UsageExecutionHandle).GetConstructors(BindingFlags.Instance|BindingFlags.NonPublic)
                    .Single(value=>value.GetParameters().Length==5);
                // Synthetic admission + real fsync owner, NOT Mongo/JWT/quota/hosted billing proof.
                var execution=(UsageExecutionHandle)constructor.Invoke(new object?[]{
                    new UsageOutboxOperation{Id=Guid.NewGuid().ToString("D"),ExecutionOwner="public-native-nft-owner",Policy=new UsageEndpointPolicy()},
                    store,new Web4UsageLedgerOptions(),null,null});
                Task<OASISResult<IWeb3NFTTransactionResponse>> Invoke() {
                    if(action=="mint")return provider.MintNFTAsync(new MintWeb3NFTRequest{
                        MintedByAvatarId=Avatar,CollectionPublicKey=address,NumberToMint=1,Amount=1,NFTStandardType=NFTStandardType.PSP34,
                        Title="Retained public native NFT",Symbol="OASIS",Description="Synthetic local lifecycle",
                        ImageUrl="oasis://test/public-nft",SendToAddressAfterMinting=i==3?Bob:null,
                        MetaData=id==null?null:new(){{"polkadot:token-id",id}}});
                    if(action=="transfer")return provider.SendNFTAsync(new SendWeb3NFTRequest{
                        FromNFTTokenAddress=address,FromWalletAddress=Alice,ToWalletAddress=Bob,TokenId=id!,Amount=1,
                        OwnerPublicKey=Alice,OwnerPrivateKey="0x"+string.Concat(Enumerable.Repeat("02",32))});
                    return provider.BurnNFTAsync(new BurnWeb3NFTRequest{
                        BurntByAvatarId=Avatar,NFTTokenAddress=address,TokenId=id!,OwnerPublicKey=Bob,
                        OwnerPrivateKey="0x"+string.Concat(Enumerable.Repeat("03",32)),OwnerSeedPhrase=null!});
                }
                using var timeout=new CancellationTokenSource(TimeSpan.FromMinutes(3));
                var result=await provider.ExecutePreparedNativeOperationAsync(Invoke,execution,$"public-nft-v1-{i}",signer,timeout.Token);
                Assert.IsFalse(result.IsError,result.Exception?.ToString()??result.Message);
                Assert.IsTrue(result.IsSaved);Assert.AreEqual(1,owner.Calls);
                Assert.AreEqual(address,result.Result.Web3NFT.NFTTokenAddress);
                Assert.AreEqual(NFTStandardType.PSP34,result.Result.Web3NFT.NFTStandardType.Value);
                if(i==3) {
                    generatedId=result.Result.Web3NFT.TokenId;
                    using var native=JsonDocument.Parse(generatedId);
                    Assert.IsTrue(native.RootElement.TryGetProperty("U128",out _));
                    Assert.AreEqual(result.Result.TransactionResult,result.Result.Web3NFT.SendNFTTransactionHash);
                    Assert.AreEqual("mint-to-recipient",result.Result.Web3NFT.MetaData["polkadot:delivery-kind"]);
                } else Assert.AreEqual(id,result.Result.Web3NFT.TokenId);
                records.Add(record);
            }
        }
        Assert.IsTrue(records.Count>0&&records.Count<=5);
        var receipts=new List<JsonElement>();var ids=new List<JsonElement>();
        for(var i=0;i<records.Count;i++) {
            var record=records[i];
            var recovered=await provider.RecoverNftPreparationAsync(record.Attempt);
            Assert.IsFalse(recovered.IsError,recovered.Message);
            var original=recovered.Result;
            Assert.IsTrue(original.GetProperty("recoveredFromSignedOriginal").GetBoolean());
            Assert.AreEqual(record.From!=null,original.GetProperty("sourceOwnerPreconditionRetained").GetBoolean());
            if(record.From!=null)Assert.AreEqual(record.From,original.GetProperty("expectedFrom").GetString());
            Assert.AreEqual(record.Action,original.GetProperty("action").GetString());
            var token=original.GetProperty("tokenId");ids.Add(token.Clone());
            if(record.TokenId!=null) {
                using var declared=JsonDocument.Parse(record.TokenId);Assert.IsTrue(JsonElement.DeepEquals(token,declared.RootElement));
            }
            var outcome=await provider.ReadOriginalNftOutcomeAsync(original,maximumBlocks:32);
            Assert.IsFalse(outcome.IsError,outcome.Message);Assert.IsTrue(outcome.Result.GetProperty("found").GetBoolean());
            var receipt=outcome.Result.GetProperty("receipt");
            Assert.IsTrue(receipt.GetProperty("dispatchSucceeded").GetBoolean());
            Assert.IsTrue(receipt.GetProperty("nftEffectMatchesRequest").GetBoolean());
            Assert.AreEqual(record.From,receipt.GetProperty("from").GetString());Assert.AreEqual(record.To,receipt.GetProperty("to").GetString());
            receipts.Add(receipt.Clone());
        }
        Assert.AreEqual(5,records.Count,"Known partial sequence remains preserved; never replay or automatically complete it.");
        Assert.IsTrue(JsonElement.DeepEquals(ids[0],ids[1])&&JsonElement.DeepEquals(ids[1],ids[2]));
        Assert.IsTrue(JsonElement.DeepEquals(ids[3],ids[4]));
        if(!File.Exists(completed)) {
            using var file=new FileStream(completed,FileMode.CreateNew,FileAccess.Write,FileShare.None);
            file.Write(JsonSerializer.SerializeToUtf8Bytes(new{records,receipts,nativeTokenIds=ids,
                publicCSharpMethodsVerified=true,syntheticWalletLookup=true,fileBackedAdmissionOnly=true,hostedBillingVerified=false}));
            file.Flush(true);
        }
        if(File.Exists(pending)) {
            var persisted=File.ReadAllLines(pending).Select(line=>JsonSerializer.Deserialize<AttemptRecord>(line)!).ToArray();
            CollectionAssert.AreEqual(records.Select(row=>row.Attempt.ProviderRequestId).ToArray(),
                persisted.Select(row=>row.Attempt.ProviderRequestId).ToArray());
            File.Delete(pending);
        }
    }

    public sealed class AttemptRecord {
        public string Action{get;set;}=null!;public string? TokenId{get;set;}public string? From{get;set;}public string? To{get;set;}
        public UsageProviderAttempt Attempt{get;set;}=null!;
    }
    public class FlushedNftOwner:DispatchProxy {
        public string Journal=null!;public AttemptRecord Record=null!;public int Calls;
        protected override object? Invoke(MethodInfo? method,object?[]? args) {
            if(method?.Name!="BeginProviderAttemptAsync")throw new InvalidOperationException("Unexpected native NFT owner call.");
            Record.Attempt=(UsageProviderAttempt)args![2]!;
            using(var file=new FileStream(Journal,FileMode.Append,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)) {
                file.Write(JsonSerializer.SerializeToUtf8Bytes(Record));file.WriteByte(10);file.Flush(true);
            }
            Calls++;return Task.CompletedTask;
        }
    }
    private sealed class LocalProvider(string code):PolkadotOASIS("http://127.0.0.1:19944",Genesis,"",null,null,new PolkadotNftConfiguration(code,Alice)) {
        protected override Task<OASISResult<IProviderWallet>> ResolveAssetMintWalletAsync(Guid avatarId)=>Task.FromResult(
            new OASISResult<IProviderWallet>{Result=new ProviderWallet{AvatarId=avatarId,ProviderType=Core.Enums.ProviderType.PolkadotOASIS,
                WalletAddress=Alice,PublicKey=Alice,PrivateKey="0x"+string.Concat(Enumerable.Repeat("02",32))}});
    }
}
