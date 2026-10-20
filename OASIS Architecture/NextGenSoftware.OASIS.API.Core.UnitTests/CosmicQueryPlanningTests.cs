using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public class CosmicQueryPlanningTests
{
    [Fact]
    public void MetadataPresenceAndNullCapabilitiesDoNotGrantScalarMetadataTranslation()
    {
        var exists = CosmicQuery<Holon>.Create().Where(h => CosmicQueryMetadata.Exists(h, "x")).Result;
        var isNull = CosmicQuery<Holon>.Create().Where(h => CosmicQueryMetadata.IsNull(h, "x")).Result;
        var profile = new CosmicQueryCapabilities(CosmicQueryFeatures.MetadataExists, Array.Empty<CosmicQueryFieldCapability>());
        Assert.False(profile.Admit(exists).IsError); Assert.True(profile.Admit(isNull).IsError);
        var scalar = CosmicQuery<Holon>.Create().Where(h => CosmicQueryMetadata.Value<int>(h, "x") == 1).Result;
        Assert.True(profile.Admit(scalar).IsError);
        var nullOnly = new CosmicQueryCapabilities(CosmicQueryFeatures.MetadataNull, Array.Empty<CosmicQueryFieldCapability>());
        Assert.False(nullOnly.Admit(isNull).IsError); Assert.True(nullOnly.Admit(exists).IsError);
    }

    [Fact]
    public void ExactFieldAdmissionDoesNotInferSupportFromAnotherFieldOfTheSameType()
    {
        var capability = new CosmicQueryCapabilities(CosmicQueryFeatures.NullLiteral,
            new CosmicQueryFieldCapability("Name", typeof(string), CosmicQueryComparisonOperator.Equal),
            new CosmicQueryFieldCapability("Version", typeof(int), CosmicQueryComparisonOperator.Equal, CosmicQueryComparisonOperator.GreaterThan));
        Assert.True(capability.RestrictsCanonicalFields);
        Assert.False(capability.Admit(CosmicQuery<Holon>.Create().Where(h => h.Name == "ok").Result).IsError);
        Assert.True(capability.Admit(CosmicQuery<Holon>.Create().Where(h => h.Description == "ok").Result).IsError);
        Assert.True(capability.Admit(CosmicQuery<Holon>.Create().Where(h => h.Name != "ok").Result).IsError);
        Assert.False(capability.Admit(CosmicQuery<Holon>.Create().Where(h => h.Version > 2).Result).IsError);
        var wrongType = new CosmicQueryCapabilities(CosmicQueryFeatures.None,
            new CosmicQueryFieldCapability("Version", typeof(long), CosmicQueryComparisonOperator.GreaterThan));
        Assert.True(wrongType.Admit(CosmicQuery<Holon>.Create().Where(h => h.Version > 2).Result).IsError);
    }

    [Fact]
    public void ExactFieldProfilesAreImmutableAndRejectAmbiguousDeclarations()
    {
        var operations = new[] { CosmicQueryComparisonOperator.Equal };
        var field = new CosmicQueryFieldCapability("Name", typeof(string), operations);
        var fields = new[] { field };
        var profile = new CosmicQueryCapabilities(CosmicQueryFeatures.None, fields);
        fields[0] = new CosmicQueryFieldCapability("Description", typeof(string), CosmicQueryComparisonOperator.NotEqual);
        operations[0] = CosmicQueryComparisonOperator.NotEqual;
        Assert.Same(field, Assert.Single(profile.CanonicalFields));
        Assert.Equal(CosmicQueryComparisonOperator.Equal, Assert.Single(field.Comparisons));
        Assert.Throws<ArgumentException>(() => new CosmicQueryCapabilities(CosmicQueryFeatures.None, field, field));
        Assert.Throws<ArgumentException>(() => new CosmicQueryCapabilities(CosmicQueryFeatures.None, new CosmicQueryFieldCapability[] { null! }));
        Assert.Throws<ArgumentException>(() => new CosmicQueryFieldCapability(" ", typeof(string), CosmicQueryComparisonOperator.Equal));
    }

    private static CosmicQueryCapabilities Capability(CosmicQueryFeatures features = CosmicQueryFeatures.None, params Type[] types)
        => new(features, types, System.Enum.GetValues<CosmicQueryComparisonOperator>());

    [Fact]
    public void CurrentDefaultNeverExpandsToOtherAuthorizedTargets()
    {
        var targets = new[] { new CosmicQueryTarget(ProviderType.MongoDBOASIS, Capability()), new CosmicQueryTarget(ProviderType.SQLLiteDBOASIS, Capability()) };
        var query = CosmicQuery<Holon>.Create();
        var plan = CosmicQueryPlanner.Plan(query, ProviderType.SQLLiteDBOASIS, targets);
        Assert.False(plan.IsError, plan.Message); Assert.Same(query, plan.Result.Query);
        Assert.Equal(ProviderType.SQLLiteDBOASIS, Assert.Single(plan.Result.Targets).Provider);
        var unavailable = CosmicQueryPlanner.Plan(query, ProviderType.Default, targets);
        Assert.True(unavailable.IsError); Assert.Equal("CQL_PLAN_TARGET_UNAVAILABLE", unavailable.ErrorCode); Assert.Null(unavailable.Result);
    }

    [Fact]
    public void NamedScopePreservesOrderAndFailsWholePlanWhenOneTargetCannotPushDown()
    {
        var scope = CosmicQueryScope.Named(ProviderType.SQLLiteDBOASIS, ProviderType.MongoDBOASIS).Result;
        var query = CosmicQuery<Holon>.Create(scope).Where(h => h.Version > 2).Result;
        var capable = new CosmicQueryTarget(ProviderType.SQLLiteDBOASIS, Capability(CosmicQueryFeatures.None, typeof(int)));
        var unsupported = new CosmicQueryTarget(ProviderType.MongoDBOASIS, Capability());
        var failed = CosmicQueryPlanner.Plan(query, ProviderType.SQLLiteDBOASIS, new[] { capable, unsupported });
        Assert.True(failed.IsError); Assert.Equal("CQL_CAPABILITY_UNSUPPORTED", failed.ErrorCode); Assert.Null(failed.Result);
        var supported = new CosmicQueryTarget(ProviderType.MongoDBOASIS, Capability(CosmicQueryFeatures.None, typeof(int)));
        var success = CosmicQueryPlanner.Plan(query, ProviderType.MongoDBOASIS, new[] { supported, capable });
        Assert.False(success.IsError, success.Message); Assert.Equal(scope.Providers, success.Result.Targets.Select(t => t.Provider));
    }

    [Fact]
    public void AllEligibleDoesNotSilentlyDropUnsupportedTargetsOrAcceptEmptySnapshot()
    {
        var query = CosmicQuery<Holon>.Create(CosmicQueryScope.AllEligible).Where(h => h.IsActive).Result;
        var failed = CosmicQueryPlanner.Plan(query, ProviderType.None, new[] { new CosmicQueryTarget(ProviderType.MongoDBOASIS, Capability()) });
        Assert.True(failed.IsError); Assert.Equal("CQL_CAPABILITY_UNSUPPORTED", failed.ErrorCode);
        var empty = CosmicQueryPlanner.Plan(query, ProviderType.None, Array.Empty<CosmicQueryTarget>());
        Assert.True(empty.IsError); Assert.Equal("CQL_PLAN_NO_TARGET", empty.ErrorCode);
    }

    [Fact]
    public void CapabilitiesAdmitLogicalMetadataNullableAndComparisonRequirementsIndependently()
    {
        var metadata = CosmicQuery<Holon>.Create().Where(h => CosmicQueryMetadata.Value<int?>(h, "score") != null).Result;
        Assert.True(Capability(CosmicQueryFeatures.Metadata, typeof(int?)).Admit(metadata).IsError);
        Assert.True(Capability(CosmicQueryFeatures.Metadata | CosmicQueryFeatures.NullLiteral, typeof(int)).Admit(metadata).IsError);
        Assert.False(Capability(CosmicQueryFeatures.Metadata | CosmicQueryFeatures.NullLiteral, typeof(int?)).Admit(metadata).IsError);
        var logical = CosmicQuery<Holon>.Create().Where(h => !h.IsActive || h.IsPublic).Result;
        Assert.True(Capability(CosmicQueryFeatures.Or, typeof(bool)).Admit(logical).IsError);
        Assert.False(Capability(CosmicQueryFeatures.Or | CosmicQueryFeatures.Not, typeof(bool)).Admit(logical).IsError);
        var chained = CosmicQuery<Holon>.Create().Where(h => h.IsActive).Result.Where(h => h.IsPublic).Result;
        Assert.True(Capability(CosmicQueryFeatures.None, typeof(bool)).Admit(chained).IsError);
        Assert.False(Capability(CosmicQueryFeatures.And, typeof(bool)).Admit(chained).IsError);
        var constant = CosmicQuery<Holon>.Create().Where(h => true).Result;
        Assert.True(Capability().Admit(constant).IsError);
        Assert.False(Capability(CosmicQueryFeatures.BooleanConstant).Admit(constant).IsError);
        var limited = new CosmicQueryCapabilities(CosmicQueryFeatures.None, new[] { typeof(int) }, new[] { CosmicQueryComparisonOperator.Equal });
        Assert.True(limited.Admit(CosmicQuery<Holon>.Create().Where(h => h.Version > 1).Result).IsError);
    }

    [Fact]
    public void SnapshotsAreDetachedAndMalformedRegistryCannotProduceAPlan()
    {
        var types = new[] { typeof(int) }; var operations = new[] { CosmicQueryComparisonOperator.Equal };
        var capability = new CosmicQueryCapabilities(CosmicQueryFeatures.None, types, operations);
        types[0] = typeof(string); operations[0] = CosmicQueryComparisonOperator.GreaterThan;
        Assert.Equal(typeof(int), Assert.Single(capability.ValueTypes)); Assert.Equal(CosmicQueryComparisonOperator.Equal, Assert.Single(capability.Comparisons));
        var target = new CosmicQueryTarget(ProviderType.MongoDBOASIS, capability);
        var duplicate = CosmicQueryPlanner.Plan(CosmicQuery<Holon>.Create(), ProviderType.MongoDBOASIS, new[] { target, target });
        Assert.True(duplicate.IsError); Assert.Equal("CQL_PLAN_INVALID", duplicate.ErrorCode);
        var missing = CosmicQueryPlanner.Plan(CosmicQuery<Holon>.Create(), ProviderType.MongoDBOASIS, new CosmicQueryTarget[] { null! });
        Assert.True(missing.IsError); Assert.Equal("CQL_PLAN_INVALID", missing.ErrorCode);
        var oversized = CosmicQueryPlanner.Plan(CosmicQuery<Holon>.Create(), ProviderType.MongoDBOASIS, new CosmicQueryTarget[65]);
        Assert.True(oversized.IsError); Assert.Equal("CQL_PLAN_INVALID", oversized.ErrorCode);
        var source = new[] { target }; var plan = CosmicQueryPlanner.Plan(CosmicQuery<Holon>.Create(), ProviderType.MongoDBOASIS, source);
        Assert.False(plan.IsError, plan.Message); source[0] = null!; Assert.Same(target, Assert.Single(plan.Result.Targets));
    }
}
