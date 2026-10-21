using System.Net;
using System.Text;
using Microsoft.Azure.Cosmos;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Protected;
using NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.Infrastructure;

namespace NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.UnitTests;

[TestClass]
public class CosmosStreamOwnershipTests
{
    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task CollectionReadsAwaitEverySdkPageAndDisposeOnSuccessOrFailure(bool failLaterPage)
    {
        var first=new TrackedResponse(HttpStatusCode.OK);first.SetContent("{\"Documents\":[{\"id\":\"owned-first\"}]}");
        var second=new TrackedResponse(failLaterPage?HttpStatusCode.BadRequest:HttpStatusCode.OK);second.SetContent("{\"Documents\":[{\"id\":\"owned-second\"}]}");
        var firstGate=new TaskCompletionSource<ResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);var secondGate=new TaskCompletionSource<ResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);var secondRequested=new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var iterator=new Mock<FeedIterator>();iterator.SetupSequence(i=>i.HasMoreResults).Returns(true).Returns(true).Returns(false);var calls=0;
        iterator.Setup(i=>i.ReadNextAsync(It.IsAny<CancellationToken>())).Returns(()=>{if(++calls==1)return firstGate.Task;secondRequested.TrySetResult(true);return secondGate.Task;});
        var container=new Mock<Container>();container.Setup(c=>c.GetItemQueryStreamIterator(It.IsAny<QueryDefinition>(),It.IsAny<string>(),It.IsAny<QueryRequestOptions>())).Returns(iterator.Object);
        var pending=new CosmosDbClient(container.Object).ReadAllDocumentsAsync();Assert.IsFalse(pending.IsCompleted);firstGate.SetResult(first);
        await secondRequested.Task.WaitAsync(TimeSpan.FromSeconds(5));Assert.IsFalse(pending.IsCompleted);Assert.IsTrue(first.Disposed);secondGate.SetResult(second);
        if(failLaterPage){var error=await Assert.ThrowsExceptionAsync<CosmosException>(()=>pending);Assert.AreEqual(HttpStatusCode.BadRequest,error.StatusCode);}
        else {var rows=await pending;Assert.AreEqual(2,rows.Count);StringAssert.Contains(rows[0],"owned-first");StringAssert.Contains(rows[1],"owned-second");}
        Assert.AreEqual(2,calls);Assert.IsTrue(second.Disposed);iterator.Protected().Verify("Dispose",Times.Once(),new object[]{true});
    }

    [DataTestMethod]
    [DataRow("create",false)][DataRow("create",true)]
    [DataRow("upsert",false)][DataRow("upsert",true)]
    [DataRow("replace",false)][DataRow("replace",true)]
    public async Task AcknowledgedBodyFailureRetainsMutationMarkerAndDisposesResponse(string operation,bool missing)
    {
        var id=Guid.NewGuid();var cause=new IOException("owned body read failure");
        var response=new TrackedResponse(HttpStatusCode.OK);
        if(!missing)response.Content=new FaultingReadStream(cause);
        var container=new Mock<Container>();
        if(operation!="create")CosmosSdkTestRecords.Existing(container,id);
        container.Setup(c=>c.CreateItemStreamAsync(It.IsAny<Stream>(),It.IsAny<PartitionKey>(),It.IsAny<ItemRequestOptions>(),It.IsAny<CancellationToken>())).ReturnsAsync(response);
        container.Setup(c=>c.ReplaceItemStreamAsync(It.IsAny<Stream>(),It.IsAny<string>(),It.IsAny<PartitionKey>(),It.IsAny<ItemRequestOptions>(),It.IsAny<CancellationToken>())).ReturnsAsync(response);
        var client=new CosmosDbClient(container.Object);
        var failed=await Assert.ThrowsExceptionAsync<CosmosMutationReconciliationException>(async()=>{
            if(operation=="create")await client.CreateDocumentAsync(new{Id=id},null);
            else if(operation=="upsert")await client.UpsertDocumentAsync(new{Id=id},null);
            else await client.ReplaceDocumentAsync(id.ToString(),new{Id=id},null);
        });
        Assert.IsTrue(failed.Acknowledged);Assert.AreEqual(id.ToString(),failed.DocumentId);Assert.IsTrue(response.Disposed);
        if(missing)Assert.IsInstanceOfType<InvalidDataException>(failed.InnerException);else Assert.AreSame(cause,failed.InnerException);
    }

    private sealed class FaultingReadStream(IOException cause):MemoryStream
    {
        public override int Read(byte[] buffer,int offset,int count)=>throw cause;
        public override Task<int> ReadAsync(byte[] buffer,int offset,int count,CancellationToken token)=>Task.FromException<int>(cause);
        public override ValueTask<int> ReadAsync(Memory<byte> buffer,CancellationToken token=default)=>ValueTask.FromException<int>(cause);
    }

    [DataTestMethod]
    [DataRow("read")] [DataRow("query")] [DataRow("mutation")]
    public async Task ServiceErrorsRetainActualSubstatusAndActivity(string operation)
    {
        var id=Guid.NewGuid();
        var response=new TrackedResponse(HttpStatusCode.BadRequest);response.SetContent("{}");
        response.Headers.Set("x-ms-substatus","1002");response.Headers.Set("x-ms-activity-id","owned-activity");
        var container=new Mock<Container>();
        container.Setup(c=>c.ReadItemStreamAsync(It.IsAny<string>(),It.IsAny<PartitionKey>(),It.IsAny<ItemRequestOptions>(),It.IsAny<CancellationToken>())).ReturnsAsync(response);
        container.Setup(c=>c.UpsertItemStreamAsync(It.IsAny<Stream>(),It.IsAny<PartitionKey>(),It.IsAny<ItemRequestOptions>(),It.IsAny<CancellationToken>())).ReturnsAsync(response);
        container.Setup(c=>c.ReplaceItemStreamAsync(It.IsAny<Stream>(),It.IsAny<string>(),It.IsAny<PartitionKey>(),It.IsAny<ItemRequestOptions>(),It.IsAny<CancellationToken>())).ReturnsAsync(response);
        if(operation=="mutation")CosmosSdkTestRecords.Existing(container,id);
        var iterator=new Mock<FeedIterator>();iterator.Setup(i=>i.HasMoreResults).Returns(true);
        iterator.Setup(i=>i.ReadNextAsync(It.IsAny<CancellationToken>())).ReturnsAsync(response);
        container.Setup(c=>c.GetItemQueryStreamIterator(It.IsAny<QueryDefinition>(),It.IsAny<string>(),It.IsAny<QueryRequestOptions>())).Returns(iterator.Object);
        var client=new CosmosDbClient(container.Object);
        var failure=await Assert.ThrowsExceptionAsync<CosmosException>(async()=>
        {
            if(operation=="read") await client.ReadDocumentAsync("owned",null);
            else if(operation=="mutation") await client.UpsertDocumentAsync(new {Id=id},null);
            else client.ReadAllDocuments();
        });
        Assert.AreEqual(HttpStatusCode.BadRequest,failure.StatusCode);Assert.AreEqual(1002,failure.SubStatusCode);
        Assert.AreEqual("owned-activity",failure.ActivityId);Assert.IsTrue(response.Disposed);
    }

    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public async Task MissingItemRequiresReadableParentContainer(bool missingContainer)
    {
        var response=new TrackedResponse(HttpStatusCode.NotFound);response.SetContent("{}");
        var container=new Mock<Container>();
        container.Setup(c=>c.ReadItemStreamAsync(It.IsAny<string>(),It.IsAny<PartitionKey>(),It.IsAny<ItemRequestOptions>(),It.IsAny<CancellationToken>())).ReturnsAsync(response);
        var cause=new CosmosException("owned missing container",HttpStatusCode.NotFound,0,"owned",0);
        if(missingContainer) container.Setup(c=>c.ReadContainerAsync(It.IsAny<ContainerRequestOptions>(),It.IsAny<CancellationToken>())).ThrowsAsync(cause);
        else container.Setup(c=>c.ReadContainerAsync(It.IsAny<ContainerRequestOptions>(),It.IsAny<CancellationToken>())).ReturnsAsync(new Mock<ContainerResponse>().Object);
        var client=new CosmosDbClient(container.Object);
        if(missingContainer) Assert.AreSame(cause,await Assert.ThrowsExceptionAsync<CosmosException>(()=>client.ReadDocumentAsync("owned",null)));
        else Assert.IsNull(await client.ReadDocumentAsync("owned",null));
        Assert.IsTrue(response.Disposed);
    }

    [DataTestMethod]
    [DataRow("unique")] [DataRow("duplicate")] [DataRow("error")]
    public void SelectorsReadAllPagesAndNeverHideDuplicateOrLaterFailure(string mode)
    {
        var pages=new[] {new TrackedResponse(HttpStatusCode.OK),new TrackedResponse(HttpStatusCode.OK),
            new TrackedResponse(mode=="error" ? HttpStatusCode.BadRequest:HttpStatusCode.OK)};
        pages[0].SetContent("{\"Documents\":[]}");
        pages[1].SetContent("{\"Documents\":[{\"id\":\"owned-one\"}]}");
        pages[2].SetContent(mode=="duplicate" ? "{\"Documents\":[{\"id\":\"owned-two\"}]}":"{\"Documents\":[]}");
        var queue=new Queue<ResponseMessage>(pages);
        var iterator=new Mock<FeedIterator>();
        iterator.SetupSequence(i=>i.HasMoreResults).Returns(true).Returns(true).Returns(true).Returns(false);
        iterator.Setup(i=>i.ReadNextAsync(It.IsAny<CancellationToken>())).Returns(()=>Task.FromResult(queue.Dequeue()));
        var container=new Mock<Container>();
        container.Setup(c=>c.GetItemQueryStreamIterator(It.IsAny<QueryDefinition>(),It.IsAny<string>(),It.IsAny<QueryRequestOptions>())).Returns(iterator.Object);
        var client=new CosmosDbClient(container.Object);
        if(mode=="duplicate") Assert.ThrowsException<NextGenSoftware.OASIS.Common.StorageIdentityAmbiguousException>(()=>client.ReadDocumentByField("Username","owned"));
        else if(mode=="error") Assert.ThrowsException<CosmosException>(()=>client.ReadDocumentByField("Username","owned"));
        else StringAssert.Contains(client.ReadDocumentByField("Username","owned"),"owned-one");
        Assert.IsTrue(pages.All(page=>page.Disposed));
    }

    [TestMethod]
    public void SelectorRejectsSqlPropertyInjectionBeforeSdkCall()
    {
        var container=new Mock<Container>(MockBehavior.Strict);
        var client=new CosmosDbClient(container.Object);
        Assert.ThrowsException<ArgumentException>(()=>client.ReadDocumentByField("Username OR true --","owned"));
        container.VerifyNoOtherCalls();
    }

    [DataTestMethod]
    [DataRow(false,"{}")] [DataRow(true,"{}")]
    [DataRow(false,"{\"Documents\":null}")] [DataRow(true,"{\"Documents\":null}")]
    [DataRow(false,"{\"Documents\":[null]}")] [DataRow(true,"{\"Documents\":[null]}")]
    [DataRow(false,"{\"Documents\":[42]}")] [DataRow(true,"{\"Documents\":[42]}")]
    [DataRow(false,"not-json")] [DataRow(true,"not-json")]
    public void MalformedLaterQueryPageNeverReturnsPartialSuccess(bool selector,string malformed)
    {
        var first=new TrackedResponse(HttpStatusCode.OK);first.SetContent("{\"Documents\":[{\"id\":\"owned\"}]}");
        var second=new TrackedResponse(HttpStatusCode.OK);second.SetContent(malformed);
        var pages=new Queue<ResponseMessage>(new[] {first,second});
        var iterator=new Mock<FeedIterator>();
        iterator.SetupSequence(i=>i.HasMoreResults).Returns(true).Returns(true).Returns(false);
        iterator.Setup(i=>i.ReadNextAsync(It.IsAny<CancellationToken>())).Returns(()=>Task.FromResult(pages.Dequeue()));
        var container=new Mock<Container>();
        container.Setup(c=>c.GetItemQueryStreamIterator(It.IsAny<QueryDefinition>(),It.IsAny<string>(),It.IsAny<QueryRequestOptions>())).Returns(iterator.Object);
        var client=new CosmosDbClient(container.Object);
        Assert.ThrowsException<InvalidDataException>(()=> {if(selector) client.ReadDocumentByField("Username","owned");else client.ReadAllDocuments();});
        Assert.IsTrue(first.Disposed);Assert.IsTrue(second.Disposed);
    }

    private sealed class TrackedResponse(HttpStatusCode status) : ResponseMessage(status)
    {
        public bool Disposed;
        public void SetContent(string value) => Content=new MemoryStream(Encoding.UTF8.GetBytes(value));
        protected override void Dispose(bool disposing) {Disposed=true;base.Dispose(disposing);}
    }

    [DataTestMethod]
    [DataRow("read",false)] [DataRow("read",true)]
    [DataRow("create",false)] [DataRow("create",true)]
    [DataRow("upsert",false)] [DataRow("upsert",true)]
    [DataRow("replace",false)] [DataRow("replace",true)]
    [DataRow("delete",false)] [DataRow("delete",true)]
    public async Task ItemPathsDisposeSdkResponseAndOwnedRequestStreams(string operation,bool failure)
    {
        var document=new {Id=Guid.NewGuid()};
        var response=new TrackedResponse(failure ? HttpStatusCode.BadRequest:HttpStatusCode.OK);response.SetContent("{}");
        if(!failure && operation is "create" or "upsert" or "replace") {
            var acknowledged=Newtonsoft.Json.Linq.JObject.Parse(NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.OasisJson.Serialize((object)document));
            acknowledged["id"]=document.Id.ToString();response.SetContent(acknowledged.ToString());
        }
        var container=new Mock<Container>();Stream? request=null;
        container.Setup(c=>c.ReadItemStreamAsync(It.IsAny<string>(),It.IsAny<PartitionKey>(),It.IsAny<ItemRequestOptions>(),It.IsAny<CancellationToken>())).ReturnsAsync(response);
        container.Setup(c=>c.DeleteItemStreamAsync(It.IsAny<string>(),It.IsAny<PartitionKey>(),It.IsAny<ItemRequestOptions>(),It.IsAny<CancellationToken>())).ReturnsAsync(response);
        container.Setup(c=>c.CreateItemStreamAsync(It.IsAny<Stream>(),It.IsAny<PartitionKey>(),It.IsAny<ItemRequestOptions>(),It.IsAny<CancellationToken>()))
            .Callback<Stream,PartitionKey,ItemRequestOptions,CancellationToken>((s,_,_,_)=>request=s).ReturnsAsync(response);
        container.Setup(c=>c.UpsertItemStreamAsync(It.IsAny<Stream>(),It.IsAny<PartitionKey>(),It.IsAny<ItemRequestOptions>(),It.IsAny<CancellationToken>()))
            .Callback<Stream,PartitionKey,ItemRequestOptions,CancellationToken>((s,_,_,_)=>request=s).ReturnsAsync(response);
        container.Setup(c=>c.ReplaceItemStreamAsync(It.IsAny<Stream>(),It.IsAny<string>(),It.IsAny<PartitionKey>(),It.IsAny<ItemRequestOptions>(),It.IsAny<CancellationToken>()))
            .Callback<Stream,string,PartitionKey,ItemRequestOptions,CancellationToken>((s,_,_,_,_)=>request=s).ReturnsAsync(response);
        var client=new CosmosDbClient(container.Object);
        if(operation is "upsert" or "replace")CosmosSdkTestRecords.Existing(container,document.Id);
        async Task Execute()
        {
            switch(operation)
            {
                case "read":await client.ReadDocumentAsync(document.Id.ToString(),null);break;
                case "create":await client.CreateDocumentAsync(document,null);break;
                case "upsert":await client.UpsertDocumentAsync(document,null);break;
                case "replace":await client.ReplaceDocumentAsync(document.Id.ToString(),document,null);break;
                case "delete":await client.DeleteDocumentAsync(document.Id.ToString(),null);break;
            }
        }
        if(failure) await Assert.ThrowsExceptionAsync<CosmosException>(Execute);else await Execute();
        Assert.IsTrue(response.Disposed);
        if(operation!="read" && operation!="delete") {Assert.IsNotNull(request);Assert.IsFalse(request.CanRead);}
    }

    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public async Task UnknownUpsertOutcomesRetainCauseAndReconciliationMarker(bool transport)
    {
        var id=Guid.NewGuid();var cause=new IOException("owned lost response");
        var response=new TrackedResponse(HttpStatusCode.InternalServerError);response.SetContent("{}");
        var container=new Mock<Container>();
        CosmosSdkTestRecords.Existing(container,id);
        container.Setup(c=>c.ReplaceItemStreamAsync(It.IsAny<Stream>(),It.IsAny<string>(),It.IsAny<PartitionKey>(),It.IsAny<ItemRequestOptions>(),It.IsAny<CancellationToken>()))
            .Returns(()=>transport ? Task.FromException<ResponseMessage>(cause):Task.FromResult<ResponseMessage>(response));
        var failure=await Assert.ThrowsExceptionAsync<CosmosMutationReconciliationException>(()=>new CosmosDbClient(container.Object).UpsertDocumentAsync(new {Id=id},null));
        Assert.AreEqual(id.ToString(),failure.DocumentId);Assert.AreEqual(string.Empty,failure.OriginalTransactionId);
        Assert.IsTrue(NextGenSoftware.OASIS.API.Core.Helpers.NativeMutationReconciliation.IsRequired(failure));
        if(transport) Assert.AreSame(cause,failure.InnerException);
        else {Assert.IsInstanceOfType(failure.InnerException,typeof(CosmosException));Assert.IsTrue(response.Disposed);}
    }

    [DataTestMethod]
    [DataRow(false,false)] [DataRow(false,true)] [DataRow(true,false)] [DataRow(true,true)]
    public void QueryPathsDisposeSdkResponse(bool all,bool failure)
    {
        var response=new TrackedResponse(failure ? HttpStatusCode.BadRequest:HttpStatusCode.OK);response.SetContent("{\"Documents\":[]}");
        var iterator=new Mock<FeedIterator>();
        iterator.SetupSequence(i=>i.HasMoreResults).Returns(true).Returns(false);
        iterator.Setup(i=>i.ReadNextAsync(It.IsAny<CancellationToken>())).ReturnsAsync(response);
        var container=new Mock<Container>();
        container.Setup(c=>c.GetItemQueryStreamIterator(It.IsAny<QueryDefinition>(),It.IsAny<string>(),It.IsAny<QueryRequestOptions>())).Returns(iterator.Object);
        var client=new CosmosDbClient(container.Object);
        void Execute() {if(all) client.ReadAllDocuments();else client.ReadDocumentByField("Username","owned");}
        if(failure) Assert.ThrowsException<CosmosException>(Execute);else Execute();
        Assert.IsTrue(response.Disposed);
    }
}
