using Moq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.DNA;
namespace NextGenSoftware.OASIS.API.Core.UnitTests;
public sealed class CosmicStatelessGetTests
{
    private static CosmicStatelessSession Open(Mock<ICosmicQueryStorageProvider> provider)
    {
        var dna=new OASISDNA {OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS {HyperDriveMode=HyperDriveModes.V2}};
        return CosmicStatelessSession.Create(new HolonManager(null,dna,new ProviderManager(null,dna)),
            provider.Object,CosmicQueryReadAccess.OwnerOrPublic(Guid.NewGuid()).Result).Result;
    }
    [Fact]
    public async Task InvalidAndCancelledKeysNeverDispatchNativeReads()
    {
        var provider=new Mock<ICosmicQueryStorageProvider>(MockBehavior.Strict);
        provider.SetupGet(p=>p.QueryProviderType).Returns(ProviderType.MongoDBOASIS);
        using var session=Open(provider);
        Assert.Equal("COSMIC_STATELESS_ENTITY_INVALID",(await session.GetAsync<Holon>(Guid.Empty)).ErrorCode);
        Assert.Equal("CQL_QUERY_CANCELLED",session.Get<Holon>(Guid.NewGuid(),new CancellationToken(true)).ErrorCode);
        Assert.All(provider.Invocations,call=>Assert.Equal("get_QueryProviderType",call.Method.Name));
    }
    [Fact]
    public async Task AuthorityDriftAndDisposedLifetimeRefuseBeforeNativeDispatch()
    {
        var authority=ProviderType.MongoDBOASIS;
        var provider=new Mock<ICosmicQueryStorageProvider>(MockBehavior.Strict);
        provider.SetupGet(p=>p.QueryProviderType).Returns(()=>authority);
        var session=Open(provider);authority=ProviderType.SQLLiteDBOASIS;
        Assert.Equal("COSMIC_STATELESS_AUTHORITY_CHANGED",(await session.GetAsync<Holon>(Guid.NewGuid())).ErrorCode);
        authority=ProviderType.MongoDBOASIS;session.Dispose();
        Assert.Equal("COSMIC_STATELESS_DISPOSED",session.Get<Holon>(Guid.NewGuid()).ErrorCode);
        Assert.All(provider.Invocations,call=>Assert.Equal("get_QueryProviderType",call.Method.Name));
    }
}
