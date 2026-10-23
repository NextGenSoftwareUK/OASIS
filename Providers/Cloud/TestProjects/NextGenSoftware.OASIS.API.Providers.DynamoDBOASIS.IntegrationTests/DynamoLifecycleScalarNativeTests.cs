using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;

namespace NextGenSoftware.OASIS.API.Providers.DynamoDBOASIS.IntegrationTests;

public partial class DynamoHistoryNativeTests
{
    [DataTestMethod][DataRow(false,false)][DataRow(false,true)][DataRow(true,false)][DataRow(true,true)]
    public async Task NativeSoftDeletePreservesCurrentAndArchivedScalarMetadata(bool aws,bool synchronous)
    {
        Assert.AreEqual("http://127.0.0.1:18000",Environment.GetEnvironmentVariable("DYNAMODB_SERVICE_URL"));
        var journal=Path.Combine(Environment.GetEnvironmentVariable("OASIS_DYNAMO_HISTORY_WORK")!,$"lifecycle-scalars-{aws}-{synchronous}.pending.json");
        Assert.IsFalse(File.Exists(journal));var id=Guid.NewGuid();var standalone=Guid.NewGuid();
        var rows=new[]{("OasisAvatars",id),("OasisAvatarDetails",id),("OasisAvatarDetails",standalone)};
        DynamoDBOASIS provider=aws?new NextGenSoftware.OASIS.API.Providers.AWSOASIS.AWSOASIS("us-east-1","ownedhistorytest","ownedhistorytest","http://127.0.0.1:18000"):
            new DynamoDBOASIS("ownedhistorytest","ownedhistorytest","us-east-1","http://127.0.0.1:18000");
        provider.IsVersionControlEnabled=true;using var verifier=Client();
        const string date="2030-01-02T03:04:05.0000000+05:30",number="9007199254740993.125";using var precise=JsonDocument.Parse(number);
        Dictionary<string,object> Meta()=>new(){["literal-date"]=date,["literal-number"]=precise.RootElement.Clone()};
        async Task<Amazon.DynamoDBv2.Model.GetItemResponse> Read(string table,Guid key)=>await verifier.GetItemAsync(new Amazon.DynamoDBv2.Model.GetItemRequest{
            TableName=table,ConsistentRead=true,Key=new(){{"id",new(){S=key.ToString()}}}});
        try{
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
            foreach(var row in rows)Assert.AreEqual(0,(await Read(row.Item1,row.Item2)).Item.Count);
            using(var file=new FileStream(journal,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)){
                await file.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(new{id,standalone,aws,synchronous,state="before-owned-scalar-lifecycle"}));file.Flush(true);
            }
            var name="owned-scalar-lifecycle-"+id.ToString("N");
            Assert.IsFalse((await provider.SaveAvatarAsync(new Avatar{Id=id,Username=name,Email=name+"@example.invalid",IsActive=true,MetaData=Meta()})).IsError);
            foreach(var detailId in new[]{id,standalone}){
                var detailName=detailId==id?name:name+"-standalone";
                Assert.IsFalse((await provider.SaveAvatarDetailAsync(new AvatarDetail{Id=detailId,Username=detailName,Email=detailName+"@example.invalid",IsActive=true,MetaData=Meta()})).IsError);
            }
            var paired=synchronous?provider.DeleteAvatar(id):await provider.DeleteAvatarAsync(id);
            var detail=synchronous?provider.DeleteAvatarDetail(standalone):await provider.DeleteAvatarDetailAsync(standalone);
            Assert.IsFalse(paired.IsError,paired.Exception?.ToString());Assert.IsFalse(detail.IsError,detail.Exception?.ToString());
            var originals=new Dictionary<(string,Guid),string>();
            foreach(var row in rows){
                var observed=await Read(row.Item1,row.Item2);Assert.AreEqual(System.Net.HttpStatusCode.OK,observed.HttpStatusCode);
                var raw=observed.Item["data_json"].S;originals.Add(row,raw);using var json=JsonDocument.Parse(raw);
                var current=json.RootElement;Assert.AreEqual(2,current.GetProperty("Version").GetInt32());
                var archive=current.GetProperty("_oasis_record_history").GetProperty("records")[0];Assert.AreEqual(1,archive.GetProperty("Version").GetInt32());
                foreach(var value in new[]{current,archive}){
                    var meta=value.GetProperty("MetaData");Assert.AreEqual(date,meta.GetProperty("literal-date").GetString());
                    Assert.AreEqual(number,meta.GetProperty("literal-number").GetRawText());
                }
            }
            foreach(var row in rows)await RemoveOwned(verifier,row.Item1,row.Item2,originals[row]);
            File.Delete(journal);
        }finally{await provider.DeActivateProviderAsync();}
    }
}
