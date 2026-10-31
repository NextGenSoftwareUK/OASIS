using System.Reflection;
using System.Text.Json;
using Google.Cloud.Firestore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Security;
using NextGenSoftware.OASIS.API.Core.Serialization;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.GoogleCloudOASIS.IntegrationTests;

[TestClass][DoNotParallelize][TestCategory("RuntimeEncryptedNative")]
public class GoogleEncryptedReplayNativeTests
{
    private sealed class Resolver:IHolonMetadataKeyResolver
    {
        public bool DenyOldPreparation;public bool UsedOldPreparation;
        public OASISResult<HolonMetadataKeyMaterial> ResolveKey(HolonMetadataKeyRequest request) {
            if(request.KeyId=="key-a"&&request.Purpose==HolonMetadataKeyPurpose.DecryptPreparation) {
                UsedOldPreparation=true;if(DenyOldPreparation)return new(){IsError=true,ErrorCode="OWNED_OLD_KEY_DENIED"};
            }
            if(request.KeyId is not ("key-a" or "key-b"))return new(){IsError=true};
            return new(new HolonMetadataKeyMaterial(Enumerable.Repeat(request.KeyId=="key-a"?(byte)17:(byte)34,32).ToArray()));
        }
    }
    private sealed class Owner():GoogleCloudOASIS("oasis-google-native",enableStorage:false,enableFirestore:true,enableBigQuery:false)
    {
        public int Commits;
        protected override async Task<IList<WriteResult>> CommitFirestoreConditionalBatchAsync(WriteBatch batch,CancellationToken token) {
            Commits++;return await base.CommitFirestoreConditionalBatchAsync(batch,token);
        }
    }
    [DataTestMethod][DataRow(false,false)][DataRow(false,true)][DataRow(true,false)][DataRow(true,true)]
    public async Task NativeV2EncryptedGraphAndBatchReplayAcrossFreshManagerAndKeyRotation(bool synchronous,bool batch)
    {
        Assert.AreEqual("127.0.0.1:18080",Environment.GetEnvironmentVariable("FIRESTORE_EMULATOR_HOST"));
        var work=Environment.GetEnvironmentVariable("OASIS_FIRESTORE_HISTORY_WORK")!;
        Assert.IsTrue(File.Exists(Path.Combine(work,"history-durability-ready.json")));
        Assert.AreEqual(0,Directory.GetFiles(work,"*.pending.json").Length);
        var journal=Path.Combine(work,$"history-native-encrypted-replay-{synchronous}-{batch}.pending.json");
        var operation=Guid.NewGuid();var actor=Guid.NewGuid();var ids=Enumerable.Range(0,batch?4:2).Select(_=>Guid.NewGuid()).ToArray();
        const decimal exact=1.000000000000000000000000001m;
        HolonMetadataEncryptionPolicy Policy(Resolver keys,string active="key-a",string context="firestore-root-context")
            =>new(context,active,keys,new[]{"search-tag"});
        Holon[] Inputs(Resolver keys)=>Enumerable.Range(0,batch?2:1).Select(index=>new Holon {
            Id=ids[index*2],Name="owned Firestore encrypted root "+index,IsActive=true,
            MetaData=new(){{"private-note","root private value"},{"search-tag","public-search-tag"},{"exact",exact}},
            Children=new List<IHolon>{new Holon {
                Id=ids[index*2+1],ParentHolonId=ids[index*2],Name="owned Firestore encrypted child",IsActive=true,
                MetadataEncryptionOverride=Policy(keys,"key-b","firestore-child-context"),
                MetaData=new(){{"private-note","child private value"},{"exact",exact}}
            }}
        }).ToArray();
        Owner Provider() {
            var provider=new Owner{IsProviderActivated=true,IsVersionControlEnabled=true};
            typeof(GoogleCloudOASIS).GetField("_firestoreDb",BindingFlags.Instance|BindingFlags.NonPublic)!
                .SetValue(provider,GoogleNativeFirestore.Database());return provider;
        }
        HolonManager Manager(Owner provider,Resolver keys,string active="key-a") {
            var dna=new OASISDNA{OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS {
                HyperDriveMode=HyperDriveModes.V2,StorageProviders=new StorageProviderSettings{LogSwitchingProviders=false}
            }};
            var providers=new ProviderManager(null,dna){IsAutoFailOverEnabled=false,IsAutoReplicationEnabled=false,IsAutoLoadBalanceEnabled=false};
            return new HolonManager(provider,dna,providers){MetadataEncryptionPolicy=Policy(keys,active)};
        }
        async Task<OASISResult<IEnumerable<IHolon>>> Save(HolonManager manager,Holon[] roots) {
            var options=new CosmicWriteOptions(operation,ids.ToDictionary(id=>id,_=>(Guid?)null));
            if(batch)return synchronous?manager.SaveHolons(roots,actor,options,providerType:ProviderType.GoogleCloudOASIS):
                await manager.SaveHolonsAsync(roots,actor,options,providerType:ProviderType.GoogleCloudOASIS);
            var result=synchronous?manager.SaveHolon(roots[0],actor,options,providerType:ProviderType.GoogleCloudOASIS):
                await manager.SaveHolonAsync(roots[0],actor,options,providerType:ProviderType.GoogleCloudOASIS);
            return new(result.Result==null?null:new[]{result.Result}){IsError=result.IsError,Message=result.Message,Exception=result.Exception,ErrorCode=result.ErrorCode};
        }
        var verifier=GoogleNativeFirestore.Database();var key="GoogleCloudOASIS:"+operation.ToString("N");
        var references=ids.Select(id=>verifier.Collection("holons").Document(id.ToString())).Concat(
            new[]{"cosmicWritePreparations","cosmicWriteReceipts","cosmicOperationFences"}.Select(collection=>verifier.Collection(collection).Document(key))).ToArray();
        var first=Provider();Guid[] versions;Guid?[] revisions;
        try {
            foreach(var reference in references)Assert.IsFalse((await reference.GetSnapshotAsync()).Exists);
            using(var file=new FileStream(journal,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)) {
                await file.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(new{ids,operation,actor,synchronous,batch,state="before-encrypted-firestore-v2-save"}));file.Flush(true);
            }
            var keys=new Resolver();var input=Inputs(keys);var saved=await Save(Manager(first,keys),input);
            Assert.IsFalse(saved.IsError,saved.Message+saved.Exception);Assert.AreEqual(2,first.Commits);
            versions=input.SelectMany(root=>new[]{(IHolon)root,root.Children.Single()}).Select(row=>row.VersionId).ToArray();
            revisions=input.SelectMany(root=>new[]{(IHolon)root,root.Children.Single()}).Select(row=>CosmicConcurrency.GetProviderRevisionId(row,ProviderType.GoogleCloudOASIS)).ToArray();
        } finally {await first.DeActivateProviderAsync();}
        var stored=new Dictionary<string,DocumentSnapshot>();
        foreach(var reference in references) {
            var snapshot=await reference.GetSnapshotAsync();Assert.IsTrue(snapshot.Exists);stored.Add(reference.Path,snapshot);
            if(reference.Parent.Id=="holons") {
                var json=snapshot.GetValue<string>("data_json");Assert.IsTrue(json.Contains("oasis2:"));
                Assert.IsFalse(json.Contains("root private value")||json.Contains("child private value"));
            }
        }
        var preparation=stored[references[ids.Length].Path].GetValue<string>("payloadJson");
        Assert.IsTrue(preparation.Contains("oasis-preparation1:"));
        Assert.IsFalse(preparation.Contains("root private value")||preparation.Contains("child private value"));
        var restarted=Provider();
        try {
            var keys=new Resolver();var manager=Manager(restarted,keys,"key-b");var fresh=Inputs(keys);
            var changed=Inputs(keys);changed[0].MetaData["private-note"]="changed private intent";
            Assert.IsTrue((await Save(manager,changed)).IsError);Assert.AreEqual(0,restarted.Commits);
            var deniedKeys=new Resolver{DenyOldPreparation=true};
            Assert.IsTrue((await Save(Manager(restarted,deniedKeys,"key-b"),Inputs(deniedKeys))).IsError);Assert.AreEqual(0,restarted.Commits);
            var replay=await Save(manager,fresh);Assert.IsFalse(replay.IsError,replay.Message+replay.Exception);
            Assert.AreEqual(0,restarted.Commits);Assert.IsTrue(keys.UsedOldPreparation);
            CollectionAssert.AreEqual(versions,fresh.SelectMany(root=>new[]{(IHolon)root,root.Children.Single()}).Select(row=>row.VersionId).ToArray());
            CollectionAssert.AreEqual(revisions,fresh.SelectMany(root=>new[]{(IHolon)root,root.Children.Single()}).Select(row=>CosmicConcurrency.GetProviderRevisionId(row,ProviderType.GoogleCloudOASIS)).ToArray());
            foreach(var root in fresh) {
                Assert.AreEqual("root private value",root.MetaData["private-note"].ToString());
                Assert.AreEqual("child private value",root.Children.Single().MetaData["private-note"].ToString());
                using var json=JsonDocument.Parse(StoredRecordJson.Serialize(root.MetaData));
                Assert.AreEqual(exact,json.RootElement.GetProperty("exact").GetDecimal());Assert.IsFalse(HolonChangeTracking.HasChanges(root));
            }
            foreach(var reference in references) {
                var original=stored[reference.Path];var current=await reference.GetSnapshotAsync();
                Assert.AreEqual(original.UpdateTime,current.UpdateTime,"Recovery must not rewrite any native document.");
                CollectionAssert.AreEquivalent(original.ToDictionary().Keys.ToArray(),current.ToDictionary().Keys.ToArray());
                foreach(var pair in original.ToDictionary())Assert.AreEqual(pair.Value,current.ToDictionary()[pair.Key]);
            }
            foreach(var original in stored.Values) {
                Assert.IsTrue(original.UpdateTime.HasValue);
                var ack=await original.Reference.DeleteAsync(Precondition.LastUpdated(original.UpdateTime.Value));
                Assert.AreNotEqual(default(Timestamp),ack.UpdateTime);Assert.IsFalse((await original.Reference.GetSnapshotAsync()).Exists);
            }
            File.Delete(journal);
        } finally {await restarted.DeActivateProviderAsync();}
    }
}
