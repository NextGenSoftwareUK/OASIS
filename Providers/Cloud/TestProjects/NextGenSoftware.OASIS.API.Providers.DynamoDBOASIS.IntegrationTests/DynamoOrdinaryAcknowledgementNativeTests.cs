using System.Net;
using System.Reflection;
using System.Text.Json;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.DynamoDBOASIS.IntegrationTests;

public partial class DynamoHistoryNativeTests
{
    public static IEnumerable<object[]> OrdinaryAcknowledgementNativeCases()
    {foreach(var aws in new[]{false,true})foreach(var kind in Enumerable.Range(0,3))foreach(var lost in new[]{false,true})yield return new object[]{aws,kind,lost};}

    [DataTestMethod][DynamicData(nameof(OrdinaryAcknowledgementNativeCases),DynamicDataSourceType.Method)]
    public async Task NativeOrdinarySaveCountsOnlyKnownCommitBeforeCallerPublication(bool aws,int kind,bool lost)
    {
        Assert.AreEqual("http://127.0.0.1:18000",Environment.GetEnvironmentVariable("DYNAMODB_SERVICE_URL"));
        var journal=Path.Combine(Environment.GetEnvironmentVariable("OASIS_DYNAMO_HISTORY_WORK")!,"ordinary-acknowledgement.pending.json");Assert.IsFalse(File.Exists(journal));
        var id=Guid.NewGuid();var replacement=Guid.NewGuid();var marker="owned-ordinary-ack-"+id.ToString("N");var table=kind==0?"OasisAvatars":kind==1?"OasisAvatarDetails":"OasisHolons";
        DynamoDBOASIS provider=aws?new NextGenSoftware.OASIS.API.Providers.AWSOASIS.AWSOASIS("us-east-1","ownedhistorytest","ownedhistorytest","http://127.0.0.1:18000"):new DynamoDBOASIS("ownedhistorytest","ownedhistorytest","us-east-1","http://127.0.0.1:18000");
        provider.IsVersionControlEnabled=true;var field=typeof(DynamoDBOASIS).GetField("_client",BindingFlags.Instance|BindingFlags.NonPublic)!;
        var original=(AmazonDynamoDBClient)field.GetValue(provider)!;using var client=new FaultClient();using var verifier=Client();field.SetValue(provider,client);
        IHolonBase source=kind==0?new Avatar{Username=marker,Email=marker+"@example.invalid"}:kind==1?new AvatarDetail{Username=marker,Email=marker+"@example.invalid"}:new Holon();
        source.Id=id;source.Name=marker;source.MetaData["owned_ack"]=marker;
        async Task<GetItemResponse> Read(Guid identity){var result=await verifier.GetItemAsync(new GetItemRequest{TableName=table,Key=new(){{"id",new(){S=identity.ToString()}}},ConsistentRead=true});Assert.AreEqual(HttpStatusCode.OK,result.HttpStatusCode);return result;}
        try{
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);Assert.AreEqual(0,(await Read(id)).Item.Count);Assert.AreEqual(0,(await Read(replacement)).Item.Count);
            using(var file=new FileStream(journal,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)){
                await file.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(new{id,replacement,marker,table,aws,kind,lost,state="before-owned-ordinary-ack"}));file.Flush(true);
            }
            if(lost)client.Mode="lost";else client.BeforePut=()=>{source.Id=replacement;source.Version=999;return Task.CompletedTask;};
            var outcome=kind==0?Capture(await provider.SaveAvatarAsync((IAvatar)source)):kind==1?Capture(await provider.SaveAvatarDetailAsync((IAvatarDetail)source)):Capture(await provider.SaveHolonAsync((IHolon)source,saveChildren:false));
            Assert.IsTrue(outcome.Error);Assert.IsFalse(outcome.Saved);Assert.IsTrue(NativeMutationReconciliation.IsRequired(outcome.Failure));Assert.AreEqual(1,client.Writes);Assert.AreEqual(0,client.Transactions);
            if(lost){Assert.IsInstanceOfType<DynamoDBMutationReconciliationException>(outcome.Failure);Assert.AreSame(client.Lost,outcome.Failure!.InnerException);}
            else{Assert.IsInstanceOfType<DynamoDBPartialMutationException>(outcome.Failure);Assert.AreEqual(id,((DynamoDBPartialMutationException)outcome.Failure!).AcknowledgedRecordIds.Single());}
            var observed=(await Read(id)).Item["data_json"].S;var document=JObject.Parse(observed);
            Assert.AreEqual(id,Guid.Parse(document["Id"]!.Value<string>()!));Assert.AreEqual(marker,document["MetaData"]!["owned_ack"]!.Value<string>());Assert.AreEqual(1,document["Version"]!.Value<int>());
            Assert.AreEqual(lost?id:replacement,source.Id);Assert.AreEqual(lost?0:999,source.Version);Assert.AreEqual(0,(await Read(replacement)).Item.Count);
            await RemoveOwned(verifier,table,id,observed);Assert.AreEqual(0,(await Read(id)).Item.Count);File.Delete(journal);
            // Unknown native replies remain count0; independently known ACKs count1 even when publication was refused.
            Assert.AreEqual(lost?0:1,outcome.Count);
        }finally{field.SetValue(provider,original);await provider.DeActivateProviderAsync();}
        static (bool Error,bool Saved,int Count,Exception? Failure) Capture<T>(OASISResult<T> result)=>(result.IsError,result.IsSaved,result.SavedCount,result.Exception);
    }
}
