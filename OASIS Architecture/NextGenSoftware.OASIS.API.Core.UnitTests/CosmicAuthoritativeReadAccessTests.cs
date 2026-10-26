using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public class CosmicAuthoritativeReadAccessTests
{
    [Fact]
    public void TrustedScopeSnapshotsTheWholeSetAndDoesNotAdmitPublicOrOwnerOutsideIt()
    {
        var actor = Guid.NewGuid();
        var id = Guid.NewGuid();
        var supplied = new[] { id };
        var access = CosmicQueryReadAccess.AuthoritativeRecords(actor, ProviderType.MongoDBOASIS, supplied);
        Assert.False(access.IsError, access.Message);
        supplied[0] = Guid.NewGuid();

        Assert.True(access.Result.CanRead(new Holon { Id = id, CreatedByAvatarId = Guid.NewGuid() }));
        Assert.False(access.Result.CanRead(new Holon { Id = supplied[0], IsPublic = true, CreatedByAvatarId = actor }));
        Assert.False(access.Result.CanRead(new Holon { Id = Guid.NewGuid(), MetaData = new()
            { ["tenantRole"] = "administrator", ["SHARED_WITH"] = actor.ToString("D") } }));
    }

    [Theory]
    [InlineData(ProviderType.Default)]
    [InlineData(ProviderType.None)]
    [InlineData(ProviderType.All)]
    [InlineData((ProviderType)int.MaxValue)]
    public void ATrustedCatalogueMustNameOneConcreteAuthority(ProviderType authority)
        => Assert.Equal("CQL_ACCESS_INVALID", CosmicQueryReadAccess.AuthoritativeRecords(
            Guid.NewGuid(), authority, new[] { Guid.NewGuid() }).ErrorCode);

    [Fact]
    public void InvalidRecipientCatalogueCannotBePartiallyAccepted()
    {
        var id = Guid.NewGuid();
        foreach (var ids in new IEnumerable<Guid>[] { new[] { id, id }, new[] { id, Guid.Empty },
            Enumerable.Range(0, 129).Select(_ => Guid.NewGuid()), ThrowingCatalogue(id) })
        {
            var result = CosmicQueryReadAccess.AuthoritativeRecords(Guid.NewGuid(), ProviderType.MongoDBOASIS, ids);
            Assert.True(result.IsError);
            Assert.Null(result.Result);
        }
        Assert.True(CosmicQueryReadAccess.AuthoritativeRecords(Guid.Empty, ProviderType.MongoDBOASIS, new[] { id }).IsError);
        Assert.True(CosmicQueryReadAccess.AuthoritativeRecords(Guid.NewGuid(), ProviderType.MongoDBOASIS, null!).IsError);
    }

    private static IEnumerable<Guid> ThrowingCatalogue(Guid id)
    {
        yield return id;
        throw new InvalidOperationException("Incomplete authoritative catalogue.");
    }

    [Fact]
    public void EmptyAuthoritativeScopeDeniesEvenPublicRecords()
    {
        var access = CosmicQueryReadAccess.AuthoritativeRecords(Guid.NewGuid(), ProviderType.MongoDBOASIS, Array.Empty<Guid>());
        Assert.False(access.IsError, access.Message);
        Assert.False(access.Result.CanRead(new Holon { Id = Guid.NewGuid(), IsPublic = true }));
    }

    [Fact]
    public void SameActorCannotReplaceOneTrustedScopeWithAnotherOrWithPublicAccess()
    {
        var actor = Guid.NewGuid();
        var ids = new[] { Guid.NewGuid(), Guid.NewGuid() };
        var first = CosmicQueryReadAccess.AuthoritativeRecords(actor, ProviderType.MongoDBOASIS, ids).Result;
        var query = CosmicQuery<Holon>.Create().WithReadAccess(first).Result;
        var equivalent = CosmicQueryReadAccess.AuthoritativeRecords(actor, ProviderType.MongoDBOASIS, ids.Reverse()).Result;
        Assert.False(query.WithReadAccess(equivalent).IsError);
        Assert.Equal("CQL_ACCESS_REBIND_FORBIDDEN", query.WithReadAccess(CosmicQueryReadAccess.OwnerOrPublic(actor).Result).ErrorCode);
        var changed = CosmicQueryReadAccess.AuthoritativeRecords(actor, ProviderType.MongoDBOASIS, new[] { ids[0] }).Result;
        Assert.Equal("CQL_ACCESS_REBIND_FORBIDDEN", query.WithReadAccess(changed).ErrorCode);
    }

    [Theory]
    [InlineData(ProviderType.SQLLiteDBOASIS)]
    [InlineData(ProviderType.Neo4jOASIS)]
    public void PlannerCannotSubstituteAnotherAuthorityForTheSameLogicalIds(ProviderType selected)
    {
        var access = CosmicQueryReadAccess.AuthoritativeRecords(Guid.NewGuid(), ProviderType.MongoDBOASIS,
            new[] { Guid.NewGuid() }).Result;
        var query = CosmicQuery<Holon>.Create().WithReadAccess(access).Result;
        var caps = new CosmicQueryCapabilities(CosmicQueryFeatures.Membership,
            new[] { new CosmicQueryFieldCapability("Id", typeof(Guid), CosmicQueryComparisonOperator.Equal) });
        var plan = CosmicQueryPlanner.Plan(query, selected, new[] { new CosmicQueryTarget(selected, caps) });
        Assert.Equal("CQL_ACCESS_AUTHORITY_MISMATCH", plan.ErrorCode);
        Assert.Null(plan.Result);
    }

    [Fact]
    public void JoinSourcesCannotMixDistinctPoliciesForTheSameActor()
    {
        var actor = Guid.NewGuid();
        var first = CosmicQueryReadAccess.AuthoritativeRecords(actor, ProviderType.MongoDBOASIS, new[] { Guid.NewGuid() }).Result;
        var second = CosmicQueryReadAccess.AuthoritativeRecords(actor, ProviderType.MongoDBOASIS, new[] { Guid.NewGuid() }).Result;
        var left = CosmicQuery<Holon>.Create().WithReadAccess(first).Result;
        var right = CosmicQuery<Holon>.Create().WithReadAccess(second).Result;
        var join = left.Join(right, row => row.Id, row => row.Id, CosmicJoinOptions.Create().Result);
        Assert.Equal("CQL_JOIN_ACCESS_MISMATCH", join.ErrorCode);
        Assert.Null(join.Result);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PublicationRequiresTheTrustedIdentityEvenWhenAdapterReturnsAPublicRow(bool admitted)
    {
        var id = Guid.NewGuid();
        var access = CosmicQueryReadAccess.AuthoritativeRecords(Guid.NewGuid(), ProviderType.MongoDBOASIS, new[] { id }).Result;
        var row = new Holon { Id = admitted ? id : Guid.NewGuid(), IsPublic = !admitted };
        var adapter = new CatalogueAdapter(row);
        var query = CosmicQuery<Holon>.Create().WithReadAccess(access).Result;
        var plan = CosmicQueryPlanner.Plan(query, ProviderType.MongoDBOASIS,
            new[] { new CosmicQueryTarget(ProviderType.MongoDBOASIS, adapter.QueryCapabilities) });
        Assert.False(plan.IsError, plan.Message);
        var result = await CosmicQueryExecutor.ExecuteAsync(plan.Result, new ICosmicQueryStorageProvider[] { adapter });
        Assert.Equal(!admitted, result.IsError);
        if (!admitted) Assert.Equal("CQL_VISIBILITY_VIOLATION", result.ErrorCode);
        else Assert.Equal(id, result.Result.Providers.Single().Rows.Single().Id);
    }

    private sealed class CatalogueAdapter(IHolon row) : ICosmicQueryStorageProvider
    {
        public ProviderType QueryProviderType => ProviderType.MongoDBOASIS;
        public CosmicQueryCapabilities QueryCapabilities => new(CosmicQueryFeatures.Membership,
            new[] { new CosmicQueryFieldCapability("Id", typeof(Guid), CosmicQueryComparisonOperator.Equal) });
        public Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(CosmicQueryPlan<T> plan,
            int maximumResults = 100, CancellationToken cancellationToken = default) where T : IHolon
            => Task.FromResult(new OASISResult<IReadOnlyList<IHolon>>(new[] { row }) { IsLoaded = true });
    }
}
