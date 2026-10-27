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
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public partial class CosmicChainIndexedExecutionTests
{
    [Fact]
    public async Task ActualControllerConsumesFrozenIntentAndReservationBeforeIndexedHttpReads()
    {
        var nativeRegistry=new Mock<ICqlV1Providers>(MockBehavior.Strict);
        var states=new List<(string Mode,BillingReader Reader,List<UsageProviderAttempt> Begins,List<UsageProviderReceipt> Receipts,List<string> NoExecution)>();
        var builder=WebApplication.CreateBuilder(new WebApplicationOptions{Args=Array.Empty<string>(),ApplicationName=typeof(CqlV1Controller).Assembly.FullName});
        builder.Logging.ClearProviders();builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddSingleton(nativeRegistry.Object);builder.Services.AddSingleton<CqlV1ResourceFilter>();
        builder.Services.AddControllers().AddApplicationPart(typeof(CqlV1Controller).Assembly);
        await using var app=builder.Build();app.UseRouting();
        app.Use(async(http,next)=>{
            if(http.Request.Headers.Authorization!="Bearer indexed-fixture"){await next();return;}
            // Controlled identity/reservation/outbox/transport fixture; not production JWT or settlement.
            var mode=http.Request.Headers["X-Fixture-Case"].ToString();
            var f=Fixture(mode=="empty"?"empty":"rows");
            var selectedPosition=mode=="wide-slot"?new CosmicChainQueryPosition(9007199254740993L,"wide-block"):position;
            var selectedPage=mode=="wide-slot"?new NextGenSoftware.OASIS.Common.OASISResult<CosmicChainIndexRows>(new(
                new(source,selectedPosition,0),new(source.SourceProvider,source.NetworkIdentity,selectedPosition,DateTimeOffset.UtcNow),
                f.Reader.Result.Result.Rows,f.Reader.Result.Result.Receipts.Select(receipt=>new CosmicChainQueryRecordReceipt(source,selectedPosition,receipt.CommitReference,receipt.Record)))){IsLoaded=true}:f.Reader.Result;
            var reader=new BillingReader(source,selectedPage,mode=="uncertain"?"receipt":"rows");
            var binding=new CqlIndexedV1Binding(reader,Mock.Of<NextGenSoftware.OASIS.API.Core.Interfaces.ICosmicChainQuerySourceObserver>(),
                Tariffs(),selectedPosition,10,TimeSpan.FromSeconds(60),CosmicQueryReadAccess.OwnerOrPublic(actor).Result,Manager(),2);
            var registry=new BindingRegistry(binding);
            var captured=CqlIndexedV1Preparation.Prepare(actor,WebRequest(),registry);Assert.False(captured.IsError,captured.Message);
            var begins=new List<UsageProviderAttempt>();var receipts=new List<UsageProviderReceipt>();var noExecution=new List<string>();
            var store=new Mock<IUsageOutboxStore>(MockBehavior.Strict);
            store.Setup(s=>s.BeginProviderAttemptAsync(It.IsAny<string>(),It.IsAny<string>(),It.IsAny<UsageProviderAttempt>(),It.IsAny<CancellationToken>()))
                .Callback((string id,string owner,UsageProviderAttempt attempt,CancellationToken ct)=>begins.Add(attempt)).Returns(Task.CompletedTask);
            store.Setup(s=>s.AppendReceiptAsync(It.IsAny<string>(),It.IsAny<string>(),It.IsAny<UsageProviderReceipt>(),It.IsAny<CancellationToken>()))
                .Returns((string id,string owner,UsageProviderReceipt receipt,CancellationToken ct)=>{
                    if(mode=="uncertain")throw new IOException("acknowledgement unknown");
                    receipt.Validate();receipts.Add(receipt);return Task.CompletedTask;});
            store.Setup(s=>s.RecordNoProviderExecutionAsync(It.IsAny<string>(),It.IsAny<string>(),It.IsAny<string>(),It.IsAny<CancellationToken>()))
                .Callback((string id,string owner,string reason,CancellationToken ct)=>noExecution.Add(reason)).Returns(Task.CompletedTask);
            var handle=new UsageExecutionHandle(new UsageOutboxOperation{Id=Guid.NewGuid().ToString("D"),UserId=actor.ToString("D"),ExecutionOwner="owner",
                Fingerprint=mode=="fingerprint"?new string('0',64):captured.Result.RequestFingerprint,Policy=new UsageEndpointPolicy{RequiresProviderMeasurement=true,ReservedUnits=mode=="unit-budget"?1:100,MaximumProviderAttempts=mode=="attempt-budget"?1:100,
                    EstimatedCostUsd=mode=="under-reserved"?0:captured.Result.MaximumServiceCostUsd}},store.Object,new Web4UsageLedgerOptions(),
                new SubscriptionAuthorizationResult(),Mock.Of<IUsageReservationAuthority>());
            http.User=new ClaimsPrincipal(new ClaimsIdentity(new[]{new Claim(ClaimTypes.NameIdentifier,actor.ToString("D"))},"fixture"));
            http.Items["Avatar"]=new Avatar{Id=actor};http.Items[Web4UsageMeasurement.ExecutionKey]=handle;
            if(mode!="missing"){
                var bound=CqlIndexedV1RequestContext.Bind(http,captured.Result,handle);
                Assert.Equal(mode is "under-reserved" or "fingerprint" or "attempt-budget" or "unit-budget",bound.IsError);
            }
            var prepared=new PreparedStorageExecution(handle,"indexed-http",new Dictionary<ProviderType,string>(),Array.Empty<ProviderType>(),null,null,false,new[]{binding.Tariffs});
            states.Add((mode,reader,begins,receipts,noExecution));
            await prepared.RunAsync(async()=>{await next();return true;});
            Assert.Equal(1,registry.Calls); // Dispatch never resolves the alias again.
        });
        app.MapControllers();await app.StartAsync();
        try{
            var addresses=app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!;
            var listener=Assert.Single(addresses.Addresses);
            Console.WriteLine("Indexed HTTP fixture listener: "+listener);
            using var client=new HttpClient{BaseAddress=new Uri(listener),Timeout=TimeSpan.FromSeconds(10)};
            const string valid="{\"alias\":\"solana-demo\",\"query\":{\"orderBy\":\"Id\",\"take\":1}}";
            using(var anonymous=await client.PostAsync("/api/data/query-cql/indexed",new StringContent(valid,Encoding.UTF8,"application/json")))
                Assert.Equal(HttpStatusCode.Unauthorized,anonymous.StatusCode);
            Assert.Empty(states);
            client.DefaultRequestHeaders.Authorization=new("Bearer","indexed-fixture");
            foreach(var mode in new[]{"missing","under-reserved","fingerprint","attempt-budget","unit-budget","changed","unknown","rows","empty","wide-slot","uncertain"}){
                client.DefaultRequestHeaders.Remove("X-Fixture-Case");client.DefaultRequestHeaders.Add("X-Fixture-Case",mode);
                var body=mode=="changed"?valid.Replace("\"take\":1","\"take\":2"):mode=="unknown"?valid.Replace("\"alias\"","\"endpoint\":\"https://caller/\",\"alias\""):valid;
                using var response=await client.PostAsync("/api/data/query-cql/indexed",new StringContent(body,Encoding.UTF8,"application/json"));
                var text=await response.Content.ReadAsStringAsync();var state=states.Last();
                var status=mode is "rows" or "empty" or "wide-slot"?HttpStatusCode.OK:mode=="changed"?HttpStatusCode.Conflict:mode=="unknown"?HttpStatusCode.BadRequest:HttpStatusCode.ServiceUnavailable;
                Assert.True(response.StatusCode==status,text);
                if(mode is "rows" or "empty" or "wide-slot"){
                    using var json=JsonDocument.Parse(text);var result=json.RootElement.GetProperty("result");
                    Assert.True(json.RootElement.GetProperty("isLoaded").GetBoolean());
                    Assert.Equal(mode=="empty"?0:1,json.RootElement.GetProperty("loadedCount").GetInt32());
                    Assert.Equal("indexed",result.GetProperty("mode").GetString());
                    Assert.Equal(source.SourceProvider.ToString(),result.GetProperty("provider").GetString());
                    Assert.Equal(source.ExecutionProvider.ToString(),result.GetProperty("executionProvider").GetString());
                    Assert.Equal(mode=="empty"?0:1,result.GetProperty("count").GetInt32());
                    Assert.Equal(mode=="wide-slot"?"9007199254740993":"1",result.GetProperty("checkpoint").GetProperty("position").GetProperty("slot").GetString());
                    Assert.Equal(mode=="empty"?0:1,result.GetProperty("receipts").GetArrayLength());
                    Assert.Equal(mode=="empty"?4:7,state.Begins.Count);Assert.Equal(state.Begins.Count,state.Receipts.Count);
                    Assert.DoesNotContain("providerUniqueStorageKey",text,StringComparison.OrdinalIgnoreCase);
                    Assert.Empty(state.NoExecution);
                }else if(mode=="uncertain"){
                    Assert.Contains("CQL_INDEXED_ATTEMPT_UNCERTAIN",text);Assert.Single(state.Begins);Assert.Empty(state.Receipts);Assert.Empty(state.NoExecution);
                }else{
                    Assert.Equal(0,state.Reader.Calls);Assert.Empty(state.Begins);Assert.Single(state.NoExecution);
                }
            }
            nativeRegistry.VerifyNoOtherCalls();
        }finally{await app.StopAsync();Console.WriteLine("Indexed HTTP fixture listener stopped.");}
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task RequestContextIsSingleUseAndRequiresTheExactCapturedTariffPolicy(bool otherPolicy)
    {
        var f=Fixture("rows");var binding=WebBinding(f.Reader);
        var preparation=CqlIndexedV1Preparation.Prepare(actor,WebRequest(),new BindingRegistry(binding)).Result;
        var store=new Mock<IUsageOutboxStore>(MockBehavior.Strict);
        var handle=new UsageExecutionHandle(new UsageOutboxOperation{Id="context-once",UserId=actor.ToString("D"),Fingerprint=preparation.RequestFingerprint,
            Policy=new UsageEndpointPolicy{RequiresProviderMeasurement=true,ReservedUnits=100,EstimatedCostUsd=1m,MaximumProviderAttempts=100}},
            store.Object,new Web4UsageLedgerOptions(),new SubscriptionAuthorizationResult(),Mock.Of<IUsageReservationAuthority>());
        var http=new DefaultHttpContext();http.Items[Web4UsageMeasurement.ExecutionKey]=handle;
        Assert.False(CqlIndexedV1RequestContext.Bind(http,preparation,handle).IsError);
        Assert.True(CqlIndexedV1RequestContext.Bind(http,preparation,handle).IsError);
        var prepared=new PreparedStorageExecution(handle,"context-once",new Dictionary<ProviderType,string>(),Array.Empty<ProviderType>(),null,null,false,
            new[]{otherPolicy?Tariffs():binding.Tariffs});
        await prepared.RunAsync(()=>{
            Assert.Equal(!otherPolicy,CqlIndexedV1RequestContext.TryConsume(http,prepared,out _));
            Assert.False(CqlIndexedV1RequestContext.TryConsume(http,prepared,out _));
            return Task.FromResult(true);
        });
        Assert.Equal(0,f.Reader.Calls);store.VerifyNoOtherCalls();
    }

    [Fact]
    public void PhysicalReadPolicyCannotExceedTheGlobalUsageAttemptCeiling()
    {Assert.Throws<ArgumentException>(()=>Tariffs(513));}
}
