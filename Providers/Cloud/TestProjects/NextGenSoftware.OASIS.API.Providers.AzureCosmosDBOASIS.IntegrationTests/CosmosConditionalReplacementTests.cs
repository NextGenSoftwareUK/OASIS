using System.Net;
using Microsoft.Azure.Cosmos;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.Infrastructure;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.IntegrationTests;

public partial class AzureCosmosDBOASISIntegrationTests
{
    [DataTestMethod][DataRow(false,false)][DataRow(true,false)][DataRow(false,true)][DataRow(true,true)]
    public async Task NativeConditionalReplacementPreservesCompetingUpdate(bool afterRead,bool history)
    {
        _provider.IsVersionControlEnabled=history;
        var handler=new BeforeReplaceHandler();
        var options=new CosmosClientOptions{ConnectionMode=ConnectionMode.Gateway,RequestTimeout=TimeSpan.FromSeconds(10),MaxRetryAttemptsOnRateLimitedRequests=0,
            HttpClientFactory=()=>new HttpClient(new HttpClientHandler{ServerCertificateCustomValidationCallback=HttpClientHandler.DangerousAcceptAnyServerCertificateValidator})};
        options.CustomHandlers.Add(handler);
        using var client=new CosmosClient("https://127.0.0.1:8081","C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==",options);
        var repository=new AvatarRepository(new CosmosDbClientFactory(client,DatabaseName,new(){"avatarItems","avatarDetailItems","holonItems"}));
        var id=Guid.NewGuid();var verified=false;
        try
        {
            Assert.IsFalse((await _provider.SaveAvatarAsync(new Avatar{Id=id,IsActive=true,Username="owned-cas-"+id,FirstName="original"})).IsError);
            var expected=await repository.GetByIdAsync(id.ToString());
            var replacement=OasisJson.Project<Avatar>(expected);replacement.FirstName="must not overwrite";
            var competitor=OasisJson.Project<Avatar>(expected);competitor.FirstName="independently committed";
            async Task Change(){var changed=await _provider.SaveAvatarAsync(competitor);Assert.IsFalse(changed.IsError,changed.Message);}
            if(afterRead)handler.BeforeReplace=Change;else await Change();
            if(afterRead)
            {
                var rejected=await Assert.ThrowsExceptionAsync<CosmosException>(()=>repository.ReplaceIfUnchangedAsync(expected,replacement));
                Assert.AreEqual(HttpStatusCode.PreconditionFailed,rejected.StatusCode);
                Assert.IsFalse(NativeMutationReconciliation.IsRequired(rejected));Assert.AreEqual(1,handler.ReplaceCalls);
            }
            else
            {
                await Assert.ThrowsExceptionAsync<StorageLifecycleConflictException>(()=>repository.ReplaceIfUnchangedAsync(expected,replacement));
                Assert.AreEqual(0,handler.ReplaceCalls);
            }
            var retained=await _provider.LoadAvatarAsync(id);
            Assert.IsFalse(retained.IsError,retained.Message);Assert.AreEqual("independently committed",retained.Result.FirstName);
            if(history)
            {
                Assert.AreEqual(2,retained.Result.Version);
                var original=await _provider.LoadAvatarAsync(id,1);
                Assert.IsFalse(original.IsError,original.Message);Assert.AreEqual("original",original.Result.FirstName);
                var native=new CosmosDbClient(_client.GetContainer(DatabaseName,"avatarItems"));
                var json=Newtonsoft.Json.Linq.JObject.Parse(await native.ReadDocumentAsync(id.ToString(),new PartitionKey(id.ToString())));
                Assert.AreEqual(1,((Newtonsoft.Json.Linq.JArray)json[CosmosEmbeddedHistory.PropertyName]!["records"]!).Count,
                    "Rejected writer must not publish an extra archive or overwrite the winning version.");
                Assert.AreEqual(original.Result.VersionId,retained.Result.PreviousVersionId);
                Assert.AreEqual(1,replacement.Version);Assert.AreEqual(1,expected.Version);
            }
            Assert.AreEqual("must not overwrite",replacement.FirstName);Assert.AreEqual("original",expected.FirstName);
            Assert.IsFalse((await _provider.DeleteAvatarAsync(id,false)).IsError);
            if(history)Assert.IsTrue((await _provider.LoadAvatarAsync(id,1)).IsError,"Hard deletion removes this item including its archived versions.");
            verified=true;
        }
        finally{if(!verified)_ownsDatabase=false;}
    }

    private sealed class BeforeReplaceHandler:RequestHandler
    {
        public Func<Task>? BeforeReplace;
        public int ReplaceCalls;
        public override async Task<ResponseMessage> SendAsync(RequestMessage request,CancellationToken cancellationToken)
        {
            if(request.Method==HttpMethod.Put)
            {
                ReplaceCalls++;
                var action=Interlocked.Exchange(ref BeforeReplace,null);
                if(action!=null)await action();
            }
            return await base.SendAsync(request,cancellationToken);
        }
    }
}
