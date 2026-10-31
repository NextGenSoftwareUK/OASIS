using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public partial class CosmicChainIndexedExecutionTests
{
    private sealed class BillingProjectionReader(BillingReader inner, string mode) : ICosmicChainQueryIndexProjectionReader
    {
        public CosmicChainQuerySource Source => inner.Source;
        public CosmicQueryCapabilities IndexedRowCapabilities => inner.IndexedRowCapabilities;
        public CosmicProjectionCapabilities IndexedProjectionCapabilities => new(new[] {
            new CosmicProjectionFieldCapability("Name", typeof(string)) }, Array.Empty<Type>());
        public Task<OASISResult<CosmicChainIndexRows>> QueryRowsAsync<T>(CosmicQueryPlan<T> plan,
            ICosmicChainQuerySourceObserver observer, int maximumResults = 100, CancellationToken cancellationToken = default) where T : IHolon
            => throw new InvalidOperationException("Selected HTTP must dispatch its native selected-column contract.");
        public async Task<OASISResult<CosmicChainIndexProjectionRows>> QueryProjectionAsync<T, TResult>(CosmicProjectionPlan<T, TResult> plan,
            ICosmicChainQuerySourceObserver observer, int maximumResults = 100, CancellationToken cancellationToken = default) where T : IHolon
        {
            // Controlled transport fixture; native SQL is proved by SQLite/live gates.
            var result = await inner.QueryRowsAsync(plan.SourcePlan, observer, maximumResults, cancellationToken);
            if(result.IsError) return new() { IsError = true, ErrorCode = result.ErrorCode, Message = result.Message };
            return new(new(result.Result, result.Result.Rows.Select(row => new CosmicProjectionRow(
                mode == "projection-forged" ? "forged" : row.Name)))) { IsLoaded = true };
        }
    }

    [Fact]
    public void IndexedSelectedPreparationFreezesColumnsAndSeparatesRowIntent()
    {
        var fixture = Fixture("rows");
        var reader = new BillingProjectionReader(new BillingReader(source, fixture.Reader.Result, "rows"), "rows");
        var binding = new CqlIndexedV1Binding(reader, Moq.Mock.Of<ICosmicChainQuerySourceObserver>(), Tariffs(), position, 0,
            TimeSpan.FromMinutes(1), CosmicQueryReadAccess.OwnerOrPublic(actor).Result, Manager());
        var request = WebRequest();
        request.Columns = new[] { new CqlProjectionV1Column { Alias = "label", Field = "Name" } };
        var prepared = CqlIndexedV1Preparation.Prepare(actor, request, new BindingRegistry(binding));
        Assert.False(prepared.IsError, prepared.Message);
        Assert.NotNull(prepared.Result.ProjectionPlan);
        Assert.True(prepared.Result.Matches(request));
        var fingerprint = prepared.Result.RequestFingerprint;
        request.Columns[0].Alias = "changed";
        Assert.False(prepared.Result.Matches(request));
        Assert.Equal("label", prepared.Result.ProjectionPlan.Query.Columns.Single().Alias);
        request.Columns = null;
        Assert.False(prepared.Result.Matches(request));
        var row = CqlIndexedV1Preparation.Prepare(actor, request, new BindingRegistry(binding));
        Assert.False(row.IsError, row.Message);
        Assert.NotEqual(fingerprint, row.Result.RequestFingerprint);
        Assert.Null(row.Result.ProjectionPlan);
    }

    [Fact]
    public void IndexedColumnsAreCapturedBeforeTheServerResolverCallback()
    {
        var fixture = Fixture("rows");
        var reader = new BillingProjectionReader(new BillingReader(source, fixture.Reader.Result, "rows"), "rows");
        var binding = new CqlIndexedV1Binding(reader, Moq.Mock.Of<ICosmicChainQuerySourceObserver>(), Tariffs(), position, 0,
            TimeSpan.FromMinutes(1), CosmicQueryReadAccess.OwnerOrPublic(actor).Result, Manager());
        var request = WebRequest();
        request.Columns = new[] { new CqlProjectionV1Column { Alias = "label", Field = "Name" } };
        var registry = new BindingRegistry(binding, () => { request.Columns[0].Alias = "changed"; request.Columns[0].Field = "Id"; });
        var prepared = CqlIndexedV1Preparation.Prepare(actor, request, registry);
        Assert.False(prepared.IsError, prepared.Message);
        Assert.Equal("label", prepared.Result.ProjectionPlan.Query.Columns.Single().Alias);
        Assert.Equal("Name", prepared.Result.ProjectionPlan.Query.Columns.Single().Path.Segments.Single());
        Assert.False(prepared.Result.Matches(request));
        Assert.Equal(1, registry.Calls);
    }
    [Theory]
    [InlineData(null)] [InlineData(0)] [InlineData(2)]
    public void IndexedScalarPreparationFreezesOperationAndWindowBeforeAliasResolution(int? take)
    {
        var reader=new CardinalityReader(source,position,"valid");
        var binding=new CqlIndexedV1Binding(reader,Moq.Mock.Of<ICosmicChainQuerySourceObserver>(),Tariffs(),position,10,
            TimeSpan.FromMinutes(1),CosmicQueryReadAccess.OwnerOrPublic(actor).Result,Manager());
        var request=new CqlIndexedScalarV1Request{Alias="solana-demo",Query=new(){Operation="LongCount",Take=take,OrderBy="Id"}};
        var registry=new BindingRegistry(binding,()=>{request.Query.Operation="Count";request.Query.Take=25;});
        var prepared=CqlIndexedV1Preparation.Prepare(actor,request,registry);
        Assert.False(prepared.IsError,prepared.Message);
        Assert.NotNull(prepared.Result.LongCountPlan);Assert.Null(prepared.Result.CountPlan);
        Assert.Equal(take,prepared.Result.LongCountPlan.Query.Source.Limit);
        Assert.False(prepared.Result.Matches(request));
        request.Query.Operation="LongCount";request.Query.Take=take;
        Assert.True(prepared.Result.Matches(request));
        Assert.False(prepared.Result.Matches(WebRequest()));
        Assert.Equal(1,registry.Calls);Assert.Equal(0,reader.Calls);
    }

    [Theory]
    [InlineData("Count")] [InlineData("LongCount")] [InlineData("Any")]
    [InlineData("selector")] [InlineData("graph")] [InlineData("provider")]
    [InlineData("row-only")] [InlineData("uncertain")] [InlineData("mutation")]
    public async Task IndexedScalarStockHttpCapturesNativeCardinalityAndRejectsUnadmittedIntent(string mode)
    {
        var reader=new CardinalityReader(source,position,mode=="LongCount"?"overflow":"valid");
        ICosmicChainQueryIndexReader actual=mode=="row-only"?Fixture("rows").Reader:reader;
        var binding=new CqlIndexedV1Binding(actual,Moq.Mock.Of<ICosmicChainQuerySourceObserver>(),Tariffs(),position,10,
            TimeSpan.FromMinutes(1),CosmicQueryReadAccess.OwnerOrPublic(actor).Result,Manager());
        var registry=new BindingRegistry(binding);
        var ledger=new StockLedger(actor,()=>Assert.Equal(0,reader.Physical)){FailReceipt=mode=="uncertain"};
        var native=new Moq.Mock<ICqlV1Providers>(Moq.MockBehavior.Strict);
        var options=new NextGenSoftware.OASIS.API.Core.Services.Subscriptions.Web4UsageLedgerOptions{
            Service="WEB4",EndpointPolicy=_=>new(){MeterCategory="queries",PricingCatalogueVersion="scalar-fixture",
                RequiresProviderMeasurement=true,ReservedUnits=1,MaximumProviderAttempts=512}};
        var builder=Microsoft.AspNetCore.Builder.WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddSingleton<ICqlIndexedV1Bindings>(registry);builder.Services.AddCqlIndexedReadAdmission();
        builder.Services.AddSingleton(native.Object);builder.Services.AddSingleton(options);
        builder.Services.AddSingleton(new NextGenSoftware.OASIS.API.Core.Services.Subscriptions.Web4UsageExecution(ledger.Authority.Object,ledger.Store.Object,options));
        builder.Services.AddSingleton<CqlV1ResourceFilter>();
        builder.Services.AddControllers().AddApplicationPart(typeof(NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers.CqlV1Controller).Assembly);
        await using var app=builder.Build();app.UseRouting();
        app.Use(async(http,next)=>{
            http.User=new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(new[]{
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier,actor.ToString("D"))},"fixture"));
            http.Items["Avatar"]=new Avatar{Id=actor};await next();
        });
        app.UseMiddleware<NextGenSoftware.OASIS.API.Core.Services.Subscriptions.Web4UsageLedgerMiddleware>();
        app.UseMiddleware<NextGenSoftware.OASIS.API.Core.Services.Subscriptions.PreparedStorageAdmissionMiddleware>();
        if(mode=="mutation")app.Use(async(http,next)=>{
            // Reservation already captured LongCount. MVC must not dispatch a substituted Count.
            var bytes=System.Text.Encoding.UTF8.GetBytes("{\"alias\":\"solana-demo\",\"query\":{\"operation\":\"Count\",\"orderBy\":\"Id\"}}");
            http.Request.Body=new MemoryStream(bytes);http.Request.ContentLength=bytes.Length;await next();
        });
        app.MapControllers();await app.StartAsync();
        try
        {
            var addresses=app.Services.GetRequiredService<Microsoft.AspNetCore.Hosting.Server.IServer>().Features
                .Get<Microsoft.AspNetCore.Hosting.Server.Features.IServerAddressesFeature>()!;
            using var client=new HttpClient{BaseAddress=new Uri(Assert.Single(addresses.Addresses))};
            client.DefaultRequestHeaders.Authorization=new("Bearer","fixture");
            client.DefaultRequestHeaders.Add("Idempotency-Key","scalar-fixture");
            var query=mode is "Count" or "Any"?mode:"LongCount";
            var extra=mode=="selector"?",\"selection\":{\"field\":\"Version\"}":mode=="graph"?",\"graph\":{}":mode=="provider"?",\"provider\":\"SolanaOASIS\"":"";
            var body="{\"alias\":\"solana-demo\",\"query\":{\"operation\":\""+query+"\",\"orderBy\":\"Id\""+extra+"}}";
            using var response=await client.PostAsync("/api/data/query-cql/indexed/scalar",new StringContent(body,System.Text.Encoding.UTF8,"application/json"));
            var text=await response.Content.ReadAsStringAsync();
            if(mode is "selector" or "graph" or "provider" or "row-only")
            {
                Assert.True(response.StatusCode==System.Net.HttpStatusCode.BadRequest,text);
                Assert.Empty(ledger.Authorizations);Assert.Equal(0,reader.Calls);return;
            }
            if(mode=="mutation")
            {
                Assert.True(response.StatusCode==System.Net.HttpStatusCode.Conflict,text);
                Assert.Contains("CQL_INDEX_INTENT_MISMATCH",text);Assert.Equal(0,reader.Calls);return;
            }
            if(mode=="uncertain")
            {
                Assert.True(response.StatusCode==System.Net.HttpStatusCode.ServiceUnavailable,text);
                Assert.Equal("indeterminate",Assert.Single(ledger.Operations.Values).State);
                Assert.Empty(ledger.Settlements);Assert.Equal(1,reader.Physical);return;
            }
            Assert.True(response.StatusCode==System.Net.HttpStatusCode.OK,text);
            using var json=System.Text.Json.JsonDocument.Parse(text);var result=json.RootElement.GetProperty("result");
            Assert.Equal("authenticated-index-snapshot",result.GetProperty("assurance").GetString());
            Assert.Equal(query,result.GetProperty("operation").GetString());
            if(mode=="LongCount")Assert.Equal("2147483648",result.GetProperty("value").GetString());
            else if(mode=="Any")Assert.True(result.GetProperty("value").GetBoolean());
            else Assert.Equal(2,result.GetProperty("value").GetInt32());
            Assert.False(result.TryGetProperty("receipts",out _));Assert.False(result.TryGetProperty("rows",out _));
            Assert.DoesNotContain("historyHash",text,StringComparison.OrdinalIgnoreCase);
            Assert.Equal(4,reader.Physical);Assert.Equal(4,Assert.Single(ledger.Settlements).Units);
            Assert.Equal(100,Assert.Single(ledger.Authorizations).RequestedUnits);
            native.VerifyNoOtherCalls();
        }
        finally{await app.StopAsync();}
    }

}
