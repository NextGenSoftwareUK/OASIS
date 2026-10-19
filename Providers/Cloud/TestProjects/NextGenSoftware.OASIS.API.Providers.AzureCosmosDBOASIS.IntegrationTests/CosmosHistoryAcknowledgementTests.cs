using System.Reflection;
using System.Text;
using Microsoft.Azure.Cosmos;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.Infrastructure;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.IntegrationTests;

public partial class AzureCosmosDBOASISIntegrationTests
{
    [DataTestMethod][DataRow("missing")][DataRow("archive")][DataRow("transport")]
    public async Task NativeVersionedLostAcknowledgementPreservesOriginalWithoutReplayOrPublication(string mode)
    {
        _provider.IsVersionControlEnabled=true;
        var root=new Holon{Id=Guid.NewGuid(),IsActive=true,Name="owned-history-ack-original"};
        Assert.IsFalse((await _provider.SaveHolonAsync(root,saveChildren:false)).IsError);
        var firstVersionId=root.VersionId;
        var child=new Holon{Id=Guid.NewGuid(),IsActive=true,Name="owned-history-ack-child"};
        root.Name="owned-history-ack-replacement";root.Children.Add(child);
        var handler=new BeforeReplaceHandler();
        handler.AfterReplace=async response=> {
            if(!response.IsSuccessStatusCode)return response;
            if(mode=="transport") {response.Dispose();throw new IOException("Controlled lost response after native commit.");}
            if(mode=="missing") {response.Content?.Dispose();response.Content=null;return response;}
            using var reader=new StreamReader(response.Content);
            var altered=JObject.Parse(await reader.ReadToEndAsync());
            altered[CosmosEmbeddedHistory.PropertyName]!["records"]=new JArray();
            response.Content=new MemoryStream(Encoding.UTF8.GetBytes(altered.ToString()));
            return response;
        };
        var options=new CosmosClientOptions{ConnectionMode=ConnectionMode.Gateway,RequestTimeout=TimeSpan.FromSeconds(10),MaxRetryAttemptsOnRateLimitedRequests=0,
            HttpClientFactory=()=>new HttpClient(new HttpClientHandler{ServerCertificateCustomValidationCallback=HttpClientHandler.DangerousAcceptAnyServerCertificateValidator})};
        options.CustomHandlers.Add(handler);
        using var client=new CosmosClient("https://127.0.0.1:8081","C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==",options);
        var field=typeof(AzureCosmosDBOASIS).GetField("holonRepository",BindingFlags.Instance|BindingFlags.NonPublic)!;
        var healthy=field.GetValue(_provider);var inspected=false;
        field.SetValue(_provider,new HolonRepository(new CosmosDbClientFactory(client,DatabaseName,new(){"avatarItems","avatarDetailItems","holonItems"},()=>true)));
        try {
            var result=await _provider.SaveHolonAsync(root);
            field.SetValue(_provider,healthy);
            var native=new CosmosDbClient(_client.GetContainer(DatabaseName,"holonItems"));
            var stored=JObject.Parse(await native.ReadDocumentAsync(root.Id.ToString(),new PartitionKey(root.Id.ToString())));
            var childJson=await native.ReadDocumentAsync(child.Id.ToString(),new PartitionKey(child.Id.ToString()));
            Assert.AreEqual(root.Id.ToString(),stored["id"]!.Value<string>());Assert.AreEqual(root.Name,stored["Name"]!.Value<string>());
            Assert.AreEqual(2,stored["Version"]!.Value<int>());Assert.AreEqual(1,((JArray)stored[CosmosEmbeddedHistory.PropertyName]!["records"]!).Count);
            var original=await CosmosEmbeddedHistory.ReadAsync(root.Id,stored,1,json=>OasisJson.DeserializeStored<Holon>(json));
            Assert.AreEqual("owned-history-ack-original",original.Name);Assert.AreEqual(firstVersionId,original.VersionId);
            // Readback has resolved this exact original's actual native state; never repeat its save.
            await native.DeleteDocumentAsync(root.Id.ToString(),new PartitionKey(root.Id.ToString()));
            if(childJson!=null) {
                var observed=JObject.Parse(childJson);Assert.AreEqual(child.Id.ToString(),observed["id"]!.Value<string>());Assert.AreEqual(child.Name,observed["Name"]!.Value<string>());
                await native.DeleteDocumentAsync(child.Id.ToString(),new PartitionKey(child.Id.ToString()));
            }
            inspected=true;
            Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsSaved);Assert.IsTrue(NativeMutationReconciliation.IsRequired(result.Exception));
            var uncertain=result.Exception as CosmosMutationReconciliationException;Assert.IsNotNull(uncertain);
            Assert.AreEqual(root.Id.ToString(),uncertain.DocumentId);Assert.AreEqual(mode!="transport",uncertain.Acknowledged);
            Assert.AreEqual(1,handler.ReplaceCalls,"Never repeat the original native mutation.");Assert.IsNull(childJson,"Uncertain parent must stop descendant dispatch.");
            Assert.AreEqual(1,root.Version);Assert.AreEqual(firstVersionId,root.VersionId);Assert.AreEqual(0,child.Version);
        } finally {field.SetValue(_provider,healthy);if(!inspected)_ownsDatabase=false;}
    }
}
