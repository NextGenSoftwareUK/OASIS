using Moq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.DNA;
namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed class CosmicSessionRegistrationTests
{
    [Fact]
    public void RangesShareSingleRegistrationRulesAndPreserveAcceptedRootsWithoutIo()
    {
        var dna=new OASISDNA {OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS {HyperDriveMode=HyperDriveModes.V2}};
        var provider=new Mock<ICosmicQueryStorageProvider>(MockBehavior.Strict);
        provider.SetupGet(p=>p.QueryProviderType).Returns(ProviderType.MongoDBOASIS);
        using var session=CosmicSession.Create(new HolonManager(null,dna,new ProviderManager(null,dna)),
            provider.Object,CosmicQueryReadAccess.OwnerOrPublic(Guid.NewGuid()).Result).Result;
        var a=new Holon{Id=Guid.NewGuid(),Name="a"};
        var b=new Holon{Id=Guid.NewGuid(),Name="b"};
        Assert.False(session.AddRange(new[]{a,b}).IsError);
        Assert.Equal(CosmicEntityState.Added,session.State(a).Result);
        Assert.Equal("COSMIC_SESSION_ENTITY_TRACKED",session.Add(a).ErrorCode);
        session.Clear();
        Assert.False(session.AttachRange(new[]{a,b}).IsError);
        Assert.Equal(CosmicEntityState.Unchanged,session.State(a).Result);
        a.Name="edited";Assert.Equal(CosmicEntityState.Modified,session.State(a).Result);
        var c=new Holon{Id=Guid.NewGuid()};
        var failed=session.AddRange(new[]{c,new Holon{Id=b.Id}});
        Assert.True(failed.IsError);
        Assert.Equal("COSMIC_SESSION_IDENTITY_CONFLICT",failed.ErrorCode);
        Assert.Same(c,Assert.Single(failed.Result));
        Assert.Equal(CosmicEntityState.Added,session.State(c).Result);
        Assert.Equal(CosmicEntityState.Unchanged,session.State(b).Result);
        IEnumerable<Holon> Reentrant()
        {
            Assert.Equal("COSMIC_SESSION_BUSY",session.Clear().ErrorCode);
            yield return new Holon{Id=Guid.NewGuid()};
        }
        Assert.False(session.AddRange(Reentrant()).IsError);
        Assert.False(session.Add(new Holon()).IsError);
        Assert.All(provider.Invocations,call=>Assert.Equal("get_QueryProviderType",call.Method.Name));
    }
}
