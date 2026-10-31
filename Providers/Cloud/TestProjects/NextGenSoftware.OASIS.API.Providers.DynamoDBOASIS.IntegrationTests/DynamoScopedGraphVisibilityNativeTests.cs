using System.Text.Json;
using Amazon.DynamoDBv2.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Objects.Search;

namespace NextGenSoftware.OASIS.API.Providers.DynamoDBOASIS.IntegrationTests;

public partial class DynamoHistoryNativeTests
{
    [DataTestMethod][DataRow(false,false)][DataRow(false,true)][DataRow(true,false)][DataRow(true,true)]
    public async Task NativeScopedSearchAndExportsPreserveForeignMembership(bool aws,bool synchronous)
    {
        var work=Environment.GetEnvironmentVariable("OASIS_DYNAMO_HISTORY_WORK")!;
        var journal=Path.Combine(work,$"scoped-query-{aws}-{synchronous}.pending.json");Assert.IsFalse(File.Exists(journal));
        var actor=Guid.NewGuid();var root=new Holon{Id=Guid.NewGuid(),CreatedByAvatarId=actor,Name="owned-scoped-root",MetaData=new(){{"scope",actor.ToString("D")}}};
        var owned=new Holon{Id=Guid.NewGuid(),CreatedByAvatarId=actor,Name="owned-scoped-child"};
        var hidden=new Holon{Id=Guid.NewGuid(),CreatedByAvatarId=Guid.NewGuid(),Name="foreign-private-child"};
        var foreignPublic=new Holon{Id=Guid.NewGuid(),CreatedByAvatarId=Guid.NewGuid(),IsPublic=true,Name="foreign-public-child"};
        var leaf=new Holon{Id=Guid.NewGuid(),CreatedByAvatarId=actor,Name="owned-under-private-branch"};
        root.Children.Add(owned);root.Children.Add(hidden);root.Children.Add(foreignPublic);hidden.Children.Add(leaf);
        var rows=new[]{root,owned,hidden,foreignPublic,leaf};
        var provider=aws?(DynamoDBOASIS)new NextGenSoftware.OASIS.API.Providers.AWSOASIS.AWSOASIS("us-east-1","ownedhistorytest","ownedhistorytest","http://127.0.0.1:18000"):new DynamoDBOASIS("ownedhistorytest","ownedhistorytest","us-east-1","http://127.0.0.1:18000");
        provider.IsVersionControlEnabled=true;using var native=Client();
        async Task<string?> Read(Guid id){var observed=await native.GetItemAsync(new GetItemRequest{TableName="OasisHolons",Key=new(){{"id",new(){S=id.ToString()}}},ConsistentRead=true});return observed.Item.Count==0?null:observed.Item["data_json"].S;}
        try{
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);foreach(var row in rows)Assert.IsNull(await Read(row.Id));
            await using(var file=new FileStream(journal,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)){
                await JsonSerializer.SerializeAsync(file,new{actor,aws,synchronous,ids=rows.Select(row=>row.Id).ToArray(),state="before-owned-scoped-query-create"});file.Flush(true);
            }
            var saved=await provider.SaveHolonAsync(root);Assert.IsFalse(saved.IsError,saved.Exception?.ToString());Assert.AreEqual(5,saved.SavedCount);
            var originals=new Dictionary<Guid,string>();foreach(var row in rows){var json=await Read(row.Id);Assert.IsNotNull(json);originals.Add(row.Id,json);}
            var projected=new List<Holon>();
            foreach(var version in new[]{0,1}){
                var query=new SearchParams{SearchOnlyForCurrentAvatar=true,AvatarId=actor,FilterByMetaData=new(){{"scope",actor.ToString("D")}}};
                var search=synchronous?provider.Search(query,version:version):await provider.SearchAsync(query,version:version);
                Assert.IsFalse(search.IsError,search.Exception?.ToString());Assert.AreEqual(1,search.LoadedCount);projected.Add((Holon)search.Result.SearchResultHolons.Single());
                var export=synchronous?provider.ExportAllDataForAvatarById(actor,version):await provider.ExportAllDataForAvatarByIdAsync(actor,version);
                Assert.IsFalse(export.IsError,export.Exception?.ToString());projected.Add((Holon)export.Result.Single(row=>row.Id==root.Id));
            }
            foreach(var view in projected){
                view.AcceptChanges();var rejected=synchronous?provider.SaveHolon(view):await provider.SaveHolonAsync(view);
                Assert.IsTrue(rejected.IsError);Assert.IsFalse(rejected.IsSaved);Assert.IsInstanceOfType<InvalidOperationException>(rejected.Exception);
            }
            foreach(var original in originals)Assert.AreEqual(original.Value,await Read(original.Key),"Scoped reads/rejected replacement must preserve native records/history.");
            foreach(var row in rows.Reverse())await RemoveOwned(native,"OasisHolons",row.Id,originals[row.Id]);
            foreach(var row in rows)Assert.IsNull(await Read(row.Id));File.Delete(journal);
            // Output assertions after exact owned teardown keep regressions from leaving avoidable fixtures.
            foreach(var view in projected){CollectionAssert.AreEqual(new[]{owned.Id},view.Children.Select(row=>row.Id).ToArray());Assert.AreEqual(root.ChildRoleManifestJson,view.ChildRoleManifestJson);}
        }finally{await provider.DeActivateProviderAsync();}
    }
}
