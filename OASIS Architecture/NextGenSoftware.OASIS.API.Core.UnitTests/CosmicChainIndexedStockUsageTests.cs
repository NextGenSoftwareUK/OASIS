using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public partial class CosmicChainIndexedExecutionTests
{
    private sealed class StockLedger
    {
        internal readonly Mock<IUsageOutboxStore> Store=new(MockBehavior.Strict);
        internal readonly Mock<IUsageReservationAuthority> Authority=new(MockBehavior.Strict);
        internal readonly Dictionary<string,UsageOutboxOperation> Operations=new();
        internal readonly List<UsageAuthorizationRequest> Authorizations=new();
        internal readonly List<UsageSettlementRequest> Settlements=new();
        internal bool FailReceipt;
        internal StockLedger(Guid actor,Action beforeAuthorize)
        {
            Authority.Setup(s=>s.AuthorizeUsageAsync(It.IsAny<string>(),It.IsAny<UsageAuthorizationRequest>(),It.IsAny<CancellationToken>()))
                .Returns((string token,UsageAuthorizationRequest request,CancellationToken ct)=>{
                    beforeAuthorize();Authorizations.Add(request);return Task.FromResult(new SubscriptionAuthorizationResult{
                        Allowed=true,UserId=actor.ToString("D"),OperationId=request.OperationId,Status="authorized",ExpiresAtUtc=DateTime.UtcNow.AddMinutes(5)});
                });
            Authority.Setup(s=>s.StartUsageAsync(It.IsAny<UsageStartRequest>(),It.IsAny<CancellationToken>()))
                .Returns((UsageStartRequest request,CancellationToken ct)=>Task.FromResult(new UsageStartResult{Started=true,OperationId=request.OperationId}));
            Authority.Setup(s=>s.SettleUsageAsync(It.IsAny<UsageSettlementRequest>(),It.IsAny<CancellationToken>()))
                .Returns((UsageSettlementRequest request,CancellationToken ct)=>{Settlements.Add(request);return Task.FromResult(new UsageSettlementResult{Settled=true,OperationId=request.OperationId});});
            Store.Setup(s=>s.TryBeginAsync(It.IsAny<UsageOutboxOperation>(),It.IsAny<CancellationToken>()))
                .Returns((UsageOutboxOperation operation,CancellationToken ct)=>Task.FromResult(Operations.TryAdd(operation.Id,operation)));
            Store.Setup(s=>s.GetAsync(It.IsAny<string>(),It.IsAny<CancellationToken>()))
                .Returns((string id,CancellationToken ct)=>Task.FromResult(Operations.GetValueOrDefault(id)));
            Store.Setup(s=>s.BeginProviderAttemptAsync(It.IsAny<string>(),It.IsAny<string>(),It.IsAny<UsageProviderAttempt>(),It.IsAny<CancellationToken>()))
                .Returns((string id,string owner,UsageProviderAttempt attempt,CancellationToken ct)=>{
                    var op=Operations[id];Assert.Equal(op.ExecutionOwner,owner);Assert.True(op.ProviderAttempts.Count<op.Policy.MaximumProviderAttempts);
                    Assert.DoesNotContain(op.ProviderAttempts,p=>p.ClientRequestId==attempt.ClientRequestId);
                    op.ProviderAttempts.Add(attempt);op.EvidenceRevision++;return Task.CompletedTask;
                });
            Store.Setup(s=>s.AppendReceiptAsync(It.IsAny<string>(),It.IsAny<string>(),It.IsAny<UsageProviderReceipt>(),It.IsAny<CancellationToken>()))
                .Returns((string id,string owner,UsageProviderReceipt receipt,CancellationToken ct)=>{
                    if(FailReceipt)throw new IOException("controlled uncertain acknowledgement");
                    receipt.Validate();var op=Operations[id];Assert.Equal(op.ExecutionOwner,owner);
                    Assert.Contains(op.ProviderAttempts,p=>p.ClientRequestId==receipt.ClientRequestId&&p.Provider==receipt.Provider);
                    op.Receipts.Add(UsageReceiptAccounting.Snapshot(receipt));op.EvidenceRevision++;return Task.CompletedTask;
                });
            Store.Setup(s=>s.RecordNoProviderExecutionAsync(It.IsAny<string>(),It.IsAny<string>(),It.IsAny<string>(),It.IsAny<CancellationToken>()))
                .Returns((string id,string owner,string reason,CancellationToken ct)=>{
                    var op=Operations[id];Assert.Empty(op.ProviderAttempts);Assert.Empty(op.Receipts);op.NoProviderExecutionReason=reason;op.EvidenceRevision++;return Task.CompletedTask;
                });
            Store.Setup(s=>s.RecordObservedOutcomeAsync(It.IsAny<string>(),It.IsAny<string>(),It.IsAny<string>(),It.IsAny<CancellationToken>()))
                .Returns((string id,string owner,string outcome,CancellationToken ct)=>{
                    var op=Operations[id];Assert.True(op.ObservedOutcome==null||op.ObservedOutcome==outcome);op.ObservedOutcome=outcome;return Task.FromResult(op);
                });
            Store.Setup(s=>s.EnqueueAndClaimAsync(It.IsAny<string>(),It.IsAny<string>(),It.IsAny<UsageSettlementRequest>(),It.IsAny<long>(),It.IsAny<string>(),It.IsAny<DateTime>(),It.IsAny<TimeSpan>(),It.IsAny<CancellationToken>()))
                .Returns((string id,string owner,UsageSettlementRequest settlement,long revision,string worker,DateTime now,TimeSpan lease,CancellationToken ct)=>{
                    var op=Operations[id];Assert.Equal(op.EvidenceRevision,revision);op.Settlement=settlement;op.State="pending";
                    op.WorkerOwner=worker;op.LeaseUntilUtc=now.Add(lease);op.Attempts++;return Task.FromResult(op);
                });
            Store.Setup(s=>s.AcknowledgeAsync(It.IsAny<string>(),It.IsAny<string>(),It.IsAny<DateTime>(),It.IsAny<CancellationToken>()))
                .Returns((string id,string worker,DateTime now,CancellationToken ct)=>{Operations[id].State="settled";return Task.CompletedTask;});
            Store.Setup(s=>s.MarkIndeterminateAsync(It.IsAny<string>(),It.IsAny<string>(),It.IsAny<string>(),It.IsAny<CancellationToken>()))
                .Returns((string id,string owner,string reason,CancellationToken ct)=>{Operations[id].State="indeterminate";Operations[id].LastError=reason;return Task.CompletedTask;});
        }
    }

    [Theory]
    [InlineData("rows")] [InlineData("empty")] [InlineData("bad-json")] [InlineData("graph")]
    [InlineData("policy-ceiling")] [InlineData("uncertain")]
    [InlineData("projection-rows")] [InlineData("projection-empty")] [InlineData("projection-metadata")]
    [InlineData("projection-empty-columns")] [InlineData("projection-row-only")] [InlineData("projection-duplicate")]
    [InlineData("projection-uncertain")] [InlineData("projection-forged")] [InlineData("projection-include")]
    public async Task StockLedgerAndStorageMiddlewareReserveExecuteAndSettleIndexedRows(string mode)
    {
        var empty=mode is "empty" or "projection-empty";
        var projected=mode.StartsWith("projection-",StringComparison.Ordinal);
        var f=Fixture(empty?"empty":"rows");var reader=new BillingReader(source,f.Reader.Result,"rows");
        var actualReader=projected&&mode!="projection-row-only"?(NextGenSoftware.OASIS.API.Core.Interfaces.ICosmicChainQueryIndexReader)new BillingProjectionReader(reader,mode):reader;
        var binding=new CqlIndexedV1Binding(actualReader,Mock.Of<NextGenSoftware.OASIS.API.Core.Interfaces.ICosmicChainQuerySourceObserver>(),
            Tariffs(),position,10,TimeSpan.FromMinutes(1),CosmicQueryReadAccess.OwnerOrPublic(actor).Result,Manager());
        var registry=new BindingRegistry(binding);var native=new Mock<IPreparedStorageAdmissionFactory>(MockBehavior.Strict);
        var nativeProviders=new Mock<ICqlV1Providers>(MockBehavior.Strict);
        var allowedBeforeAuthorization=0;
        var ledger=new StockLedger(actor,()=>Assert.Equal(allowedBeforeAuthorization,reader.PhysicalCalls)){FailReceipt=mode is "uncertain" or "projection-uncertain"};
        var policy=new UsageEndpointPolicy{MeterCategory="queries",PricingCatalogueVersion="stock-indexed-v1",RequiresProviderMeasurement=true,
            ReservedUnits=1,EstimatedCostUsd=0,MaximumProviderAttempts=mode=="policy-ceiling"?99:512,MaximumNativeFeeCostUsd=0};
        var options=new Web4UsageLedgerOptions{Service="WEB4",EndpointPolicy=_=>policy};
        var builder=WebApplication.CreateBuilder(new WebApplicationOptions{Args=Array.Empty<string>(),ApplicationName=typeof(CqlV1Controller).Assembly.FullName});
        builder.Logging.ClearProviders();builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddSingleton<ICqlIndexedV1Bindings>(registry);builder.Services.AddSingleton(native.Object);
        builder.Services.AddCqlIndexedReadAdmission();builder.Services.AddSingleton(nativeProviders.Object);
        builder.Services.AddSingleton(options);builder.Services.AddSingleton(new Web4UsageExecution(ledger.Authority.Object,ledger.Store.Object,options));
        builder.Services.AddSingleton<CqlV1ResourceFilter>();builder.Services.AddControllers().AddApplicationPart(typeof(CqlV1Controller).Assembly);
        await using var app=builder.Build();app.UseRouting();
        app.Use(async(http,next)=>{
            // Controlled validated identity; production JWT remains a separate gate.
            http.User=new ClaimsPrincipal(new ClaimsIdentity(new[]{new Claim(ClaimTypes.NameIdentifier,actor.ToString("D"))},"fixture"));
            http.Items["Avatar"]=new Avatar{Id=actor};await next();
        });
        app.UseMiddleware<Web4UsageLedgerMiddleware>();app.UseMiddleware<PreparedStorageAdmissionMiddleware>();app.MapControllers();
        await app.StartAsync();
        try{
            var addresses=app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!;
            using var client=new HttpClient{BaseAddress=new Uri(Assert.Single(addresses.Addresses)),Timeout=TimeSpan.FromSeconds(10)};
            client.DefaultRequestHeaders.Authorization=new("Bearer","stock-fixture");client.DefaultRequestHeaders.Add("Idempotency-Key","indexed-stock-key");
            var body="{\"alias\":\"solana-demo\",\"query\":{\"orderBy\":\"Id\",\"take\":1}}";
            if(projected){
                var columns=mode=="projection-empty-columns"?"[]":mode=="projection-metadata"?"[{\"alias\":\"label\",\"metadataPath\":[\"private\"],\"valueType\":\"String\"}]":
                    mode=="projection-duplicate"?"[{\"alias\":\"label\",\"field\":\"Name\"},{\"alias\":\"label\",\"field\":\"Id\"}]":"[{\"alias\":\"label\",\"field\":\"Name\"}]";
                body=body.Replace("\"alias\":\"solana-demo\"","\"alias\":\"solana-demo\",\"columns\":"+columns);
                if(mode=="projection-include")body=body.Replace("\"take\":1","\"take\":1,\"includeMetadata\":true");
            }
            var sent=mode=="bad-json"?"{broken":mode=="graph"?body.Replace("\"take\":1","\"take\":1,\"graph\":{}"):body;
            using var response=await client.PostAsync("/api/data/query-cql/indexed",new StringContent(sent,Encoding.UTF8,"application/json"));
            var text=await response.Content.ReadAsStringAsync();
            if(mode is "bad-json" or "graph" or "policy-ceiling" or "projection-metadata" or "projection-empty-columns" or "projection-row-only" or "projection-duplicate" or "projection-include"){
                Assert.True(response.StatusCode==(mode=="policy-ceiling"?HttpStatusCode.ServiceUnavailable:HttpStatusCode.BadRequest),text);
                Assert.Empty(ledger.Authorizations);Assert.Empty(ledger.Operations);Assert.Equal(0,reader.PhysicalCalls);
                Assert.Equal(mode is "policy-ceiling" or "projection-metadata" or "projection-row-only" or "projection-duplicate"?1:0,registry.Calls);native.VerifyNoOtherCalls();return;
            }
            if(mode is "uncertain" or "projection-uncertain"){
                Assert.True(response.StatusCode==HttpStatusCode.ServiceUnavailable,text);
                Assert.Contains("MEASUREMENT_REQUIRED",text);Assert.DoesNotContain("\"rows\"",text);
                var pending=Assert.Single(ledger.Operations.Values);Assert.Equal("indeterminate",pending.State);
                Assert.Single(pending.ProviderAttempts);Assert.Empty(pending.Receipts);Assert.Null(pending.NoProviderExecutionReason);
                Assert.Empty(ledger.Settlements);Assert.Equal(1,reader.PhysicalCalls);
                allowedBeforeAuthorization=reader.PhysicalCalls;
                using var denied=await client.PostAsync("/api/data/query-cql/indexed",new StringContent(body,Encoding.UTF8,"application/json"));
                Assert.Equal(HttpStatusCode.Conflict,denied.StatusCode);Assert.Equal(1,reader.PhysicalCalls);return;
            }
            if(mode=="projection-forged"){
                Assert.Equal(HttpStatusCode.ServiceUnavailable,response.StatusCode);Assert.Contains("CQL_SOURCE_RECORD_MISMATCH",text);
                Assert.Single(ledger.Settlements);Assert.Equal(7,reader.PhysicalCalls);Assert.Equal("failed",ledger.Operations.Values.Single().ObservedOutcome);return;
            }
            Assert.True(response.StatusCode==HttpStatusCode.OK,text);
            if(projected){
                using var selectedJson=JsonDocument.Parse(text);var selectedPage=selectedJson.RootElement.GetProperty("result");
                Assert.Equal("indexed-projection",selectedPage.GetProperty("mode").GetString());
                Assert.Equal("label",selectedPage.GetProperty("columns")[0].GetProperty("alias").GetString());
                Assert.Equal(empty?0:1,selectedPage.GetProperty("rows").GetArrayLength());
                if(!empty)Assert.Equal("original",selectedPage.GetProperty("rows")[0][0].GetString());
            }
            Assert.Equal("settled",Assert.Single(response.Headers.GetValues("X-OASIS-Usage-Settlement")));
            var authorization=Assert.Single(ledger.Authorizations);Assert.Equal(100,authorization.RequestedUnits);Assert.Equal(1m,authorization.EstimatedCostUsd);
            var semantic=CqlIndexedV1Preparation.Prepare(actor,WebRequest(),new BindingRegistry(binding)).Result.RequestFingerprint;
            Assert.NotEqual(semantic,authorization.RequestFingerprint); // Existing raw HTTP idempotency identity is preserved.
            var operation=Assert.Single(ledger.Operations.Values);Assert.Equal(authorization.RequestFingerprint,operation.Fingerprint);
            Assert.Equal("settled",operation.State);var settled=Assert.Single(ledger.Settlements);
            Assert.Equal(empty?4:7,settled.Units);Assert.Equal(empty?0.016m:0.022m,settled.ActualCostUsd);
            Assert.Equal(empty?4:7,operation.ProviderAttempts.Count);Assert.Equal(operation.ProviderAttempts.Count,operation.Receipts.Count);
            Assert.Equal(1,registry.Calls);Assert.Equal(1,policy.ReservedUnits);Assert.Equal(0,policy.EstimatedCostUsd);
            // Same operation cannot execute again, even if authority repeats an authorization.
            var physical=reader.PhysicalCalls;allowedBeforeAuthorization=physical;
            using var replay=await client.PostAsync("/api/data/query-cql/indexed",new StringContent(body,Encoding.UTF8,"application/json"));
            Assert.Equal(HttpStatusCode.Conflict,replay.StatusCode);Assert.Equal(physical,reader.PhysicalCalls);
            Assert.Single(ledger.Settlements);Assert.Single(ledger.Operations);native.VerifyNoOtherCalls();nativeProviders.VerifyNoOtherCalls();
        }finally{await app.StopAsync();}
    }

    [Fact]
    public async Task IndexedHostingPreservesTheExistingFactoryForOtherRoutesAndRejectsDuplicateOwners()
    {
        var f=Fixture("rows");var registry=new BindingRegistry(WebBinding(f.Reader));
        var store=new Mock<IUsageOutboxStore>(MockBehavior.Strict);
        var handle=new UsageExecutionHandle(new UsageOutboxOperation{Id="native-route",UserId=actor.ToString("D"),Policy=new()},
            store.Object,new(),new(),Mock.Of<IUsageReservationAuthority>());
        var nativePolicy=new PreparedStorageExecution(handle,"native-route",new Dictionary<ProviderType,string>(),Array.Empty<ProviderType>());
        var native=new Mock<IPreparedStorageAdmissionFactory>(MockBehavior.Strict);
        native.Setup(s=>s.CreateAsync(It.IsAny<HttpContext>(),handle,It.IsAny<CancellationToken>())).ReturnsAsync(nativePolicy);
        var services=new ServiceCollection();services.AddSingleton<ICqlIndexedV1Bindings>(registry);services.AddSingleton(native.Object);
        services.AddCqlIndexedReadAdmission();Assert.Throws<InvalidOperationException>(()=>services.AddCqlIndexedReadAdmission());
        using var provider=services.BuildServiceProvider();var context=new DefaultHttpContext();context.Request.Path="/native";context.Request.Method="POST";
        var original=new UsageEndpointPolicy();
        Assert.Same(original,await provider.GetRequiredService<IUsageRequestPreparation>().PrepareAsync(context,original,new string('a',64),default));
        Assert.Same(nativePolicy,await provider.GetRequiredService<IPreparedStorageAdmissionFactory>().CreateAsync(context,handle,default));
        Assert.Equal(0,registry.Calls);native.Verify(s=>s.CreateAsync(context,handle,default),Times.Once);native.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("null-service")] [InlineData("null-policy")] [InlineData("downgrade")]
    public async Task SelectedMeteredRoutesCannotLosePreparationOrRequiredMeasurement(string mode)
    {
        var services=new ServiceCollection();
        if(mode=="null-service")services.AddSingleton<IUsageRequestPreparation>(_=>null!);
        else{
            var hook=new Mock<IUsageRequestPreparation>(MockBehavior.Strict);
            hook.Setup(h=>h.PrepareAsync(It.IsAny<HttpContext>(),It.IsAny<UsageEndpointPolicy>(),It.IsAny<string>(),It.IsAny<CancellationToken>()))
                .ReturnsAsync(mode=="null-policy"?null!:new UsageEndpointPolicy{RequiresProviderMeasurement=false});
            services.AddSingleton(hook.Object);
        }
        using var provider=services.BuildServiceProvider();
        var context=new DefaultHttpContext{RequestServices=provider};
        context.User=new ClaimsPrincipal(new ClaimsIdentity(new[]{new Claim(ClaimTypes.NameIdentifier,actor.ToString("D"))},"fixture"));
        context.Request.Method="POST";context.Request.Path="/measured";context.Request.Headers.Authorization="Bearer fixture";
        context.Request.Headers["Idempotency-Key"]="required-preparation";context.Request.ContentType="application/json";
        context.Request.Body=new MemoryStream(Encoding.UTF8.GetBytes("{}"));context.Response.Body=new MemoryStream();
        var authority=new Mock<IUsageReservationAuthority>(MockBehavior.Strict);var store=new Mock<IUsageOutboxStore>(MockBehavior.Strict);
        var options=new Web4UsageLedgerOptions{Service="WEB4",EndpointPolicy=_=>new UsageEndpointPolicy{RequiresProviderMeasurement=true}};
        var next=0;var middleware=new Web4UsageLedgerMiddleware(_=>{next++;return Task.CompletedTask;},Microsoft.Extensions.Logging.Abstractions.NullLogger<Web4UsageLedgerMiddleware>.Instance);
        await middleware.InvokeAsync(context,new Web4UsageExecution(authority.Object,store.Object,options),options);
        Assert.Equal(503,context.Response.StatusCode);Assert.Equal(0,next);
        context.Response.Body.Position=0;using var body=new StreamReader(context.Response.Body);
        Assert.Contains("USAGE_PREPARATION_UNAVAILABLE",await body.ReadToEndAsync());authority.VerifyNoOtherCalls();store.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("units")] [InlineData("cost")]
    public async Task ANewPreparedQueryCannotResetItsHandleReservationBudget(string limit)
    {
        var f=Fixture("empty");var reader=new BillingReader(source,f.Reader.Result,"rows");
        var begins=new List<UsageProviderAttempt>();var receipts=new List<UsageProviderReceipt>();
        var store=new Mock<IUsageOutboxStore>(MockBehavior.Strict);
        store.Setup(s=>s.BeginProviderAttemptAsync(It.IsAny<string>(),It.IsAny<string>(),It.IsAny<UsageProviderAttempt>(),It.IsAny<CancellationToken>()))
            .Callback((string id,string owner,UsageProviderAttempt attempt,CancellationToken ct)=>begins.Add(attempt)).Returns(Task.CompletedTask);
        store.Setup(s=>s.AppendReceiptAsync(It.IsAny<string>(),It.IsAny<string>(),It.IsAny<UsageProviderReceipt>(),It.IsAny<CancellationToken>()))
            .Callback((string id,string owner,UsageProviderReceipt receipt,CancellationToken ct)=>receipts.Add(receipt)).Returns(Task.CompletedTask);
        var handle=new UsageExecutionHandle(new UsageOutboxOperation{Id="shared-index-budget",UserId=actor.ToString("D"),
            Policy=new UsageEndpointPolicy{RequiresProviderMeasurement=true,ReservedUnits=limit=="units"?4:512,EstimatedCostUsd=0.04m,MaximumProviderAttempts=512}},
            store.Object,new Web4UsageLedgerOptions(),new(),Mock.Of<IUsageReservationAuthority>());
        var count=limit=="units"?2:3;
        for(var query=0;query<count;query++){
            var policy=new PreparedStorageExecution(handle,"shared-"+query,new Dictionary<ProviderType,string>(),Array.Empty<ProviderType>(),null,null,false,new[]{Tariffs(4)});
            var result=await policy.RunAsync(()=>CosmicIndexedQueryExecutor.ExecuteAsync(Plan(),reader,Mock.Of<NextGenSoftware.OASIS.API.Core.Interfaces.ICosmicChainQuerySourceObserver>()));
            if(query==count-1)Assert.Equal("CQL_QUERY_LIMIT",result.ErrorCode);
            else Assert.False(result.IsError,result.Message+result.Exception);
        }
        Assert.Equal(limit=="units"?4:8,reader.PhysicalCalls);Assert.Equal(reader.PhysicalCalls,begins.Count);Assert.Equal(begins.Count,receipts.Count);
    }
}
