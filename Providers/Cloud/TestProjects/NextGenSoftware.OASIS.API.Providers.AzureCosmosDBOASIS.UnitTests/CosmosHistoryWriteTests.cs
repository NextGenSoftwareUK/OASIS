using System.Net;
using System.Text;
using Microsoft.Azure.Cosmos;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.Infrastructure;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.UnitTests;

[TestClass]
public class CosmosHistoryWriteTests
{
    [TestMethod]
    public async Task PlannedSdkWriteUsesCapturedMembershipWithSameObservedEtagAndHistory()
    {
        var root=new Holon{Id=Guid.NewGuid(),Name="captured root"};
        var child=new Holon{Id=Guid.NewGuid(),ParentHolonId=root.Id};root.Children.Add(child);
        var plan=HolonGraphWritePlan.Create(new[]{root},true,true,0,0).Nodes[0];
        var expected=NextGenSoftware.OASIS.API.Core.Serialization.HolonChildRoleManifestJson.Serialize(plan.RelationshipManifest);
        root.Children.Clear();root.Name="caller changed after capture";
        var prior=await CosmosEmbeddedHistory.StageAsync(new Holon{Id=root.Id,Name="original"},null!,Decode);prior["_etag"]="captured-etag";
        var container=new Mock<Container>(MockBehavior.Strict);var writes=0;
        container.Setup(c=>c.ReadItemStreamAsync(root.Id.ToString(),It.IsAny<PartitionKey>(),It.IsAny<ItemRequestOptions>(),It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResponseMessage(HttpStatusCode.OK){Content=Bytes(prior.ToString())});
        container.Setup(c=>c.ReplaceItemStreamAsync(It.IsAny<Stream>(),root.Id.ToString(),It.IsAny<PartitionKey>(),It.IsAny<ItemRequestOptions>(),It.IsAny<CancellationToken>()))
            .Returns(async(Stream stream,string _,PartitionKey __,ItemRequestOptions options,CancellationToken ___)=>{
                writes++;Assert.AreEqual("captured-etag",options.IfMatchEtag);
                using var reader=new StreamReader(stream,Encoding.UTF8,false,1024,true);
                return new ResponseMessage(HttpStatusCode.OK){Content=Bytes(await reader.ReadToEndAsync())};
            });
        var json=await new CosmosDbClient(container.Object).UpsertPlannedDocumentAsync(plan.Snapshot,plan,new PartitionKey(root.Id.ToString()));
        var current=await CosmosEmbeddedHistory.ReadAsync(root.Id,JObject.Parse(json),0,Decode);
        var old=await CosmosEmbeddedHistory.ReadAsync(root.Id,JObject.Parse(json),1,Decode);
        Assert.AreEqual(1,writes);Assert.AreEqual(expected,current.ChildRoleManifestJson);
        Assert.AreEqual("captured root",current.Name);Assert.AreEqual("original",old.Name);Assert.IsNull(old.ChildRoleManifestJson);
        Assert.IsNull(plan.Snapshot.ChildRoleManifestJson,"SDK staging must not mutate the captured proposal.");
        Assert.AreEqual(2,current.Version);Assert.AreEqual(old.VersionId,current.PreviousVersionId);
    }

    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task RootOnlyUpsertPreservesObservedRolesIncludingLegacyNull(bool hasStoredRoles)
    {
        var id=Guid.NewGuid();
        string Manifest()=>NextGenSoftware.OASIS.API.Core.Serialization.HolonChildRoleManifestJson.Serialize(
            new HolonChildRoleManifest(1,id,
                new[]{new HolonChildRole("Children",new[]{Guid.NewGuid()})}));
        var observedRoles=hasStoredRoles ? Manifest():null;
        var source=new Holon{Id=id,Name="original",ChildRoleManifestJson=observedRoles};
        var prior=await CosmosEmbeddedHistory.StageAsync(source,null!,Decode);prior["_etag"]="observed-role-etag";
        var proposed=await CosmosEmbeddedHistory.ReadAsync(id,prior,0,Decode);
        proposed.Name="root-only replacement";proposed.ChildRoleManifestJson=Manifest();
        var callerRoles=proposed.ChildRoleManifestJson;
        var container=new Mock<Container>(MockBehavior.Strict);
        container.Setup(c=>c.ReadItemStreamAsync(id.ToString(),It.IsAny<PartitionKey>(),It.IsAny<ItemRequestOptions>(),It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResponseMessage(HttpStatusCode.OK){Content=Bytes(prior.ToString())});
        var writes=0;
        container.Setup(c=>c.ReplaceItemStreamAsync(It.IsAny<Stream>(),id.ToString(),It.IsAny<PartitionKey>(),It.IsAny<ItemRequestOptions>(),It.IsAny<CancellationToken>()))
            .Returns(async(Stream stream,string _,PartitionKey __,ItemRequestOptions options,CancellationToken ___)=>{
                writes++;Assert.AreEqual("observed-role-etag",options.IfMatchEtag);
                using var reader=new StreamReader(stream,Encoding.UTF8,false,1024,true);
                var stored=await reader.ReadToEndAsync();
                return new ResponseMessage(HttpStatusCode.OK){Content=Bytes(stored)};
            });
        var result=await new CosmosDbClient(container.Object).UpsertDocumentAsync(proposed,new PartitionKey(id.ToString()));
        var current=await CosmosEmbeddedHistory.ReadAsync(id,JObject.Parse(result),0,Decode);
        var historical=await CosmosEmbeddedHistory.ReadAsync(id,JObject.Parse(result),1,Decode);
        Assert.AreEqual(1,writes);
        Assert.AreEqual(callerRoles,proposed.ChildRoleManifestJson,"SDK staging must not mutate the caller proposal.");
        Assert.AreEqual(observedRoles,historical.ChildRoleManifestJson,"History must retain the prior membership.");
        Assert.AreEqual(observedRoles,current.ChildRoleManifestJson,"Root-only save must preserve the observed membership, including null.");
        Assert.AreEqual(hasStoredRoles,JObject.Parse(result).Property(nameof(Holon.ChildRoleManifestJson))!=null,
            "Null roles must retain canonical omission, not introduce a JSON-null field that breaks later conditional comparisons.");
    }

    [DataTestMethod]
    [DataRow("stored-type")] [DataRow("metadata")] [DataRow("domain")]
    public async Task ConditionalComparisonRetainsStoredTypeMetadataAndDomainConflicts(string changed)
    {
        var source=new Holon{Id=Guid.NewGuid(),Name="owned",MetaData=new Dictionary<string,object>{{"tag","owned"}}};
        var prior=await CosmosEmbeddedHistory.StageAsync(source,null!,Decode);prior["_etag"]="observed";
        var expected=await CosmosEmbeddedHistory.ReadAsync(source.Id,prior,0,Decode);
        var proposed=OasisJson.Project<Holon>(expected);proposed.Name="replacement";
        if(changed=="stored-type")prior["$type"]="foreign.Type, Foreign";
        else if(changed=="metadata")((JObject)prior["MetaData"]!)["tag"]="changed";
        else prior["Name"]="changed";
        var container=new Mock<Container>(MockBehavior.Strict);
        container.Setup(c=>c.ReadItemStreamAsync(source.Id.ToString(),It.IsAny<PartitionKey>(),It.IsAny<ItemRequestOptions>(),It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResponseMessage(HttpStatusCode.OK){Content=Bytes(prior.ToString())});
        var client=new CosmosDbClient(container.Object);
        await Assert.ThrowsExceptionAsync<NextGenSoftware.OASIS.API.Core.Helpers.StorageLifecycleConflictException>(
            ()=>client.ReplaceUnchangedDocumentAsync(source.Id.ToString(),expected,proposed,new PartitionKey(source.Id.ToString())));
        container.Verify(c=>c.ReplaceItemStreamAsync(It.IsAny<Stream>(),It.IsAny<string>(),It.IsAny<PartitionKey>(),It.IsAny<ItemRequestOptions>(),It.IsAny<CancellationToken>()),Times.Never());
    }

    [DataTestMethod][DataRow("upsert")][DataRow("replace")][DataRow("conditional")]
    public async Task OrdinaryWritersRetainStoredArchiveEvenWithHistoryOptionDisabled(string operation)
    {
        var source=new Holon{Id=Guid.NewGuid(),Name="original"};
        var prior=await CosmosEmbeddedHistory.StageAsync(source,null!,Decode);prior["_etag"]="observed";
        var expected=await CosmosEmbeddedHistory.ReadAsync(source.Id,prior,0,Decode);
        var expectedJson=JObject.Parse(OasisJson.Serialize(expected));var persistedJson=CosmosEmbeddedHistory.CanonicalPayload(prior);
        var differences=expectedJson.Properties().Where(p=>!persistedJson.TryGetValue(p.Name,out var value)||!StoredRecordPayloadEquality.Equivalent(p.Value,value)).Select(p=>p.Name);
        Assert.IsTrue(StoredRecordPayloadEquality.Equivalent(expectedJson,persistedJson),"Roundtrip changed fields: "+string.Join(",",differences));
        var proposed=OasisJson.Project<Holon>(expected);proposed.Name="replacement";
        var container=new Mock<Container>();var writes=0;
        container.Setup(c=>c.ReadItemStreamAsync(source.Id.ToString(),It.IsAny<PartitionKey>(),It.IsAny<ItemRequestOptions>(),It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResponseMessage(HttpStatusCode.OK){Content=Bytes(prior.ToString())});
        container.Setup(c=>c.ReplaceItemStreamAsync(It.IsAny<Stream>(),source.Id.ToString(),It.IsAny<PartitionKey>(),It.IsAny<ItemRequestOptions>(),It.IsAny<CancellationToken>()))
            .Returns(async(Stream stream,string _,PartitionKey __,ItemRequestOptions options,CancellationToken ___)=>{
                writes++;Assert.AreEqual("observed",options.IfMatchEtag);
                using var reader=new StreamReader(stream,Encoding.UTF8,false,1024,true);
                var stored=JObject.Parse(await reader.ReadToEndAsync());
                Assert.AreEqual(2,stored["Version"]!.Value<int>());
                Assert.AreEqual(1,((JArray)stored[CosmosEmbeddedHistory.PropertyName]!["records"]!).Count);
                return new ResponseMessage(HttpStatusCode.OK){Content=Bytes(stored.ToString())};
            });
        var client=new CosmosDbClient(container.Object);var pk=new PartitionKey(source.Id.ToString());
        var result=operation=="upsert" ? await client.UpsertDocumentAsync(proposed,pk)
            : operation=="replace" ? await client.ReplaceDocumentAsync(source.Id.ToString(),proposed,pk)
            : await client.ReplaceUnchangedDocumentAsync(source.Id.ToString(),expected,proposed,pk);
        Assert.AreEqual(1,writes);Assert.AreEqual(1,proposed.Version);
        Assert.AreEqual("original",(await CosmosEmbeddedHistory.ReadAsync(source.Id,JObject.Parse(result),1,Decode)).Name);
        container.Verify(c=>c.UpsertItemStreamAsync(It.IsAny<Stream>(),It.IsAny<PartitionKey>(),It.IsAny<ItemRequestOptions>(),It.IsAny<CancellationToken>()),Times.Never());
    }

    [DataTestMethod][DataRow(false,"accepted")][DataRow(true,"accepted")]
    [DataRow(true,"history-mismatch")][DataRow(true,"missing-body")][DataRow(true,"conflict")][DataRow(true,"no-etag")]
    [DataRow(true,"caller-mutation")]
    [DataRow(true,"role-mismatch")]
    public async Task VersionedSdkWriteIsConditionalAndChecksWholeArchiveAcknowledgement(bool exists,string mode)
    {
        var input=new Holon{Id=Guid.NewGuid(),Name="original"};
        var previous=await CosmosEmbeddedHistory.StageAsync(input,null!,Decode);
        if(mode!="no-etag") previous["_etag"]="observed-etag";
        input.Name="replacement";
        var container=new Mock<Container>(); int creates=0,replaces=0;
        var read=new ResponseMessage(exists?HttpStatusCode.OK:HttpStatusCode.NotFound);
        if(exists) read.Content=Bytes(previous.ToString());
        container.Setup(c=>c.ReadItemStreamAsync(input.Id.ToString(),It.IsAny<PartitionKey>(),It.IsAny<ItemRequestOptions>(),It.IsAny<CancellationToken>()))
            .Returns(()=>{if(mode=="caller-mutation")input.Name="caller changed during read";return Task.FromResult(read);});
        container.Setup(c=>c.ReadContainerAsync(It.IsAny<ContainerRequestOptions>(),It.IsAny<CancellationToken>())).ReturnsAsync(Mock.Of<ContainerResponse>());
        async Task<ResponseMessage> Reply(Stream stream)
        {
            if(mode=="conflict") return new ResponseMessage(HttpStatusCode.PreconditionFailed);
            using var reader=new StreamReader(stream,Encoding.UTF8,false,1024,true);
            var document=JObject.Parse(await reader.ReadToEndAsync());
            Assert.AreEqual("replacement",document["Name"]!.Value<string>());
            Assert.AreEqual(exists?2:1,document["Version"]!.Value<int>());
            Assert.AreEqual(exists?1:0,((JArray)document[CosmosEmbeddedHistory.PropertyName]!["records"]!).Count);
            if(mode=="history-mismatch") document[CosmosEmbeddedHistory.PropertyName]!["records"]=new JArray();
            if(mode=="role-mismatch")document[nameof(Holon.ChildRoleManifestJson)]="tampered acknowledged role";
            return new ResponseMessage(HttpStatusCode.OK){Content=mode=="missing-body"?null:Bytes(document.ToString())};
        }
        container.Setup(c=>c.CreateItemStreamAsync(It.IsAny<Stream>(),It.IsAny<PartitionKey>(),It.IsAny<ItemRequestOptions>(),It.IsAny<CancellationToken>()))
            .Returns((Stream s,PartitionKey _,ItemRequestOptions __,CancellationToken ___)=>{creates++;return Reply(s);});
        container.Setup(c=>c.ReplaceItemStreamAsync(It.IsAny<Stream>(),input.Id.ToString(),It.IsAny<PartitionKey>(),It.IsAny<ItemRequestOptions>(),It.IsAny<CancellationToken>()))
            .Returns((Stream s,string _,PartitionKey __,ItemRequestOptions options,CancellationToken ___)=>{
                replaces++;Assert.AreEqual("observed-etag",options.IfMatchEtag);return Reply(s);});
        Task<string> Write()=>new CosmosDbClient(container.Object).UpsertVersionedDocumentAsync(input,Decode,new PartitionKey(input.Id.ToString()));
        if(mode is "accepted" or "caller-mutation") Assert.IsNotNull(await Write());
        else if(mode=="no-etag") await Assert.ThrowsExceptionAsync<InvalidDataException>(Write);
        else if(mode=="conflict") await Assert.ThrowsExceptionAsync<CosmosException>(Write);
        else await Assert.ThrowsExceptionAsync<CosmosMutationReconciliationException>(Write);
        Assert.AreEqual(0,input.Version);Assert.AreEqual(Guid.Empty,input.VersionId);
        Assert.AreEqual(!exists?1:0,creates); Assert.AreEqual(exists&&mode!="no-etag"?1:0,replaces);
    }
    private static MemoryStream Bytes(string json)=>new(Encoding.UTF8.GetBytes(json));
    private static Holon Decode(string json)=>OasisJson.DeserializeStored<Holon>(json);
}
