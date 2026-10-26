using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public class CosmicFederationPlanningTests
{
    private static CosmicQueryCapabilities Capabilities(bool paging=true)
    {
        CosmicQueryFieldCapability Field(string name,Type type)=>new(name,type,
            new[]{CosmicQueryComparisonOperator.Equal},Array.Empty<CosmicQueryStringOperator>(),supportsOrdering:true);
        return new(CosmicQueryFeatures.And|CosmicQueryFeatures.Or|CosmicQueryFeatures.Membership
            |CosmicQueryFeatures.Ordering|(paging?CosmicQueryFeatures.Paging:0),
            Field("Name",typeof(string)),Field("Id",typeof(Guid)),Field("IsPublic",typeof(bool)),Field("CreatedByAvatarId",typeof(Guid)));
    }
    private static CosmicQueryTarget Target(ProviderType provider,bool paging=true)=>new(provider,Capabilities(paging));

    [Fact]
    public void ExplicitFederationPreservesSourceWindowsSeparatelyFromTheGlobalWindow()
    {
        var source=CosmicQuery<Holon>.Create(CosmicQueryScope.AllEligible).OrderBy(row=>row.Name).Skip(3).Take(5).Result;
        var targets=new[]{Target(ProviderType.MongoDBOASIS),Target(ProviderType.SQLLiteDBOASIS)};
        Assert.True(CosmicQueryPlanner.Plan(source,ProviderType.MongoDBOASIS,targets).IsError);
        var options=CosmicFederationOptions.Create(skip:7,take:2).Result;
        var planned=CosmicFederationPlanner.Plan(source,options,ProviderType.MongoDBOASIS,targets);
        Assert.False(planned.IsError,planned.Message);
        Assert.Same(source,planned.Result.Source);
        Assert.Equal(7,planned.Result.Options.Skip);
        Assert.Equal(2,planned.Result.Options.Take);
        Assert.Equal(2,planned.Result.Targets.Count);
        foreach(var target in planned.Result.Targets)
        {
            Assert.Equal(3,target.Query.Offset);
            Assert.Equal(5,target.Query.Limit);
            Assert.Single(target.Targets);
            Assert.Equal(target.Targets[0].Provider,Assert.Single(target.Query.Scope.Providers));
        }
    }

    [Fact]
    public void PriorityAndEligibleSnapshotsCannotBeChangedByTheCallerAfterPlanning()
    {
        var priority=new[]{ProviderType.Neo4jOASIS,ProviderType.MongoDBOASIS};
        var options=CosmicFederationOptions.Create(CosmicFederationIdentityMode.ProviderPriority,providerPriority:priority).Result;
        var targets=new[]{Target(ProviderType.MongoDBOASIS),Target(ProviderType.Neo4jOASIS)};
        var plan=CosmicFederationPlanner.Plan(CosmicQuery<Holon>.Create(CosmicQueryScope.AllEligible),options,ProviderType.MongoDBOASIS,targets);
        Assert.False(plan.IsError,plan.Message);
        priority[0]=ProviderType.SQLLiteDBOASIS;
        targets[0]=Target(ProviderType.SQLLiteDBOASIS);
        Assert.Equal(ProviderType.Neo4jOASIS,plan.Result.Options.ProviderPriority[0]);
        Assert.Equal(ProviderType.MongoDBOASIS,plan.Result.Targets[0].Targets[0].Provider);
    }

    [Fact]
    public void CurrentDefaultResolvesToOneImmutableConcreteTarget()
    {
        var plan=CosmicFederationPlanner.Plan(CosmicQuery<Holon>.Create(),CosmicFederationOptions.Create().Result,
            ProviderType.SQLLiteDBOASIS,new[]{Target(ProviderType.MongoDBOASIS),Target(ProviderType.SQLLiteDBOASIS)});
        Assert.False(plan.IsError,plan.Message);
        Assert.Equal(ProviderType.SQLLiteDBOASIS,Assert.Single(Assert.Single(plan.Result.Targets).Targets).Provider);
    }

    [Fact]
    public void ARequiredLaterTargetCannotDisappearOrHaveItsCapabilityIgnored()
    {
        var source=CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.MongoDBOASIS,ProviderType.Neo4jOASIS).Result).Take(1).Result;
        var missing=CosmicFederationPlanner.Plan(source,CosmicFederationOptions.Create().Result,ProviderType.MongoDBOASIS,
            new[]{Target(ProviderType.MongoDBOASIS)});
        Assert.Equal("CQL_PLAN_TARGET_UNAVAILABLE",missing.ErrorCode);
        Assert.Null(missing.Result);
        var denied=CosmicFederationPlanner.Plan(source,CosmicFederationOptions.Create().Result,ProviderType.MongoDBOASIS,
            new[]{Target(ProviderType.MongoDBOASIS),Target(ProviderType.Neo4jOASIS,false)});
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED",denied.ErrorCode);
        Assert.Null(denied.Result);
    }

    [Fact]
    public void AuthorityBoundAccessCannotBeReusedOnAnotherReplica()
    {
        var policy=CosmicQueryReadAccess.AuthoritativeRecords(Guid.NewGuid(),ProviderType.MongoDBOASIS,new[]{Guid.NewGuid()}).Result;
        var query=CosmicQuery<Holon>.Create(CosmicQueryScope.AllEligible).WithReadAccess(policy).Result;
        var result=CosmicFederationPlanner.Plan(query,CosmicFederationOptions.Create().Result,ProviderType.MongoDBOASIS,
            new[]{Target(ProviderType.MongoDBOASIS),Target(ProviderType.SQLLiteDBOASIS)});
        Assert.Equal("CQL_ACCESS_AUTHORITY_MISMATCH",result.ErrorCode);
        Assert.Null(result.Result);
    }

    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)]
    public void UnenrolledAgreementRepeatabilityAndGraphContractsFailExplicitly(int mode)
    {
        var source=CosmicQuery<Holon>.Create();
        if(mode==2)source=source.WithChildren(CosmicQueryGraphOptions.Create().Result).Result;
        var options=CosmicFederationOptions.Create(identityMode:mode==0?CosmicFederationIdentityMode.RequireAgreement:CosmicFederationIdentityMode.PreserveCopies,
            consistency:mode==1?CosmicFederationConsistency.RepeatableSnapshot:CosmicFederationConsistency.MovingProviderReads).Result;
        var result=CosmicFederationPlanner.Plan(source,options,ProviderType.MongoDBOASIS,new[]{Target(ProviderType.MongoDBOASIS)});
        Assert.Equal("CQL_FEDERATION_CAPABILITY_UNSUPPORTED",result.ErrorCode);
        Assert.Null(result.Result);
    }

    [Fact]
    public void AllTargetsNeedAnAttemptReservationAndExactPriorityCoverage()
    {
        var source=CosmicQuery<Holon>.Create(CosmicQueryScope.AllEligible);
        var targets=new[]{Target(ProviderType.MongoDBOASIS),Target(ProviderType.Neo4jOASIS)};
        Assert.Equal("CQL_FEDERATION_BUDGET_INVALID",CosmicFederationPlanner.Plan(source,
            CosmicFederationOptions.Create(maximumProviderAttempts:1).Result,ProviderType.MongoDBOASIS,targets).ErrorCode);
        Assert.Equal("CQL_FEDERATION_PRIORITY_INVALID",CosmicFederationPlanner.Plan(source,
            CosmicFederationOptions.Create(CosmicFederationIdentityMode.ProviderPriority,
                providerPriority:new[]{ProviderType.MongoDBOASIS}).Result,ProviderType.MongoDBOASIS,targets).ErrorCode);
    }

    [Fact]
    public void InvalidOrAmbiguousOptionsAreNeverRepairedOrTruncated()
    {
        Assert.True(CosmicFederationOptions.Create(maximumInputRows:0).IsError);
        Assert.True(CosmicFederationOptions.Create(maximumInputBytes:0).IsError);
        Assert.True(CosmicFederationOptions.Create(maximumOutputBytes:int.MaxValue).IsError);
        Assert.True(CosmicFederationOptions.Create(maximumDurationMilliseconds:0).IsError);
        Assert.True(CosmicFederationOptions.Create(providerPriority:new[]{ProviderType.MongoDBOASIS}).IsError);
        Assert.True(CosmicFederationOptions.Create(CosmicFederationIdentityMode.ProviderPriority).IsError);
        Assert.True(CosmicFederationOptions.Create(CosmicFederationIdentityMode.ProviderPriority,
            providerPriority:new[]{ProviderType.MongoDBOASIS,ProviderType.MongoDBOASIS}).IsError);
    }

    [Fact]
    public void ProviderSpecificPoliciesAreCapturedBeforeIndependentSourceWindows()
    {
        var actor = Guid.NewGuid();
        var mongo = CosmicQueryReadAccess.AuthoritativeRecords(actor, ProviderType.MongoDBOASIS, new[] { Guid.NewGuid() }).Result;
        var sqlite = CosmicQueryReadAccess.AuthoritativeRecords(actor, ProviderType.SQLLiteDBOASIS, new[] { Guid.NewGuid() }).Result;
        var policies = new[] { new CosmicFederationReadPolicy(ProviderType.MongoDBOASIS, mongo),
            new CosmicFederationReadPolicy(ProviderType.SQLLiteDBOASIS, sqlite) };
        var source = CosmicQuery<Holon>.Create(CosmicQueryScope.AllEligible).OrderBy(row => row.Name).Skip(2).Take(3).Result;
        var plan = CosmicFederationPlanner.Plan(source, CosmicFederationOptions.Create().Result, ProviderType.MongoDBOASIS,
            new[] { Target(ProviderType.MongoDBOASIS), Target(ProviderType.SQLLiteDBOASIS) }, policies);
        Assert.False(plan.IsError, plan.Message);
        policies[0] = policies[1];
        Assert.Null(source.ReadAccess);
        Assert.Same(mongo, plan.Result.Targets[0].Query.ReadAccess);
        Assert.Same(sqlite, plan.Result.Targets[1].Query.ReadAccess);
        Assert.All(plan.Result.Targets, target => { Assert.Equal(2, target.Query.Offset); Assert.Equal(3, target.Query.Limit); });
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    public void IncompleteDuplicateForeignOrMalformedTargetPoliciesRejectTheWholePlan(int invalid)
    {
        var actor = Guid.NewGuid();
        CosmicFederationReadPolicy Bind(ProviderType provider, Guid? id = null) => new(provider,
            CosmicQueryReadAccess.AuthoritativeRecords(id ?? actor, provider, Array.Empty<Guid>()).Result);
        var mongo = Bind(ProviderType.MongoDBOASIS);
        var sqlite = Bind(ProviderType.SQLLiteDBOASIS);
        var policies = invalid switch
        {
            0 => Array.Empty<CosmicFederationReadPolicy>(),
            1 => new[] { mongo, mongo },
            2 => new[] { mongo, Bind(ProviderType.SQLLiteDBOASIS, Guid.NewGuid()) },
            3 => new[] { new CosmicFederationReadPolicy(ProviderType.MongoDBOASIS, sqlite.Access), sqlite },
            4 => new[] { mongo, null! },
            5 => new[] { new CosmicFederationReadPolicy(ProviderType.MongoDBOASIS, null!), sqlite },
            _ => new[] { mongo, sqlite, Bind(ProviderType.Neo4jOASIS) }
        };
        var plan = CosmicFederationPlanner.Plan(CosmicQuery<Holon>.Create(CosmicQueryScope.AllEligible),
            CosmicFederationOptions.Create().Result, ProviderType.MongoDBOASIS,
            new[] { Target(ProviderType.MongoDBOASIS), Target(ProviderType.SQLLiteDBOASIS) }, policies);
        Assert.Equal("CQL_FEDERATION_ACCESS_INVALID", plan.ErrorCode);
        Assert.Null(plan.Result);
    }

    [Fact]
    public void PerTargetBindingCannotReplaceAnExistingSourceAccessContract()
    {
        var actor = Guid.NewGuid();
        var source = CosmicQuery<Holon>.Create().WithReadAccess(CosmicQueryReadAccess.OwnerOrPublic(actor).Result).Take(1).Result;
        var replacement = CosmicQueryReadAccess.AuthoritativeRecords(actor, ProviderType.MongoDBOASIS, Array.Empty<Guid>()).Result;
        var plan = CosmicFederationPlanner.Plan(source, CosmicFederationOptions.Create().Result, ProviderType.MongoDBOASIS,
            new[] { Target(ProviderType.MongoDBOASIS) }, new[] { new CosmicFederationReadPolicy(ProviderType.MongoDBOASIS, replacement) });
        Assert.Equal("CQL_ACCESS_REBIND_FORBIDDEN", plan.ErrorCode);
        Assert.Null(plan.Result);
    }

    [Fact]
    public void AnEquivalentExistingPolicyRemainsValidAfterPaging()
    {
        var actor = Guid.NewGuid();
        var access = CosmicQueryReadAccess.OwnerOrPublic(actor).Result;
        var source = CosmicQuery<Holon>.Create().WithReadAccess(access).Take(1).Result;
        var plan = CosmicFederationPlanner.Plan(source, CosmicFederationOptions.Create().Result, ProviderType.MongoDBOASIS,
            new[] { Target(ProviderType.MongoDBOASIS) }, new[] { new CosmicFederationReadPolicy(ProviderType.MongoDBOASIS, access) });
        Assert.False(plan.IsError, plan.Message);
        Assert.Same(access, plan.Result.Targets.Single().Query.ReadAccess);
    }
}
