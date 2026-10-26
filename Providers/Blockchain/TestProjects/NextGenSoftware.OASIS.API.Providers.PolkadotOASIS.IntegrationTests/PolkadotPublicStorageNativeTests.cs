using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.PolkadotOASIS.IntegrationTests;

[TestClass][DoNotParallelize]
public class PolkadotPublicStorageNativeTests
{
    [TestMethod][TestCategory("LocalNative")]
    public async Task CanonicalPublicAvatarDetailAndHolonUseEncryptedNativeStorage()
    {
        Assert.AreEqual("1",Environment.GetEnvironmentVariable("OASIS_POLKADOT_LOCAL_NATIVE"));
        var directory=Environment.GetEnvironmentVariable("OASIS_POLKADOT_RECORD_EVIDENCE")!;
        var bridge=Environment.GetEnvironmentVariable("OASIS_POLKADOT_SDK_BRIDGE")!;
        var expected=Path.GetFullPath(Path.Combine(Path.GetDirectoryName(bridge)!,"../../../../Docs/WIP/HyperDrive-Providers/NativeEvidence/Polkadot"));
        Assert.AreEqual(expected,Path.GetFullPath(directory));
        var pending=Path.Combine(directory,"public-storage-pending.jsonl");
        var completed=Path.Combine(directory,"public-storage.completed.json");
        Assert.IsFalse(File.Exists(pending),"Preserve/reconcile every original in the pending sequence; never replay.");
        using var setupFile=JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(directory,"reusable-record-store.json")));
        var setup=setupFile.RootElement;
        const string genesis="0xe8cf5d7d636a08721efaa17acc52ed3d895fe2468dccb2520cc8e249ddb6b208";
        Assert.AreEqual(genesis,setup.GetProperty("genesisHash").GetString());
        Assert.IsTrue(setup.GetProperty("recoveredFromFinalizedChain").GetBoolean());
        var configuration=new PolkadotStorageConfiguration(setup.GetProperty("contractAccountId").GetString()!,
            setup.GetProperty("contractCodeHash").GetString()!,setup.GetProperty("ownerAccountId").GetString()!,
            ()=>Enumerable.Repeat((byte)73,32).ToArray()); // Public synthetic fixture only; separate from signer.
        using var provider=new PolkadotOASIS("http://127.0.0.1:19944",genesis,"0x"+string.Concat(Enumerable.Repeat("02",32)),null,configuration);
        Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
        var raw=provider.CreateRecordBackend(configuration.ContractAccountId,configuration.CodeHash,configuration.OwnerAccountId);
        var originals=new List<JsonElement>();var receipts=new List<JsonElement>();
        var avatarId=Guid.Parse("8d64c2fa-e612-4338-b95a-ae150111cca1");
        var holonId=Guid.Parse("8d64c2fa-e612-4338-b95a-ae150111cca2");
        const string username="polkadot-canonical-native-v1";
        const string email="polkadot-canonical-native-v1@example.invalid";
        static T Confirm<T>(OASISResult<T> result) {
            if(result.IsError)throw result.Exception??new InvalidOperationException(result.Message);
            return result.Result;
        }
        if(File.Exists(completed)) {
            using var prior=JsonDocument.Parse(await File.ReadAllTextAsync(completed));
            foreach(var item in prior.RootElement.GetProperty("originals").EnumerateArray()) originals.Add(item.Clone());
        } else {
            Assert.AreEqual(0,(await raw.ListKeysAsync("")).Count,"Existing untracked cells must be preserved.");
            using var deadline=new CancellationTokenSource(TimeSpan.FromMinutes(10));
            await provider.WithRecordStoragePreparationAsync(async()=>{
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
        Assert.IsTrue(originals.Count>=10,"Canonical avatar/detail/holon lifecycle must acknowledge all physical cells.");
        Assert.AreEqual(0,(await raw.ListKeysAsync("")).Count);
        Assert.AreEqual(OASISResultErrorCodes.RecordNotFound,(await provider.LoadAvatarAsync(avatarId)).ErrorCode);
        if(!File.Exists(completed)) {
            var bytes=JsonSerializer.SerializeToUtf8Bytes(new{avatarId,holonId,originals,receipts});
            using(var output=new FileStream(completed,FileMode.CreateNew,FileAccess.Write,FileShare.None)){output.Write(bytes);output.Flush(flushToDisk:true);}
            var lines=await File.ReadAllLinesAsync(pending);
            Assert.AreEqual(originals.Count,lines.Length);
            for(var i=0;i<lines.Length;i++) {
                using var stored=JsonDocument.Parse(lines[i]);
                Assert.AreEqual(originals[i].GetRawText(),stored.RootElement.GetRawText(),
                    "Preserve the complete acknowledged original, not only its transaction hash.");
            }
            File.Delete(pending); // All exact originals remain in the flushed completed sequence.
        }
        Console.WriteLine($"Canonical public storage native gate: {originals.Count} exact originals verified; synthetic records/indexes removed.");
    }
}
