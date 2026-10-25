using System.Text.Json;
using Amazon.DynamoDBv2.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;

namespace NextGenSoftware.OASIS.API.Providers.DynamoDBOASIS.IntegrationTests;

public partial class DynamoHistoryNativeTests
{
    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task NativeOrdinaryPutReportsCapacityThroughProductionPipeline(bool aws)
    {
        Assert.AreEqual("http://127.0.0.1:18000",Environment.GetEnvironmentVariable("DYNAMODB_SERVICE_URL"));
        var work=Environment.GetEnvironmentVariable("OASIS_DYNAMO_HISTORY_WORK")!;
        var journal=Path.Combine(work,$"put-capacity-{aws}.pending.json");Assert.IsFalse(File.Exists(journal));
        var id=Guid.NewGuid();var marker="owned-put-capacity-"+id.ToString("N");
        DynamoDBOASIS provider=aws?new NextGenSoftware.OASIS.API.Providers.AWSOASIS.AWSOASIS("us-east-1","ownedhistorytest","ownedhistorytest","http://127.0.0.1:18000"):
            new DynamoDBOASIS("ownedhistorytest","ownedhistorytest","us-east-1","http://127.0.0.1:18000");
        provider.IsVersionControlEnabled=true;using var verifier=Client();string? observed=null;
        var operations=new List<(string Operation,IReadOnlyList<NativeResourceUsageObservation> Rows)>();
        try{
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
            var key=new Dictionary<string,AttributeValue>{{"id",new(){S=id.ToString()}}};
            Assert.AreEqual(0,(await verifier.GetItemAsync(new(){TableName="OasisHolons",Key=key,ConsistentRead=true})).Item.Count);
            using(var file=new FileStream(journal,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)){
                await file.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(new{id,marker,table="OasisHolons",aws,state="before-owned-put-capacity"}));file.Flush(true);
            }
            var saved=await provider.ObserveNativeResourceUsageAsync("native-put-capacity",()=>provider.SaveHolonAsync(new Holon{Id=id,Name=marker},saveChildren:false),
                (operation,rows)=>{operations.Add((operation,rows));return Task.CompletedTask;});
            var actual=await verifier.GetItemAsync(new(){TableName="OasisHolons",Key=key,ConsistentRead=true});
            var raw=actual.Item["data_json"].S;
            var canonical=NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.OasisJson.DeserializeStored<Holon>(raw);
            Assert.AreEqual(id,canonical.Id);Assert.AreEqual(marker,canonical.Name);observed=raw;
            Assert.IsFalse(saved.IsError,saved.Exception?.ToString());Assert.IsTrue(saved.IsSaved);Assert.AreEqual(1,saved.SavedCount);
            CollectionAssert.AreEqual(new[]{"GetItem","PutItem"},operations.Select(row=>row.Operation).ToArray());
            var put=operations.Single(row=>row.Operation=="PutItem").Rows;
            Assert.IsTrue(put.Any(row=>row.Unit=="capacity-units"&&row.View=="Total"&&row.Quantity>0));
            foreach(var row in put){Assert.AreEqual("native-put-capacity",row.ProviderRequestId);Assert.IsFalse(string.IsNullOrWhiteSpace(row.NativeRequestId));Assert.AreEqual(aws?"AWSOASIS":"DynamoDBOASIS",row.Provider);}
        }finally{
            if(observed!=null){await RemoveOwned(verifier,"OasisHolons",id,observed);var absent=await verifier.GetItemAsync(new(){TableName="OasisHolons",Key=new(){{"id",new(){S=id.ToString()}}},ConsistentRead=true});Assert.AreEqual(0,absent.Item.Count);File.Delete(journal);}
            await provider.DeActivateProviderAsync();
        }
    }
}
