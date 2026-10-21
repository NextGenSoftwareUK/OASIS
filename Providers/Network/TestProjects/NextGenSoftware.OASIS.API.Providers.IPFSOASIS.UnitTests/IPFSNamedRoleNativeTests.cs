using System.Collections.ObjectModel;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Serialization;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.IPFSOASIS.UnitTests;

public partial class IPFSOASISTests
{
    private sealed class NamedOwner:Holon
    {
        public List<Holon> Components {get;set;}=new();
        public override ReadOnlyCollection<IHolon> AllChildren=>Children.Concat<IHolon>(Components).ToList().AsReadOnly();
        public override IReadOnlyList<HolonChildCollection> CaptureChildCollections()=>base.CaptureChildCollections()
            .Concat(new[]{new HolonChildCollection(nameof(Components),Components,rows=>Components=rows.Cast<Holon>().ToList())}).ToArray();
    }

    [Theory][InlineData(false)][InlineData(true)]
    public async Task NativeNamedRolesPersistWithHistoryAndRootOnlyObservation(bool sync)
    {
        await WithOwnedProvider(async provider=>{
            var root=new NamedOwner{Id=Guid.NewGuid(),Version=1,Name="native named owner"};var child=new Holon{Id=Guid.NewGuid(),Version=1,Name="native named child"};root.Components.Add(child);
            var expected=HolonChildRoleManifestJson.Serialize(HolonChildCollectionState.Capture(root).CreateManifest(root.Id,row=>row.Id));
            var saved=sync?provider.SaveHolon(root):await provider.SaveHolonAsync(root);Assert.False(saved.IsError,saved.Exception?.ToString());
            var loaded=await provider.LoadHolonAsync(root.Id,loadChildren:false);Assert.False(loaded.IsError,loaded.Message);Assert.Equal(expected,((Holon)loaded.Result).ChildRoleManifestJson);
            Assert.Equal(expected,root.ChildRoleManifestJson);Assert.Equal(root.Id,child.ParentHolonId);
            var childBefore=child.ProviderUniqueStorageKey[Core.Enums.ProviderType.IPFSOASIS];
            var scalar=new Holon{Id=root.Id,Version=2,Name="native scalar update"};var updated=sync?provider.SaveHolon(scalar,saveChildren:false):await provider.SaveHolonAsync(scalar,saveChildren:false);Assert.False(updated.IsError,updated.Exception?.ToString());
            Assert.Equal(expected,scalar.ChildRoleManifestJson);
            var historical=await provider.LoadHolonAsync(root.Id,loadChildren:false,version:1);Assert.False(historical.IsError,historical.Message);Assert.Equal(expected,((Holon)historical.Result).ChildRoleManifestJson);
            var independent=await provider.LoadHolonAsync(child.Id,loadChildren:false);Assert.False(independent.IsError,independent.Message);Assert.Equal(childBefore,independent.Result.ProviderUniqueStorageKey[Core.Enums.ProviderType.IPFSOASIS]);
            root.Version=3;root.Components.Clear();var unlink=sync?provider.SaveHolon(root):await provider.SaveHolonAsync(root);Assert.False(unlink.IsError,unlink.Exception?.ToString());
            var empty=sync?provider.LoadHolon(root.Id,loadChildrenFromProvider:true):await provider.LoadHolonAsync(root.Id,loadChildrenFromProvider:true);Assert.False(empty.IsError,empty.Message);Assert.Empty(empty.Result.Children);
            var all=await provider.LoadAllHolonsAsync(Core.Enums.HolonType.All,loadChildrenFromProvider:true);Assert.False(all.IsError,all.Message);Assert.Empty(all.Result.Single(row=>row.Id==root.Id).Children);Assert.Contains(all.Result,row=>row.Id==child.Id);
            Assert.Equal(childBefore,(await provider.LoadHolonAsync(child.Id,loadChildren:false)).Result.ProviderUniqueStorageKey[Core.Enums.ProviderType.IPFSOASIS]);
            var legacy=new Holon{Id=Guid.NewGuid(),Name="legacy null"};await provider.SaveHolonToFile(legacy);Assert.Null(legacy.ChildRoleManifestJson);
            legacy.ChildRoleManifestJson=HolonChildRoleManifestJson.Serialize(new HolonChildRoleManifest(1,legacy.Id,new[]{new HolonChildRole("Children",new[]{Guid.NewGuid()})}));
            await provider.SaveHolonToFile(legacy);Assert.Null(legacy.ChildRoleManifestJson);
            Assert.Null(((Holon)(await provider.LoadHolonAsync(legacy.Id,loadChildren:false)).Result).ChildRoleManifestJson);
        });
    }
}
