using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Holons;
using Provider=NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Neo4jOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public partial class Neo4jNativeAvatarTests
{
    [DataTestMethod][DataRow(false,false)][DataRow(false,true)][DataRow(true,false)][DataRow(true,true)]
    public async Task NativeExportsResolveCurrentIdentityAndEnumerateIndependentStoredRootVersions(bool sync,bool email)
    {
        const string uri="bolt://127.0.0.1:17687";Assert.AreEqual(uri,Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_URI"));
        var work=Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_WORK")!;
        Assert.AreEqual(0,Directory.GetFiles(work,"neo-export-*.pending.json").Length,"Reconcile original export fixtures before another case.");
        var stem=Path.Combine(work,$"neo-export-{sync}-{email}");var pending=stem+".pending.json";var observed=stem+".observed.json";Assert.IsFalse(File.Exists(observed));
        using var raw=GraphDatabase.Driver(uri,AuthTokens.Basic("neo4j","oasis-local-test-only"));using var check=raw.AsyncSession();
        Assert.AreEqual(0L,(await(await check.RunAsync("MATCH (n) WHERE n:Holon OR n:Avatar RETURN count(n) AS count")).SingleAsync())["count"].As<long>());
        var marker=Guid.NewGuid().ToString("N");var firstName="export-first-"+marker;var currentName="export-current-"+marker;
        var avatar=new Avatar{Id=Guid.NewGuid(),Username=firstName,Email=firstName+"@example.invalid",IsActive=true};
        var own=new Holon{Id=Guid.NewGuid(),CreatedByAvatarId=avatar.Id,IsActive=true};var foreign=new Holon{Id=Guid.NewGuid(),CreatedByAvatarId=Guid.NewGuid(),IsActive=true};
        var ids=new[]{avatar.Id.ToString(),own.Id.ToString(),foreign.Id.ToString()};
        static async Task WriteNew(string path,object value){using var file=new FileStream(path,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough);await file.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(value));file.Flush(true);}
        await WriteNew(pending,new{uri,ids,stage="before-owned-export-create"});
        var provider=new Provider(uri,"neo4j","oasis-local-test-only"){IsVersionControlEnabled=true};
        async Task<string> Graph(){var found=await(await check.RunAsync("MATCH (n) WHERE n.Id IN $ids OR n.RecordId IN $ids RETURN n ORDER BY id(n)",new{ids})).ToListAsync();return JsonSerializer.Serialize(found.Select(row=>row["n"].As<INode>()).Select(node=>new{id=node.Id,fields=new SortedDictionary<string,object>(node.Properties.ToDictionary(pair=>pair.Key,pair=>pair.Value),StringComparer.Ordinal)}));}
        try{
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
            Assert.IsFalse((await provider.SaveAvatarAsync(avatar)).IsError);avatar.Username=currentName;avatar.Email=currentName+"@example.invalid";
            Assert.IsFalse((await provider.SaveAvatarAsync(avatar)).IsError);Assert.AreEqual(2,avatar.Version);
            for(var version=1;version<=3;version++){
                own.Name="own-v"+version;foreign.Name="foreign-v"+version;
                Assert.IsFalse((await provider.SaveHolonAsync(own,saveChildren:false)).IsError);Assert.IsFalse((await provider.SaveHolonAsync(foreign,saveChildren:false)).IsError);
            }
            Assert.AreEqual(3,own.Version);var before=await Graph();await WriteNew(observed,new{sha256=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(before)))});
            foreach(var version in new[]{-1,0,1,2,3,4}){
                var result=email?sync?provider.ExportAllDataForAvatarByEmail(avatar.Email,version):await provider.ExportAllDataForAvatarByEmailAsync(avatar.Email,version)
                    :sync?provider.ExportAllDataForAvatarByUsername(avatar.Username,version):await provider.ExportAllDataForAvatarByUsernameAsync(avatar.Username,version);
                Assert.IsFalse(result.IsError,result.Exception?.ToString());Assert.IsTrue(result.IsLoaded);
                var expected=version==-1?new[]{1,2,3}:version==4?Array.Empty<int>():new[]{version==0?3:version};
                CollectionAssert.AreEqual(expected,result.Result.Select(row=>row.Version).Order().ToArray());Assert.AreEqual(expected.Length,result.LoadedCount);
                foreach(var row in result.Result){Assert.AreEqual(own.Id,row.Id);Assert.AreEqual("own-v"+row.Version,row.Name);}
            }
            var all=sync?provider.ExportAll(-1):await provider.ExportAllAsync(-1);Assert.IsFalse(all.IsError,all.Exception?.ToString());Assert.AreEqual(6,all.LoadedCount);
            CollectionAssert.AreEquivalent(new[]{1,2,3},all.Result.Where(row=>row.Id==foreign.Id).Select(row=>row.Version).ToArray());
            var retired=email?await provider.ExportAllDataForAvatarByEmailAsync(firstName+"@example.invalid",1):await provider.ExportAllDataForAvatarByUsernameAsync(firstName,1);
            Assert.IsTrue(retired.IsError,"Retired historical aliases are not current export identity selectors.");
            Assert.AreEqual(firstName,(await provider.LoadAvatarAsync(avatar.Id,1)).Result.Username,"Explicit avatar history remains available separately.");
            Assert.AreEqual(before,await Graph(),"Export reads cannot rewrite native snapshots or archives.");
            Assert.IsFalse((await provider.DeleteHolonAsync(own.Id)).IsError);Assert.IsFalse((await provider.DeleteHolonAsync(foreign.Id)).IsError);
            Assert.IsFalse((await provider.DeleteAvatarAsync(avatar.Id,false)).IsError);
            Assert.AreEqual("[]",await Graph());File.Delete(observed);File.Delete(pending);
        }finally{await provider.DeActivateProviderAsync();}
    }
}
