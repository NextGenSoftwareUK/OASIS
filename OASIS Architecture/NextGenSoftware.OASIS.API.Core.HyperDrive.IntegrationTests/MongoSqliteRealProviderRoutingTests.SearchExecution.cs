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
