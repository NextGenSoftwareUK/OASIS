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
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.DynamoDBOASIS.IntegrationTests;

public partial class DynamoHistoryNativeTests
{
    public static IEnumerable<object[]> OrdinarySaveLifecycleNativeCases()
    {foreach(var aws in new[]{false,true})foreach(var kind in Enumerable.Range(0,3))foreach(var change in Enumerable.Range(0,4))foreach(var history in new[]{false,true})yield return new object[]{aws,kind,change,history};}

    [DataTestMethod][DynamicData(nameof(OrdinarySaveLifecycleNativeCases),DynamicDataSourceType.Method)]
    public async Task NativeOrdinarySaveMustPreserveObservedTombstone(bool aws,int kind,int change,bool history)
    {
        Assert.AreEqual("http://127.0.0.1:18000",Environment.GetEnvironmentVariable("DYNAMODB_SERVICE_URL"));
        var work=Environment.GetEnvironmentVariable("OASIS_DYNAMO_HISTORY_WORK")!;var journal=Path.Combine(work,"ordinary-save-lifecycle.pending.json");Assert.IsFalse(File.Exists(journal));
        var id=Guid.NewGuid();var marker="owned-ordinary-lifecycle-"+id.ToString("N");var table=kind==0?"OasisAvatars":kind==1?"OasisAvatarDetails":"OasisHolons";
        DynamoDBOASIS provider=aws?new NextGenSoftware.OASIS.API.Providers.AWSOASIS.AWSOASIS("us-east-1","ownedhistorytest","ownedhistorytest","http://127.0.0.1:18000"):new DynamoDBOASIS("ownedhistorytest","ownedhistorytest","us-east-1","http://127.0.0.1:18000");
        provider.IsVersionControlEnabled=history;var field=typeof(DynamoDBOASIS).GetField("_client",BindingFlags.Instance|BindingFlags.NonPublic)!;
        var original=(AmazonDynamoDBClient)field.GetValue(provider)!;using var client=new FaultClient();using var verifier=Client();field.SetValue(provider,client);
        IHolonBase source=kind==0?new Avatar{Username=marker,Email=marker+"@example.invalid"}:kind==1?new AvatarDetail{Username=marker,Email=marker+"@example.invalid",Karma=123,XP=456}:new Holon();
        source.Id=id;source.Name=marker;source.IsActive=false;source.DeletedDate=new DateTime(2026,10,20,0,0,0,DateTimeKind.Utc);source.DeletedByAvatarId=Guid.NewGuid();source.MetaData["owned_lifecycle"]=marker;
        async Task<GetItemResponse> Read(){var result=await verifier.GetItemAsync(new GetItemRequest{TableName=table,Key=new(){{"id",new(){S=id.ToString()}}},ConsistentRead=true});Assert.AreEqual(HttpStatusCode.OK,result.HttpStatusCode);return result;}
        try{
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);Assert.AreEqual(0,(await Read()).Item.Count);
            using(var file=new FileStream(journal,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)){
                await file.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(new{id,marker,table,aws,kind,change,history,state="before-owned-ordinary-lifecycle"}));file.Flush(true);
            }
            await SavePublic(provider,source);var before=(await Read()).Item["data_json"].S;
            if(change==0){source.IsActive=true;source.DeletedDate=DateTime.MinValue;}else if(change==1)source.DeletedDate=source.DeletedDate.AddTicks(1);else if(change==2)source.DeletedByAvatarId=Guid.NewGuid();else source.Name=marker+"-compatible";
            var proposed=OasisJson.Serialize(source);var writes=client.Writes;
            (bool Error,bool Saved,int Count,string? Code,Exception? Failure) outcome=kind==0?Capture(await provider.SaveAvatarAsync((IAvatar)source)):kind==1?Capture(await provider.SaveAvatarDetailAsync((IAvatarDetail)source)):Capture(await provider.SaveHolonAsync((IHolon)source,saveChildren:false));
            Assert.IsFalse(NativeMutationReconciliation.IsRequired(outcome.Failure),"Preserve the original intent on native uncertainty; do not replay or infer cleanup authority.");
            var observed=(await Read()).Item["data_json"].S;var document=JObject.Parse(observed);
            Assert.AreEqual(id,Guid.Parse(document["Id"]!.Value<string>()!));Assert.AreEqual(marker,document["MetaData"]!["owned_lifecycle"]!.Value<string>());
            var callerAfter=OasisJson.Serialize(source);var actualWrites=client.Writes-writes;
            await RemoveOwned(verifier,table,id,observed);Assert.AreEqual(0,(await Read()).Item.Count);File.Delete(journal);
            // A semantic red follows independently inspected exact cleanup, not an abandoned native fixture.
            Assert.AreEqual(change!=3,outcome.Error,outcome.Failure?.ToString());Assert.AreEqual(change==3,outcome.Saved);Assert.AreEqual(change==3?1:0,actualWrites);
            Assert.AreEqual(change==3?1:0,outcome.Count,"Count only acknowledged mutations, including compatible tombstone edits.");
            if(change!=3){Assert.IsInstanceOfType<StorageLifecycleConflictException>(outcome.Failure);Assert.AreEqual(StorageLifecycleConflictException.ErrorCode,outcome.Code);Assert.AreEqual(before,observed);Assert.AreEqual(proposed,callerAfter);}
            else{var prior=JObject.Parse(before);Assert.AreEqual(prior["DeletedDate"]!.Value<DateTime>(),document["DeletedDate"]!.Value<DateTime>());Assert.AreEqual(prior["DeletedByAvatarId"]!.Value<string>(),document["DeletedByAvatarId"]!.Value<string>());}
        }finally{field.SetValue(provider,original);await provider.DeActivateProviderAsync();}
        static (bool Error,bool Saved,int Count,string? Code,Exception? Failure) Capture<T>(OASISResult<T> result)=>(result.IsError,result.IsSaved,result.SavedCount,result.ErrorCode,result.Exception);
    }
}
