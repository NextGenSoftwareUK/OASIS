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
    [DataTestMethod][DataRow("upsert")][DataRow("replace")][DataRow("conditional")]
    public async Task OrdinaryWritersRetainStoredArchiveEvenWithHistoryOptionDisabled(string operation)
    {
        var source=new Holon{Id=Guid.NewGuid(),Name="original"};
        var prior=await CosmosEmbeddedHistory.StageAsync(source,null!,Decode);prior["_etag"]="observed";
        var expected=await CosmosEmbeddedHistory.ReadAsync(source.Id,prior,0,Decode);
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
