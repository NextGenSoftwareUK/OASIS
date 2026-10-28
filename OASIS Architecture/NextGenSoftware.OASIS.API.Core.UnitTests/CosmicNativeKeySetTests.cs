using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed class CosmicNativeKeySetTests
{
    private const string Key="NativeRecordKey";
    private static CosmicQueryCapabilities Profile()=>CosmicQueryCapabilities.ForStringMetadataKeyLookup(Key,128,3);
    private static CosmicQuery<Holon> Query(string[] keys)=>CosmicQuery<Holon>.Create().Where(row=>keys.Contains(CosmicQueryMetadata.Value<string>(row,Key))).Result;
    [Fact]
    public void NativeMembershipIsFrozenBoundedAndIndependentOfPointShape()
    {
        var keys=new[]{"one","two","three"};var query=Query(keys);keys[0]="changed";
        var profile=Profile();Assert.False(profile.Admit(query).IsError);
        var selected=profile.StringKeySetLookup.SelectKeys(query);Assert.False(selected.IsError);
        Assert.Equal(new[]{"one","two","three"},selected.Result);
        Assert.True(profile.StringPointLookup.SelectKey(query).IsError);
        var point=CosmicQuery<Holon>.Create().Where(row=>CosmicQueryMetadata.Value<string>(row,Key)=="one").Result;
        Assert.False(profile.Admit(point).IsError);Assert.Equal("one",profile.StringPointLookup.SelectKey(point).Result);
    }
    [Theory]
    [InlineData("")] [InlineData("two words")] [InlineData("line\nfeed")] [InlineData("é")]
    public void InvalidSourceKeysAreRejectedBeforeDispatch(string key)
        =>Assert.Equal("CQL_CAPABILITY_UNSUPPORTED",Profile().Admit(Query(new[]{key})).ErrorCode);
    [Fact]
    public void SourceBoundExtraPredicatesOrderingAndGraphsAreNotSilentlyScanned()
    {
        var profile=Profile();var query=Query(new[]{"one","two"});
        Assert.True(profile.Admit(Query(new[]{"one","two","three","four"})).IsError);
        Assert.True(profile.Admit(query.Where(row=>row.Name=="coffee").Result).IsError);
        Assert.True(profile.Admit(query.OrderBy(row=>row.Name).Result).IsError);
        Assert.True(profile.Admit(query.WithChildren(CosmicQueryGraphOptions.Create(maxChildDepth:1).Result).Result).IsError);
        var foreign=CosmicQuery<Holon>.Create().Where(row=>new[]{"one","two"}.Contains(CosmicQueryMetadata.Value<string>(row,"Other"))).Result;
        Assert.True(profile.Admit(foreign).IsError);
    }
    [Fact]
    public void ScopedKeySetCanPlanAlongsideMongoWithoutGrantingPointFederation()
    {
        var scope=CosmicQueryScope.Named(ProviderType.IPFSOASIS,ProviderType.MongoDBOASIS).Result;
        var keys=new[]{"one","two"};
        var query=CosmicQuery<Holon>.Create(scope).Where(row=>keys.Contains(CosmicQueryMetadata.Value<string>(row,Key))).Result;
        var mongo=new CosmicQueryCapabilities(CosmicQueryFeatures.Metadata|CosmicQueryFeatures.Membership,
            Array.Empty<CosmicQueryFieldCapability>(),new[]{new CosmicQueryMetadataCapability(typeof(string),new[]{CosmicQueryComparisonOperator.Equal},supportsMembership:true)});
        var targets=new[]{new CosmicQueryTarget(ProviderType.IPFSOASIS,Profile()),new CosmicQueryTarget(ProviderType.MongoDBOASIS,mongo)};
        var plan=CosmicQueryPlanner.Plan(query,ProviderType.MongoDBOASIS,targets);
        Assert.False(plan.IsError,plan.Message);Assert.Equal(2,plan.Result.Targets.Count);
        var point=CosmicQuery<Holon>.Create(scope).Where(row=>CosmicQueryMetadata.Value<string>(row,Key)=="one").Result;
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED",CosmicQueryPlanner.Plan(point,ProviderType.MongoDBOASIS,targets).ErrorCode);
    }
    [Fact]
    public void MetadataRootNarrowingPreservesKeySetAdmissionWithoutBroadeningIt()
    {
        var query=Query(new[]{"one","two"});var profile=Profile();
        var allowed=profile.WithMetadataRoots(new[]{Key});Assert.Same(profile.StringKeySetLookup,allowed.StringKeySetLookup);
        Assert.False(allowed.Admit(query).IsError);
        Assert.True(profile.WithMetadataRoots(new[]{"Other"}).Admit(query).IsError);
    }
}
