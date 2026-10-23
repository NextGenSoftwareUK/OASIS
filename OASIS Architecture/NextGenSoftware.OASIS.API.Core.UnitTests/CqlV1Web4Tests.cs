using System.Security.Claims;
using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging;
using Moq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public class CqlV1Web4Tests
{
    [Theory]
    [InlineData("Id")] [InlineData("Name")] [InlineData("Description")]
    [InlineData("Version")] [InlineData("IsActive")] [InlineData("ParentHolonId")]
    public void ChildCanonicalFieldsBindTheirDeclaredInheritedInterfaceMember(string field)
    {
        object value=field switch{"Id" or "ParentHolonId"=>Guid.NewGuid().ToString("D"),"Version"=>3,"IsActive"=>true,_=>"value"};
        var request=new CqlV1Request{AnyChildren=new[]{new CqlV1ChildExists{Filters=new[]{
            new CqlV1Filter{Field=field,Value=JsonSerializer.SerializeToElement(value)}}}}};
        var query=CqlV1Compiler.Compile(request,Guid.NewGuid(),ProviderType.SQLLiteDBOASIS);
        var child=Assert.Single(query.Predicates.OfType<CosmicQueryAnyChild>());
        Assert.Equal(field,Assert.Single(Assert.IsType<CosmicQueryComparison>(child.Predicate).Path.Segments));
    }

    [Fact]
    public void ChildExistenceGroupsFreezePredicatesAndRetainServerActorBeforePaging()
    {
        var actor=Guid.NewGuid();
        var first=new CqlV1Filter{Field="Name",Operator=CqlV1Operator.Equal,Value=JsonSerializer.SerializeToElement("public child")};
        var request=new CqlV1Request{Skip=1,Take=2,AnyChildren=new[]{
            new CqlV1ChildExists{Filters=new[]{first}},
            new CqlV1ChildExists{AnyOf=new[]{new CqlV1Filter{MetadataPath=new[]{"stock"},Scalar=CqlV1Scalar.Integer,
                Operator=CqlV1Operator.GreaterThan,Value=JsonSerializer.SerializeToElement(9007199254740993L)}}}}};
        var query=CqlV1Compiler.Compile(request,actor,ProviderType.MongoDBOASIS);
        Assert.Equal(actor,query.ReadAccess.ActorId);Assert.Equal(1,query.Offset);Assert.Equal(2,query.Limit);
        var clauses=query.Predicates.OfType<CosmicQueryAnyChild>().ToArray();Assert.Equal(2,clauses.Length);
        first.Value=JsonSerializer.SerializeToElement("edited after compilation");
        Assert.Equal("public child",Assert.IsType<CosmicQueryComparison>(clauses[0].Predicate).Literal.Value);
        Assert.Equal(9007199254740993L,Assert.IsType<CosmicQueryComparison>(clauses[1].Predicate).Literal.Value);
    }

    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)]
    [InlineData(4)] [InlineData(5)] [InlineData(6)]
    public void ChildExistenceRejectsInvalidOrUnboundedDescriptions(int fault)
    {
        var filter=new CqlV1Filter{Field="Name",Value=JsonSerializer.SerializeToElement("match")};
        var group=new CqlV1ChildExists{Filters=new[]{filter}};
        var request=new CqlV1Request{AnyChildren=new[]{group}};
        switch(fault)
        {
            case 0:request.AnyChildren=null!;break;
            case 1:request.AnyChildren=new CqlV1ChildExists[]{null!};break;
            case 2:group.Filters=null!;break;
            case 3:group.Filters=Array.Empty<CqlV1Filter>();break;
            case 4:request.AnyChildren=Enumerable.Repeat(group,5).ToArray();break;
            case 5:request.Filters=Enumerable.Repeat(filter,12).ToArray();break;
            case 6:filter.Field="CreatedByAvatarId";break;
        }
        Assert.Throws<ArgumentException>(()=>CqlV1Compiler.Compile(request,Guid.NewGuid(),ProviderType.MongoDBOASIS));
    }

    [Fact]
    public void DescendantGroupsFreezeBoundsAndExpressionsWithTheServerActor()
    {
        var actor=Guid.NewGuid();
        var filter=new CqlV1Filter{MetadataPath=new[]{"precision"},Scalar=CqlV1Scalar.Integer,
            Value=JsonSerializer.SerializeToElement(9007199254740993L)};
        var group=new CqlV1DescendantExists{MaximumDepth=32,MaximumNodes=10000,AnyOf=new[]{filter}};
        var request=new CqlV1Request{AnyDescendants=new[]{group},Skip=1,Take=2};
        var query=CqlV1Compiler.Compile(request,actor,ProviderType.MongoDBOASIS);
        var clause=Assert.Single(query.Predicates.OfType<CosmicQueryAnyDescendant>());
        group.MaximumDepth=1;group.MaximumNodes=1;filter.Value=JsonSerializer.SerializeToElement(0);
        Assert.Equal(32,clause.Traversal.MaximumDepth);Assert.Equal(10000,clause.Traversal.MaximumNodes);
        Assert.Equal(9007199254740993L,Assert.IsType<CosmicQueryComparison>(clause.Predicate).Literal.Value);
        Assert.Equal(actor,query.ReadAccess.ActorId);Assert.Equal(1,query.Offset);Assert.Equal(2,query.Limit);
        Assert.Null(query.GraphOptions);
    }

    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)] [InlineData(4)]
    [InlineData(5)] [InlineData(6)] [InlineData(7)] [InlineData(8)] [InlineData(9)]
    [InlineData(10)] [InlineData(11)] [InlineData(12)] [InlineData(13)]
    public void DescendantGroupsRejectInvalidBoundsAndSharedBudgetOverflow(int fault)
    {
        var filter=new CqlV1Filter{Field="Name",Value=JsonSerializer.SerializeToElement("match")};
        var group=new CqlV1DescendantExists{Filters=new[]{filter}};
        var request=new CqlV1Request{AnyDescendants=new[]{group}};
        switch(fault)
        {
            case 0:request.AnyDescendants=null!;break;
            case 1:request.AnyDescendants=new CqlV1DescendantExists[]{null!};break;
            case 2:group.Filters=null!;break;
            case 3:group.Filters=Array.Empty<CqlV1Filter>();break;
            case 4:group.MaximumDepth=0;break;
            case 5:group.MaximumDepth=33;break;
            case 6:group.MaximumNodes=0;break;
            case 7:group.MaximumNodes=10001;break;
            case 8:request.AnyDescendants=Enumerable.Repeat(group,5).ToArray();break;
            case 9:request.AnyChildren=Enumerable.Repeat(new CqlV1ChildExists{Filters=new[]{filter}},4).ToArray();break;
            case 10:request.Filters=Enumerable.Repeat(filter,12).ToArray();break;
            case 11:filter.Field="CreatedByAvatarId";break;
            case 12:group.AnyOf=null!;break;
            case 13:request.AnyChildren=new[]{new CqlV1ChildExists{Filters=Enumerable.Repeat(filter,12).ToArray()}};break;
        }
        Assert.Throws<ArgumentException>(()=>CqlV1Compiler.Compile(request,Guid.NewGuid(),ProviderType.Neo4jOASIS));
    }

    [Fact]
    public void GraphRequestBindsImmutablePolicyAndShapeBeforeBusinessPaging()
    {
        var actor=Guid.NewGuid();
        var request=new CqlV1Request{Take=1,Graph=new CqlV1Graph{MaxChildDepth=3,ChildType="Quest",MaximumNodes=12}};
        var query=CqlV1Compiler.Compile(request,actor,ProviderType.MongoDBOASIS);
        Assert.Equal(actor,query.ReadAccess.ActorId);Assert.Equal(3,query.GraphOptions.MaxChildDepth);
        Assert.Equal(HolonType.Quest,query.GraphOptions.ChildType);Assert.Equal(12,query.GraphOptions.MaximumNodes);
        Assert.Equal(1,query.Limit);
        request.Graph.MaxChildDepth=8;request.Graph.ChildType="All";
        Assert.Equal(3,query.GraphOptions.MaxChildDepth);Assert.Equal(HolonType.Quest,query.GraphOptions.ChildType);
    }

    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)]
    [InlineData(4)] [InlineData(5)] [InlineData(6)] [InlineData(7)]
    public void GraphRequestRejectsUnboundedOrNoncanonicalShape(int fault)
    {
        var shape=new CqlV1Graph();
        switch(fault)
        {
            case 0:shape.MaxChildDepth=0;break;case 1:shape.MaxChildDepth=9;break;
            case 2:shape.MaximumNodes=0;break;case 3:shape.MaximumNodes=1001;break;
            case 4:shape.MaximumPayloadBytes=0;break;case 5:shape.MaximumPayloadBytes=1048577;break;
            case 6:shape.ChildType="0";break;case 7:shape.Consistency="snapshot";break;
        }
        Assert.Throws<ArgumentException>(()=>CqlV1Compiler.Compile(new CqlV1Request{Graph=shape},Guid.NewGuid(),ProviderType.MongoDBOASIS));
    }

    [Fact]
    public void GraphProjectionContainsSelectedFieldsWithoutPrivateMembershipOrProviderState()
    {
        var hidden=Guid.NewGuid();var ancestor=Guid.NewGuid();
        var root=new Holon{Id=Guid.NewGuid(),Name="public root",IsPublic=true,ParentHolonId=ancestor};
        var child=new Holon{Id=Guid.NewGuid(),ParentHolonId=root.Id,IsPublic=true};
        root.Children.Add(child);
        root.ChildRoleManifestJson=Core.Serialization.HolonChildRoleManifestJson.Serialize(new HolonChildRoleManifest(
            1,root.Id,new[]{new HolonChildRole("Children",new[]{child.Id,hidden})}));
        root.ProviderUniqueStorageKey[ProviderType.MongoDBOASIS]="private-provider-key";
        root.MetaData["city"]="London";
        var projected=CqlV1Projection.Create(new[]{root},CosmicQueryReadAccess.OwnerOrPublic(Guid.NewGuid()).Result,
            true,CosmicQueryGraphOptions.Create(maximumNodes:2).Result);
        var row=Assert.Single(projected);Assert.Equal(Guid.Empty,row.ParentHolonId);
        Assert.Equal(root.Id,Assert.Single(row.Children).ParentHolonId);
        var json=JsonSerializer.Serialize(projected);
        Assert.Contains("London",json);
        Assert.DoesNotContain(hidden.ToString(),json);Assert.DoesNotContain(ancestor.ToString(),json);
        Assert.DoesNotContain("ChildRoleManifest",json);Assert.DoesNotContain("ProviderUnique",json);
        Assert.DoesNotContain("private-provider-key",json);
    }

    [Fact]
    public void PublicGraphDepthCannotExceedTheTransportBound()
    {
        var root=new Holon{Id=Guid.NewGuid(),IsPublic=true};var parent=root;
        for(var depth=0;depth<=CqlV1Projection.MaximumDepth;depth++)
        {
            var child=new Holon{Id=Guid.NewGuid(),ParentHolonId=parent.Id,IsPublic=true};
            parent.Children.Add(child);parent=child;
        }
        var error=Assert.Throws<CqlV1Projection.Rejected>(()=>CqlV1Projection.Create(new[]{root},
            CosmicQueryReadAccess.OwnerOrPublic(Guid.NewGuid()).Result,false,CosmicQueryGraphOptions.Create().Result));
        Assert.Equal("CQL_RESPONSE_LIMIT",error.Code);
    }

    [Fact]
    public void CancelledProjectionDoesNotReturnAnEmptySuccess()
    {
        using var cancellation=new CancellationTokenSource();cancellation.Cancel();
        Assert.Throws<OperationCanceledException>(()=>CqlV1Projection.Create(Array.Empty<IHolon>(),
            CosmicQueryReadAccess.OwnerOrPublic(Guid.NewGuid()).Result,false,null,cancellation.Token));
    }

    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)]
    public void GraphProjectionRejectsForeignCyclesDepthAndBudget(int fault)
    {
        var root=new Holon{Id=Guid.NewGuid(),IsPublic=true};
        var child=new Holon{Id=Guid.NewGuid(),ParentHolonId=root.Id,IsPublic=fault!=0};
        root.Children.Add(child);
        if(fault==1)child.Children.Add(root);
        if(fault==2)child.Children.Add(new Holon{Id=Guid.NewGuid(),ParentHolonId=child.Id,IsPublic=true});
        var options=CosmicQueryGraphOptions.Create(maximumNodes:fault==3?1:10,maxChildDepth:fault==2?1:0).Result;
        var error=Assert.Throws<CqlV1Projection.Rejected>(()=>CqlV1Projection.Create(new[]{root},
            CosmicQueryReadAccess.OwnerOrPublic(Guid.NewGuid()).Result,false,options));
        Assert.Equal(fault==0?"CQL_VISIBILITY_VIOLATION":fault==3?"CQL_RESPONSE_LIMIT":"CQL_PROVIDER_RESULT_INVALID",error.Code);
    }

    [Fact]
    public void OverlappingRootTreesRemainIndependentWhileLeafShapeIsUnchanged()
    {
        var root=new Holon{Id=Guid.NewGuid(),IsPublic=true,ParentHolonId=Guid.NewGuid()};
        var child=new Holon{Id=Guid.NewGuid(),IsPublic=true,ParentHolonId=root.Id};root.Children.Add(child);
        var policy=CosmicQueryReadAccess.OwnerOrPublic(Guid.NewGuid()).Result;
        var graphs=CqlV1Projection.Create(new[]{root,child},policy,false,CosmicQueryGraphOptions.Create().Result);
        Assert.Equal(2,graphs.Count);Assert.Single(graphs[0].Children);Assert.Empty(graphs[1].Children);
        var leaf=Assert.Single(CqlV1Projection.Create(new[]{root},policy,false,null));
        Assert.Equal(root.ParentHolonId,leaf.ParentHolonId);
        Assert.DoesNotContain("Children",JsonSerializer.Serialize(leaf));
    }

    [Theory]
    [InlineData(CqlV1Operator.Contains)]
    [InlineData(CqlV1Operator.StartsWith)]
    [InlineData(CqlV1Operator.EndsWith)]
    public void TransportStringOperatorsCompileWithPortableOrdinalSemantics(CqlV1Operator operation)
    {
        var request = new CqlV1Request
        {
            Filters = new[] { new CqlV1Filter
            {
                Field = "Name", Operator = operation, Value = JsonSerializer.SerializeToElement("coffee")
            }}
        };
        var query = CqlV1Compiler.Compile(request, Guid.NewGuid(), ProviderType.SQLLiteDBOASIS);
        Assert.Equal(2, query.Predicates.Count); // Mandatory access predicate plus the native string predicate.
    }
    private static readonly Guid Actor = Guid.Parse("b1c6016e-f31c-47cc-8ba7-cf1811d4b872");
    private static CosmicQueryCapabilities Capabilities
    {
        get
        {
            var equal = new[] { CosmicQueryComparisonOperator.Equal, CosmicQueryComparisonOperator.NotEqual };
            CosmicQueryFieldCapability Field(string name, Type type) => new(name, type, equal,
                Array.Empty<CosmicQueryStringOperator>(), supportsOrdering: true);
            return new(CosmicQueryFeatures.Or | CosmicQueryFeatures.And | CosmicQueryFeatures.BooleanConstant |
                CosmicQueryFeatures.Ordering | CosmicQueryFeatures.Paging,
                Field("Name", typeof(string)), Field("Id", typeof(Guid)), Field("Version", typeof(int)),
                Field("IsPublic", typeof(bool)), Field("CreatedByAvatarId", typeof(Guid)));
        }
    }

    private sealed class Source : ICqlV1Providers
    {
        internal readonly Mock<IOASISStorageProvider> Storage = new();
        internal readonly Mock<ICosmicQueryStorageProvider> Adapter;
        internal int Lookups, Managers;
        internal CosmicQueryPlan<Holon>? Observed;
        internal List<IHolon> Rows = new();
        internal Source(ProviderType type)
        {
            Storage.SetupGet(p => p.ProviderType).Returns(new EnumValue<ProviderType>(type));
            Adapter = Storage.As<ICosmicQueryStorageProvider>();
            Adapter.SetupGet(p => p.QueryProviderType).Returns(type);
            Adapter.SetupGet(p => p.QueryCapabilities).Returns(Capabilities);
            Adapter.Setup(p => p.ExecuteCosmicQueryAsync(It.IsAny<CosmicQueryPlan<Holon>>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Returns((CosmicQueryPlan<Holon> plan, int limit, CancellationToken token) =>
                {
                    Observed = plan;
                    return Task.FromResult(new OASISResult<IReadOnlyList<IHolon>>(Rows));
                });
        }
        public IOASISStorageProvider Resolve(ProviderType? requested) { Lookups++; return Storage.Object; }
        public HolonManager CreateManager(IOASISStorageProvider provider)
        {
            Managers++;
            var dna = new OASISDNA { OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS { HyperDriveMode = HyperDriveModes.V2 } };
            return new HolonManager(provider, dna, new ProviderManager(null, dna));
        }
    }

    private static (DefaultHttpContext Http, PreparedStorageExecution Policy, Mock<IUsageOutboxStore> Store, List<UsageProviderReceipt> Receipts)
        Admission(ProviderType type, Guid? reservedActor = null, bool tariff = true)
    {
        var store = new Mock<IUsageOutboxStore>();
        var receipts = new List<UsageProviderReceipt>();
        store.Setup(s => s.AppendReceiptAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<UsageProviderReceipt>(), It.IsAny<CancellationToken>()))
            .Callback((string id, string owner, UsageProviderReceipt receipt, CancellationToken token) => { receipt.Validate(); receipts.Add(receipt); })
            .Returns(Task.CompletedTask);
        var handle = new UsageExecutionHandle(new UsageOutboxOperation
        {
            Id = Guid.NewGuid().ToString("D"), UserId = (reservedActor ?? Actor).ToString("D"), ExecutionOwner = "test",
            Policy = new UsageEndpointPolicy { RequiresProviderMeasurement = true }
        }, store.Object, new Web4UsageLedgerOptions(), new SubscriptionAuthorizationResult(), Mock.Of<IUsageReservationAuthority>());
        var http = new DefaultHttpContext();
        http.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, Actor.ToString("D")) }, "test-validated"));
        http.Items["Avatar"] = new Avatar { Id = Actor };
        http.Items[Web4UsageMeasurement.ExecutionKey] = handle;
        var policy = new PreparedStorageExecution(handle, "cql-http-test", new Dictionary<ProviderType, string>(), new[] { type },
            tariff ? new[] { new StorageQueryTariff(type, 0.002m, "test-v1") } : null);
        return (http, policy, store, receipts);
    }

    private static string Code(IActionResult result) => Assert.IsType<OASISResult<object>>(Assert.IsType<ObjectResult>(result).Value).ErrorCode;
    private static JsonElement Json(string value) => JsonDocument.Parse(value).RootElement.Clone();

    [Theory]
    [InlineData(ProviderType.MongoDBOASIS)]
    [InlineData(ProviderType.SQLLiteDBOASIS)]
    [InlineData(ProviderType.Neo4jOASIS)]
    public async Task AccessRestrictionPrecedesNativePageAndBindsExactTariffReceipt(ProviderType type)
    {
        var source = new Source(type);
        source.Rows.Add(new Holon { Id = Guid.NewGuid(), Name = "visible", CreatedByAvatarId = Actor });
        var admitted = Admission(type);
        var request = new CqlV1Request { Provider = type.ToString(), Skip = 3, Take = 5 };
        var result = await admitted.Policy.RunAsync(() => CqlV1Endpoint.ExecuteAsync(admitted.Http, request, source));

        var file = Assert.IsType<FileContentResult>(result);
        var predicate = Assert.IsType<CosmicQueryLogical>(Assert.Single(source.Observed!.Query.Predicates));
        Assert.False(predicate.IsAnd);
        var publicBranch = Assert.IsType<CosmicQueryComparison>(predicate.Left);
        var ownerBranch = Assert.IsType<CosmicQueryComparison>(predicate.Right);
        Assert.Equal("IsPublic", Assert.Single(publicBranch.Path.Segments));
        Assert.Equal(true, publicBranch.Literal.Value);
        Assert.Equal("CreatedByAvatarId", Assert.Single(ownerBranch.Path.Segments));
        Assert.Equal(Actor, ownerBranch.Literal.Value);
        Assert.Equal(3, source.Observed.Query.Offset);
        Assert.Equal(5, source.Observed.Query.Limit);
        var receipt = Assert.Single(admitted.Receipts);
        Assert.Equal(type.ToString(), receipt.Provider);
        Assert.Equal(1, receipt.Units);
        Assert.Equal(0.002m, receipt.ActualCostUsd);
        Assert.Equal("test-v1", receipt.PricingCatalogueVersion);
        Assert.Equal(receipt.ClientRequestId, receipt.ProviderRequestId);
        using var response = JsonDocument.Parse(file.FileContents);
        Assert.Equal(type.ToString(), response.RootElement.GetProperty("result").GetProperty("provider").GetString());
        Assert.DoesNotContain("providerUniqueStorageKey", System.Text.Encoding.UTF8.GetString(file.FileContents), StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(ProviderType.MongoDBOASIS)]
    [InlineData(ProviderType.SQLLiteDBOASIS)]
    [InlineData(ProviderType.Neo4jOASIS)]
    public async Task WrongReservationActorCannotResolveOrRead(ProviderType type)
    {
        var source = new Source(type);
        var admitted = Admission(type, Guid.NewGuid());
        var result = await admitted.Policy.RunAsync(() => CqlV1Endpoint.ExecuteAsync(admitted.Http, new(), source));
        Assert.Equal("CQL_ADMISSION_REQUIRED", Code(result));
        Assert.Equal(0, source.Lookups);
        Assert.Empty(admitted.Receipts);
    }

    [Fact]
    public async Task MissingAdmissionAndAnonymousRequestsCannotResolve()
    {
        var source = new Source(ProviderType.SQLLiteDBOASIS);
        var admitted = Admission(ProviderType.SQLLiteDBOASIS);
        Assert.Equal("CQL_ADMISSION_REQUIRED", Code(await CqlV1Endpoint.ExecuteAsync(admitted.Http, new(), source)));
        admitted.Http.User = new ClaimsPrincipal(new ClaimsIdentity());
        Assert.Equal("CQL_AUTHENTICATION_REQUIRED", Code(await CqlV1Endpoint.ExecuteAsync(admitted.Http, new(), source)));
        Assert.Equal(0, source.Lookups);
    }

    [Fact]
    public async Task MissingExplicitTariffCannotCreateManagerOrRead()
    {
        var type = ProviderType.SQLLiteDBOASIS;
        var source = new Source(type); var admitted = Admission(type, tariff: false);
        var result = await admitted.Policy.RunAsync(() => CqlV1Endpoint.ExecuteAsync(admitted.Http, new(), source));
        Assert.Equal("CQL_PROVIDER_NOT_ELIGIBLE", Code(result));
        Assert.Equal(0, source.Managers);
        Assert.Empty(admitted.Receipts);
    }

    [Fact]
    public async Task ExplicitProviderCannotBeSilentlyRedirectedByTheHostLookup()
    {
        var source = new Source(ProviderType.SQLLiteDBOASIS);
        var admitted = Admission(ProviderType.SQLLiteDBOASIS);
        var result = await admitted.Policy.RunAsync(() => CqlV1Endpoint.ExecuteAsync(admitted.Http,
            new() { Provider = "MongoDBOASIS" }, source));
        Assert.Equal("CQL_PROVIDER_NOT_ELIGIBLE", Code(result));
        Assert.Equal(0, source.Managers);
    }

    [Fact]
    public async Task ReturnedRowsCannotExceedTheRequestedPage()
    {
        var source = new Source(ProviderType.SQLLiteDBOASIS);
        source.Rows.AddRange(Enumerable.Range(0, 2).Select(_ => new Holon { Id = Guid.NewGuid(), CreatedByAvatarId = Actor }));
        var admitted = Admission(ProviderType.SQLLiteDBOASIS);
        var result = await admitted.Policy.RunAsync(() => CqlV1Endpoint.ExecuteAsync(admitted.Http, new() { Take = 1 }, source));
        Assert.Equal("CQL_PROVIDER_RESULT_INVALID", Code(result));
        Assert.Single(admitted.Receipts);
    }

    [Fact]
    public async Task FailedReceiptPersistenceCannotPublishSuccessfulRows()
    {
        var source = new Source(ProviderType.SQLLiteDBOASIS);
        var admitted = Admission(ProviderType.SQLLiteDBOASIS);
        admitted.Store.Setup(s => s.AppendReceiptAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<UsageProviderReceipt>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IOException("Controlled accounting outage."));
        var result = await admitted.Policy.RunAsync(() => CqlV1Endpoint.ExecuteAsync(admitted.Http, new(), source));
        Assert.Equal(PreparedStorageExecution.RejectedCode, Code(result));
    }

    [Fact]
    public async Task ForgedShareIndexOrProviderViolationCannotPublishPrivateRows()
    {
        var type = ProviderType.Neo4jOASIS;
        var source = new Source(type);
        source.Rows.Add(new Holon { Id = Guid.NewGuid(), CreatedByAvatarId = Guid.NewGuid(),
            MetaData = new() { ["SHARED_WITH_" + Actor.ToString("N")] = "true" } });
        var admitted = Admission(type);
        var result = await admitted.Policy.RunAsync(() => CqlV1Endpoint.ExecuteAsync(admitted.Http, new(), source));
        Assert.Equal("CQL_VISIBILITY_VIOLATION", Code(result));
        Assert.Single(admitted.Receipts); // Attempt tariff is independent of logical success.
    }

    [Theory]
    [InlineData("All")]
    [InlineData("Default")]
    [InlineData("999")]
    [InlineData("EthereumOASIS")]
    public async Task InvalidProviderDoesNotResolve(string provider)
    {
        var type = ProviderType.SQLLiteDBOASIS;
        var source = new Source(type); var admitted = Admission(type);
        var result = await admitted.Policy.RunAsync(() => CqlV1Endpoint.ExecuteAsync(admitted.Http, new() { Provider = provider }, source));
        Assert.Equal("CQL_REQUEST_INVALID", Code(result));
        Assert.Equal(0, source.Lookups);
    }

    [Fact]
    public void DescriptionCompilerSupportsNestedMetadataAndBoundedOrWithoutErasingAccess()
    {
        var request = new CqlV1Request
        {
            Filters = new[] { new CqlV1Filter { MetadataPath = new[] { "location", "city" }, Value = Json("\"London\"") } },
            AnyOf = new[]
            {
                new CqlV1Filter { Field = "Name", Value = Json("\"coffee\"") },
                new CqlV1Filter { MetadataPath = new[] { "Rating" }, Scalar = CqlV1Scalar.Integer, Operator = CqlV1Operator.GreaterThan, Value = Json("7") }
            }
        };
        var query = CqlV1Compiler.Compile(request, Actor, ProviderType.SQLLiteDBOASIS);
        Assert.Equal(3, query.Predicates.Count);
        var metadata = Assert.IsType<CosmicQueryComparison>(query.Predicates[1]);
        Assert.Equal(new[] { "location", "city" }, metadata.Path.Segments);
        Assert.Equal("London", metadata.Literal.Value);
        Assert.False(Assert.IsType<CosmicQueryLogical>(query.Predicates[2]).IsAnd);
    }

    [Fact]
    public void BoundsTypesAndForbiddenDtoMembersRejectExplicitly()
    {
        foreach (var request in new[]
        {
            new CqlV1Request { Take = 101 }, new CqlV1Request { Skip = -1 },
            new CqlV1Request { Filters = Enumerable.Repeat(new CqlV1Filter(), 13).ToArray() },
            new CqlV1Request { Filters = new[] { new CqlV1Filter { Field = "CreatedByAvatarId", Value = Json("\"forged\"") } } },
            new CqlV1Request { Filters = new[] { new CqlV1Filter { MetadataPath = new[] { "x" }, Scalar = CqlV1Scalar.Integer, Value = Json("1.1") } } }
        }) Assert.Throws<ArgumentException>(() => CqlV1Compiler.Compile(request, Actor, ProviderType.SQLLiteDBOASIS));
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<CqlV1Request>("{\"actorId\":\"forged\"}"));
    }

    [Fact]
    public async Task ResponseByteLimitPreventsUnboundedJsonPublication()
    {
        var type = ProviderType.SQLLiteDBOASIS;
        var source = new Source(type);
        source.Rows.Add(new Holon { Id = Guid.NewGuid(), CreatedByAvatarId = Actor, Description = new string('x', CqlV1Endpoint.MaximumResponseBytes) });
        var admitted = Admission(type);
        var result = await admitted.Policy.RunAsync(() => CqlV1Endpoint.ExecuteAsync(admitted.Http, new(), source));
        Assert.Equal("CQL_RESPONSE_LIMIT", Code(result));
    }

    [Fact]
    public async Task PreActionModelValidationRejectionRecordsNoExecutionEvidence()
    {
        var admitted = Admission(ProviderType.SQLLiteDBOASIS);
        var action = new ActionContext(admitted.Http, new RouteData(), new ActionDescriptor(), new ModelStateDictionary());
        var context = new ResourceExecutingContext(action, new List<IFilterMetadata>(), new List<IValueProviderFactory>());
        await new CqlV1ResourceFilter().OnResourceExecutionAsync(context, () => Task.FromResult(
            new ResourceExecutedContext(action, new List<IFilterMetadata>()) { Result = new BadRequestResult() }));
        admitted.Store.Verify(s => s.RecordNoProviderExecutionAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task EndpointSizeLimitAppliesBeforeReservationAndFingerprintBuffering(bool contentLengthKnown)
    {
        var admitted = Admission(ProviderType.SQLLiteDBOASIS);
        var http = admitted.Http;
        http.Request.Method = "POST";
        http.Request.Path = "/api/data/query-cql";
        http.Request.Headers.Authorization = "Bearer test-validated";
        http.Request.Headers["Idempotency-Key"] = "bounded-query-test";
        http.Request.Body = new MemoryStream(new byte[40000]);
        http.Request.ContentLength = contentLengthKnown ? 40000 : null;
        http.Response.Body = new MemoryStream();
        http.SetEndpoint(new Endpoint(_ => Task.CompletedTask, new EndpointMetadataCollection(new RequestSizeLimitAttribute(32768)), "CQL"));
        var authority = new Mock<IUsageReservationAuthority>(MockBehavior.Strict);
        var options = new Web4UsageLedgerOptions { Service = "WEB4", MaximumRequestBytes = 1000000, EndpointPolicy = _ => new UsageEndpointPolicy() };
        var nextCalls = 0;
        var middleware = new Web4UsageLedgerMiddleware(_ => { nextCalls++; return Task.CompletedTask; }, NullLogger<Web4UsageLedgerMiddleware>.Instance);

        await middleware.InvokeAsync(http, new Web4UsageExecution(authority.Object, admitted.Store.Object, options), options);

        Assert.Equal(413, http.Response.StatusCode);
        Assert.Equal(0, nextCalls);
        authority.VerifyNoOtherCalls();
        Assert.Empty(admitted.Receipts);
        if (!contentLengthKnown) Assert.Equal(32769, http.Request.Body.Position);
    }

    private static Dictionary<string, string?> OrdinaryConfiguration(decimal cost = 0.002m, decimal reservation = 0.01m)
    {
        var json = JsonSerializer.Serialize(new StorageAdmissionDeployment
        {
            NativeProviders = Array.Empty<NativeStorageAdmissionBinding>(),
            OrdinaryProviders = new[] { ProviderType.SQLLiteDBOASIS },
            QueryTariffs = new[] { new StorageQueryTariff(ProviderType.SQLLiteDBOASIS, cost, "test-v1") },
            Routes = new[] { new StorageAdmissionRoute("POST", "/api/data/query-cql", new UsageEndpointPolicy
            {
                MeterCategory = "cql.query", PricingCatalogueVersion = "test-v1", EstimatedCostUsd = reservation,
                ReservedUnits = 1, MaximumProviderAttempts = 1, MaximumNativeFeeCostUsd = 0, RequiresProviderMeasurement = true
            }) }
        }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter<ProviderType>() } });
        return new() { ["PREPARED_STORAGE_ADMISSION_JSON"] = json };
    }

    [Fact]
    public void OrdinaryQueryCompositionNeedsNoNativePriceServiceOrCoinGeckoCredential()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder().AddInMemoryCollection(OrdinaryConfiguration()).Build();
        var policy = ConfiguredStorageAdmission.AddConfiguredStorageAdmission(services, config, true, _ => null!);
        var http = new DefaultHttpContext(); http.Request.Method = "POST"; http.Request.Path = "/api/data/query-cql";
        Assert.Equal("cql.query", policy(http).MeterCategory);
        Assert.Single(services, s => s.ServiceType == typeof(IPreparedStorageAdmissionFactory));
    }

    [Fact]
    public void QueryTariffCannotExceedReservationOrBindAnUnpermittedProvider()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(OrdinaryConfiguration(cost: 1, reservation: 0.1m)).Build();
        Assert.Throws<InvalidOperationException>(() => ConfiguredStorageAdmission.AddConfiguredStorageAdmission(new ServiceCollection(), config, true, _ => null!));
        var admitted = Admission(ProviderType.SQLLiteDBOASIS);
        var handle = (UsageExecutionHandle)admitted.Http.Items[Web4UsageMeasurement.ExecutionKey]!;
        Assert.Throws<ArgumentException>(() => new PreparedStorageExecution(handle, "test", new Dictionary<ProviderType, string>(),
            new[] { ProviderType.SQLLiteDBOASIS }, new[] { new StorageQueryTariff(ProviderType.MongoDBOASIS, 0, "test-v1") }));
    }

    [Fact]
    public async Task RealMvcRouteRejectsAnonymousUnknownMembersAndOversizeBeforeProviderRead()
    {
        var source = new Source(ProviderType.SQLLiteDBOASIS);
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            ApplicationName = typeof(NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers.CqlV1Controller).Assembly.FullName,
            Args = Array.Empty<string>()
        });
        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddSingleton<ICqlV1Providers>(source);
        builder.Services.AddSingleton<CqlV1ResourceFilter>();
        builder.Services.AddControllers().AddApplicationPart(typeof(NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers.CqlV1Controller).Assembly);
        await using var app = builder.Build();
        app.UseRouting();
        app.Use(async (http, next) =>
        {
            // Controlled validated-identity/admission fixture. Production uses the existing
            // JWT + durable usage + prepared-storage middleware, not this test fixture.
            if (http.Request.Headers.Authorization == "Bearer controlled-test")
            {
                var admitted = Admission(ProviderType.SQLLiteDBOASIS);
                http.User = admitted.Http.User;
                foreach (var item in admitted.Http.Items) http.Items[item.Key] = item.Value;
                await admitted.Policy.RunAsync(async () => { await next(); return true; });
            }
            else await next();
        });
        app.MapControllers();
        await app.StartAsync();
        try
        {
            var addresses = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!;
            using var client = new HttpClient { BaseAddress = new Uri(Assert.Single(addresses.Addresses)), Timeout = TimeSpan.FromSeconds(10) };
            using var anonymous = await client.PostAsync("/api/data/query-cql", new StringContent("{}", Encoding.UTF8, "application/json"));
            Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
            Assert.Equal(0, source.Lookups);

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "controlled-test");
            using var forged = await client.PostAsync("/api/data/query-cql", new StringContent("{\"actorId\":\"forged\"}", Encoding.UTF8, "application/json"));
            Assert.Equal(HttpStatusCode.BadRequest, forged.StatusCode);
            Assert.Equal(0, source.Lookups);

            using var oversized = await client.PostAsync("/api/data/query-cql", new StringContent(
                "{\"provider\":\"" + new string('x', 40000) + "\"}", Encoding.UTF8, "application/json"));
            Assert.True(oversized.StatusCode is HttpStatusCode.RequestEntityTooLarge or HttpStatusCode.BadRequest);
            Assert.Equal(0, source.Lookups);

            using var success = await client.PostAsync("/api/data/query-cql", new StringContent("{}", Encoding.UTF8, "application/json"));
            Assert.Equal(HttpStatusCode.OK, success.StatusCode);
            using var json = JsonDocument.Parse(await success.Content.ReadAsStringAsync());
            Assert.Equal(0, json.RootElement.GetProperty("result").GetProperty("count").GetInt32());
            Assert.Equal(1, source.Lookups);
        }
        finally { await app.StopAsync(); }
    }
    [Theory]
    [InlineData(true,true)] [InlineData(true,false)]
    [InlineData(false,true)] [InlineData(false,false)]
    public async Task RealKestrelOversizeRepliesAreCompleteBeforeReservation(bool knownLength,bool serverLimit)
    {
        var admitted=Admission(ProviderType.SQLLiteDBOASIS);
        var authority=new Mock<IUsageReservationAuthority>(MockBehavior.Strict);
        var failures=new OversizeFailureLogger();
        var options=new Web4UsageLedgerOptions {Service="WEB4",MaximumRequestBytes=1000000,
            EndpointPolicy=http=>http.Request.Path=="/bounded"?new UsageEndpointPolicy():null};
        var builder=WebApplication.CreateBuilder(new WebApplicationOptions {Args=Array.Empty<string>()});
        builder.Logging.ClearProviders();builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.WebHost.ConfigureKestrel(server=>server.Limits.MaxRequestBodySize=serverLimit?32768:1000000);
        await using var app=builder.Build();
        app.UseRouting();
        app.Use(async(HttpContext http,RequestDelegate next)=>
        {
            http.User=admitted.Http.User;
            var middleware=new Web4UsageLedgerMiddleware(next,failures);
            await middleware.InvokeAsync(http,new Web4UsageExecution(authority.Object,admitted.Store.Object,options),options);
        });
        app.MapPost("/bounded",()=>Results.Ok()).WithMetadata(new RequestSizeLimitAttribute(32768));
        app.MapGet("/probe",()=>Results.Ok(new {ready=true}));
        await app.StartAsync();
        try
        {
            var address=app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
            using var client=new HttpClient {BaseAddress=new Uri(address),Timeout=TimeSpan.FromSeconds(10)};
            client.DefaultRequestHeaders.Authorization=new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer","controlled");
            client.DefaultRequestHeaders.Add("Idempotency-Key","unreserved-oversize");
            for(var repeat=0;repeat<8;repeat++)
            {
                using HttpContent body=knownLength?new ByteArrayContent(new byte[40000]):new UnknownLengthOversizeContent();
                using var request=new HttpRequestMessage(HttpMethod.Post,"/bounded"){Content=body};
                // A declared upload above the native ceiling negotiates rejection
                // before sending bytes. Kestrel may otherwise reset an unread body.
                request.Headers.ExpectContinue=knownLength;
                using var response=await client.SendAsync(request);
                Assert.True(response.StatusCode==HttpStatusCode.RequestEntityTooLarge,
                    response.StatusCode+" / "+failures.Last);
                var bytes=await response.Content.ReadAsByteArrayAsync();
                Assert.Equal(bytes.LongLength,response.Content.Headers.ContentLength.GetValueOrDefault(-1));
                using var json=JsonDocument.Parse(bytes);
                Assert.Equal("REQUEST_TOO_LARGE",json.RootElement.GetProperty("code").GetString());
                using var probe=await client.GetAsync("/probe");
                Assert.Equal(HttpStatusCode.OK,probe.StatusCode);
            }
            authority.VerifyNoOtherCalls();
        }
        finally {await app.StopAsync();}
    }

    private sealed class OversizeFailureLogger : ILogger<Web4UsageLedgerMiddleware>
    {
        internal Exception? Last;
        public IDisposable? BeginScope<TState>(TState state) where TState:notnull=>null;
        public bool IsEnabled(LogLevel level)=>true;
        public void Log<TState>(LogLevel level,EventId id,TState state,Exception? exception,Func<TState,Exception?,string> formatter)
        {Last=exception;}
    }

    private sealed class UnknownLengthOversizeContent : HttpContent
    {
        protected override bool TryComputeLength(out long length){length=0;return false;}
        protected override Task SerializeToStreamAsync(Stream stream,TransportContext? context)
            =>stream.WriteAsync(new byte[40000]).AsTask();
    }

}
