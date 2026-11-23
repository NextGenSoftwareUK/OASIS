using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
namespace NextGenSoftware.OASIS.API.Core.UnitTests;
public sealed class CosmicProjectedSetCapabilityTests
{
    private static CosmicProjectedSetQuery<Holon,string> Query(CosmicSetQueryKind kind)
    {
        var root=CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.MongoDBOASIS).Result)
            .WithReadAccess(CosmicQueryReadAccess.OwnerOrPublic(Guid.NewGuid()).Result).Result;
        var selected=root.Select(h=>h.Name).Result;
        return CosmicProjectedSetQuery<Holon,string>.Create(selected,selected,kind).Result;
    }
    private static CosmicProjectionCompositionCapabilities Stages(CosmicSelectedStageFeatures stages)
        =>new(stages,CosmicQueryFeatures.None,Array.Empty<CosmicQueryMetadataCapability>(),Array.Empty<Type>());
    [Fact]
    public void PlainTupleSetsDoNotRequireAnInventedResultStage()
    {
        foreach(var kind in Enum.GetValues<CosmicSetQueryKind>())
        {
            var capabilities=new CosmicProjectedSetCapabilities(
                CosmicSetQueryFeatures.Union|CosmicSetQueryFeatures.Concat|CosmicSetQueryFeatures.Intersect|CosmicSetQueryFeatures.Except,
                new[]{typeof(string)},new[]{typeof(string)},Stages(CosmicSelectedStageFeatures.None));
            var result=capabilities.Admit(Query(kind));
            Assert.False(result.IsError,result.Message);
        }
    }
    [Fact]
    public void ConcatPublicationDoesNotGrantTupleEqualityAndRegistriesAreCaptured()
    {
        var values=new[]{typeof(string)};
        var capabilities=new CosmicProjectedSetCapabilities(CosmicSetQueryFeatures.Concat|CosmicSetQueryFeatures.Union,
            values,Array.Empty<Type>(),Stages(CosmicSelectedStageFeatures.None));
        values[0]=typeof(long);
        Assert.False(capabilities.Admit(Query(CosmicSetQueryKind.Concat)).IsError);
        Assert.Equal(typeof(string),Assert.Single(capabilities.ValueDomains));
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED",capabilities.Admit(Query(CosmicSetQueryKind.Union)).ErrorCode);
    }
    [Fact]
    public void ZeroTakeCannotBypassOperationEqualityOrOutputStageAdmission()
    {
        var query=Query(CosmicSetQueryKind.Union).Take(0).Result;
        var noEquality=new CosmicProjectedSetCapabilities(CosmicSetQueryFeatures.Union,new[]{typeof(string)},
            Array.Empty<Type>(),Stages(CosmicSelectedStageFeatures.Paging));
        Assert.True(noEquality.Admit(query).IsError);
        var noPaging=new CosmicProjectedSetCapabilities(CosmicSetQueryFeatures.Union,new[]{typeof(string)},
            new[]{typeof(string)},Stages(CosmicSelectedStageFeatures.None));
        Assert.True(noPaging.Admit(query).IsError);
        var allowed=new CosmicProjectedSetCapabilities(CosmicSetQueryFeatures.Union,new[]{typeof(string)},
            new[]{typeof(string)},Stages(CosmicSelectedStageFeatures.Paging));
        Assert.False(allowed.Admit(query).IsError);
    }
}
