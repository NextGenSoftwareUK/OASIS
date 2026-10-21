using System.Collections.ObjectModel;
using System.Net;
using System.Text.Json;
using Microsoft.Azure.Cosmos;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Serialization;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.IntegrationTests;

public partial class AzureCosmosDBOASISIntegrationTests
{
    private sealed class NamedRoleOwner:Holon
    {
        public List<Holon> Components {get;set;}=new();
        public override ReadOnlyCollection<IHolon> AllChildren=>Children.Concat<IHolon>(Components).ToList().AsReadOnly();
        public override IReadOnlyList<HolonChildCollection> CaptureChildCollections()=>base.CaptureChildCollections()
            .Concat(new[]{new HolonChildCollection(nameof(Components),Components,rows=>Components=rows.Cast<Holon>().ToList())}).ToArray();
    }

    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task NativeNamedRolesRetainObservedMembershipIncludingLegacyNull(bool sync)
    {
        var root=new NamedRoleOwner{Id=Guid.NewGuid(),Name="owned cosmos named owner"};
        var child=new Holon{Id=Guid.NewGuid(),Name="owned cosmos named child"};root.Components.Add(child);
        var expected=HolonChildRoleManifestJson.Serialize(HolonChildCollectionState.Capture(root).CreateManifest(root.Id,row=>row.Id));
        var journal=$"C:/Source/OASIS/TestResults/CosmosDBCurrent/named-roles-{sync}.pending.json";
        Assert.IsFalse(File.Exists(journal));var container=_client.GetContainer(DatabaseName,"holonItems");
        async Task<(string Json,string Etag)> Read(Guid id)
        {
            using var response=await container.ReadItemStreamAsync(id.ToString(),new PartitionKey(id.ToString()));response.EnsureSuccessStatusCode();
            using var reader=new StreamReader(response.Content);return(await reader.ReadToEndAsync(),response.Headers.ETag);
        }
        foreach(var id in new[]{root.Id,child.Id}){using var absent=await container.ReadItemStreamAsync(id.ToString(),new PartitionKey(id.ToString()));Assert.AreEqual(HttpStatusCode.NotFound,absent.StatusCode);}
        using(var file=new FileStream(journal,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough))
        {await file.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(new{ids=new[]{root.Id,child.Id},collection="holonItems",state="before-owned-named-role-graph"}));file.Flush(true);}
        _provider.IsVersionControlEnabled=true;
        var saved=sync?_provider.SaveHolon(root):await _provider.SaveHolonAsync(root);Assert.IsFalse(saved.IsError,saved.Exception?.ToString());
        var initial=await Read(root.Id);var childBefore=await Read(child.Id);var published=root.ChildRoleManifestJson;
        var scalar=new Holon{Id=root.Id,Name="owned cosmos scalar update"};
        var updated=sync?_provider.SaveHolon(scalar,saveChildren:false):await _provider.SaveHolonAsync(scalar,saveChildren:false);Assert.IsFalse(updated.IsError,updated.Exception?.ToString());
        var current=await Read(root.Id);var preserved=JObject.Parse(current.Json)[nameof(Holon.ChildRoleManifestJson)]?.Value<string>();
        var history=await _provider.LoadHolonAsync(root.Id,loadChildren:false,version:1);Assert.IsFalse(history.IsError,history.Exception?.ToString());
        using(var patched=await container.PatchItemStreamAsync(root.Id.ToString(),new PartitionKey(root.Id.ToString()),
            new[]{PatchOperation.Set<string?>("/ChildRoleManifestJson",null)},new PatchItemRequestOptions{IfMatchEtag=current.Etag}))patched.EnsureSuccessStatusCode();
        scalar.Name="owned cosmos legacy update";scalar.ChildRoleManifestJson=expected;
        var legacySaved=sync?_provider.SaveHolon(scalar,saveChildren:false):await _provider.SaveHolonAsync(scalar,saveChildren:false);Assert.IsFalse(legacySaved.IsError,legacySaved.Exception?.ToString());
        current=await Read(root.Id);var legacyManifest=JObject.Parse(current.Json)[nameof(Holon.ChildRoleManifestJson)]?.Value<string>();var childAfter=await Read(child.Id);
        Assert.AreEqual(childBefore.Json,childAfter.Json);Assert.AreEqual(childBefore.Etag,childAfter.Etag);
        foreach(var pair in new[]{(root.Id,current.Etag),(child.Id,childAfter.Etag)})
        {
            using var deleted=await container.DeleteItemStreamAsync(pair.Id.ToString(),new PartitionKey(pair.Id.ToString()),new ItemRequestOptions{IfMatchEtag=pair.Etag});Assert.AreEqual(HttpStatusCode.NoContent,deleted.StatusCode);
            using var absent=await container.ReadItemStreamAsync(pair.Id.ToString(),new PartitionKey(pair.Id.ToString()));Assert.AreEqual(HttpStatusCode.NotFound,absent.StatusCode);
        }
        await container.ReadContainerAsync();File.Delete(journal);
        Assert.AreEqual(expected,JObject.Parse(initial.Json)[nameof(Holon.ChildRoleManifestJson)]?.Value<string>());
        Assert.AreEqual(expected,published);Assert.AreEqual(expected,preserved);Assert.AreEqual(expected,((Holon)history.Result).ChildRoleManifestJson);
        Assert.IsNull(legacyManifest);Assert.IsNull(scalar.ChildRoleManifestJson);
    }
}
