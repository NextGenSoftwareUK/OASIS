using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects.Search;

namespace NextGenSoftware.OASIS.API.Providers.SOLIDOASIS.UnitTests;

public class SolidSearchTests
{
    [Theory][InlineData(false,false)][InlineData(false,true)][InlineData(true,false)][InlineData(true,true)]
    public async Task EmptyAndFailedReadsDoNotInventLoadedRecords(bool search,bool fail)
    {
        var transport=new SolidRecordHttpTests.Transport{Failure=fail?"read":""};
        using var provider=new SOLIDOASIS(Root.AbsoluteUri,"",Key(),transport);Assert.False((await provider.ActivateProviderAsync()).IsError);
        if(search){
            var result=await provider.SearchAsync(new SearchParams{SearchOnlyForCurrentAvatar=false},loadChildren:false);
            Assert.Equal(fail,result.IsError);Assert.Equal(!fail,result.IsLoaded);Assert.Equal(0,result.LoadedCount);
            if(fail)Assert.Null(result.Result);else Assert.Equal(0,result.Result.NumberOfResults);
        }else{
            var result=await provider.LoadAllHolonsAsync(loadChildren:false);
            Assert.Equal(fail,result.IsError);Assert.Equal(!fail,result.IsLoaded);Assert.Equal(0,result.LoadedCount);
            if(fail)Assert.Null(result.Result);else Assert.Empty(result.Result);
        }
    }
    private static readonly Uri Root=new("https://pod.invalid/private/");
    private static readonly Guid Owner=Guid.Parse("09d3be1f-bdd9-4b61-bccf-6377a9f92350");
    private static byte[] Key()=>Enumerable.Range(1,32).Select(x=>(byte)x).ToArray();
    private static SolidRecordHttpTests.Transport Records()
    {
        var transport=new SolidRecordHttpTests.Transport();using var codec=new SolidPrivateRecordCodec(Root,Key());
        IAvatar avatar=new Avatar{Id=Guid.NewGuid(),Username="oak avatar",Password="private-password-canary",CreatedByAvatarId=Owner,MetaData=new(){["team"]="park"}};
        transport.Records[codec.Resource<IAvatar>(avatar.Id).AbsoluteUri]=codec.Encode(avatar);
        foreach(IHolon row in new IHolon[]{
            new Holon{Id=Guid.NewGuid(),Name="oak owned",CreatedByAvatarId=Owner,Version=7,CreatedDate=new DateTime(2026,10,1,0,0,0,DateTimeKind.Utc),MetaData=new(){["team"]="park"}},
            new Holon{Id=Guid.NewGuid(),Name="oak foreign",CreatedByAvatarId=Guid.NewGuid(),Version=2,CreatedDate=new DateTime(2026,9,1,0,0,0,DateTimeKind.Utc),MetaData=new(){["team"]="park"}},
            new Holon{Id=Guid.NewGuid(),Name="oak deleted",CreatedByAvatarId=Owner,DeletedDate=DateTime.UtcNow,MetaData=new(){["team"]="park"}}})
            transport.Records[codec.Resource<IHolon>(row.Id).AbsoluteUri]=codec.Encode(row);
        return transport;
    }
    private static SearchParams Query(int scenario)=>new(){SearchOnlyForCurrentAvatar=scenario==2,AvatarId=Owner,
        FilterByMetaData=new(){{"team","park"}},SearchGroups=scenario==0?null!:new List<ISearchGroupBase>{
            new SearchTextGroup{SearchQuery="oak",SearchHolons=true,HolonType=HolonType.All,HolonSearchParams=new SearchHolonParams{Name=true}}}};
    [Theory][InlineData(0,false)][InlineData(0,true)][InlineData(1,false)][InlineData(1,true)][InlineData(2,false)][InlineData(2,true)]
    public async Task SearchReusesLocalMasksScopeAndDeletedRecordSemantics(int scenario,bool synchronous)
    {
        var transport=Records();using var provider=new SOLIDOASIS(Root.AbsoluteUri,"",Key(),transport);Assert.False((await provider.ActivateProviderAsync()).IsError);
        var query=Query(scenario);var result=synchronous?provider.Search(query,loadChildren:false):await provider.SearchAsync(query,loadChildren:false);
        Assert.False(result.IsError,result.Exception?.ToString());Assert.Equal(scenario==0?3:scenario==1?2:1,result.Result.NumberOfResults);
        Assert.Equal(scenario==0?1:0,result.Result.SearchResultAvatars.Count);Assert.DoesNotContain(result.Result.SearchResultHolons,row=>row.DeletedDate!=DateTime.MinValue);
        Assert.True(result.IsLoaded);Assert.Equal(result.Result.NumberOfResults,result.LoadedCount);
    }
    [Fact]
    public async Task CanonicalNumericAndDateMasksSurviveRequestSnapshot()
    {
        var transport=Records();using var provider=new SOLIDOASIS(Root.AbsoluteUri,"",Key(),transport);Assert.False((await provider.ActivateProviderAsync()).IsError);
        foreach(ISearchGroupBase group in new ISearchGroupBase[]{
            new SearchNumberGroup{Number=7,NumberOperator=SearchOperatorType.EqualTo,HolonType=HolonType.All,SearchHolons=true,HolonSearchParams=new SearchHolonParams{Version=true}},
            new SearchDateGroup{Date=new DateTime(2026,9,15,0,0,0,DateTimeKind.Utc),DateOperator=SearchOperatorType.GreaterThan,HolonType=HolonType.All,SearchHolons=true,HolonSearchParams=new SearchHolonParams{CreatedDate=true}}}){
            var query=new SearchParams{SearchOnlyForCurrentAvatar=false,SearchGroups=new List<ISearchGroupBase>{group}};
            var result=await provider.SearchAsync(query,loadChildren:false);Assert.False(result.IsError,result.Exception?.ToString());Assert.Equal("oak owned",Assert.Single(result.Result.SearchResultHolons).Name);
        }
    }
    [Fact]
    public async Task InvalidCurrentAvatarScopeFailsBeforeStorageIo()
    {
        var transport=Records();using var provider=new SOLIDOASIS(Root.AbsoluteUri,"",Key(),transport);Assert.False((await provider.ActivateProviderAsync()).IsError);
        var result=await provider.SearchAsync(new SearchParams());Assert.True(result.IsError);Assert.Equal("HYPERDRIVE_PAYLOAD_INVALID",result.ErrorCode);Assert.Equal(0,transport.Reads);
    }
    [Fact]
    public async Task CallerQueryMutationCannotChangeInFlightSearch()
    {
        var transport=Records();using var provider=new SOLIDOASIS(Root.AbsoluteUri,"",Key(),transport);Assert.False((await provider.ActivateProviderAsync()).IsError);
        transport.ReadGate=new(TaskCreationOptions.RunContinuationsAsynchronously);var query=Query(0);
        var pending=provider.SearchAsync(query,loadChildren:false);
        try{
            Assert.False(pending.IsCompleted);query.FilterByMetaData["team"]="changed";query.SearchOnlyForCurrentAvatar=true;query.AvatarId=Guid.NewGuid();
            transport.ReadGate.TrySetResult(true);var result=await pending;Assert.False(result.IsError,result.Message);Assert.Equal(3,result.Result.NumberOfResults);
        }finally{transport.ReadGate.TrySetResult(true);}
    }
    [Fact]
    public async Task CredentialTextIsNotSearchableThroughDefaultAvatarTextMask()
    {
        var transport=Records();using var provider=new SOLIDOASIS(Root.AbsoluteUri,"",Key(),transport);Assert.False((await provider.ActivateProviderAsync()).IsError);
        var query=new SearchParams{SearchOnlyForCurrentAvatar=false,SearchGroups=new List<ISearchGroupBase>{new SearchTextGroup{SearchQuery="private-password-canary",SearchAvatars=true,HolonType=HolonType.All}}};
        var result=await provider.SearchAsync(query,loadChildren:false);Assert.False(result.IsError,result.Exception?.ToString());Assert.Equal(0,result.Result.NumberOfResults);
    }
}
