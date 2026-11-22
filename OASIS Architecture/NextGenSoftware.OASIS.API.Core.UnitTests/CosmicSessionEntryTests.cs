using Moq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.DNA;
namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed class CosmicSessionEntryTests
{
    [Theory]
    [InlineData(ProviderType.MongoDBOASIS)]
    [InlineData(ProviderType.SQLLiteDBOASIS)]
    [InlineData(ProviderType.Neo4jOASIS)]
    public void EntryInspectionKeepsDetachedTrackedReadOnlyAndDisposedSemanticsWithoutIo(ProviderType authority)
    {
        var dna=new OASISDNA {OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS {HyperDriveMode=HyperDriveModes.V2}};
        var manager=new HolonManager(null,dna,new ProviderManager(null,dna));
        var provider=new Mock<ICosmicQueryStorageProvider>(MockBehavior.Strict);
        provider.SetupGet(value=>value.QueryProviderType).Returns(authority);
        using var session=CosmicSession.Create(manager,provider.Object,
            CosmicQueryReadAccess.OwnerOrPublic(Guid.NewGuid()).Result).Result;
        var entity=new Holon{Id=Guid.NewGuid(),Name="original",IsActive=true};
        var detached=session.Entry(entity);
        Assert.False(detached.IsError,detached.Message);
        Assert.Equal(CosmicEntityState.Detached,detached.Result.State);
        Assert.Same(entity,detached.Result.Entity);
        Assert.Empty(session.GetEntries().Result);
        Assert.Equal("COSMIC_SESSION_ENTITY_INVALID",session.Entry(null!).ErrorCode);
        Assert.False(session.Add(entity).IsError);
        var added=session.Entry(entity).Result;
        Assert.Equal(CosmicEntityState.Added,added.State);Assert.False(added.IsReadOnly);
        var duplicate=new Holon{Id=entity.Id,Name="untracked duplicate"};
        Assert.Equal(CosmicEntityState.Detached,session.Entry(duplicate).Result.State);
        Assert.Single(session.GetEntries().Result);
        Assert.False(session.Clear().IsError);
        Assert.Equal(CosmicEntityState.Detached,session.Entry(entity).Result.State);
        Assert.False(session.Attach(entity).IsError);
        Assert.Equal(CosmicEntityState.Unchanged,session.Entry(entity).Result.State);
        entity.Name="edited";
        var modified=session.Entry(entity).Result;
        Assert.Equal(CosmicEntityState.Modified,modified.State);
        Assert.False(session.SetReadOnly(entity,true).IsError);
        var readOnly=session.Entry(entity).Result;
        Assert.True(readOnly.IsReadOnly);Assert.Equal(CosmicEntityState.Unchanged,readOnly.State);
        Assert.Equal(CosmicEntityState.Modified,modified.State);
        Assert.Equal(CosmicEntityState.Added,added.State);
        Assert.Equal(readOnly.State,Assert.Single(session.GetEntries().Result).State);
        session.Dispose();
        Assert.True(session.Entry(entity).IsError);
        Assert.All(provider.Invocations,call=>Assert.Equal("get_QueryProviderType",call.Method.Name));
    }
}
