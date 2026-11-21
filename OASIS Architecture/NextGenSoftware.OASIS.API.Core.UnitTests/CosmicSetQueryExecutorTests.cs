using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;
namespace NextGenSoftware.OASIS.API.Core.UnitTests;
public sealed class CosmicSetQueryExecutorTests
{
    private class Flat:ICosmicQueryStorageProvider
    {
        public bool DenyName;public int FlatCalls;
        public IHolon[] Data={new Holon {Id=Guid.NewGuid(),IsPublic=true}};
        public ProviderType QueryProviderType=>ProviderType.MongoDBOASIS;
        public CosmicQueryCapabilities QueryCapabilities
        {
            get
            {
                var fields=new List<CosmicQueryFieldCapability>{
                    new("IsPublic",typeof(bool),CosmicQueryComparisonOperator.Equal),
                    new("CreatedByAvatarId",typeof(Guid),CosmicQueryComparisonOperator.Equal)};
                if(!DenyName)fields.Add(new("Name",typeof(string),new[]{CosmicQueryComparisonOperator.Equal},
                    Array.Empty<CosmicQueryStringOperator>(),supportsOrdering:true));
                return new(CosmicQueryFeatures.And|CosmicQueryFeatures.Or|CosmicQueryFeatures.BooleanConstant|
                    CosmicQueryFeatures.Ordering|CosmicQueryFeatures.Paging,fields.ToArray());
            }
        }
        public Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(
            CosmicQueryPlan<T> plan,int maximumResults=100,CancellationToken cancellationToken=default) where T:IHolon
        {FlatCalls++;return Task.FromResult(new OASISResult<IReadOnlyList<IHolon>>(Data) {IsLoaded=true});}
    }
    private sealed class Native:Flat,ICosmicSetQueryStorageProvider
    {
        public int SetCalls;public bool Loaded=true;
        public CosmicSetQueryCapabilities SetQueryCapabilities=>new(
            CosmicSetQueryFeatures.Union|CosmicSetQueryFeatures.Concat|CosmicSetQueryFeatures.Intersect|CosmicSetQueryFeatures.Except,
            new CosmicEntityCompositionCapabilities(CosmicEntityStageFeatures.Paging|CosmicEntityStageFeatures.Filter|
                CosmicEntityStageFeatures.Ordering,QueryCapabilities));
        public Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicSetQueryAsync<T>(
            CosmicSetQueryPlan<T> plan,int maximumResults=100,CancellationToken cancellationToken=default) where T:IHolon
        {SetCalls++;return Task.FromResult(new OASISResult<IReadOnlyList<IHolon>>(Data) {IsLoaded=Loaded});}
    }
    private static CosmicSetQueryPlan<Holon> Plan(Native native,CosmicSetQueryKind kind,bool rightName=false)
    {
        var actor=Guid.NewGuid();
        var root=CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.MongoDBOASIS).Result)
            .WithReadAccess(CosmicQueryReadAccess.OwnerOrPublic(actor).Result).Result;
        var right=rightName?root.Where(h=>h.Name=="right").Result:root;
        var query=CosmicSetQuery<Holon>.Create(root,right,kind).Result;
        if(rightName)query=query.Take(0).Result;
        var result=CosmicSetQueryPlanner.Plan(query,ProviderType.MongoDBOASIS,
            new CosmicQueryTarget(ProviderType.MongoDBOASIS,native.QueryCapabilities),native.SetQueryCapabilities);
        Assert.False(result.IsError,result.Message);return result.Result;
    }
    [Fact]
    public async Task ConcatPublishesDuplicateOccurrencesWithoutWeakeningOrdinaryQueries()
    {
        var native=new Native();var row=native.Data[0];native.Data=new[]{row,row};
        var plan=Plan(native,CosmicSetQueryKind.Concat);
        var result=await CosmicSetQueryExecutor.ExecuteAsync(plan,new ICosmicQueryStorageProvider[]{native});
        Assert.False(result.IsError,result.Message);Assert.Equal(2,result.Result.TotalRows);
        var rows=Assert.Single(result.Result.Providers).Rows;
        Assert.Equal(row.Id,rows[0].Id);Assert.Equal(row.Id,rows[1].Id);
        Assert.NotSame(row,rows[0]);Assert.NotSame(rows[0],rows[1]);
        Assert.Equal(1,native.SetCalls);Assert.Equal(0,native.FlatCalls);
        var ordinary=await CosmicQueryExecutor.ExecuteAsync(plan.Left,new ICosmicQueryStorageProvider[]{native});
        Assert.Equal("CQL_PROVIDER_RESULT_INVALID",ordinary.ErrorCode);Assert.Equal(1,native.FlatCalls);
        var distinct=await CosmicSetQueryExecutor.ExecuteAsync(Plan(native,CosmicSetQueryKind.Union),new ICosmicQueryStorageProvider[]{native});
        Assert.Equal("CQL_PROVIDER_RESULT_INVALID",distinct.ErrorCode);
    }
    [Fact]
    public async Task ActualRightInputAndNativeContractAreAdmittedBeforeDispatch()
    {
        var native=new Native();var plan=Plan(native,CosmicSetQueryKind.Union,rightName:true);
        native.DenyName=true;
        var changed=await CosmicSetQueryExecutor.ExecuteAsync(plan,new ICosmicQueryStorageProvider[]{native});
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED",changed.ErrorCode);
        Assert.Equal(0,native.SetCalls);Assert.Equal(0,native.FlatCalls);
        var flat=new Flat();
        var missing=await CosmicSetQueryExecutor.ExecuteAsync(plan,new ICosmicQueryStorageProvider[]{flat});
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED",missing.ErrorCode);Assert.Equal(0,flat.FlatCalls);
    }
    [Fact]
    public async Task NativeCompletionAndDetachedVisibilityRemainMandatory()
    {
        var native=new Native {Loaded=false};var plan=Plan(native,CosmicSetQueryKind.Union);
        var incomplete=await CosmicSetQueryExecutor.ExecuteAsync(plan,new ICosmicQueryStorageProvider[]{native});
        Assert.Equal("CQL_PROVIDER_RESULT_INVALID",incomplete.ErrorCode);
        native.Loaded=true;native.Data=new IHolon[]{new Holon {Id=Guid.NewGuid(),CreatedByAvatarId=Guid.NewGuid(),IsPublic=false}};
        var forbidden=await CosmicSetQueryExecutor.ExecuteAsync(plan,new ICosmicQueryStorageProvider[]{native});
        Assert.Equal("CQL_VISIBILITY_VIOLATION",forbidden.ErrorCode);
    }
}
