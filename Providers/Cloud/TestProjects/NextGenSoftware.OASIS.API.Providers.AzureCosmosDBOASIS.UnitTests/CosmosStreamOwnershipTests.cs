using System.Net;
using System.Text;
using Microsoft.Azure.Cosmos;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.Infrastructure;

namespace NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.UnitTests;

[TestClass]
public class CosmosStreamOwnershipTests
{
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
    public async Task ItemPathsDisposeSdkResponseAndOwnedRequestStreams(string operation,bool failure)
    {
        var response=new TrackedResponse(failure ? HttpStatusCode.BadRequest:HttpStatusCode.OK);response.SetContent("{}");
        var container=new Mock<Container>();Stream? request=null;
        container.Setup(c=>c.ReadItemStreamAsync(It.IsAny<string>(),It.IsAny<PartitionKey>(),It.IsAny<ItemRequestOptions>(),It.IsAny<CancellationToken>())).ReturnsAsync(response);
        container.Setup(c=>c.CreateItemStreamAsync(It.IsAny<Stream>(),It.IsAny<PartitionKey>(),It.IsAny<ItemRequestOptions>(),It.IsAny<CancellationToken>()))
            .Callback<Stream,PartitionKey,ItemRequestOptions,CancellationToken>((s,_,_,_)=>request=s).ReturnsAsync(response);
        container.Setup(c=>c.UpsertItemStreamAsync(It.IsAny<Stream>(),It.IsAny<PartitionKey>(),It.IsAny<ItemRequestOptions>(),It.IsAny<CancellationToken>()))
            .Callback<Stream,PartitionKey,ItemRequestOptions,CancellationToken>((s,_,_,_)=>request=s).ReturnsAsync(response);
        container.Setup(c=>c.ReplaceItemStreamAsync(It.IsAny<Stream>(),It.IsAny<string>(),It.IsAny<PartitionKey>(),It.IsAny<ItemRequestOptions>(),It.IsAny<CancellationToken>()))
            .Callback<Stream,string,PartitionKey,ItemRequestOptions,CancellationToken>((s,_,_,_,_)=>request=s).ReturnsAsync(response);
        var client=new CosmosDbClient(container.Object);
        var document=new {Id=Guid.NewGuid()};
        async Task Execute()
        {
            switch(operation)
            {
                case "read":await client.ReadDocumentAsync(document.Id.ToString(),null);break;
                case "create":await client.CreateDocumentAsync(document,null);break;
                case "upsert":await client.UpsertDocumentAsync(document,null);break;
                case "replace":await client.ReplaceDocumentAsync(document.Id.ToString(),document,null);break;
            }
        }
        if(failure) await Assert.ThrowsExceptionAsync<CosmosException>(Execute);else await Execute();
        Assert.IsTrue(response.Disposed);
        if(operation!="read") {Assert.IsNotNull(request);Assert.IsFalse(request.CanRead);}
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
