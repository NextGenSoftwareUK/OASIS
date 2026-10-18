using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Services;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.SuiOASIS.IntegrationTests;

[TestClass,DoNotParallelize]
public class SuiHttpPreparedTests
{
    private sealed class PriceSource:IAssetUsdQuoteSource
    {
        public int Calls;
        public Task<OASISResult<AssetUsdQuote>> GetUsdQuoteAsync(string symbol,CancellationToken token=default)
        {
            token.ThrowIfCancellationRequested();Assert.AreEqual("SUI",symbol);Calls++;
            var now=DateTime.UtcNow.AddSeconds(-1);
            return Task.FromResult(new OASISResult<AssetUsdQuote>{Result=new("sui",1m,now,now,"synthetic-local-http","fixture-v1")});
        }
    }
    private sealed class Authority(string user):HttpMessageHandler
    {
        public readonly List<UsageSettlementRequest> Deliveries=new();
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)
        {
            var body=await request.Content!.ReadAsStringAsync(token);object response;
            if(request.RequestUri!.AbsolutePath.EndsWith("authorize")){
                var input=JsonSerializer.Deserialize<UsageAuthorizationRequest>(body)!;
                response=new SubscriptionAuthorizationResult{Allowed=true,StatusCode=200,UserId=user,OperationId=input.OperationId,Status="authorized",ExpiresAtUtc=DateTime.UtcNow.AddMinutes(5)};
            }else if(request.RequestUri.AbsolutePath.EndsWith("start")){
                var input=JsonSerializer.Deserialize<UsageStartRequest>(body)!;response=new UsageStartResult{Started=true,OperationId=input.OperationId,Status="executing"};
            }else{
                Assert.AreEqual(UsageSettlementProtocol.NativePath,request.RequestUri.AbsolutePath);
                var input=JsonSerializer.Deserialize<UsageSettlementRequest>(body)!;Deliveries.Add(input);
                if(Deliveries.Count==1)return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable){Content=new StringContent("controlled first delivery unavailable")};
                response=new UsageSettlementResult{Settled=true,OperationId=input.OperationId};
            }
            return new HttpResponseMessage(HttpStatusCode.OK){Content=new StringContent(JsonSerializer.Serialize(response),Encoding.UTF8,"application/json")};
        }
    }

    [TestMethod]
    public async Task NativeHttpReservesExecutesAndRecoversWithoutReplay()
    {
        const string connection="mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        Assert.AreEqual(connection,Environment.GetEnvironmentVariable("SUBSCRIPTION_TEST_MONGODB_URI"));
        var root=new DirectoryInfo(AppContext.BaseDirectory);
        while(root!=null&&!File.Exists(Path.Combine(root.FullName,"Scripts/TestHosts/start-sui-test-network.sh")))root=root.Parent;
        Assert.IsNotNull(root);var journal=Path.Combine(root!.FullName,"TestResults/SuiCurrent/prepared-http-native-pending.jsonl");
        Assert.IsFalse(File.Exists(journal));const string database="usage_sui_http_prepared";
        var mongo=new MongoClient(connection);
        using(var databases=await mongo.ListDatabaseNamesAsync())Assert.IsFalse((await databases.ToListAsync()).Contains(database));
        using var lease=new FileStream(journal,FileMode.CreateNew,FileAccess.Write,FileShare.Read);
        using var deadline=new CancellationTokenSource(TimeSpan.FromMinutes(5));var token=deadline.Token;
        async Task Journal(object value){await lease.WriteAsync(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value)+"\n"),token);lease.Flush(true);}
        var user=Guid.NewGuid().ToString("D");var holon=new Holon{Id=Guid.NewGuid(),Name="owned HTTP prepared holon"};
        await Journal(new{database,user,holonId=holon.Id,recovery="Preserve original outbox/ledger; never replay unknown HTTP mutations."});
        using var deployment=JsonDocument.Parse(File.ReadAllText(Environment.GetEnvironmentVariable("OASIS_SUI_TEST_DEPLOYMENT")!));var settings=deployment.RootElement;
        var rpc=Environment.GetEnvironmentVariable("OASIS_SUI_TEST_RPC")!;Assert.IsTrue(new Uri(rpc).IsLoopback);
        using var provider=SuiOASIS.CreateWithNativeFeeLimits(new SuiNativeFeeLimits(1000000000),rpc,"localnet",settings.GetProperty("chainId").GetString()!,
            settings.GetProperty("packageAddress").GetString()!,settings.GetProperty("privateKey").GetString()!,settings.GetProperty("storageObjectId").GetString()!,
            storageEncryptionKeyResolver:()=>SHA256.HashData(Encoding.UTF8.GetBytes("oasis-sui-prepared-storage-fixture-v1")));
        var store=new MongoUsageOutboxStore(connection,database);await store.InitializeAsync(token);
        using var transport=new Authority(user);using var authorityHttp=new HttpClient(transport);
        var authority=new Web4SubscriptionAuthorizationClient(authorityHttp,"https://controlled-authority.invalid","WEB6","test-only-key");
        var options=new Web4UsageLedgerOptions{Service="WEB6",PersistenceTimeout=TimeSpan.FromSeconds(10),WorkerLease=TimeSpan.FromSeconds(5),
            EndpointPolicy=_=>new UsageEndpointPolicy{MeterCategory="api.request",ReservedUnits=1,EstimatedCostUsd=10m,MaximumNativeFeeCostUsd=10m,RequiresProviderMeasurement=true}};
        var prices=new PriceSource();var quotes=new NativeFeePriceQuoteFactory(new("fixture-v1",new[]{new NativeFeeAssetMapping("SuiOASIS",settings.GetProperty("chainId").GetString()!,"MIST","SUI","sui",9)}),prices,TimeSpan.FromMinutes(5),TimeSpan.FromMinutes(3));
        var factory=new PricedStorageAdmissionFactory(quotes,new[]{new NativeStorageAdmissionBinding(ProviderType.SuiOASIS,settings.GetProperty("chainId").GetString()!,settings.GetProperty("address").GetString()!,new[]{"MIST"})},Array.Empty<ProviderType>());
        var builder=WebApplication.CreateBuilder();builder.Logging.ClearProviders();builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddSingleton<IUsageOutboxStore>(store);builder.Services.AddSingleton<IUsageReservationAuthority>(authority);builder.Services.AddSingleton(options);
        builder.Services.AddSingleton<Web4UsageExecution>();builder.Services.AddSingleton<IPreparedStorageAdmissionFactory>(factory);
        builder.Services.AddSingleton<IPreparedNativeFeeEvidenceReader>(provider);
        builder.Services.AddSingleton<IHostedService>(services=>new StorageAdmissionRecoveryReadiness(
            new[]{(Provider:"SuiOASIS",Chain:settings.GetProperty("chainId").GetString()!)},services.GetServices<IPreparedNativeFeeEvidenceReader>()));
        await using var app=builder.Build();string? operationId=null;var endpoints=0;var complete=false;
        app.Use(async(context,next)=>{if(context.Request.Headers.Authorization=="Bearer local-http-test-only")context.User=new ClaimsPrincipal(new ClaimsIdentity(new[]{new Claim(ClaimTypes.NameIdentifier,user)},"controlled-test-auth"));await next(context);});
        app.UseMiddleware<Web4UsageLedgerMiddleware>();app.UseMiddleware<PreparedStorageAdmissionMiddleware>();
        app.Run(async context=>{
            endpoints++;var handle=Web4UsageMeasurement.Execution(context);operationId=handle.OperationId;
            await Journal(new{operationId}); // Durable ownership before manager acquisition or native mutation.
            var dna=new OASISDNA{OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS{HyperDriveMode=HyperDriveModes.V2,StorageProviders=new StorageProviderSettings()}};
            dna.OASIS.SubscriptionConfig.PlanType="Gold";
            var manager=new ProviderManager(null,dna){IsAutoFailOverEnabled=false,IsAutoReplicationEnabled=false,IsAutoLoadBalanceEnabled=false};manager.RegisterProvider(provider);
            var router=new OASISHyperDrive(manager);
            var saved=await router.RouteRequestAsync<IHolon>(new StorageOperationRequest{Operation="SaveHolon",Payload=holon,SaveChildren=false,PreferredProvider=ProviderType.SuiOASIS});Assert.IsFalse(saved.IsError,saved.Message);
            var read=await provider.LoadHolonAsync(holon.Id,loadChildren:false);Assert.IsFalse(read.IsError,read.Message);Assert.AreEqual(holon.Name,read.Result.Name);
            var removed=await router.RouteRequestAsync<IHolon>(new StorageOperationRequest{Operation="DeleteHolon",HolonId=holon.Id,SoftDelete=false,PreferredProvider=ProviderType.SuiOASIS});Assert.IsFalse(removed.IsError,removed.Message);
            var original=await store.GetAsync(operationId,token);Assert.AreEqual(2,original.ProviderAttempts.Count);
            var resolver=new PreparedNativeUsageRecoveryResolver(store,new[]{(IPreparedNativeFeeEvidenceReader)provider});Assert.IsNull(await resolver.ResolveAsync(original,token));
            await context.Response.WriteAsync("native execution complete",token);
        });
        try{
            await app.StartAsync(token);var url=app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
            Assert.IsNotNull(app.Services.GetServices<IHostedService>().OfType<StorageAdmissionRecoveryReadiness>().Single());
            Assert.AreSame(provider,app.Services.GetRequiredService<IPreparedNativeFeeEvidenceReader>());
            Assert.IsFalse(provider.IsProviderActivated);Assert.AreEqual(0,prices.Calls);
            Console.WriteLine("REAL_NATIVE_READER_STARTUP_VERIFIED without activation, quote fetch or native operation.");
            using var client=new HttpClient{BaseAddress=new Uri(url),Timeout=TimeSpan.FromMinutes(3)};
            using(var denied=await client.PostAsync("/native-storage",new StringContent("{}"),token))Assert.AreEqual(HttpStatusCode.Unauthorized,denied.StatusCode);
            Assert.AreEqual(0,endpoints);Assert.AreEqual(0,prices.Calls);Assert.IsFalse(provider.IsProviderActivated);
            using var request=new HttpRequestMessage(HttpMethod.Post,"/native-storage"){Content=new StringContent("{}",Encoding.UTF8,"application/json")};
            request.Headers.Authorization=new("Bearer","local-http-test-only");request.Headers.Add("Idempotency-Key","local-http-native");
            using var response=await client.SendAsync(request,token);var responseBody=await response.Content.ReadAsStringAsync(token);
            Assert.AreEqual(HttpStatusCode.ServiceUnavailable,response.StatusCode,responseBody);StringAssert.Contains(responseBody,"SETTLEMENT_PENDING");
            Assert.AreEqual(1,endpoints);Assert.AreEqual(1,prices.Calls);Assert.IsNotNull(operationId);
            await app.StopAsync(token);
            var reopened=new MongoUsageOutboxStore(connection,database);var pending=await reopened.GetAsync(operationId,token);
            Assert.AreEqual("succeeded",pending.ObservedOutcome);Assert.AreEqual(2,pending.ProviderAttempts.Count);Assert.AreEqual(2,pending.Receipts.Count);Assert.AreEqual("pending",pending.State);
            var originals=pending.ProviderAttempts.Select(attempt=>attempt.ProviderRequestId).ToArray();
            var worker=new Web4UsageSettlementWorker(reopened,authority,options,Array.Empty<IUsageRecoveryResolver>(),NullLogger<Web4UsageSettlementWorker>.Instance);
            var retryUntil=DateTime.UtcNow.AddSeconds(10);UsageOutboxOperation settled=pending;
            while(DateTime.UtcNow<retryUntil){await worker.RunOnceAsync(token);settled=await reopened.GetAsync(operationId,token);if(settled.State=="settled")break;await Task.Delay(100,token);}
            Assert.AreEqual("settled",settled.State);Assert.AreEqual(2,transport.Deliveries.Count);
            Assert.AreEqual(JsonSerializer.Serialize(transport.Deliveries[0]),JsonSerializer.Serialize(transport.Deliveries[1]));
            CollectionAssert.AreEqual(originals,settled.ProviderAttempts.Select(attempt=>attempt.ProviderRequestId).ToArray());Assert.AreEqual(1,endpoints);
            Assert.IsTrue(settled.Settlement.ActualCostUsd>=0m&&settled.Settlement.ActualCostUsd<=10m);
            var absent=await provider.LoadHolonAsync(holon.Id,loadChildren:false);Assert.IsNull(absent.Result);Assert.IsTrue(!absent.IsError||absent.ErrorCode==OASISResultErrorCodes.RecordNotFound);
            Console.WriteLine(JsonSerializer.Serialize(new{loopbackHttp=true,controlledAuthentication=true,syntheticPricing=true,settled,hostedAcceptance=false,replayedProvider=false}));complete=true;
        }finally{
            await app.StopAsync(CancellationToken.None);await provider.DeActivateProviderAsync();
            if(complete){await mongo.DropDatabaseAsync(database,token);lease.Dispose();File.Delete(journal);}
        }
    }
}
