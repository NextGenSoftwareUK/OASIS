using Moq;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Core.Security;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public class CosmicQueryableMetadataPolicyTests
{
    [Fact]
    public void ExportPolicyReusesEncryptionExemptionsWithoutResolvingKeys()
    {
        var resolver=new Mock<IHolonMetadataKeyResolver>(MockBehavior.Strict);
        var encryption=new HolonMetadataEncryptionPolicy("demo","key-reference",resolver.Object,new[]{"category"});
        var requested=new[]{"category","STARNETHolonId","Status"};
        var policy=new HolonQueryableMetadataPolicy(requested,encryption);
        requested[0]="private";
        Assert.Equal(new[]{"STARNETHolonId","Status","category"},policy.Keys);
        Assert.Equal(policy.ProfileHash,new HolonQueryableMetadataPolicy(new[]{"Status","category","STARNETHolonId"},encryption).ProfileHash);
        Assert.NotEqual(policy.ProfileHash,new HolonQueryableMetadataPolicy(new[]{"Status"}).ProfileHash);
        resolver.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("private")] [InlineData("status")] [InlineData("__oasis_enc__")]
    [InlineData("")] [InlineData("bad\nkey")]
    public void PrivateUnconfiguredOrInvalidRootsCannotBeExported(string key)
        =>Assert.Throws<ArgumentException>(()=>new HolonQueryableMetadataPolicy(new[]{key}));

    [Fact]
    public void CiphertextEnvelopeCannotBeEnrolledEvenByAnAdditionalKeyExemption()
    {
        var encryption=new HolonMetadataEncryptionPolicy("demo","key",Mock.Of<IHolonMetadataKeyResolver>(),new[]{"__oasis_enc__"});
        Assert.Throws<ArgumentException>(()=>new HolonQueryableMetadataPolicy(new[]{"__oasis_enc__"},encryption));
        Assert.Throws<ArgumentException>(()=>new HolonQueryableMetadataPolicy(Array.Empty<string>()));
        Assert.Throws<ArgumentException>(()=>new HolonQueryableMetadataPolicy(new[]{"Status","Status"}));
    }

    private static CosmicQueryCapabilities Capabilities => new(
        CosmicQueryFeatures.And|CosmicQueryFeatures.Or|CosmicQueryFeatures.Not|CosmicQueryFeatures.Metadata|
        CosmicQueryFeatures.MetadataExists|CosmicQueryFeatures.MetadataNull|CosmicQueryFeatures.NullLiteral|
        CosmicQueryFeatures.Ordering|CosmicQueryFeatures.Membership|CosmicQueryFeatures.StringContains|
        CosmicQueryFeatures.FieldComparison|CosmicQueryFeatures.ChildExists,
        Array.Empty<CosmicQueryFieldCapability>(),new[]{new CosmicQueryMetadataCapability(typeof(string),
            Enum.GetValues<CosmicQueryComparisonOperator>(),Enum.GetValues<CosmicQueryStringOperator>(),
            supportsMembership:true,supportsOrdering:true)});

    [Theory]
    [InlineData("comparison")] [InlineData("order")] [InlineData("exists")] [InlineData("null")]
    [InlineData("membership")] [InlineData("contains")] [InlineData("field")] [InlineData("logical")] [InlineData("child")]
    public void AllMetadataPredicateAndOrderingShapesRespectExactRootEnrollment(string shape)
    {
        CosmicQuery<Holon> Query(string root)
        {
            var query=CosmicQuery<Holon>.Create();
            var choices=new[]{"coffee","tea"};
            var result=shape switch
            {
                "order"=>query.OrderBy(row=>CosmicQueryMetadata.Value<string>(row,root,"nested")),
                "exists"=>query.Where(row=>CosmicQueryMetadata.Exists(row,root,"nested")),
                "null"=>query.Where(row=>CosmicQueryMetadata.IsNull(row,root,"nested")),
                "membership"=>query.Where(row=>choices.Contains(CosmicQueryMetadata.Value<string>(row,root))),
                "contains"=>query.Where(row=>CosmicQueryMetadata.Value<string>(row,root).Contains("coffee")),
                "field"=>query.Where(row=>CosmicQueryMetadata.Value<string>(row,"Status")==CosmicQueryMetadata.Value<string>(row,root)),
                "logical"=>query.Where(row=>CosmicQueryMetadata.Value<string>(row,"Status")=="active"||!(CosmicQueryMetadata.Value<string>(row,root)=="coffee")),
                "child"=>query.Where(row=>row.Children.Any(child=>CosmicQueryMetadata.Value<string>(child,root)=="coffee")),
                _=>query.Where(row=>CosmicQueryMetadata.Value<string>(row,root)=="coffee")
            };
            Assert.False(result.IsError,result.Message);return result.Result;
        }
        var unrestricted=Capabilities;
        var requested=new[]{"Status"};
        var restricted=unrestricted.WithMetadataRoots(requested);requested[0]="private";
        Assert.False(restricted.Admit(Query("Status")).IsError);
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED",restricted.Admit(Query("private")).ErrorCode);
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED",restricted.Admit(Query("status")).ErrorCode);
        Assert.False(unrestricted.Admit(Query("private")).IsError);
    }

    [Fact]
    public void RestrictedCapabilitiesCanOnlyNarrowAndEmptyMeansNoMetadataRoots()
    {
        var restricted=Capabilities.WithMetadataRoots(new[]{"Status","Name"});
        Assert.Throws<ArgumentException>(()=>restricted.WithMetadataRoots(new[]{"private"}));
        Assert.Equal(new[]{"Status"},restricted.WithMetadataRoots(new[]{"Status"}).MetadataRootKeys);
        var denied=restricted.WithMetadataRoots(Array.Empty<string>());
        var query=CosmicQuery<Holon>.Create().Where(row=>CosmicQueryMetadata.Exists(row,"Status")).Result;
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED",denied.Admit(query).ErrorCode);
    }
}
