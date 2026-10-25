using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed class CosmicJoinPlanningTests
{
    public sealed class Dto
    {
        public string Name { get; set; } = "";
        public int? Revision { get; set; }
        public bool HasRight { get; set; }
    }

    private static CosmicProjectionCapabilities Paths(bool parent=true,bool revision=true)
        => new(new[] {
            new CosmicProjectionFieldCapability("Id",typeof(Guid)),
            new CosmicProjectionFieldCapability("Name",typeof(string)) }
            .Concat(parent?new[]{new CosmicProjectionFieldCapability("ParentHolonId",typeof(Guid))}:Array.Empty<CosmicProjectionFieldCapability>())
            .Concat(revision?new[]{new CosmicProjectionFieldCapability("Version",typeof(int))}:Array.Empty<CosmicProjectionFieldCapability>()).ToArray(),Array.Empty<Type>());

    private static CosmicQueryCapabilities Sources(bool active=true)
    {
        var fields=new List<CosmicQueryFieldCapability> {
            new("Id",typeof(Guid),new[]{CosmicQueryComparisonOperator.Equal},Array.Empty<CosmicQueryStringOperator>(),true) };
        if(active)fields.Add(new("IsActive",typeof(bool),CosmicQueryComparisonOperator.Equal));
        return new(CosmicQueryFeatures.Ordering|CosmicQueryFeatures.Paging|CosmicQueryFeatures.GraphExpansion
            |CosmicQueryFeatures.GraphSnapshot|CosmicQueryFeatures.GraphReadAccess,fields.ToArray());
    }

    private static CosmicJoinCapabilities Joins(bool left=true,bool parent=true,bool revision=true,
        bool graphs=false,bool snapshot=false,CosmicJoinOptions? limits=null)
        =>new(left?new[]{CosmicJoinKind.Inner,CosmicJoinKind.Left}:new[]{CosmicJoinKind.Inner},Paths(parent),Paths(revision:revision),
            graphs,snapshot,limits:limits);

    private static CosmicJoinSelection<Holon,Holon,Guid,Dto> Query(CosmicQueryScope? scope=null,bool window=false,
        bool snapshot=false,bool rightFilter=false)
    {
        var left=CosmicQuery<Holon>.Create(scope);
        var right=CosmicQuery<Holon>.Create(scope);
        if(window)left=left.OrderBy(row=>row.Id).Take(3).Result;
        if(snapshot)right=right.WithChildren(CosmicQueryGraphOptions.Create(CosmicQueryGraphConsistency.ProviderSnapshot).Result).Result;
        if(rightFilter)right=right.Where(row=>row.IsActive).Result;
        return left.LeftJoin(right,row=>row.Id,row=>row.ParentHolonId)
            .Select((owner,member)=>new Dto {Name=owner.Name,Revision=(int?)member.Version,HasRight=member!=null}).Result;
    }

    private static CosmicJoinTarget Target(ProviderType provider=ProviderType.MongoDBOASIS,CosmicJoinCapabilities? joins=null,
        CosmicQueryCapabilities? sources=null)=>new(provider,sources??Sources(),joins??Joins());

    [Fact]
    public void PlanPreservesBothSourcesAndWindowsWithoutInventingARowLimit()
    {
        var query=Query(window:true);
        var plan=CosmicJoinPlanner.Plan(query,ProviderType.MongoDBOASIS,new[]{Target()});
        plan.IsError.Should().BeFalse(plan.Message);
        plan.Result.LeftSourcePlan.Query.Should().BeSameAs(query.Join.Left);
        plan.Result.RightSourcePlan.Query.Should().BeSameAs(query.Join.Right);
        plan.Result.LeftSourcePlan.Query.Limit.Should().Be(3);
        plan.Result.RightSourcePlan.Query.Limit.Should().BeNull();
        CosmicJoinPlanner.Admit(plan.Result,ProviderType.MongoDBOASIS,Sources(),Joins()).IsError.Should().BeFalse();
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void ReadableProjectionPathsDoNotGrantKeyEqualityOrOutputPermission(bool output)
    {
        var plan=CosmicJoinPlanner.Plan(Query(),ProviderType.MongoDBOASIS,new[]{Target(joins:Joins(parent:output,revision:!output))});
        plan.ErrorCode.Should().Be("CQL_CAPABILITY_UNSUPPORTED");
    }

    [Fact]
    public void LeftJoinRequiresItsOwnKindEnrollment()
        =>CosmicJoinPlanner.Plan(Query(),ProviderType.MongoDBOASIS,new[]{Target(joins:Joins(left:false))})
            .ErrorCode.Should().Be("CQL_CAPABILITY_UNSUPPORTED");

    [Fact]
    public void ActualProviderCannotBorrowAnotherIdentity()
    {
        var plan=CosmicJoinPlanner.Plan(Query(),ProviderType.MongoDBOASIS,new[]{Target()}).Result;
        CosmicJoinPlanner.Admit(plan,ProviderType.Neo4jOASIS,Sources(),Joins()).ErrorCode.Should().Be("CQL_PLAN_INVALID");
    }

    [Fact]
    public void ActualJoinContractsAreReadmittedRatherThanTrustingThePlannedClaim()
    {
        var plan=CosmicJoinPlanner.Plan(Query(),ProviderType.MongoDBOASIS,new[]{Target()}).Result;
        CosmicJoinPlanner.Admit(plan,ProviderType.MongoDBOASIS,Sources(),Joins(parent:false)).ErrorCode.Should().Be("CQL_CAPABILITY_UNSUPPORTED");
    }

    [Fact]
    public void ActualSourceContractChecksTheRightSourceToo()
    {
        var plan=CosmicJoinPlanner.Plan(Query(rightFilter:true),ProviderType.MongoDBOASIS,new[]{Target()}).Result;
        CosmicJoinPlanner.Admit(plan,ProviderType.MongoDBOASIS,Sources(active:false),Joins()).ErrorCode.Should().Be("CQL_CAPABILITY_UNSUPPORTED");
    }

    [Fact]
    public void IndependentSnapshotSupportDoesNotGrantACommonJoinSnapshot()
    {
        var query=Query(snapshot:true);
        CosmicJoinPlanner.Plan(query,ProviderType.MongoDBOASIS,new[]{Target(joins:Joins(graphs:true))})
            .ErrorCode.Should().Be("CQL_CONSISTENCY_UNSUPPORTED");
        CosmicJoinPlanner.Plan(query,ProviderType.MongoDBOASIS,new[]{Target(joins:Joins(graphs:true,snapshot:true))})
            .IsError.Should().BeFalse();
    }

    [Fact]
    public void GraphSourceValidationRequiresExplicitJoinEnrollment()
        =>CosmicJoinPlanner.Plan(Query(snapshot:true),ProviderType.MongoDBOASIS,new[]{Target()})
            .ErrorCode.Should().Be("CQL_CAPABILITY_UNSUPPORTED");

    [Fact]
    public void EverySelectedProviderPreflightsBeforeAPlanIsReturned()
    {
        var targets=new[]{Target(),Target(ProviderType.Neo4jOASIS,Joins(left:false))};
        CosmicJoinPlanner.Plan(Query(CosmicQueryScope.AllEligible),ProviderType.MongoDBOASIS,targets)
            .ErrorCode.Should().Be("CQL_CAPABILITY_UNSUPPORTED");
    }

    [Fact]
    public void MultiProviderWindowsRetainTheExistingFederationRestriction()
        =>CosmicJoinPlanner.Plan(Query(CosmicQueryScope.AllEligible,window:true),ProviderType.MongoDBOASIS,
            new[]{Target(),Target(ProviderType.Neo4jOASIS)}).ErrorCode.Should().Be("CQL_CAPABILITY_UNSUPPORTED");

    [Fact]
    public void MultipleTargetsCannotEnterASingleNativeReadmission()
    {
        var plan=CosmicJoinPlanner.Plan(Query(CosmicQueryScope.AllEligible),ProviderType.MongoDBOASIS,
            new[]{Target(),Target(ProviderType.Neo4jOASIS)}).Result;
        CosmicJoinPlanner.Admit(plan,ProviderType.MongoDBOASIS,Sources(),Joins()).ErrorCode.Should().Be("CQL_PLAN_INVALID");
    }

    [Fact]
    public void DuplicatedAndUnavailableTargetsFailExplicitly()
    {
        CosmicJoinPlanner.Plan(Query(),ProviderType.MongoDBOASIS,new[]{Target(),Target()}).ErrorCode.Should().Be("CQL_PLAN_INVALID");
        CosmicJoinPlanner.Plan(Query(CosmicQueryScope.Named(ProviderType.Neo4jOASIS).Result),ProviderType.MongoDBOASIS,
            new[]{Target()}).ErrorCode.Should().Be("CQL_PLAN_TARGET_UNAVAILABLE");
    }

    [Fact]
    public void CapabilityKindsAreDetachedFromTheirCallerArray()
    {
        var kinds=new[]{CosmicJoinKind.Left};
        var caps=new CosmicJoinCapabilities(kinds,Paths(),Paths());
        kinds[0]=CosmicJoinKind.Inner;
        caps.Admit(Query()).IsError.Should().BeFalse();
    }

    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)]
    [InlineData(4)] [InlineData(5)] [InlineData(6)]
    public void EveryRequestedWorkBudgetIsCheckedAgainstTheActualAdapter(int index)
    {
        var limits=index switch {
            0=>CosmicJoinOptions.Create(maximumLeftRows:999).Result,
            1=>CosmicJoinOptions.Create(maximumRightRows:999).Result,
            2=>CosmicJoinOptions.Create(maximumPairs:9999).Result,
            3=>CosmicJoinOptions.Create(maximumResults:999).Result,
            4=>CosmicJoinOptions.Create(maximumLeftPayloadBytes:1).Result,
            5=>CosmicJoinOptions.Create(maximumRightPayloadBytes:1).Result,
            _=>CosmicJoinOptions.Create(maximumPayloadBytes:1).Result };
        CosmicJoinPlanner.Plan(Query(),ProviderType.MongoDBOASIS,new[]{Target(joins:Joins(limits:limits))})
            .ErrorCode.Should().Be("CQL_CAPABILITY_UNSUPPORTED");
    }

    [Fact]
    public void InvalidKindAndSnapshotDeclarationsCannotConstructCapabilities()
    {
        Action invalid=()=>new CosmicJoinCapabilities(new[]{(CosmicJoinKind)99},Paths(),Paths());
        invalid.Should().Throw<ArgumentException>();
        Action inconsistent=()=>Joins(snapshot:true);
        inconsistent.Should().Throw<ArgumentException>();
    }
    [Fact]
    public void ZeroTakeCannotBypassUnsupportedKeyAdmission()
    {
        var left=CosmicQuery<Holon>.Create().Take(0).Result;
        var query=left.LeftJoin(CosmicQuery<Holon>.Create(),row=>row.Id,row=>row.ParentHolonId)
            .Select((owner,member)=>owner.Name).Result;
        CosmicJoinPlanner.Plan(query,ProviderType.MongoDBOASIS,new[]{Target(joins:Joins(parent:false))})
            .ErrorCode.Should().Be("CQL_CAPABILITY_UNSUPPORTED");
    }

    [Fact]
    public void NullableMetadataEqualityNeedsItsOwnExactDomain()
    {
        var query=CosmicQuery<Holon>.Create().Join(CosmicQuery<Holon>.Create(),
            row=>CosmicQueryMetadata.Value<int?>(row,"key"),row=>CosmicQueryMetadata.Value<int?>(row,"key"))
            .Select((owner,member)=>owner.Name).Result;
        var keys=new CosmicProjectionCapabilities(Array.Empty<CosmicProjectionFieldCapability>(),new[]{typeof(int)});
        var caps=new CosmicJoinCapabilities(new[]{CosmicJoinKind.Inner},keys,Paths());
        CosmicJoinPlanner.Plan(query,ProviderType.MongoDBOASIS,new[]{Target(joins:caps)})
            .ErrorCode.Should().Be("CQL_CAPABILITY_UNSUPPORTED");
    }

    [Fact]
    public void KeyAndOutputWidthsRemainIndependent()
    {
        var query=CosmicQuery<Holon>.Create().LeftJoin(CosmicQuery<Holon>.Create(),
            row=>new {row.Id,row.Name},row=>new {Id=row.ParentHolonId,row.Name})
            .Select((owner,member)=>new {owner.Name,HasRight=member!=null}).Result;
        var narrowKeys=new CosmicJoinCapabilities(new[]{CosmicJoinKind.Left},Paths(),Paths(),maximumKeyComponents:1);
        narrowKeys.Admit(query).ErrorCode.Should().Be("CQL_CAPABILITY_UNSUPPORTED");
        var narrowOutput=new CosmicJoinCapabilities(new[]{CosmicJoinKind.Left},Paths(),Paths(),maximumOutputColumns:1);
        narrowOutput.Admit(query).ErrorCode.Should().Be("CQL_CAPABILITY_UNSUPPORTED");
    }

    [Fact]
    public void BoundActorPredicatesCannotDisappearDuringSourcePlanning()
    {
        var access=CosmicQueryReadAccess.OwnerOrPublic(Guid.NewGuid()).Result;
        var left=CosmicQuery<Holon>.Create().WithReadAccess(access).Result;
        var right=CosmicQuery<Holon>.Create().WithReadAccess(access).Result;
        var query=left.Join(right,row=>row.Id,row=>row.ParentHolonId).Select((owner,member)=>owner.Name).Result;
        CosmicJoinPlanner.Plan(query,ProviderType.MongoDBOASIS,new[]{Target()})
            .ErrorCode.Should().Be("CQL_CAPABILITY_UNSUPPORTED");
        var trusted=new CosmicQueryCapabilities(CosmicQueryFeatures.Or,new[] {
            new CosmicQueryFieldCapability("IsPublic",typeof(bool),CosmicQueryComparisonOperator.Equal),
            new CosmicQueryFieldCapability("CreatedByAvatarId",typeof(Guid),CosmicQueryComparisonOperator.Equal) });
        var plan=CosmicJoinPlanner.Plan(query,ProviderType.MongoDBOASIS,new[]{Target(sources:trusted)});
        plan.IsError.Should().BeFalse(plan.Message);
        plan.Result.LeftSourcePlan.Query.ReadAccess.Should().BeSameAs(access);
        plan.Result.RightSourcePlan.Query.ReadAccess.Should().BeSameAs(access);
    }

    [Fact]
    public void PlannedSnapshotCannotOverrideWeakerActualJoinIsolation()
    {
        var query=Query(snapshot:true);
        var plan=CosmicJoinPlanner.Plan(query,ProviderType.MongoDBOASIS,
            new[]{Target(joins:Joins(graphs:true,snapshot:true))}).Result;
        CosmicJoinPlanner.Admit(plan,ProviderType.MongoDBOASIS,Sources(),Joins(graphs:true))
            .ErrorCode.Should().Be("CQL_CONSISTENCY_UNSUPPORTED");
    }
}