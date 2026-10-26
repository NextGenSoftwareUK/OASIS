using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.Common;
using AttemptRecord=NextGenSoftware.OASIS.API.Providers.PolkadotOASIS.IntegrationTests.PolkadotNftPublicMutationNativeTests.AttemptRecord;
using FlushedOwner=NextGenSoftware.OASIS.API.Providers.PolkadotOASIS.IntegrationTests.PolkadotNftPublicMutationNativeTests.FlushedNftOwner;

namespace NextGenSoftware.OASIS.API.Providers.PolkadotOASIS.IntegrationTests;

[TestClass][DoNotParallelize]
public class PolkadotTokenEscrowNativeTests
{
    private const string Genesis="0xe8cf5d7d636a08721efaa17acc52ed3d895fe2468dccb2520cc8e249ddb6b208";
    private const string Sender="0x1a4fee48c1ba1a48e8cd43782a8485d635aa91cfb82cbb477f0c1c576bc4031c";
    private const string Units="18446744073709551620",Supply="1208925819614629174706176";
    private static readonly Guid Avatar=Guid.Parse("da06227d-87d7-4c71-a6ef-c88ded5e7201");

    [TestMethod][TestCategory("LocalNative")]
    public async Task PublicTokenCustodyUsesExactDecimalAndRecoversOriginalSignedFields()
    {
        Assert.AreEqual("1",Environment.GetEnvironmentVariable("OASIS_POLKADOT_LOCAL_NATIVE"));
        var cohort=Environment.GetEnvironmentVariable("OASIS_POLKADOT_TOKEN_PUBLIC_COHORT")??"1";
        Assert.IsTrue(cohort is "1" or "2");
        var lockId=Guid.Parse(cohort=="1"?"e7b34731-98cc-497d-9f11-5d5b8dd87fb1":"e7b34731-98cc-497d-9f11-5d5b8dd87fb2");
        var prefix=cohort=="1"?"psp22-public":"psp22-public-postfix";
        var sdk=Path.GetDirectoryName(Environment.GetEnvironmentVariable("OASIS_POLKADOT_SDK_BRIDGE"))!;
        var folder=Path.GetFullPath(Path.Combine(sdk,"../../../../Docs/WIP/HyperDrive-Providers/NativeEvidence/Polkadot"));
        var pending=Path.Combine(folder,prefix+"-pending.jsonl");
        var completed=Path.Combine(folder,prefix+".completed.json");
        using var tokenFile=JsonDocument.Parse(File.ReadAllText(Path.Combine(folder,"psp22-test-token-deployment.completed.json")));
        using var escrowFile=JsonDocument.Parse(File.ReadAllText(Path.Combine(folder,"psp22-escrow-deployment.completed.json")));
        var tokenAddress=tokenFile.RootElement.GetProperty("address").GetString()!;
        var escrow=escrowFile.RootElement.GetProperty("address").GetString()!;
        var code=escrowFile.RootElement.GetProperty("codeHash").GetString()!;
        var recovering=File.Exists(completed)||File.Exists(pending);
        if(Environment.GetEnvironmentVariable("OASIS_POLKADOT_TOKEN_PUBLIC_RECOVERY")=="1")Assert.IsTrue(recovering);
        var records=new List<AttemptRecord>();
        if(File.Exists(completed)) {
            using var file=JsonDocument.Parse(File.ReadAllText(completed));
            records.AddRange(file.RootElement.GetProperty("records").Deserialize<List<AttemptRecord>>()!);
        } else if(File.Exists(pending))records.AddRange(File.ReadAllLines(pending).Select(line=>JsonSerializer.Deserialize<AttemptRecord>(line)!));
        using var provider=new LocalProvider(escrow,code);
        var active=await provider.ActivateProviderAsync();Assert.IsFalse(active.IsError,active.Message);
        var observations=new List<JsonElement>();
        var releaseOnly=Environment.GetEnvironmentVariable("OASIS_POLKADOT_TOKEN_PUBLIC_RELEASE")=="1";
        if(releaseOnly) {
            Assert.IsTrue(File.Exists(pending)&&!File.Exists(completed));Assert.AreEqual(1,records.Count);
            Assert.AreEqual("lock",records[0].Action);
            var original=await provider.RecoverTokenEscrowPreparationAsync(records[0].Attempt);
            Assert.IsFalse(original.IsError,original.Message);
            var observed=await provider.ReadOriginalTokenEscrowOutcomeAsync(original.Result,maximumBlocks:32);
            Assert.IsFalse(observed.IsError,observed.Message);Assert.IsTrue(observed.Result.GetProperty("found").GetBoolean());
            var receipt=observed.Result.GetProperty("receipt");
            Assert.IsTrue(receipt.GetProperty("dispatchSucceeded").GetBoolean());
            Assert.IsTrue(receipt.GetProperty("tokenEscrowEffectMatchesRequest").GetBoolean());
            Assert.AreEqual(Units,receipt.GetProperty("amountUnits").GetString());
        }
        if(!recovering||releaseOnly) {
            Assert.IsTrue(File.Exists(Path.Combine(folder,prefix+"-approval.completed.json")),"Separate durable approval original required.");
            var before=await State(sdk);AssertState(before,releaseOnly?0:-1);observations.Add(before);
            for(var step=releaseOnly?1:0;step<2;step++) {
                var record=new AttemptRecord{Action=step==0?"lock":"unlock",From=Sender,To=escrow};
                var store=DispatchProxy.Create<IUsageOutboxStore,FlushedOwner>();
                var owner=(FlushedOwner)(object)store;owner.Journal=pending;owner.Record=record;
                var execution=(UsageExecutionHandle)typeof(UsageExecutionHandle).GetConstructors(BindingFlags.Instance|BindingFlags.NonPublic)
                    .Single(value=>value.GetParameters().Length==5).Invoke(new object?[]{new UsageOutboxOperation{
                        Id=Guid.NewGuid().ToString("D"),ExecutionOwner="public-native-token-owner",Policy=new UsageEndpointPolicy()},
                        store,new Web4UsageLedgerOptions(),null,null});
                using var timeout=new CancellationTokenSource(TimeSpan.FromMinutes(3));
                var result=await provider.ExecutePreparedNativeOperationAsync(()=>step==0
                    ?provider.LockTokenAsync(new LockWeb3TokenRequest{LockedByAvatarId=Avatar,LockId=lockId,TokenAddress=tokenAddress,
                        Amount=18_446_744.073709551620m,EscrowContractAddress=escrow})
                    :provider.UnlockTokenAsync(new UnlockWeb3TokenRequest{UnlockedByAvatarId=Avatar,LockId=lockId,TokenAddress=tokenAddress,
                        EscrowContractAddress=escrow}),execution,$"public-token-{cohort}-{step}",Sender,timeout.Token);
                Assert.IsFalse(result.IsError,result.Exception?.ToString()??result.Message);
                Assert.IsTrue(result.IsSaved);Assert.AreEqual(1,owner.Calls);records.Add(record);
                var state=await State(sdk);AssertState(state,step);observations.Add(state);
            }
        }
        Assert.IsTrue(records.Count>0&&records.Count<=2);
        var receipts=new List<JsonElement>();
        foreach(var record in records) {
            var recovered=await provider.RecoverTokenEscrowPreparationAsync(record.Attempt);
            Assert.IsFalse(recovered.IsError,recovered.Exception?.ToString()??recovered.Message);
            Assert.AreEqual(record.Action,recovered.Result.GetProperty("action").GetString());
            Assert.AreEqual(Units,recovered.Result.GetProperty("expectedAmountUnits").GetString());
            var outcome=await provider.ReadOriginalTokenEscrowOutcomeAsync(recovered.Result,maximumBlocks:32);
            Assert.IsFalse(outcome.IsError,outcome.Message);Assert.IsTrue(outcome.Result.GetProperty("found").GetBoolean());
            var receipt=outcome.Result.GetProperty("receipt");
            Assert.IsTrue(receipt.GetProperty("dispatchSucceeded").GetBoolean());
            Assert.IsTrue(receipt.GetProperty("tokenEscrowEffectMatchesRequest").GetBoolean());
            Assert.AreEqual(Units,receipt.GetProperty("amountUnits").GetString());receipts.Add(receipt.Clone());
        }
        Assert.AreEqual(2,records.Count,"Partial sequence preserved; never replay or automatically finish it.");
        var final=await State(sdk);AssertState(final,1);
        if(!File.Exists(completed)) {
            using var file=new FileStream(completed,FileMode.CreateNew,FileAccess.Write,FileShare.None);
            file.Write(JsonSerializer.SerializeToUtf8Bytes(new{records,receipts,observations,final,
                publicCSharpTokenMethodsVerified=true,syntheticWalletLookup=true,fileBackedAdmissionOnly=true,hostedAdmissionVerified=false}));file.Flush(true);
        }
        if(File.Exists(pending)) {
            CollectionAssert.AreEqual(records.Select(row=>row.Attempt.ProviderRequestId).ToArray(),
                File.ReadAllLines(pending).Select(line=>JsonSerializer.Deserialize<AttemptRecord>(line)!.Attempt.ProviderRequestId).ToArray());
            File.Delete(pending);
        }
    }

    private static void AssertState(JsonElement state,int step) {
        var token=state.GetProperty("token");var deposit=state.GetProperty("escrow").GetProperty("deposit");
        Assert.AreEqual(Supply,token.GetProperty("totalSupply").GetString());
        Assert.AreEqual(step==0?(System.Numerics.BigInteger.Parse(Supply)-System.Numerics.BigInteger.Parse(Units)).ToString():Supply,
            token.GetProperty("ownerBalance").GetString());
        Assert.AreEqual(step==0?Units:"0",token.GetProperty("spenderBalance").GetString());
        Assert.AreEqual(step<0?Units:"0",token.GetProperty("allowance").GetString());
        if(step<0)Assert.AreEqual(JsonValueKind.Null,deposit.ValueKind);
        else {Assert.AreEqual(Sender,deposit.GetProperty("owner").GetString());Assert.AreEqual(Units,deposit.GetProperty("amountUnits").GetString());
            Assert.AreEqual(step==1,deposit.GetProperty("released").GetBoolean());}
    }
    private static async Task<JsonElement> State(string sdk) {
        var info=new ProcessStartInfo("node"){RedirectStandardOutput=true,RedirectStandardError=true,UseShellExecute=false,CreateNoWindow=true};
        info.ArgumentList.Add(Path.Combine(sdk,"test-local-psp22-public-state.mjs"));
        using var process=Process.Start(info)!;var output=process.StandardOutput.ReadToEndAsync();var error=process.StandardError.ReadToEndAsync();
        using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(45));await process.WaitForExitAsync(timeout.Token);
        Assert.AreEqual(0,process.ExitCode,await error);using var document=JsonDocument.Parse(await output);return document.RootElement.Clone();
    }
    private sealed class LocalProvider(string escrow,string code):PolkadotOASIS("http://127.0.0.1:19944",Genesis,"",null,null,null,
        new PolkadotTokenEscrowConfiguration(escrow,code)) {
        protected override Task<OASISResult<IProviderWallet>> ResolveAssetMintWalletAsync(Guid avatarId) {
            Assert.AreEqual(Avatar,avatarId);return Task.FromResult(new OASISResult<IProviderWallet>{Result=new ProviderWallet{
                AvatarId=avatarId,ProviderType=Core.Enums.ProviderType.PolkadotOASIS,WalletAddress=Sender,PublicKey=Sender,
                PrivateKey="0x"+string.Concat(Enumerable.Repeat("02",32))}});
        }
    }
}
