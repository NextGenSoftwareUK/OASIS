using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Azure.Cosmos;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Serialization;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.IntegrationTests;

public partial class AzureCosmosDBOASISIntegrationTests
{
    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task NativePartialReadCannotReplaceMembershipAndCompleteSaveKeepsObservation(bool synchronous)
    {
        var work=Environment.GetEnvironmentVariable("OASIS_COSMOSDB_WORK")!;
        var journal=Path.Combine(work,$"partial-graph-{synchronous}.pending.json");var evidence=Path.Combine(work,$"partial-graph-{synchronous}.cleanup.json");
        Assert.IsFalse(File.Exists(journal));Assert.IsFalse(File.Exists(evidence));
        var root=new Holon{Id=Guid.NewGuid(),Name="owned cosmos partial root"};var child=new Holon{Id=Guid.NewGuid(),ParentHolonId=root.Id,Name="owned cosmos partial child"};root.Children.Add(child);
        var ids=new[]{root.Id,child.Id};var container=_client.GetContainer(DatabaseName,"holonItems");
        async Task<(string Json,string Etag)> Read(Guid id)
        {
            using var response=await container.ReadItemStreamAsync(id.ToString(),new PartitionKey(id.ToString()));response.EnsureSuccessStatusCode();
            using var reader=new StreamReader(response.Content);return(await reader.ReadToEndAsync(),response.Headers.ETag);
        }
        foreach(var id in ids){using var absent=await container.ReadItemStreamAsync(id.ToString(),new PartitionKey(id.ToString()));Assert.AreEqual(HttpStatusCode.NotFound,absent.StatusCode);}
        using(var file=new FileStream(journal,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough))
        {await file.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(new{ids,collection="holonItems",state="before-owned-partial-graph"}));file.Flush(true);}
        _provider.IsVersionControlEnabled=true;
        var saved=synchronous?_provider.SaveHolon(root):await _provider.SaveHolonAsync(root);
        Assert.IsFalse(saved.IsError,saved.Exception?.ToString());Assert.AreEqual(2,saved.SavedCount);
        Assert.AreEqual(2,HolonGraphWritePlan.CreateForSave(new[]{root},true,true,0,0).Nodes.Count);
        var originals=new List<(Guid Id,string Json,string Etag)>();foreach(var id in ids){var snapshot=await Read(id);originals.Add((id,snapshot.Json,snapshot.Etag));}
        var loaded=synchronous?_provider.LoadHolon(root.Id,loadChildren:false):await _provider.LoadHolonAsync(root.Id,loadChildren:false);
        Assert.IsFalse(loaded.IsError,loaded.Exception?.ToString());var partial=(Holon)loaded.Result;Assert.AreEqual(0,partial.Children.Count);
        CollectionAssert.AreEquivalent(new[]{child.Id},HolonChildRoleManifestJson.Deserialize(partial.ChildRoleManifestJson).Roles.SelectMany(role=>role.ChildIds).ToArray());
        partial.AcceptChanges();var rejected=synchronous?_provider.SaveHolon(partial):await _provider.SaveHolonAsync(partial);
        Assert.IsTrue(rejected.IsError);Assert.IsInstanceOfType<InvalidOperationException>(rejected.Exception);Assert.IsFalse(rejected.IsSaved);Assert.AreEqual(0,rejected.SavedCount);
        foreach(var original in originals){var current=await Read(original.Id);Assert.AreEqual(original.Etag,current.Etag);Assert.AreEqual(original.Json,current.Json);}
        using(var file=new FileStream(evidence,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough))
        {await file.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(originals.Select(row=>new{row.Id,row.Etag,SHA256=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(row.Json)))})));file.Flush(true);}
        foreach(var original in originals)
        {
            var current=await Read(original.Id);Assert.AreEqual(original.Etag,current.Etag);Assert.AreEqual(original.Json,current.Json);
            using var deleted=await container.DeleteItemStreamAsync(original.Id.ToString(),new PartitionKey(original.Id.ToString()),new ItemRequestOptions{IfMatchEtag=original.Etag});Assert.AreEqual(HttpStatusCode.NoContent,deleted.StatusCode);
            using var absent=await container.ReadItemStreamAsync(original.Id.ToString(),new PartitionKey(original.Id.ToString()));Assert.AreEqual(HttpStatusCode.NotFound,absent.StatusCode);
        }
        File.Delete(journal);
    }
}
