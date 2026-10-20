using System.Collections.ObjectModel;
using System.Text.Json;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Serialization;

namespace NextGenSoftware.OASIS.API.Providers.DynamoDBOASIS.IntegrationTests;

public partial class DynamoHistoryNativeTests
{
    public sealed class NativeRoleOwner:Holon
    {
        public List<Holon> Components {get;set;}=new();
        public override ReadOnlyCollection<IHolon> AllChildren=>Children.Concat<IHolon>(Components).ToList().AsReadOnly();
        public override IReadOnlyList<HolonChildCollection> CaptureChildCollections()=>base.CaptureChildCollections()
            .Concat(new[]{new HolonChildCollection(nameof(Components),Components,members=>Components=members.Cast<Holon>().ToList())}).ToArray();
    }

    [DataTestMethod][DataRow(false,false)][DataRow(false,true)][DataRow(true,false)][DataRow(true,true)]
    public async Task NativeNamedRolesPreserveSelectedMembershipHistoryAndObservedNull(bool aws,bool synchronous)
    {
        Assert.AreEqual("http://127.0.0.1:18000",Environment.GetEnvironmentVariable("DYNAMODB_SERVICE_URL"));
        var journal=Path.Combine(Environment.GetEnvironmentVariable("OASIS_DYNAMO_HISTORY_WORK")!,$"named-child-roles-{aws}-{synchronous}.pending.json");Assert.IsFalse(File.Exists(journal));
        var root=new NativeRoleOwner{Id=Guid.NewGuid(),Name="owned Dynamo named-role root",IsActive=true};
        var child=new Holon{Id=Guid.NewGuid(),ParentHolonId=root.Id,Name="owned Dynamo named-role component",IsActive=true};root.Components.Add(child);
        var expected=HolonChildRoleManifestJson.Serialize(HolonChildCollectionState.Capture(root).CreateManifest(root.Id,row=>row.Id));
        DynamoDBOASIS provider=aws?new NextGenSoftware.OASIS.API.Providers.AWSOASIS.AWSOASIS("us-east-1","ownedhistorytest","ownedhistorytest","http://127.0.0.1:18000")
            :new DynamoDBOASIS("ownedhistorytest","ownedhistorytest","us-east-1","http://127.0.0.1:18000");
        provider.IsVersionControlEnabled=true;
        using var verifier=Client();
        async Task<Dictionary<string,AttributeValue>> Read(Guid id)=>(await verifier.GetItemAsync(new GetItemRequest{TableName="OasisHolons",Key=new(){{"id",new(){S=id.ToString()}}},ConsistentRead=true})).Item;
        try{
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);foreach(var id in new[]{root.Id,child.Id})Assert.AreEqual(0,(await Read(id)).Count);
            using(var file=new FileStream(journal,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)){await file.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(new{ids=new[]{root.Id,child.Id},aws,synchronous,state="before-owned-named-role-fixture"}));file.Flush(true);}
            var saved=synchronous?provider.SaveHolon(root):await provider.SaveHolonAsync(root);Assert.IsFalse(saved.IsError,saved.Exception?.ToString());
            var initialJson=(await Read(root.Id))["data_json"].S;
            var initialManifest=StoredRecordJson.DeserializeStored<Holon>(initialJson)!.ChildRoleManifestJson;
            var publishedManifest=root.ChildRoleManifestJson;
            var childBefore=(await Read(child.Id))["data_json"].S;
            var scalar=new Holon{Id=root.Id,Name="owned Dynamo scalar update",IsActive=true};
            var partial=synchronous?provider.SaveHolon(scalar,saveChildren:false):await provider.SaveHolonAsync(scalar,saveChildren:false);Assert.IsFalse(partial.IsError,partial.Exception?.ToString());
            var observed=await Read(root.Id);var rootJson=observed["data_json"].S;
            var preserved=StoredRecordJson.DeserializeStored<Holon>(rootJson)!.ChildRoleManifestJson;
            var legacyDocument=JObject.Parse(rootJson);legacyDocument[nameof(Holon.ChildRoleManifestJson)]=JValue.CreateNull();var legacyJson=legacyDocument.ToString(Newtonsoft.Json.Formatting.None);
            var legacyItem=new Dictionary<string,AttributeValue>(observed){["data_json"]=new(){S=legacyJson}};
            var seeded=await verifier.PutItemAsync(new PutItemRequest{TableName="OasisHolons",Item=legacyItem,ConditionExpression="#data=:expected",ExpressionAttributeNames=new(){{"#data","data_json"}},ExpressionAttributeValues=new(){{":expected",new(){S=rootJson}}},ReturnValues=ReturnValue.ALL_OLD});
            Assert.AreEqual(rootJson,seeded.Attributes["data_json"].S);
            scalar=new Holon{Id=root.Id,Name="owned Dynamo legacy update",IsActive=true,ChildRoleManifestJson=expected};
            var legacySaved=synchronous?provider.SaveHolon(scalar,saveChildren:false):await provider.SaveHolonAsync(scalar,saveChildren:false);Assert.IsFalse(legacySaved.IsError,legacySaved.Exception?.ToString());
            rootJson=(await Read(root.Id))["data_json"].S;var legacyAfter=StoredRecordJson.DeserializeStored<Holon>(rootJson)!;
            var historical=synchronous?provider.LoadHolon(root.Id,loadChildren:false,version:1):await provider.LoadHolonAsync(root.Id,loadChildren:false,version:1);Assert.IsFalse(historical.IsError,historical.Exception?.ToString());
            var historicalManifest=((Holon)historical.Result).ChildRoleManifestJson;
            Assert.AreEqual(root.Name,historical.Result.Name);Assert.AreEqual(scalar.Name,legacyAfter.Name);Assert.AreEqual(childBefore,(await Read(child.Id))["data_json"].S);
            // Verify/conditionally remove exact acknowledged fixture bodies before role assertions.
            await RemoveOwned(verifier,"OasisHolons",root.Id,rootJson);await RemoveOwned(verifier,"OasisHolons",child.Id,childBefore);
            foreach(var id in new[]{root.Id,child.Id})Assert.AreEqual(0,(await Read(id)).Count);File.Delete(journal);
            Assert.AreEqual(expected,initialManifest);Assert.AreEqual(expected,publishedManifest);Assert.AreEqual(expected,preserved);Assert.AreEqual(expected,historicalManifest);
            Assert.IsNull(legacyAfter.ChildRoleManifestJson,"Persisted null must beat stale caller role membership.");Assert.IsNull(scalar.ChildRoleManifestJson,"Caller publication must reflect the exact committed observation.");
        }finally{await provider.DeActivateProviderAsync();}
    }
}
