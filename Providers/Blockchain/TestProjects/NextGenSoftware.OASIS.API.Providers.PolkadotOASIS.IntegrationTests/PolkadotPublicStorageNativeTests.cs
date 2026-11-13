using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.TestSupport;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.PolkadotOASIS.IntegrationTests;

[TestClass][DoNotParallelize]
public class PolkadotPublicStorageNativeTests
{
    [TestMethod][TestCategory("LocalNative")]
    public Task CanonicalPublicAvatarDetailAndHolonUseEncryptedNativeStorage() => RunNativeStorageAsync(false);

    [TestMethod][TestCategory("LocalNative")]
    public async Task NativeGraphAndParentQuery()
    {
        if (Environment.GetEnvironmentVariable("OASIS_POLKADOT_NATIVE_GRAPH") != "1")
            Assert.Inconclusive("Explicitly admit the isolated Polkadot graph fixture.");
        await RunNativeStorageAsync(true);
    }

    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    [TestCategory("LocalNative")]
    public async Task CoreStorageSuiteAgainstPolkadot(bool sync)
    {
        if (Environment.GetEnvironmentVariable("OASIS_POLKADOT_STORAGE_CONFORMANCE") != "1")
            Assert.Inconclusive("Enable only for the retained isolated Polkadot fixture.");

        await RunNativeStorageAsync(false, sync);
    }

    private async Task RunNativeStorageAsync(bool graph, bool? conformanceSync = null)
    {
        Assert.AreEqual("1",Environment.GetEnvironmentVariable("OASIS_POLKADOT_LOCAL_NATIVE"));
        var directory=Environment.GetEnvironmentVariable("OASIS_POLKADOT_RECORD_EVIDENCE")!;
        var bridge=Environment.GetEnvironmentVariable("OASIS_POLKADOT_SDK_BRIDGE")!;
        var fresh = Environment.GetEnvironmentVariable("OASIS_POLKADOT_FRESH_STORAGE") == "1";
        Assert.IsTrue(!fresh || conformanceSync.HasValue, "Fresh admission is restricted to storage conformance.");
        var expected = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(bridge)!,
            fresh ? "../../../../TestResults/PolkadotFreshStorage" : "../../../../Docs/WIP/HyperDrive-Providers/NativeEvidence/Polkadot"));
        Assert.AreEqual(expected,Path.GetFullPath(directory));
        var prefix = conformanceSync.HasValue
            ? (conformanceSync.Value ? "core-storage-sync" : "core-storage-async")
            : (graph ? "native-graph" : "public-storage");
        var pending=Path.Combine(directory,prefix+"-pending.jsonl");
        var completed=Path.Combine(directory,prefix+".completed.json");
        var recoveringCompleted=File.Exists(completed);
        if(Environment.GetEnvironmentVariable("OASIS_POLKADOT_RECORD_RECOVERY")=="1")
            Assert.IsTrue(recoveringCompleted,"Read-only recovery must never enter the write branch.");
        Assert.IsFalse(File.Exists(pending)&&!recoveringCompleted,
            "Preserve/reconcile every original in an incomplete sequence; never replay.");
        using var setupFile=JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(directory,
            fresh ? "fresh-record-store.json" : "reusable-record-store.json")));
        var setup=setupFile.RootElement;
        const string genesis="0xe8cf5d7d636a08721efaa17acc52ed3d895fe2468dccb2520cc8e249ddb6b208";
        Assert.AreEqual(genesis,setup.GetProperty("genesisHash").GetString());
        Assert.IsTrue(setup.GetProperty(fresh ? "deploymentVerified" : "recoveredFromFinalizedChain").GetBoolean());
        var configuration=new PolkadotStorageConfiguration(setup.GetProperty("contractAccountId").GetString()!,
            setup.GetProperty("contractCodeHash").GetString()!,setup.GetProperty("ownerAccountId").GetString()!,
            ()=>Enumerable.Repeat((byte)73,32).ToArray()); // Public synthetic fixture only; separate from signer.
        using var provider=new PolkadotOASIS("http://127.0.0.1:19944",genesis,"0x"+string.Concat(Enumerable.Repeat("02",32)),null,configuration);
        Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
        var raw=provider.CreateRecordBackend(configuration.ContractAccountId,configuration.CodeHash,configuration.OwnerAccountId);
        var originals=new List<JsonElement>();var receipts=new List<JsonElement>();
        if (conformanceSync.HasValue)
        {
            Assert.IsFalse(recoveringCompleted, "Preserve the prior completed run without replay.");
            Assert.IsFalse(Directory.EnumerateFiles(directory, "*-pending.jsonl").Any(),
                "Reconcile all pending fixture transactions first.");
            Assert.AreEqual(0, (await raw.ListKeysAsync("")).Count);
            // Fresh CI authoring and SDK subprocesses need time to finalize every mutation.
            using var deadline = new CancellationTokenSource(TimeSpan.FromMinutes(fresh ? 18 : 10));
            var reportPath = Path.Combine(directory, prefix + ".report.json");
            Assert.IsFalse(File.Exists(reportPath) || File.Exists(reportPath + ".next"));
            var otherPrefix = conformanceSync.Value ? "core-storage-async" : "core-storage-sync";
            var otherReport = Path.Combine(directory, otherPrefix + ".report.json");
            Assert.IsFalse(File.Exists(otherReport + ".next"), "Reconcile the interrupted fixture first.");
            if (File.Exists(otherReport))
            {
                var prior = JsonSerializer.Deserialize<CoreStorageConformance.Report>(await File.ReadAllTextAsync(otherReport));
                Assert.IsTrue(prior?.FixtureCleanupComplete == true, "Reconcile the prior mode's fixture first.");
            }
            Task Checkpoint(CoreStorageConformance.Report report)
            {
                using (var stream = new FileStream(reportPath + ".next", FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    JsonSerializer.Serialize(stream, report);
                    stream.Flush(flushToDisk: true);
                }
                File.Move(reportPath + ".next", reportPath, overwrite: true);
                return Task.CompletedTask;
            }

            var result = await provider.WithRecordStoragePreparationAsync(
                () => CoreStorageConformance.RunAsync(provider, nameof(PolkadotOASIS), "native",
                    conformanceSync.Value, report => report.Succeeded ? Task.CompletedTask : Checkpoint(report),
                    expectedDefaultDeletion: "absent"),
                (action, key) => new PreparedSubmissionServer((original, token) =>
                {
                    token.ThrowIfCancellationRequested();
                    Assert.AreEqual(action, original.GetProperty("action").GetString());
                    Assert.AreEqual(Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(key)),
                        original.GetProperty("keyBase64").GetString());
                    using var stream = new FileStream(pending, FileMode.Append, FileAccess.Write, FileShare.None);
                    JsonSerializer.Serialize(stream, original);
                    stream.WriteByte(10);
                    stream.Flush(flushToDisk: true);
                    originals.Add(original.Clone());
                    return Task.CompletedTask;
                }), deadline.Token);

            Assert.IsTrue(result.Succeeded,
                $"{result.CurrentCase}: {result.FailureType} {result.FailureContract}; " +
                $"{result.Passed.Count} checks passed, preparation deadline expired: {deadline.IsCancellationRequested}.");
            Assert.IsTrue(result.FixtureCleanupComplete);
            Assert.AreEqual(CoreStorageConformance.ChecksPerMode, result.Passed.Count);
            result.Succeeded = false;
            result.FixtureCleanupComplete = false;
            result.CurrentCase = "verify-native-receipts-and-empty-contract";
            await Checkpoint(result);
            Assert.AreEqual(0, (await raw.ListKeysAsync("")).Count);
            Assert.IsTrue(originals.Count > 0, "No native mutations were journaled.");
            foreach (var original in originals)
            {
                var outcome = await raw.ReadOriginalOutcomeAsync(original);
                Assert.IsTrue(outcome.GetProperty("found").GetBoolean());
                var receipt = outcome.GetProperty("receipt");
                Assert.IsTrue(receipt.GetProperty("finalized").GetBoolean());
                Assert.IsTrue(receipt.GetProperty("dispatchSucceeded").GetBoolean());
                Assert.IsTrue(receipt.GetProperty("recordEffectMatchesRequest").GetBoolean());
            }

            File.Move(pending, Path.Combine(directory, prefix + ".transactions.jsonl"));
            result.CurrentCase = "complete";
            result.FixtureCleanupComplete = true;
            result.Succeeded = true;
            await Checkpoint(result);
            return;
        }

        var avatarId=Guid.Parse("8d64c2fa-e612-4338-b95a-ae150111cca1");
        var holonId=Guid.Parse("8d64c2fa-e612-4338-b95a-ae150111cca2");
        var parentId = Guid.Parse("5ecade3b-d301-4915-9433-49882957e1ad");
        var childId = Guid.Parse("2239fa5c-8059-4b23-8662-e87d099561d8");
        const string username="polkadot-canonical-native-v1";
        const string email="polkadot-canonical-native-v1@example.invalid";
        static T Confirm<T>(OASISResult<T> result) {
            if(result.IsError)throw result.Exception??new InvalidOperationException(result.Message);
            return result.Result;
        }
        if(recoveringCompleted) {
            using var prior=JsonDocument.Parse(await File.ReadAllTextAsync(completed));
            foreach(var item in prior.RootElement.GetProperty("originals").EnumerateArray()) originals.Add(item.Clone());
        } else {
            Assert.AreEqual(0,(await raw.ListKeysAsync("")).Count,"Existing untracked cells must be preserved.");
            using var deadline=new CancellationTokenSource(TimeSpan.FromMinutes(10));
            await provider.WithRecordStoragePreparationAsync(async()=>{
                if (graph)
                {
                    var parent = new Holon { Id = parentId, Name = "native graph parent" };
                    var child = new Holon { Id = childId, Name = "native graph child" };
                    parent.Children.Add(child);
                    var graphSaved = await provider.SaveHolonAsync(parent, continueOnError: false);
                    Assert.IsFalse(graphSaved.IsError, graphSaved.Message);
                    Assert.IsTrue(graphSaved.IsSaved);
                    Assert.AreEqual(2, graphSaved.SavedCount);
                    var children = await provider.LoadHolonsForParentAsync(parent.Id, loadChildren: false);
                    Assert.IsFalse(children.IsError, children.Message);
                    Assert.AreEqual(1, children.LoadedCount);
                    CollectionAssert.AreEqual(new[] { child.Id }, children.Result.Select(row => row.Id).ToArray());
                    Confirm(await provider.DeleteHolonAsync(child.Id));
                    Confirm(await provider.DeleteHolonAsync(parent.Id));
                    return true;
                }
                var avatar=new Avatar{Id=avatarId,Username=username,Email=email,FirstName="Synthetic native avatar"};
                var saved=Confirm(await provider.SaveAvatarAsync(avatar));
                Assert.AreEqual(avatarId,saved.Id);
                Assert.AreEqual(avatarId,Confirm(await provider.LoadAvatarAsync(avatarId)).Id);
                Assert.AreEqual(avatarId,Confirm(await provider.LoadAvatarByUsernameAsync(username.ToUpperInvariant())).Id);
                Assert.AreEqual(avatarId,Confirm(await provider.LoadAvatarByEmailAsync(email.ToUpperInvariant())).Id);
                var detail=new AvatarDetail{Id=avatarId,Username=username,Email=email,Karma=23,XP=71};
                Confirm(await provider.SaveAvatarDetailAsync(detail));
                var byEmail=Confirm(await provider.LoadAvatarDetailByEmailAsync(email));
                Assert.AreEqual(23L,Convert.ToInt64(byEmail.Karma));Assert.AreEqual(71UL,Convert.ToUInt64(byEmail.XP));
                Assert.AreEqual(avatarId,Confirm(await provider.LoadAvatarDetailByUsernameAsync(username)).Id);
                var holon=new Holon{Id=holonId,Name="Synthetic canonical native holon",
                    MetaData=new Dictionary<string,object>{{"native:scope","polkadot-canonical-v1"}}};
                Confirm(await provider.SaveHolonAsync(holon,saveChildren:false));
                Assert.AreEqual(holonId,Confirm(await provider.LoadHolonAsync(holonId,loadChildren:false)).Id);
                var matching=Confirm(await provider.LoadHolonsByMetaDataAsync("native:scope","polkadot-canonical-v1",loadChildren:false));
                CollectionAssert.AreEqual(new[]{holonId},matching.Select(h=>h.Id).ToArray());
                var physical=await raw.ListKeysAsync("oasis/private/v1/");
                Assert.IsTrue(physical.Count>=5);
                foreach(var key in physical) {
                    Assert.IsFalse(key.Contains(username,StringComparison.Ordinal));
                    var ciphertext=await raw.GetAsync(key);
                    Assert.IsFalse(ciphertext.Contains(email,StringComparison.Ordinal));
                    Assert.IsFalse(ciphertext.Contains("Synthetic canonical native holon",StringComparison.Ordinal));
                }
                Confirm(await provider.DeleteHolonAsync(holonId));
                Assert.IsTrue(Confirm(await provider.DeleteAvatarAsync(avatarId,softDelete:false)));
                return true;
            },(action,key)=>new PreparedSubmissionServer((original,token)=>{
                token.ThrowIfCancellationRequested();
                Assert.AreEqual(action,original.GetProperty("action").GetString());
                Assert.AreEqual(Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(key)),original.GetProperty("keyBase64").GetString());
                var bytes=JsonSerializer.SerializeToUtf8Bytes(original);
                using var file=new FileStream(pending,FileMode.Append,FileAccess.Write,FileShare.None);
                file.Write(bytes);file.WriteByte(10);file.Flush(flushToDisk:true);
                originals.Add(original.Clone());return Task.CompletedTask;
            }),deadline.Token);
        }
        foreach(var original in originals) {
            var outcome=await raw.ReadOriginalOutcomeAsync(original);
            Assert.IsTrue(outcome.GetProperty("found").GetBoolean());
            var receipt=outcome.GetProperty("receipt");
            Assert.IsTrue(receipt.GetProperty("dispatchSucceeded").GetBoolean());
            Assert.IsTrue(receipt.GetProperty("recordEffectMatchesRequest").GetBoolean());receipts.Add(receipt.Clone());
        }
        Assert.IsTrue(originals.Count >= (graph ? 4 : 10), "The lifecycle must acknowledge its physical cells.");
        Assert.AreEqual(0,(await raw.ListKeysAsync("")).Count);
        if (!graph) Assert.AreEqual(OASISResultErrorCodes.RecordNotFound,(await provider.LoadAvatarAsync(avatarId)).ErrorCode);
        if(!recoveringCompleted) {
            var bytes = graph
                ? JsonSerializer.SerializeToUtf8Bytes(new { parentId, childId, originals, receipts })
                : JsonSerializer.SerializeToUtf8Bytes(new { avatarId, holonId, originals, receipts });
            using(var output=new FileStream(completed,FileMode.CreateNew,FileAccess.Write,FileShare.None)){output.Write(bytes);output.Flush(flushToDisk:true);}
        }
        if(File.Exists(pending)) {
            var lines=await File.ReadAllLinesAsync(pending);
            Assert.AreEqual(originals.Count,lines.Length);
            for(var i=0;i<lines.Length;i++) {
                using var stored=JsonDocument.Parse(lines[i]);
                Assert.IsTrue(JsonElement.DeepEquals(originals[i],stored.RootElement),
                    "Preserve the complete acknowledged original, not only its transaction hash.");
            }
            File.Delete(pending); // All exact originals remain in the flushed completed sequence.
        }
        Console.WriteLine($"Canonical public storage native gate: {originals.Count} exact originals verified; synthetic records/indexes removed; fresh durable acknowledgements {(recoveringCompleted?0:originals.Count)}.");
    }
}
