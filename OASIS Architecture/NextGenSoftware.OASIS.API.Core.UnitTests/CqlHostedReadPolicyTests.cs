using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public partial class CqlV1Web4Tests
{
    private sealed class HostedReadPolicy(Func<Guid, ProviderType, OASISResult<CosmicQueryReadAccess>> authorize) : ICqlReadAccessPolicy
    {
        public int Calls;
        public OASISResult<CosmicQueryReadAccess> Authorize(Guid actor, ProviderType authority)
        {
            Calls++;
            return authorize(actor, authority);
        }
    }

    [Fact]
    public async Task AnEnrolledServiceFactoryReturningNullCannotBecomeOwnerPublicAccess()
    {
        var source=new Source(ProviderType.MongoDBOASIS);
        source.Rows.Add(new Holon{Id=Guid.NewGuid(),IsPublic=true});
        var admitted=Admission(ProviderType.MongoDBOASIS);
        using var services=new ServiceCollection().AddSingleton<ICqlReadAccessPolicy>(_=>null!).BuildServiceProvider();
        admitted.Http.RequestServices=services;
        var result=await admitted.Policy.RunAsync(()=>CqlV1Endpoint.ExecuteAsync(admitted.Http,new CqlV1Request(),source));
        Assert.Equal("CQL_ACCESS_POLICY_INVALID",Code(result));
        Assert.Equal(503,Assert.IsType<ObjectResult>(result).StatusCode);
        Assert.Equal(0,source.Managers);
        Assert.Empty(admitted.Receipts);
    }

    [Theory]
    [InlineData(ProviderType.MongoDBOASIS)]
    [InlineData(ProviderType.SQLLiteDBOASIS)]
    [InlineData(ProviderType.Neo4jOASIS)]
    public async Task HostedReadPolicyCapturesActorAuthorityAndPrivateScopeBeforePaging(ProviderType type)
    {
        var source = new Source(type);
        var id = Guid.NewGuid();
        source.Rows.Add(new Holon { Id = id, CreatedByAvatarId = Guid.NewGuid(), IsPublic = false });
        source.Adapter.SetupGet(adapter => adapter.QueryCapabilities).Returns(new CosmicQueryCapabilities(
            Capabilities.Features | CosmicQueryFeatures.Membership, Capabilities.CanonicalFields.ToArray()));
        var admitted = Admission(type);
        var service = new HostedReadPolicy((actor, authority) =>
        {
            Assert.Equal(Actor, actor);
            Assert.Equal(type, authority);
            // Mutating the host's avatar object cannot replace the captured actor.
            ((IAvatar)admitted.Http.Items["Avatar"]!).Id = Guid.NewGuid();
            return CosmicQueryReadAccess.AuthoritativeRecords(actor, authority, new[] { id });
        });
        using var services = new ServiceCollection().AddSingleton<ICqlReadAccessPolicy>(service).BuildServiceProvider();
        admitted.Http.RequestServices = services;
        var result = await admitted.Policy.RunAsync(() => CqlV1Endpoint.ExecuteAsync(admitted.Http,
            new CqlV1Request { Skip = 2, Take = 1 }, source));
        Assert.True(result is FileContentResult, result is ObjectResult failed ? Code(failed) : result.GetType().Name);
        var file = Assert.IsType<FileContentResult>(result);
        Assert.Equal(1, service.Calls);
        Assert.Equal(1, source.Lookups);
        Assert.Equal(type, source.Observed!.Query.ReadAccess.Authority);
        Assert.Equal(Actor, source.Observed.Query.ReadAccess.ActorId);
        Assert.IsType<CosmicQueryMembership>(Assert.Single(source.Observed.Query.Predicates));
        Assert.Equal(2, source.Observed.Query.Offset);
        Assert.Equal(1, source.Observed.Query.Limit);
        using var json = JsonDocument.Parse(file.FileContents);
        Assert.Equal(id, json.RootElement.GetProperty("result").GetProperty("rows")[0].GetProperty("id").GetGuid());
        Assert.Single(admitted.Receipts);
    }

    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)]
    [InlineData(3)] [InlineData(4)] [InlineData(5)]
    public async Task EnrolledPolicyDenialStopsEveryQueryFamilyBeforeManagerAcquisition(int route)
    {
        var source = new Source(ProviderType.MongoDBOASIS);
        var admitted = Admission(ProviderType.MongoDBOASIS);
        var service = new HostedReadPolicy((_, _) => new() { IsError = true, ErrorCode = "private-denial-detail" });
        using var services = new ServiceCollection().AddSingleton<ICqlReadAccessPolicy>(service).BuildServiceProvider();
        admitted.Http.RequestServices = services;
        Task<IActionResult> Run() => route switch
        {
            0 => CqlV1Endpoint.ExecuteAsync(admitted.Http, new CqlV1Request(), source),
            1 => CqlV1Endpoint.ExecuteScalarAsync(admitted.Http, new CqlScalarV1Request { Operation = "Count" }, source),
            2 => CqlV1Endpoint.ExecuteProjectionAsync(admitted.Http, new CqlProjectionV1Request(), source),
            3 => CqlV1Endpoint.ExecuteGroupedAsync(admitted.Http, new CqlGroupedV1Request(), source),
            4 => CqlV1Endpoint.ExecuteJoinAsync(admitted.Http, new CqlJoinV1Request(), source),
            _ => CqlV1Endpoint.ExecuteCompositionAsync(admitted.Http, new CqlCompositionV1Request(), source)
        };
        var result = await admitted.Policy.RunAsync(Run);
        Assert.Equal(403, Assert.IsType<ObjectResult>(result).StatusCode);
        Assert.Equal("CQL_ACCESS_FORBIDDEN", Code(result));
        Assert.Equal(1, service.Calls);
        Assert.Equal(0, source.Managers);
        Assert.Null(source.Observed);
        Assert.Empty(admitted.Receipts);
    }

    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)]
    [InlineData(3)] [InlineData(4)] [InlineData(5)]
    public async Task MalformedOrFailedHostPolicyCannotFallBackToOwnerPublic(int mode)
    {
        var source = new Source(ProviderType.MongoDBOASIS);
        source.Rows.Add(new Holon { Id = Guid.NewGuid(), IsPublic = true });
        var admitted = Admission(ProviderType.MongoDBOASIS);
        var service = new HostedReadPolicy((actor, authority) => mode switch
        {
            0 => null!,
            1 => CosmicQueryReadAccess.OwnerOrPublic(actor),
            2 => CosmicQueryReadAccess.AuthoritativeRecords(Guid.NewGuid(), authority, Array.Empty<Guid>()),
            3 => CosmicQueryReadAccess.AuthoritativeRecords(actor, ProviderType.Neo4jOASIS, Array.Empty<Guid>()),
            4 => new() { IsWarning = true, Result = CosmicQueryReadAccess.AuthoritativeRecords(actor, authority, Array.Empty<Guid>()).Result },
            _ => throw new InvalidOperationException("Do not publish private policy details.")
        });
        using var services = new ServiceCollection().AddSingleton<ICqlReadAccessPolicy>(service).BuildServiceProvider();
        admitted.Http.RequestServices = services;
        var result = await admitted.Policy.RunAsync(() => CqlV1Endpoint.ExecuteAsync(admitted.Http, new CqlV1Request(), source));
        Assert.Equal(503, Assert.IsType<ObjectResult>(result).StatusCode);
        Assert.Equal(mode == 5 ? "CQL_ACCESS_POLICY_FAILED" : "CQL_ACCESS_POLICY_INVALID", Code(result));
        Assert.Equal(0, source.Managers);
        Assert.Empty(admitted.Receipts);
    }
}
