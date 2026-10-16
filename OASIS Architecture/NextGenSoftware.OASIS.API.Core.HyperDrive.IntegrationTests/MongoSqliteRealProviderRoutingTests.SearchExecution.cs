using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public sealed partial class MongoSqliteRealProviderRoutingTests
{
    private static async Task VerifyMongoSearchExecutionAsync(MongoDBOASIS mongo,OASISHyperDrive router,bool synchronous,IHolon root)
    {
        SearchTextGroup Text() => new() {SearchQuery=root.Name,SearchHolons=true,HolonType=root.HolonType,
            HolonSearchParams=new SearchHolonParams {Name=true}};
        foreach(var date in new[] {false,true})
        foreach(var comparison in Enum.GetValues<SearchOperatorType>())
        {
            ISearchGroupBase condition=date
                ? new SearchDateGroup {SearchHolons=true,Date=DateTime.UtcNow.AddYears(1),DateOperator=comparison,
                    PreviousSearchGroupOperator=SearchParamGroupOperator.And,HolonSearchParams=new SearchHolonParams {CreatedDate=true}}
                : new SearchNumberGroup {SearchHolons=true,Number=root.Version,NumberOperator=comparison,
                    PreviousSearchGroupOperator=SearchParamGroupOperator.And,HolonSearchParams=new SearchHolonParams {Version=true}};
            var request=new StorageOperationRequest {Operation="Search",PreferredProvider=ProviderType.MongoDBOASIS,
                SearchParams=new SearchParams {SearchOnlyForCurrentAvatar=false,SearchGroups=new() {Text(),condition}}};
            var result=synchronous ? router.RouteRequest<ISearchResults>(request):await router.RouteRequestAsync<ISearchResults>(request);
            result.IsError.Should().BeFalse(result.Exception?.ToString() ?? result.Message);
            var matches=date
                ? comparison is SearchOperatorType.LessThan or SearchOperatorType.LessThanOrEqualTo or SearchOperatorType.NotEqualTo
                : comparison is SearchOperatorType.EqualTo or SearchOperatorType.LessThanOrEqualTo or SearchOperatorType.GreaterThanOrEqualTo;
            result.Result.SearchResultHolons.Select(hit=>hit.Id).Should().Equal(matches ? new[] {root.Id}:Array.Empty<Guid>());
        }

        (bool Children,bool Recursive,int Depth)[] options={(false,true,0),(true,false,0),(true,true,1),(true,true,2)};
        foreach(var version in new[] {0,root.Version,root.Version-1})
        foreach(var option in options)
        {
            var request=new StorageOperationRequest {Operation="Search",PreferredProvider=ProviderType.MongoDBOASIS,Version=version,
                LoadChildren=option.Children,Recursive=option.Recursive,MaxChildDepth=option.Depth,
                SearchParams=new SearchParams {SearchOnlyForCurrentAvatar=false,SearchGroups=new() {Text()}}};
            var result=synchronous ? router.RouteRequest<ISearchResults>(request):await router.RouteRequestAsync<ISearchResults>(request);
            result.IsError.Should().BeFalse(result.Exception?.ToString() ?? result.Message);result.IsLoaded.Should().BeTrue();
            var exists=version==0 || version==root.Version;
            result.Result.NumberOfResults.Should().Be(exists ? 1:0);
            if(!exists) {result.Result.SearchResultHolons.Should().BeEmpty();continue;}
            var selected=result.Result.SearchResultHolons.Should().ContainSingle().Which;
            selected.Version.Should().Be(root.Version);
            if(!option.Children) selected.Children.Should().BeEmpty();
            else
            {
                var child=selected.Children.Should().ContainSingle().Which;
                child.Name.Should().Be("embedded child");
                if(!option.Recursive || option.Depth==1) child.Children.Should().BeEmpty();
                else child.Children.Should().ContainSingle().Which.Name.Should().Be("embedded grandchild");
            }
        }
        var invalid=new StorageOperationRequest {Operation="Search",PreferredProvider=ProviderType.MongoDBOASIS,
            SearchParams=new SearchParams {SearchOnlyForCurrentAvatar=false,MetaKeyValuePairMatchMode=(MetaKeyValuePairMatchMode)999,SearchGroups=new() {Text()}}};
        var rejected=synchronous ? router.RouteRequest<ISearchResults>(invalid):await router.RouteRequestAsync<ISearchResults>(invalid);
        rejected.IsError.Should().BeTrue();rejected.Result.Should().BeNull();rejected.ErrorCode.Should().Be("HYPERDRIVE_PAYLOAD_INVALID");
        rejected.Exception.Should().BeOfType<ArgumentOutOfRangeException>();

        // Observe caller-context captures without blocking or throwing from a continuation.
        var context=new RecordingSearchContext();
        var previous=SynchronizationContext.Current;
        Task<OASISResult<ISearchResults>> pending;
        try
        {
            SynchronizationContext.SetSynchronizationContext(context);
            var parameters=new SearchParams {SearchOnlyForCurrentAvatar=false,SearchGroups=new() {Text()}};
            pending=synchronous ? Task.FromResult(mongo.Search(parameters)):mongo.SearchAsync(parameters);
        }
        finally {SynchronizationContext.SetSynchronizationContext(previous);}
        var direct=await pending.ConfigureAwait(false);
        direct.IsError.Should().BeFalse(direct.Message);
        direct.Result.SearchResultHolons.Select(hit=>hit.Id).Should().Equal(root.Id);
        context.Posts.Should().Be(0,"provider I/O must not capture the caller synchronization context");
    }

    private sealed class RecordingSearchContext : SynchronizationContext
    {
        private int posts;
        public int Posts=>Volatile.Read(ref posts);
        public override void Post(SendOrPostCallback callback,object? state)
        {
            Interlocked.Increment(ref posts);
            ThreadPool.QueueUserWorkItem(_=>callback(state));
        }
    }
}
