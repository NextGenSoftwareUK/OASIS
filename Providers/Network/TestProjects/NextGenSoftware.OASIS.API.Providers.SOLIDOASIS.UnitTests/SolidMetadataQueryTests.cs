using System.Net;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.SOLIDOASIS.UnitTests;

public class SolidMetadataQueryTests
{
    private static readonly Uri Root=new("https://pod.invalid/private/");
    private static byte[] Key()=>Enumerable.Range(1,32).Select(x=>(byte)x).ToArray();
    private sealed class Transport : HttpMessageHandler
    {
        public readonly Guid Parent=Guid.NewGuid(),Child=Guid.NewGuid(),Grandchild=Guid.NewGuid();
        public readonly Dictionary<string,string> Records=new();public int Requests;
        public Transport(){using var codec=new SolidPrivateRecordCodec(Root,Key());
            foreach(IHolon record in new IHolon[]{
                new Holon{Id=Parent,MetaData=new(){["secret-zone"]="park",["rank"]=1.25m}},
                new Holon{Id=Child,ParentHolonId=Parent,MetaData=new(){["secret-zone"]="other",["rank"]=2}},
                new Holon{Id=Grandchild,ParentHolonId=Child,MetaData=new(){["secret-zone"]="other",["rank"]=3}}})
                Records[new Uri(Root,$"oasis/holons/{record.Id}.ttl").AbsoluteUri]=codec.Encode(record);
        }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)
        {
            Requests++;Assert.Equal(HttpMethod.Get,request.Method);Assert.Empty(request.RequestUri!.Query);Assert.DoesNotContain("search",request.RequestUri.AbsoluteUri);Assert.DoesNotContain("for-parent",request.RequestUri.AbsoluteUri);
            var uri=request.RequestUri;var body=uri.AbsoluteUri==Root.AbsoluteUri?"":uri.AbsolutePath.EndsWith('/')
                ?"@prefix ldp: <http://www.w3.org/ns/ldp#> . <> a ldp:BasicContainer; ldp:contains "+string.Join(",",Records.Keys.Select(key=>"<"+key+">"))+" .":Records[uri.AbsoluteUri];
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content=new StringContent(body,System.Text.Encoding.UTF8,"text/turtle")});
        }
    }
    [Theory][InlineData(0,false)][InlineData(0,true)][InlineData(1,false)][InlineData(1,true)][InlineData(2,false)][InlineData(2,true)]
    public async Task MetadataUsesSharedMatcherWithoutPrivateSearchRequests(int scenario,bool synchronous)
    {
        var transport=new Transport();using var provider=new SOLIDOASIS(Root.AbsoluteUri,"",Key(),transport);Assert.False((await provider.ActivateProviderAsync()).IsError);
        var filter=new Dictionary<string,string>{{"secret-zone","park"},{"rank",scenario==2?"2":"1.25"}};
        var result=scenario==0
            ?(synchronous?provider.LoadHolonsByMetaData("rank","1.25",loadChildren:false):await provider.LoadHolonsByMetaDataAsync("rank","1.25",loadChildren:false))
            :(synchronous?provider.LoadHolonsByMetaData(filter,scenario==1?MetaKeyValuePairMatchMode.All:MetaKeyValuePairMatchMode.Any,loadChildren:false)
                :await provider.LoadHolonsByMetaDataAsync(filter,scenario==1?MetaKeyValuePairMatchMode.All:MetaKeyValuePairMatchMode.Any,loadChildren:false));
        Assert.False(result.IsError,result.Exception?.ToString());
        Assert.Contains(result.Result,row=>row.Id==transport.Parent);Assert.Equal(scenario==2?2:1,result.Result.Count());
    }
    [Theory][InlineData(false)][InlineData(true)]
    public async Task ParentQueryProjectsProviderLinkedDescendants(bool synchronous)
    {
        var transport=new Transport();using var provider=new SOLIDOASIS(Root.AbsoluteUri,"",Key(),transport);Assert.False((await provider.ActivateProviderAsync()).IsError);
        var result=synchronous?provider.LoadHolonsForParent(transport.Parent.ToString(),loadChildren:true,loadChildrenFromProvider:true)
            :await provider.LoadHolonsForParentAsync(transport.Parent,loadChildren:true,loadChildrenFromProvider:true);
        Assert.False(result.IsError,result.Exception?.ToString());var child=Assert.Single(result.Result);Assert.Equal(transport.Child,child.Id);
        Assert.Equal(transport.Grandchild,Assert.Single(child.Children).Id);
    }
    [Fact]
    public async Task InterfaceParentCallPreservesDistinctContinueAndProviderChildFlags()
    {
        var transport=new Transport();using var provider=new SOLIDOASIS(Root.AbsoluteUri,"",Key(),transport);Assert.False((await provider.ActivateProviderAsync()).IsError);
        IOASISStorageProvider contract=provider;
        var result=contract.LoadHolonsForParent(transport.Parent.ToString(),loadChildren:true,continueOnError:false,loadChildrenFromProvider:true);
        Assert.False(result.IsError,result.Message);Assert.Equal(transport.Grandchild,Assert.Single(Assert.Single(result.Result).Children).Id);
    }
    [Fact]
    public async Task InitialDepthHonorsProjectionLimit()
    {
        var transport=new Transport();using var provider=new SOLIDOASIS(Root.AbsoluteUri,"",Key(),transport);Assert.False((await provider.ActivateProviderAsync()).IsError);
        var result=await provider.LoadHolonsForParentAsync(transport.Parent,loadChildren:true,maxChildDepth:1,curentChildDepth:1,loadChildrenFromProvider:true);
        Assert.False(result.IsError,result.Message);Assert.Empty(Assert.Single(result.Result).Children);
    }
    [Theory][InlineData(false,false)][InlineData(false,true)][InlineData(true,false)][InlineData(true,true)]
    public async Task ParentSelectionAlreadyTraversedOneEdge(bool synchronous,bool recursive)
    {
        var transport=new Transport();using var provider=new SOLIDOASIS(Root.AbsoluteUri,"",Key(),transport);Assert.False((await provider.ActivateProviderAsync()).IsError);
        var result=synchronous?provider.LoadHolonsForParent(transport.Parent.ToString(),recursive:recursive,maxChildDepth:1,loadChildrenFromProvider:true)
            :await provider.LoadHolonsForParentAsync(transport.Parent,recursive:recursive,maxChildDepth:1,loadChildrenFromProvider:true);
        Assert.False(result.IsError,result.Exception?.ToString());Assert.Equal(transport.Child,Assert.Single(result.Result).Id);Assert.Empty(Assert.Single(result.Result).Children);
    }
    [Fact]
    public async Task InvalidMetadataModeFailsBeforeHttp()
    {
        var transport=new Transport();using var provider=new SOLIDOASIS(Root.AbsoluteUri,"",Key(),transport);Assert.False((await provider.ActivateProviderAsync()).IsError);
        var result=await provider.LoadHolonsByMetaDataAsync(new Dictionary<string,string>{{"rank","1.25"}},(MetaKeyValuePairMatchMode)999,loadChildren:false);
        Assert.True(result.IsError);Assert.Equal(1,transport.Requests);
    }
}
