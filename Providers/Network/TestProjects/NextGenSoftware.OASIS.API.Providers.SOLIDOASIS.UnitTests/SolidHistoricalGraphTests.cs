using System.Net;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects.Search;

namespace NextGenSoftware.OASIS.API.Providers.SOLIDOASIS.UnitTests;

public class SolidHistoricalGraphTests
{
    [Theory]
    [InlineData(0,false)][InlineData(1,false)][InlineData(2,false)][InlineData(3,false)][InlineData(4,false)]
    [InlineData(0,true)][InlineData(1,true)][InlineData(2,true)][InlineData(3,true)][InlineData(4,true)]
    public async Task SuccessfulHolonQueriesPublishLoadedState(int route,bool synchronous)
    {
        using var codec=new SolidPrivateRecordCodec(Root,Key());var parent=Guid.NewGuid();
        IHolon row=new Holon{Id=Guid.NewGuid(),ParentHolonId=parent,MetaData=new(){["scope"]="owned"}};
        var records=new Dictionary<string,string>{{codec.Resource<IHolon>(row.Id).AbsoluteUri,codec.Encode(row)}};
        using var provider=new SOLIDOASIS(Root.AbsoluteUri,"",Key(),new Resource(records));Assert.False((await provider.ActivateProviderAsync()).IsError);
        var result=route switch{
            0=>synchronous?provider.LoadAllHolons(loadChildren:false):await provider.LoadAllHolonsAsync(loadChildren:false),
            1=>synchronous?provider.LoadHolonsForParent(parent,loadChildren:false):await provider.LoadHolonsForParentAsync(parent,loadChildren:false),
            2=>synchronous?provider.LoadHolonsForParent(parent.ToString(),loadChildren:false):await provider.LoadHolonsForParentAsync(parent.ToString(),loadChildren:false),
            3=>synchronous?provider.LoadHolonsByMetaData("scope","owned",loadChildren:false):await provider.LoadHolonsByMetaDataAsync("scope","owned",loadChildren:false),
            _=>synchronous?provider.LoadHolonsByMetaData(new(){["scope"]="owned"},NextGenSoftware.OASIS.API.Core.Enums.MetaKeyValuePairMatchMode.All,loadChildren:false):await provider.LoadHolonsByMetaDataAsync(new(){["scope"]="owned"},NextGenSoftware.OASIS.API.Core.Enums.MetaKeyValuePairMatchMode.All,loadChildren:false)};
        Assert.False(result.IsError,result.Exception?.ToString());Assert.Equal(row.Id,Assert.Single(result.Result).Id);Assert.True(result.IsLoaded);
    }

    private static readonly Uri Root=new("https://pod.invalid/private/");
    private static byte[] Key()=>Enumerable.Range(1,32).Select(value=>(byte)value).ToArray();
    private sealed class Resource(Dictionary<string,string> records):HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken cancellationToken)
        {
            Assert.Equal(HttpMethod.Get,request.Method);var uri=request.RequestUri!;
            var text=uri==Root?"":uri.AbsolutePath.EndsWith('/')
                ?"@prefix ldp: <http://www.w3.org/ns/ldp#> . <> a ldp:BasicContainer; ldp:contains "+string.Join(",",records.Keys.Select(key=>"<"+key+">"))+" ."
                :records[uri.AbsoluteUri];
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content=new StringContent(text,System.Text.Encoding.UTF8,"text/turtle")});
        }
    }

    [Theory][InlineData(0,false)][InlineData(1,false)][InlineData(2,false)][InlineData(0,true)][InlineData(1,true)][InlineData(2,true)]
    public async Task HistoricalSelectionUsesCurrentIndependentChildren(int route,bool deleted)
    {
        using var codec=new SolidPrivateRecordCodec(Root,Key());var parent=Guid.NewGuid();
        IHolon root=new Holon{Id=Guid.NewGuid(),Name="archived-root",ParentHolonId=parent};
        var root1=await codec.StageVersionAsync(root);root.Name="current-root";root.ParentHolonId=Guid.NewGuid();var root2=await codec.StageVersionAsync(root,root1.Turtle);
        IHolon child=new Holon{Id=Guid.NewGuid(),ParentHolonId=root.Id,Name="child1"};
        var child1=await codec.StageVersionAsync(child);child.Name="child2";var child2=await codec.StageVersionAsync(child,child1.Turtle);
        child.Name="child3";if(deleted)child.DeletedDate=DateTime.UtcNow;var child3=await codec.StageVersionAsync(child,child2.Turtle);
        var records=new Dictionary<string,string>{{codec.Resource<IHolon>(root.Id).AbsoluteUri,root2.Turtle},{codec.Resource<IHolon>(child.Id).AbsoluteUri,child3.Turtle}};
        using var provider=new SOLIDOASIS(Root.AbsoluteUri,"",Key(),new Resource(records));Assert.False((await provider.ActivateProviderAsync()).IsError);
        IHolon selected;
        if(route==0){var result=await provider.LoadHolonAsync(root.Id,loadChildrenFromProvider:true,version:1);Assert.False(result.IsError,result.Exception?.ToString());selected=result.Result;}
        else if(route==1){var result=provider.LoadHolonsForParent(parent.ToString(),loadChildrenFromProvider:true,version:1);Assert.False(result.IsError,result.Exception?.ToString());selected=Assert.Single(result.Result);}
        else{var result=await provider.SearchAsync(new SearchParams{SearchOnlyForCurrentAvatar=false,SearchGroups=new(){new SearchTextGroup{SearchHolons=true,SearchQuery="archived-root"}}},version:1);Assert.False(result.IsError,result.Exception?.ToString());selected=Assert.Single(result.Result.SearchResultHolons);}
        Assert.Equal(1,selected.Version);Assert.Equal("archived-root",selected.Name);Assert.Equal(parent,selected.ParentHolonId);
        if(deleted)Assert.Empty(selected.Children);else{Assert.Equal(3,Assert.Single(selected.Children).Version);Assert.Equal("child3",Assert.Single(selected.Children).Name);}
    }
}
