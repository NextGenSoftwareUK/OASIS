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
}
