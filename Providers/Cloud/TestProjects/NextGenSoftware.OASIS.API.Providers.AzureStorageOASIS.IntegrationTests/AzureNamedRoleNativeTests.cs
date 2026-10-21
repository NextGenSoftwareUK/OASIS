using System.Collections.ObjectModel;
using Azure.Storage.Blobs.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Serialization;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.AzureStorageOASIS.IntegrationTests;

public partial class AzureStorageOASISIntegrationTests
{
    private sealed class NamedRoleOwner:Holon
    {
        public List<Holon> Components {get;set;}=new();
        public override ReadOnlyCollection<IHolon> AllChildren=>Children.Concat<IHolon>(Components).ToList().AsReadOnly();
        public override IReadOnlyList<HolonChildCollection> CaptureChildCollections()=>base.CaptureChildCollections()
            .Concat(new[]{new HolonChildCollection(nameof(Components),Components,rows=>Components=rows.Cast<Holon>().ToList())}).ToArray();
    }

    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task NativeNamedRoleGraphHistoryAndRootOnlyObservation(bool sync)
    {
        RequireLive();_provider.IsVersionControlEnabled=true;
        var root=new NamedRoleOwner{Id=Guid.NewGuid(),Name="owned named owner"};var child=new Holon{Id=Guid.NewGuid(),Name="owned named child"};root.Components.Add(child);
        var expected=HolonChildRoleManifestJson.Serialize(HolonChildCollectionState.Capture(root).CreateManifest(root.Id,row=>row.Id));
        var saved=sync?_provider.SaveHolon(root):await _provider.SaveHolonAsync(root);Assert.IsFalse(saved.IsError,saved.Exception?.ToString());
        var container=_service.GetBlobContainerClient(_containers[0]);var rootBlob=container.GetBlobClient(root.ProviderUniqueStorageKey[Core.Enums.ProviderType.AzureStorageOASIS]);
        var childBlob=container.GetBlobClient(child.ProviderUniqueStorageKey[Core.Enums.ProviderType.AzureStorageOASIS]);
        var initial=(await rootBlob.DownloadContentAsync()).Value;var childBefore=(await childBlob.DownloadContentAsync()).Value;
        var selected=OasisJson.DeserializeStored<Holon>(initial.Content.ToString())!.ChildRoleManifestJson;
        var scalar=new Holon{Id=root.Id,Name="owned scalar update"};var updated=sync?_provider.SaveHolon(scalar,saveChildren:false):await _provider.SaveHolonAsync(scalar,saveChildren:false);Assert.IsFalse(updated.IsError,updated.Exception?.ToString());
        var current=(await rootBlob.DownloadContentAsync()).Value;var observed=OasisJson.DeserializeStored<Holon>(current.Content.ToString())!;
        var historical=await _provider.LoadHolonAsync(root.Id,loadChildren:false,version:1);Assert.IsFalse(historical.IsError,historical.Message);
        var legacy=Newtonsoft.Json.Linq.JObject.Parse(current.Content.ToString());legacy[nameof(Holon.ChildRoleManifestJson)]=Newtonsoft.Json.Linq.JValue.CreateNull();
        await rootBlob.UploadAsync(BinaryData.FromString(legacy.ToString(Newtonsoft.Json.Formatting.None)),new BlobUploadOptions{Conditions=new BlobRequestConditions{IfMatch=current.Details.ETag}});
        scalar.Name="owned legacy update";scalar.ChildRoleManifestJson=expected;
        var savedLegacy=sync?_provider.SaveHolon(scalar,saveChildren:false):await _provider.SaveHolonAsync(scalar,saveChildren:false);Assert.IsFalse(savedLegacy.IsError,savedLegacy.Exception?.ToString());
        current=(await rootBlob.DownloadContentAsync()).Value;var observedLegacy=OasisJson.DeserializeStored<Holon>(current.Content.ToString())!;
        var childAfter=(await childBlob.DownloadContentAsync()).Value;Assert.AreEqual(childBefore.Content.ToString(),childAfter.Content.ToString());Assert.AreEqual(childBefore.Details.ETag,childAfter.Details.ETag);
        // Independently validated successful originals are conditionally cleaned BEFORE
        // role assertions, so a reproduced mapping defect leaves no test records.
        foreach(var row in new[]{(Blob:rootBlob,Observed:current),(Blob:childBlob,Observed:childAfter)}){
            var deleted=await row.Blob.DeleteAsync(DeleteSnapshotsOption.IncludeSnapshots,new BlobRequestConditions{IfMatch=row.Observed.Details.ETag});Assert.IsTrue(deleted.Status>=200&&deleted.Status<300);Assert.IsFalse((await row.Blob.ExistsAsync()).Value);
        }
        Assert.AreEqual(expected,selected);Assert.AreEqual(expected,observed.ChildRoleManifestJson);Assert.AreEqual(expected,((Holon)historical.Result).ChildRoleManifestJson);
        Assert.IsNull(observedLegacy.ChildRoleManifestJson,"Observed persisted null must override stale caller roles.");Assert.IsNull(scalar.ChildRoleManifestJson);
    }
}
