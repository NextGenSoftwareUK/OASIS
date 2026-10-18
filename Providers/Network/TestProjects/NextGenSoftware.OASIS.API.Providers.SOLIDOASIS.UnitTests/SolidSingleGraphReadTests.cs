using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.SOLIDOASIS.UnitTests;

public class SolidSingleGraphReadTests
{
    private static readonly Uri Root=new("https://pod.invalid/private/");
    private static byte[] Key()=>Enumerable.Range(1,32).Select(x=>(byte)x).ToArray();
    [Theory]
    [InlineData(false,true,true,0,0,false)][InlineData(false,true,true,0,0,true)]
    [InlineData(true,false,true,0,0,false)][InlineData(true,false,true,0,0,true)]
    [InlineData(true,true,true,0,2,false)][InlineData(true,true,true,0,2,true)]
    [InlineData(true,true,false,0,1,false)][InlineData(true,true,false,0,1,true)]
    [InlineData(true,true,true,1,1,false)][InlineData(true,true,true,1,1,true)]
    public async Task SavedGraphRoundTripsRequestedSingleRootProjection(bool children,bool providerChildren,bool recursive,int depth,int levels,bool synchronous)
    {
        var transport=new SolidRecordHttpTests.Transport();using var provider=new SOLIDOASIS(Root.AbsoluteUri,"",Key(),transport);Assert.False((await provider.ActivateProviderAsync()).IsError);
        IHolon grand=new Holon{Name="grand"};IHolon child=new Holon{Name="child",Children=new List<IHolon>{grand}};IHolon root=new Holon{Name="root",Children=new List<IHolon>{child}};
        Assert.False((await provider.SaveHolonAsync(root)).IsError);var before=transport.Reads;
        var result=synchronous?provider.LoadHolon(root.Id.ToString("N"),children,recursive,depth,loadChildrenFromProvider:providerChildren)
            :await provider.LoadHolonAsync(root.Id,children,recursive,depth,loadChildrenFromProvider:providerChildren);
        Assert.False(result.IsError,result.Exception?.ToString());Assert.Equal(root.Id,result.Result.Id);
        if(levels==0){Assert.Empty(result.Result.Children);Assert.Equal(before+1,transport.Reads);}
        else{
            var loadedChild=Assert.Single(result.Result.Children);Assert.Equal(child.Id,loadedChild.Id);
            if(levels==2)Assert.Equal(grand.Id,Assert.Single(loadedChild.Children).Id);else Assert.Empty(loadedChild.Children);
        }
    }
    [Fact]
    public async Task CorruptChildDoesNotPublishPointRootAsSuccessfulExpandedGraph()
    {
        var transport=new SolidRecordHttpTests.Transport();using var provider=new SOLIDOASIS(Root.AbsoluteUri,"",Key(),transport);Assert.False((await provider.ActivateProviderAsync()).IsError);
        IHolon child=new Holon();IHolon root=new Holon{Children=new List<IHolon>{child}};Assert.False((await provider.SaveHolonAsync(root)).IsError);
        using(var codec=new SolidPrivateRecordCodec(Root,Key()))transport.Records[codec.Resource<IHolon>(child.Id).AbsoluteUri]="invalid RDF";
        var result=await provider.LoadHolonAsync(root.Id,loadChildrenFromProvider:true);Assert.True(result.IsError);Assert.Null(result.Result);
    }
    [Fact]
    public async Task PersistedParentCycleFailsWithOriginalGraphDiagnostic()
    {
        var transport=new SolidRecordHttpTests.Transport();var root=Guid.NewGuid();var child=Guid.NewGuid();
        using(var codec=new SolidPrivateRecordCodec(Root,Key()))foreach(IHolon row in new IHolon[]{new Holon{Id=root,ParentHolonId=child},new Holon{Id=child,ParentHolonId=root}})
            transport.Records[codec.Resource<IHolon>(row.Id).AbsoluteUri]=codec.Encode(row);
        using var provider=new SOLIDOASIS(Root.AbsoluteUri,"",Key(),transport);Assert.False((await provider.ActivateProviderAsync()).IsError);
        var result=await provider.LoadHolonAsync(root,loadChildrenFromProvider:true);Assert.True(result.IsError);Assert.Null(result.Result);Assert.IsType<InvalidDataException>(result.Exception);
    }
    [Fact]
    public async Task NegativeDepthDoesNotIssuePointRead()
    {
        var transport=new SolidRecordHttpTests.Transport();using var provider=new SOLIDOASIS(Root.AbsoluteUri,"",Key(),transport);Assert.False((await provider.ActivateProviderAsync()).IsError);
        var result=await provider.LoadHolonAsync(Guid.NewGuid(),maxChildDepth:-1);Assert.True(result.IsError);Assert.Equal(0,transport.Reads);
    }
    [Fact]
    public async Task MissingSingleRecordKeepsCanonicalNotFoundCode()
    {
        var transport=new SolidRecordHttpTests.Transport();using var provider=new SOLIDOASIS(Root.AbsoluteUri,"",Key(),transport);Assert.False((await provider.ActivateProviderAsync()).IsError);
        var result=await provider.LoadHolonAsync(Guid.NewGuid(),loadChildrenFromProvider:true);Assert.True(result.IsError);Assert.Equal(OASISResultErrorCodes.RecordNotFound,result.ErrorCode);Assert.Null(result.Result);
    }
}
