using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.PolkadotOASIS.IntegrationTests;

[TestClass][DoNotParallelize]
public class PolkadotRecordBackendNativeTests
{
    [TestMethod][TestCategory("LocalNative")]
    public async Task PrivateConditionalRecordsUseRealSdkAndDurableCSharpOwner()
    {
        Assert.AreEqual("1",Environment.GetEnvironmentVariable("OASIS_POLKADOT_LOCAL_NATIVE"));
        var directory=Environment.GetEnvironmentVariable("OASIS_POLKADOT_RECORD_EVIDENCE")!;
        Assert.IsTrue(Path.IsPathFullyQualified(directory)&&Directory.Exists(directory));
        var bridge=Environment.GetEnvironmentVariable("OASIS_POLKADOT_SDK_BRIDGE")!;
        Assert.IsTrue(File.Exists(bridge));
        var expectedDirectory=Path.GetFullPath(Path.Combine(Path.GetDirectoryName(bridge)!,"../../../../TestResults/PolkadotCurrent"));
        Assert.AreEqual(expectedDirectory,Path.GetFullPath(directory),"Use only the existing fixed native evidence directory.");
        var pending=Path.Combine(directory,"record-csharp-pending.json");
        Assert.IsFalse(File.Exists(pending),"Preserve/reconcile pending original; never replay.");
        using var owner=JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(directory,"local-record-sdk.create.completed.json")));
        var setup=owner.RootElement.GetProperty("preparation");
        var genesis=setup.GetProperty("chainId").GetString()!;
        Assert.AreEqual("0xe8cf5d7d636a08721efaa17acc52ed3d895fe2468dccb2520cc8e249ddb6b208",genesis);
        Assert.AreEqual("0x7c102aaaaef5fec4e9f8f035c0542c7c6ca23f8c4e6b8de5e0498862debef0fa",setup.GetProperty("contractCodeHash").GetString());
        using var provider=new PolkadotOASIS("http://127.0.0.1:19944",genesis,"0x"+string.Concat(Enumerable.Repeat("02",32)));
        Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
        var raw=provider.CreateRecordBackend(setup.GetProperty("contractAddress").GetString()!,
            setup.GetProperty("contractCodeHash").GetString()!,setup.GetProperty("sender").GetString()!);
        // Public synthetic encryption fixture, stable and separate from signer.
        // Never use this known key or the public test signer for real private data.
        using var encrypted=PersistedPrivateKeyValueBackend.Create(raw,()=>Enumerable.Repeat((byte)73,32).ToArray());
        var conditional=(IConditionalKeyValueBackend)encrypted;
        const string logical="native-record:synthetic@example.invalid";
        const string first="synthetic private record 🌳";
        const string second="updated synthetic private record 🌳";
        var acknowledgements=0;
        string FileFor(string label)=>Path.Combine(directory,$"record-csharp.{label}.completed.json");
        static void Durable(string file,object value) {
            var bytes=JsonSerializer.SerializeToUtf8Bytes(value);
            using var stream=new FileStream(file,FileMode.CreateNew,FileAccess.Write,FileShare.None);
            stream.Write(bytes);stream.Flush(flushToDisk:true);
        }
        async Task Confirm(string label,JsonElement prepared) {
            var found=await raw.ReadOriginalOutcomeAsync(prepared);
            Assert.IsTrue(found.GetProperty("found").GetBoolean());
            var receipt=found.GetProperty("receipt");
            Assert.IsTrue(receipt.GetProperty("dispatchSucceeded").GetBoolean());
            Assert.IsTrue(receipt.GetProperty("recordEffectMatchesRequest").GetBoolean());
            Durable(FileFor(label),new{label,preparation=prepared,receipt});
            using var original=JsonDocument.Parse(await File.ReadAllTextAsync(pending));
            Assert.AreEqual(prepared.GetProperty("transactionHash").GetString(),original.RootElement.GetProperty("preparation").GetProperty("transactionHash").GetString());
            File.Delete(pending); // Same finalized original remains fsync'd in completion evidence.
        }
        async Task Execute(string label,Func<Task> mutation) {
            if(File.Exists(FileFor(label))) {
                using var retained=JsonDocument.Parse(await File.ReadAllTextAsync(FileFor(label)));
                var result=await raw.ReadOriginalOutcomeAsync(retained.RootElement.GetProperty("preparation"));
                Assert.IsTrue(result.GetProperty("found").GetBoolean());
                Assert.IsTrue(result.GetProperty("receipt").GetProperty("recordEffectMatchesRequest").GetBoolean());
                return;
            }
            JsonElement prepared=default;
            await raw.WithPreparationAsync(async()=>{await mutation();return true;},(action,key)=>
                new PreparedSubmissionServer((original,token)=>{
                    token.ThrowIfCancellationRequested();
                    Assert.AreEqual(action,original.GetProperty("action").GetString());
                    Assert.AreEqual(Convert.ToBase64String(Encoding.UTF8.GetBytes(key)),original.GetProperty("keyBase64").GetString());
                    Assert.AreEqual(setup.GetProperty("contractAddress").GetString(),original.GetProperty("contractAddress").GetString());
                    Assert.AreEqual(setup.GetProperty("sender").GetString(),original.GetProperty("sender").GetString());
                    Assert.IsFalse(original.TryGetProperty("privateKey",out _));
                    Durable(pending,new{label,preparation=original});
                    prepared=original.Clone();acknowledgements++;
                    return Task.CompletedTask;
                }));
            await Confirm(label,prepared);
        }

        if(File.Exists(FileFor("delete"))) {
            foreach(var label in new[]{"create","replace","delete"})await Execute(label,()=>throw new AssertFailedException("Completed originals must never replay."));
        } else {
            if(!File.Exists(FileFor("create"))) Assert.IsNull(await encrypted.GetAsync(logical));
            await Execute("create",()=>conditional.CreateOnlyAsync(logical,first));
            if(!File.Exists(FileFor("replace"))) {
                Assert.AreEqual(first,await encrypted.GetAsync(logical));
                var physical=await raw.ListKeysAsync("oasis/private/v1/");
                Assert.AreEqual(1,physical.Count);
                Assert.IsFalse(physical[0].Contains(logical,StringComparison.Ordinal));
                var ciphertext=await raw.GetAsync(physical[0]);
                Assert.IsFalse(ciphertext.Contains(first,StringComparison.Ordinal));
                Assert.IsFalse(ciphertext.Contains(logical,StringComparison.Ordinal));
            }
            var observed=(await conditional.ReadObservedAsync(logical))!;
            await Execute("replace",()=>conditional.ReplaceUnchangedAsync(logical,second,observed.Revision));
            Assert.AreEqual(second,await encrypted.GetAsync(logical));
            var beforeConflict=acknowledgements;
            await Assert.ThrowsExceptionAsync<StorageLifecycleConflictException>(()=>raw.WithPreparationAsync(async()=>{
                await conditional.CreateOnlyAsync(logical,"conflicting synthetic value");return true;
            },(_,_)=>new PreparedSubmissionServer((_,_)=>throw new AssertFailedException("Conflict must fail before durable ACK."))));
            Assert.AreEqual(beforeConflict,acknowledgements);
            using(var reopened=PersistedPrivateKeyValueBackend.Create(raw,()=>Enumerable.Repeat((byte)73,32).ToArray()))
                Assert.AreEqual(second,await reopened.GetAsync(logical));
            var current=(await conditional.ReadObservedAsync(logical))!;
            await Execute("delete",()=>conditional.DeleteUnchangedAsync(logical,current.Revision));
        }
        Assert.IsNull(await encrypted.GetAsync(logical));
        Assert.AreEqual(0,(await encrypted.ListKeysAsync("native-record:")).Count);
        Console.WriteLine($"Private native C# records verified; fresh durable acknowledgements={acknowledgements}; original receipts retained; synthetic key deleted.");
    }
}
