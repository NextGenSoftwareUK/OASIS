using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.SOLIDOASIS.UnitTests;

public class SolidGraphWriteTests
{
    private sealed class NamedOwner:Holon
    {
        public List<Holon> Components {get;set;}=new();
        public override System.Collections.ObjectModel.ReadOnlyCollection<IHolon> AllChildren=>Children.Concat<IHolon>(Components).ToList().AsReadOnly();
        public override IReadOnlyList<HolonChildCollection> CaptureChildCollections()=>base.CaptureChildCollections()
            .Concat(new[]{new HolonChildCollection(nameof(Components),Components,rows=>Components=rows.Cast<Holon>().ToList())}).ToArray();
    }

    [Theory][InlineData(false)][InlineData(true)]
    public async Task CapturedNamedRolesPersistAndRootOnlyPreservesObservedNullAndHistory(bool sync)
    {
        var transport=new SolidRecordHttpTests.Transport();using var provider=new SOLIDOASIS(Root.AbsoluteUri,"",Key(),transport){IsVersionControlEnabled=true};
        Assert.False((await provider.ActivateProviderAsync()).IsError);
        var owner=new NamedOwner{Id=Guid.NewGuid(),Name="owned named owner"};var child=new Holon{Id=Guid.NewGuid(),Name="owned named child"};owner.Components.Add(child);
        var expected=NextGenSoftware.OASIS.API.Core.Serialization.HolonChildRoleManifestJson.Serialize(HolonChildCollectionState.Capture(owner).CreateManifest(owner.Id,row=>row.Id));
        var saved=sync?provider.SaveHolon(owner):await provider.SaveHolonAsync(owner);Assert.False(saved.IsError,saved.Exception?.ToString());
        using var codec=new SolidPrivateRecordCodec(Root,Key());var rootKey=codec.Resource<IHolon>(owner.Id).AbsoluteUri;var childKey=codec.Resource<IHolon>(child.Id).AbsoluteUri;
        var initial=((Holon)codec.Decode<IHolon>(transport.Records[rootKey],owner.Id)).ChildRoleManifestJson;var published=owner.ChildRoleManifestJson;var childBefore=transport.Records[childKey];
        transport.RequireConditionalWrites=true;
        var scalar=new Holon{Id=owner.Id,Name="owned scalar update"};var updated=sync?provider.SaveHolon(scalar,saveChildren:false):await provider.SaveHolonAsync(scalar,saveChildren:false);Assert.False(updated.IsError,updated.Exception?.ToString());
        var preserved=((Holon)codec.Decode<IHolon>(transport.Records[rootKey],owner.Id)).ChildRoleManifestJson;
        var history=await codec.DecodeVersionAsync<IHolon>(transport.Records[rootKey],owner.Id,1);
        var legacy=(Holon)codec.Decode<IHolon>(transport.Records[rootKey],owner.Id);legacy.ChildRoleManifestJson=null;
        transport.Records[rootKey]=(await codec.StageVersionAsync<IHolon>(legacy,transport.Records[rootKey])).Turtle;
        scalar.ChildRoleManifestJson=expected;var legacySaved=sync?provider.SaveHolon(scalar,saveChildren:false):await provider.SaveHolonAsync(scalar,saveChildren:false);Assert.False(legacySaved.IsError,legacySaved.Exception?.ToString());
        var legacyAfter=(Holon)codec.Decode<IHolon>(transport.Records[rootKey],owner.Id);
        Assert.Equal(childBefore,transport.Records[childKey]);Assert.Equal(4,transport.Writes);
        Assert.Equal(expected,initial);Assert.Equal(expected,published);Assert.Equal(expected,preserved);Assert.Equal(expected,((Holon)history).ChildRoleManifestJson);
        Assert.Null(legacyAfter.ChildRoleManifestJson);Assert.Null(scalar.ChildRoleManifestJson);
    }

    private static readonly Uri Root=new("https://pod.invalid/private/");
    private static byte[] Key()=>Enumerable.Range(1,32).Select(x=>(byte)x).ToArray();
    [Theory][InlineData(false)][InlineData(true)]
    public async Task CallerEditDuringReadbackCannotBeOverwrittenOrWrittenFromStaleGraph(bool rootEdit)
    {
        var gate=new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var transport=new SolidRecordHttpTests.Transport{ReadGate=gate};using var provider=new SOLIDOASIS(Root.AbsoluteUri,"",Key(),transport);
        Assert.False((await provider.ActivateProviderAsync()).IsError);
        var child=new Holon{Name="owned-child"};var root=new Holon{Name="owned-root",Children=new List<IHolon>{child}};
        var pending=provider.SaveHolonAsync(root);Assert.Equal(1,transport.Writes);
        (rootEdit?root:child).Description="newer local edit";gate.SetResult(true);
        var result=await pending.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.True(result.IsError);Assert.False(result.IsSaved);Assert.Null(result.Result);
        var evidence=Assert.IsType<KeyValuePartialMutationException>(result.Exception);Assert.Single(evidence.AcknowledgedKeys);
        Assert.Equal("newer local edit",(rootEdit?root:child).Description);Assert.Equal(1,transport.Writes);Assert.Equal(Guid.Empty,child.Id);
        if(rootEdit)Assert.Equal(Guid.Empty,root.Id);else Assert.NotEqual(Guid.Empty,root.Id);
    }
    [Theory]
    [InlineData(false,true,0,1,false)][InlineData(false,true,0,1,true)]
    [InlineData(true,false,0,2,false)][InlineData(true,false,0,2,true)]
    [InlineData(true,true,1,2,false)][InlineData(true,true,1,2,true)]
    [InlineData(true,true,0,3,false)][InlineData(true,true,0,3,true)]
    public async Task SelectedGraphNodesAreEncryptedAndPublishOnlyVerifiedIdentities(bool children,bool recursive,int depth,int expected,bool synchronous)
    {
        var transport=new SolidRecordHttpTests.Transport();using var provider=new SOLIDOASIS(Root.AbsoluteUri,"",Key(),transport);Assert.False((await provider.ActivateProviderAsync()).IsError);
        var grand=new Holon{Name="private-canary-grand"};var child=new Holon{Name="private-canary-child",Children=new List<IHolon>{grand}};
        IHolon root=new Holon{Name="private-canary-root",Children=new List<IHolon>{child}};
        var result=synchronous?provider.SaveHolon(root,children,recursive,depth):await provider.SaveHolonAsync(root,children,recursive,depth);
        Assert.False(result.IsError,result.Exception?.ToString());Assert.True(result.IsSaved);Assert.Equal(expected,transport.Writes);Assert.Equal(expected,transport.Records.Count);
        Assert.NotEqual(Guid.Empty,root.Id);
        if(expected>1){Assert.NotEqual(Guid.Empty,child.Id);Assert.Equal(root.Id,child.ParentHolonId);}else Assert.Equal(Guid.Empty,child.Id);
        if(expected>2){Assert.NotEqual(Guid.Empty,grand.Id);Assert.Equal(child.Id,grand.ParentHolonId);}else Assert.Equal(Guid.Empty,grand.Id);
        using var codec=new SolidPrivateRecordCodec(Root,Key());foreach(var source in new[]{root,child,grand}.Where(row=>row.Id!=Guid.Empty)){
            var record=codec.Decode<IHolon>(transport.Records[codec.Resource<IHolon>(source.Id).AbsoluteUri],source.Id);
            Assert.Equal(source.ParentHolonId,record.ParentHolonId);
            Assert.Equal(source.Id.ToString(),record.ProviderUniqueStorageKey[ProviderType.SOLIDOASIS]);
        }
    }
    [Fact]
    public async Task BatchValidatesAllRootsBeforeAnyWrite()
    {
        var transport=new SolidRecordHttpTests.Transport();using var provider=new SOLIDOASIS(Root.AbsoluteUri,"",Key(),transport);Assert.False((await provider.ActivateProviderAsync()).IsError);
        IHolon root=new Holon();var result=await provider.SaveHolonsAsync(new[]{root,null!},saveChildren:false);
        Assert.True(result.IsError);Assert.Null(result.Result);Assert.Equal(0,transport.Writes);Assert.Equal(Guid.Empty,root.Id);
    }
    [Fact]
    public async Task BatchRespectsInitialDepthAndPublishesRootAcknowledgement()
    {
        var transport=new SolidRecordHttpTests.Transport();using var provider=new SOLIDOASIS(Root.AbsoluteUri,"",Key(),transport);Assert.False((await provider.ActivateProviderAsync()).IsError);
        IHolon child=new Holon();IHolon root=new Holon{Children=new List<IHolon>{child}};
        var result=await provider.SaveHolonsAsync(new[]{root},maxChildDepth:1,curentChildDepth:1);
        Assert.False(result.IsError,result.Message);Assert.True(result.IsSaved);Assert.Same(root,Assert.Single(result.Result));
        Assert.Equal(1,transport.Writes);Assert.NotEqual(Guid.Empty,root.Id);Assert.Equal(Guid.Empty,child.Id);
    }
    [Fact]
    public async Task CyclicSelectedGraphFailsBeforeAnyWriteOrIdentityPublication()
    {
        var transport=new SolidRecordHttpTests.Transport();using var provider=new SOLIDOASIS(Root.AbsoluteUri,"",Key(),transport);Assert.False((await provider.ActivateProviderAsync()).IsError);
        var root=new Holon();root.Children=new List<IHolon>{root};
        var result=await provider.SaveHolonAsync(root);Assert.True(result.IsError);Assert.Equal(0,transport.Writes);Assert.Equal(Guid.Empty,root.Id);
    }
    [Theory][InlineData(false)][InlineData(true)]
    public async Task LaterFailureRetainsAcknowledgedOriginalsAndStopsGraph(bool uncertain)
    {
        var transport=new SolidRecordHttpTests.Transport{RejectWrite=uncertain?0:2,UncertainWrite=uncertain?2:0};
        using var provider=new SOLIDOASIS(Root.AbsoluteUri,"",Key(),transport);Assert.False((await provider.ActivateProviderAsync()).IsError);
        var child=new Holon();IHolon root=new Holon{Children=new List<IHolon>{child}};
        var result=await provider.SaveHolonAsync(root);Assert.True(result.IsError);Assert.Null(result.Result);
        var error=Assert.IsType<KeyValuePartialMutationException>(result.Exception);Assert.Single(error.AcknowledgedKeys);
        Assert.NotEqual(Guid.Empty,root.Id);Assert.Equal(Guid.Empty,child.Id);Assert.Equal(2,transport.Writes);
        Assert.IsAssignableFrom<INativeMutationReconciliationRequired>(error);
    }
    [Fact]
    public async Task UncertainFirstRecordCarriesTheNativeReconciliationMarker()
    {
        var transport=new SolidRecordHttpTests.Transport{UncertainWrite=1};using var provider=new SOLIDOASIS(Root.AbsoluteUri,"",Key(),transport);Assert.False((await provider.ActivateProviderAsync()).IsError);
        var result=await provider.SaveHolonAsync(new Holon(),saveChildren:false);Assert.True(result.IsError);
        Assert.IsAssignableFrom<INativeMutationReconciliationRequired>(result.Exception);
    }
}
