using System.Net;
using System.Reflection;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Serialization;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.DynamoDBOASIS.UnitTests;

[TestClass]
public sealed class DynamoScopedGraphVisibilityTests
{
    private sealed class Probe(Holon[] rows):AmazonDynamoDBClient(new BasicAWSCredentials("owned","owned"),new AmazonDynamoDBConfig{ServiceURL="http://127.0.0.1:18001",AuthenticationRegion="us-east-1",MaxErrorRetry=0})
    {
        internal int Gets;
        public override Task<ScanResponse> ScanAsync(ScanRequest request,CancellationToken token=default)
            =>Task.FromResult(new ScanResponse{HttpStatusCode=HttpStatusCode.OK,Items=request.TableName=="OasisAvatars"?new():rows.Select(row=>new Dictionary<string,AttributeValue>{{"id",new(){S=row.Id.ToString()}},{"data_json",new(){S=OasisJson.Serialize(row)}}}).ToList()});
        public override Task<GetItemResponse> GetItemAsync(GetItemRequest request,CancellationToken token=default){Gets++;throw new InvalidOperationException("No mutation admission SDK read is allowed.");}
    }

    public static IEnumerable<object[]> Cases(){foreach(var sync in new[]{false,true})foreach(var version in new[]{0,1})foreach(var path in new[]{"search","export","flat-search"})yield return new object[]{sync,version,path};}
    [DataTestMethod][DynamicData(nameof(Cases),DynamicDataSourceType.Method)]
    public async Task ScopedQueriesCannotExposeOrReplaceForeignBranches(bool synchronous,int version,string path)
    {
        var actor=Guid.NewGuid();var root=new Holon{Id=Guid.NewGuid(),CreatedByAvatarId=actor,Version=1,MetaData=new(){{"scope","owned-root"}}};
        var owned=new Holon{Id=Guid.NewGuid(),ParentHolonId=root.Id,CreatedByAvatarId=actor,Version=7};
        var hidden=new Holon{Id=Guid.NewGuid(),ParentHolonId=root.Id,CreatedByAvatarId=Guid.NewGuid(),Version=8};
        var foreignPublic=new Holon{Id=Guid.NewGuid(),ParentHolonId=root.Id,CreatedByAvatarId=Guid.NewGuid(),IsPublic=true,Version=9};
        var leaf=new Holon{Id=Guid.NewGuid(),ParentHolonId=hidden.Id,CreatedByAvatarId=actor,Version=10};
        root.Children.Add(owned);root.Children.Add(hidden);root.Children.Add(foreignPublic);hidden.Children.Add(leaf);
        root.ChildRoleManifestJson=HolonChildRoleManifestJson.Serialize(HolonChildCollectionState.Capture(root).CreateManifest(root.Id,row=>row.Id));
        var rows=new[]{root,owned,hidden,foreignPublic,leaf};var originals=rows.Select(OasisJson.Serialize).ToArray();
        var provider=new DynamoDBOASIS("owned","owned","us-east-1");
        var field=typeof(DynamoDBOASIS).GetField("_client",BindingFlags.Instance|BindingFlags.NonPublic)!;var original=(AmazonDynamoDBClient)field.GetValue(provider)!;
        using var probe=new Probe(rows);field.SetValue(provider,probe);
        try{
            Holon selected;
            if(path=="export"){
                var result=synchronous?provider.ExportAllDataForAvatarById(actor,version):await provider.ExportAllDataForAvatarByIdAsync(actor,version);
                Assert.IsFalse(result.IsError,result.Exception?.ToString());selected=(Holon)result.Result.Single(row=>row.Id==root.Id);
            }else{
                var query=new SearchParams{SearchOnlyForCurrentAvatar=true,AvatarId=actor,FilterByMetaData=new(){{"scope","owned-root"}}};
                var result=synchronous?provider.Search(query,loadChildren:path!="flat-search",version:version):await provider.SearchAsync(query,loadChildren:path!="flat-search",version:version);
                Assert.IsFalse(result.IsError,result.Exception?.ToString());Assert.AreEqual(1,result.LoadedCount);selected=(Holon)result.Result.SearchResultHolons.Single();
            }
            CollectionAssert.AreEqual(path=="flat-search"?Array.Empty<Guid>():new[]{owned.Id},selected.Children.Select(row=>row.Id).ToArray());
            if(path!="flat-search"){
                Assert.AreEqual(7,selected.Children.Single().Version);Assert.AreEqual(root.ChildRoleManifestJson,selected.ChildRoleManifestJson);
                selected.AcceptChanges();var rejected=await provider.SaveHolonAsync(selected);
                Assert.IsTrue(rejected.IsError);Assert.IsInstanceOfType<InvalidOperationException>(rejected.Exception);Assert.AreEqual(0,probe.Gets);
            }
            CollectionAssert.AreEqual(originals,rows.Select(OasisJson.Serialize).ToArray());Assert.AreEqual(3,root.Children.Count);Assert.AreEqual(1,hidden.Children.Count);
        }finally{field.SetValue(provider,original);await provider.DeActivateProviderAsync();}
    }
}
