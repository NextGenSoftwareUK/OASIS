using System.Reflection;
using Microsoft.Azure.Cosmos;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.Infrastructure;

namespace NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.IntegrationTests;

public partial class AzureCosmosDBOASISIntegrationTests
{
    [DataTestMethod][DataRow(false,false)][DataRow(true,false)][DataRow(false,true)][DataRow(true,true)]
    public async Task NativeImportUsesGraphSaveAndPreservesUncertainAcknowledgement(bool synchronous,bool loseAcknowledgement)
    {
        var handler=new ImportAcknowledgementHandler{Lose=loseAcknowledgement};
        var options=new CosmosClientOptions{ConnectionMode=ConnectionMode.Gateway,RequestTimeout=TimeSpan.FromSeconds(10),MaxRetryAttemptsOnRateLimitedRequests=0,
            HttpClientFactory=()=>new HttpClient(new HttpClientHandler{ServerCertificateCustomValidationCallback=HttpClientHandler.DangerousAcceptAnyServerCertificateValidator})};
        options.CustomHandlers.Add(handler);
        using var client=new CosmosClient("https://127.0.0.1:8081","C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==",options);
        var field=typeof(AzureCosmosDBOASIS).GetField("holonRepository",BindingFlags.Instance|BindingFlags.NonPublic)!;
        var healthy=field.GetValue(_provider);
        field.SetValue(_provider,new HolonRepository(new CosmosDbClientFactory(client,DatabaseName,new(){"avatarItems","avatarDetailItems","holonItems"})));
        var root=new Holon{Id=Guid.NewGuid(),Name="owned-import-root",IsActive=true};
        var child=new Holon{Id=Guid.NewGuid(),Name="owned-import-child",IsActive=true};root.Children.Add(child);
        var inspected=false;
        try
        {
            var result=synchronous?_provider.Import(new[]{root}):await _provider.ImportAsync(new[]{root});
            field.SetValue(_provider,healthy);
            var native=new CosmosDbClient(_client.GetContainer(DatabaseName,"holonItems"));
            var rootJson=await native.ReadDocumentAsync(root.Id.ToString(),new PartitionKey(root.Id.ToString()));
            var childJson=await native.ReadDocumentAsync(child.Id.ToString(),new PartitionKey(child.Id.ToString()));
            Assert.IsNotNull(rootJson,"Inspect original native commit, never repeat import.");
            var original=Newtonsoft.Json.Linq.JObject.Parse(rootJson);
            Assert.AreEqual(root.Id.ToString(),original["id"]!.ToString());
            // Remove only exact acknowledged and inspected original fixtures before regression assertions.
            await native.DeleteDocumentAsync(root.Id.ToString(),new PartitionKey(root.Id.ToString()));
            if(childJson!=null)
            {
                Assert.AreEqual(child.Id.ToString(),Newtonsoft.Json.Linq.JObject.Parse(childJson)["id"]!.ToString());
                await native.DeleteDocumentAsync(child.Id.ToString(),new PartitionKey(child.Id.ToString()));
            }
            inspected=true;
            if(loseAcknowledgement)
            {
                Assert.AreEqual(1,handler.Committed);Assert.IsNull(childJson,"Uncertain parent must stop descendant dispatch.");
                Assert.IsTrue(result.IsError);Assert.IsFalse(result.Result);Assert.IsFalse(result.IsSaved);
                Assert.IsTrue(NativeMutationReconciliation.IsRequired(result.Exception));
            }
            else
            {
                Assert.IsNotNull(childJson,"Import must persist selected recursive graph.");
                Assert.AreEqual(2,handler.Committed);Assert.IsFalse(result.IsError,result.Message);Assert.IsTrue(result.Result);Assert.IsTrue(result.IsSaved);
            }
        }
        finally{field.SetValue(_provider,healthy);if(!inspected)_ownsDatabase=false;}
    }

    private sealed class ImportAcknowledgementHandler:RequestHandler
    {
        public bool Lose;public int Committed;
        public override async Task<ResponseMessage> SendAsync(RequestMessage request,CancellationToken cancellationToken)
        {
            var response=await base.SendAsync(request,cancellationToken);
            if(request.Method==HttpMethod.Post && !string.Equals(request.Headers["x-ms-documentdb-isquery"],"true",StringComparison.OrdinalIgnoreCase) && response.IsSuccessStatusCode)
            {
                Committed++;
                if(Lose){response.Content?.Dispose();response.Content=null;}
            }
            return response;
        }
    }
}
