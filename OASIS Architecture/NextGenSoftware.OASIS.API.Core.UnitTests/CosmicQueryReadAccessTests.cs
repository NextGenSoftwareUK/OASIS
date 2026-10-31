using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.DNA;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public class CosmicQueryReadAccessTests
{
    public sealed class ForbiddenConstruction:Holon
    {
        public static int Constructions;
        public ForbiddenConstruction(){Interlocked.Increment(ref Constructions);throw new InvalidOperationException("Denied rows must not construct typed entities.");}
    }

    [Fact]
    public async Task ForeignRowsAreRejectedBeforeTypedConstruction()
    {
        ForbiddenConstruction.Constructions=0;
        var actor=Guid.NewGuid();
        var query=CosmicQuery<ForbiddenConstruction>.Create().WithReadAccess(CosmicQueryReadAccess.OwnerOrPublic(actor).Result).Result;
        var adapter=new Adapter(new Holon{Id=Guid.NewGuid(),CreatedByAvatarId=Guid.NewGuid(),IsPublic=false});
        var plan=CosmicQueryPlanner.Plan(query,ProviderType.MongoDBOASIS,
            new[]{new CosmicQueryTarget(ProviderType.MongoDBOASIS,adapter.QueryCapabilities)});
        Assert.False(plan.IsError,plan.Message);
        var dna=new NextGenSoftware.OASIS.API.DNA.OASISDNA{OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS{
            HyperDriveMode=HyperDriveModes.V2}};
        var manager=new NextGenSoftware.OASIS.API.Core.Managers.HolonManager(null,dna,
            new NextGenSoftware.OASIS.API.Core.Managers.ProviderManager(null,dna));
        var result=await manager.ExecuteQueryAsync(plan.Result,new ICosmicQueryStorageProvider[]{adapter});
        Assert.Equal("CQL_VISIBILITY_VIOLATION",result.ErrorCode);
        Assert.Equal(0,ForbiddenConstruction.Constructions);
    }

    [Fact]
    public void EmptyActorCannotCreateAnAccessBinding()
    {
        var access=CosmicQueryReadAccess.OwnerOrPublic(Guid.Empty);
        Assert.True(access.IsError);Assert.Equal("CQL_ACCESS_INVALID",access.ErrorCode);Assert.Null(access.Result);
    }

    [Fact]
    public void CompositionCannotDropOrDuplicateTheMandatoryPredicate()
    {
        var access=CosmicQueryReadAccess.OwnerOrPublic(Guid.NewGuid()).Result;
        var original=CosmicQuery<Holon>.Create();
        var bound=original.WithReadAccess(access);
        Assert.False(bound.IsError,bound.Message);
        var query=bound.Result.Where(row=>row.IsActive).OrderBy(row=>row.Name).Skip(1).Take(2).Result;
        query=query.WithScope(CosmicQueryScope.AllEligible).WithoutChildren();
        Assert.Null(original.ReadAccess);
        Assert.Same(access,query.ReadAccess);
        Assert.Equal(2,query.Predicates.Count);
        Assert.Single(bound.Result.Predicates);
        var same=bound.Result.WithReadAccess(CosmicQueryReadAccess.OwnerOrPublic(access.ActorId).Result);
        Assert.False(same.IsError,same.Message);Assert.Single(same.Result.Predicates);
    }

    [Fact]
    public void FluentBindingPreservesAnEarlierFailureAndBothNativePredicates()
    {
        var access=CosmicQueryReadAccess.OwnerOrPublic(Guid.NewGuid()).Result;
        var failed=new OASISResult<CosmicQuery<Holon>>{IsError=true,ErrorCode="original-error",Message="original"};
        Assert.Same(failed,failed.WithReadAccess(access));
        var bound=CosmicQuery<Holon>.Create().Where(row=>row.IsActive).WithReadAccess(access);
        Assert.False(bound.IsError,bound.Message);
        Assert.Same(access,bound.Result.ReadAccess);
        Assert.Equal(2,bound.Result.Predicates.Count);
    }

    [Fact]
    public void AnExistingBindingCannotBeReplacedByAnotherActor()
    {
        var query=CosmicQuery<Holon>.Create().WithReadAccess(CosmicQueryReadAccess.OwnerOrPublic(Guid.NewGuid()).Result).Result;
        var result=query.WithReadAccess(CosmicQueryReadAccess.OwnerOrPublic(Guid.NewGuid()).Result);
        Assert.True(result.IsError);Assert.Equal("CQL_ACCESS_REBIND_FORBIDDEN",result.ErrorCode);
    }

    [Fact]
    public void AccessCannotBeIntroducedAfterAWindow()
    {
        var result=CosmicQuery<Holon>.Create().Take(1).Result.WithReadAccess(CosmicQueryReadAccess.OwnerOrPublic(Guid.NewGuid()).Result);
        Assert.True(result.IsError);Assert.Equal("CQL_COMPOSITION_UNSUPPORTED",result.ErrorCode);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void NativeGraphPolicyRequiresExplicitAdapterCapability(bool graphFirst)
    {
        var access=CosmicQueryReadAccess.OwnerOrPublic(Guid.NewGuid()).Result;
        var graph=CosmicQueryGraphOptions.Create().Result;
        var result=graphFirst
            ?CosmicQuery<Holon>.Create().WithChildren(graph).Result.WithReadAccess(access)
            :CosmicQuery<Holon>.Create().WithReadAccess(access).Result.WithChildren(graph);
        Assert.False(result.IsError,result.Message);
        var caps=new CosmicQueryCapabilities(CosmicQueryFeatures.GraphExpansion|CosmicQueryFeatures.Or|CosmicQueryFeatures.And,
            new[]{new CosmicQueryFieldCapability("IsPublic",typeof(bool),CosmicQueryComparisonOperator.Equal),
                new CosmicQueryFieldCapability("CreatedByAvatarId",typeof(Guid),CosmicQueryComparisonOperator.Equal)});
        var rejected=caps.Admit(result.Result);
        Assert.True(rejected.IsError);Assert.Equal("CQL_CAPABILITY_UNSUPPORTED",rejected.ErrorCode);
        var supported=new CosmicQueryCapabilities(caps.Features|CosmicQueryFeatures.GraphReadAccess,caps.CanonicalFields.ToArray());
        Assert.False(supported.Admit(result.Result).IsError);
    }

    private sealed class Adapter(params IHolon[] rows):ICosmicQueryStorageProvider
    {
        public ProviderType QueryProviderType=>ProviderType.MongoDBOASIS;
        public CosmicQueryCapabilities QueryCapabilities=>new(CosmicQueryFeatures.Or|CosmicQueryFeatures.And,
            new[]{new CosmicQueryFieldCapability("IsPublic",typeof(bool),new[]{CosmicQueryComparisonOperator.Equal}),
                new CosmicQueryFieldCapability("CreatedByAvatarId",typeof(Guid),new[]{CosmicQueryComparisonOperator.Equal})},
            Array.Empty<CosmicQueryMetadataCapability>());
        public Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(CosmicQueryPlan<T> plan,
            int maximumResults=100,CancellationToken cancellationToken=default) where T:IHolon
            =>Task.FromResult(new OASISResult<IReadOnlyList<IHolon>>(rows){IsLoaded=true});
    }

    [Theory]
    [InlineData(false,false)] [InlineData(true,false)] [InlineData(false,true)]
    public async Task PublicationChecksAccessWithoutTreatingMetadataAsAGrant(bool isPublic,bool owned)
    {
        var actor=Guid.NewGuid();
        var row=new Holon{Id=Guid.NewGuid(),IsPublic=isPublic,CreatedByAvatarId=owned?actor:Guid.NewGuid(),
            MetaData=new(){["sharedWith"]=actor.ToString(),["tenantRole"]="administrator"}};
        var query=CosmicQuery<Holon>.Create().WithReadAccess(CosmicQueryReadAccess.OwnerOrPublic(actor).Result).Result;
        var adapter=new Adapter(row);
        var plan=CosmicQueryPlanner.Plan(query,ProviderType.MongoDBOASIS,
            new[]{new CosmicQueryTarget(ProviderType.MongoDBOASIS,adapter.QueryCapabilities)});
        Assert.False(plan.IsError,plan.Message);
        var read=await CosmicQueryExecutor.ExecuteAsync(plan.Result,new ICosmicQueryStorageProvider[]{adapter});
        Assert.Equal(!(isPublic||owned),read.IsError);
        if(read.IsError){Assert.Equal("CQL_VISIBILITY_VIOLATION",read.ErrorCode);Assert.Null(read.Result);}
        else Assert.Single(read.Result.Providers.Single().Rows);
    }
}
