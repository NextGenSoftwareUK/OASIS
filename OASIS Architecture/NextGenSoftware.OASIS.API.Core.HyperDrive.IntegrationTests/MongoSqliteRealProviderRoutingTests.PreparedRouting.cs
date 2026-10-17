using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public sealed partial class MongoSqliteRealProviderRoutingTests
{
    // Controlled authority only; actual provider activation, storage and outbox persistence below.
    private sealed class RoutingAdmissionAuthority(string user) : IUsageReservationAuthority
    {
        public Task<SubscriptionAuthorizationResult> AuthorizeUsageAsync(string bearer,UsageAuthorizationRequest request,CancellationToken token=default)
            =>Task.FromResult(new SubscriptionAuthorizationResult {Allowed=true,UserId=user,OperationId=request.OperationId,Status="authorized",ExpiresAtUtc=DateTime.UtcNow.AddMinutes(5)});
        public Task<UsageStartResult> StartUsageAsync(UsageStartRequest request,CancellationToken token=default)
            =>Task.FromResult(new UsageStartResult {Started=true,OperationId=request.OperationId});
        public Task<UsageSettlementResult> SettleUsageAsync(UsageSettlementRequest request,CancellationToken token=default)
            =>throw new InvalidOperationException("This routing test does not authorize billing or fabricated measurements.");
    }

    private static async Task<(MongoUsageOutboxStore Store,UsageExecutionHandle Execution,PreparedStorageExecution Policy)> CreatePreparedRoutingAsync(string uri,string database,string correlation,int maximumProviderAttempts=512)
    {
        var store=new MongoUsageOutboxStore(uri,database);await store.InitializeAsync(default);
        var user=Guid.NewGuid().ToString("D");
        var executor=new Web4UsageExecution(new RoutingAdmissionAuthority(user),store,new Web4UsageLedgerOptions {Service="WEB4"});
        var execution=await executor.BeginAsync("controlled-bearer",user,"prepared-"+correlation,new string('b',64),"POST /controlled-routing",
            new UsageEndpointPolicy {MeterCategory="api.request",ReservedUnits=1,RequiresProviderMeasurement=true,MaximumProviderAttempts=maximumProviderAttempts});
        var types=new[]{ProviderType.MongoDBOASIS,ProviderType.SQLLiteDBOASIS};
        return(store,execution,new PreparedStorageExecution(execution,correlation,new Dictionary<ProviderType,string>(),types));
    }

    private static async Task VerifyInheritedPreparedRoutingAsync(string uri,string database,MongoDBOASIS mongo,SQLLiteDBOASIS sqlite,bool synchronous,bool mongoPrimary)
    {
        var (store,execution,policy)=await CreatePreparedRoutingAsync(uri,database,"real-routing");
        var types=new[]{ProviderType.MongoDBOASIS,ProviderType.SQLLiteDBOASIS};
        var dna=new OASISDNA {OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS {HyperDriveMode=HyperDriveModes.V2,StorageProviders=new StorageProviderSettings()}};
        dna.OASIS.SubscriptionConfig.PlanType="Gold";
        var manager=new ProviderManager(null,dna){IsAutoReplicationEnabled=true};
        manager.RegisterProvider(mongo);manager.RegisterProvider(sqlite);
        manager.SetAndReplaceAutoReplicationListForProviders(types.Select(type=>new EnumValue<ProviderType>(type))).IsError.Should().BeFalse();
        var router=new OASISHyperDrive(manager);
        (await mongo.DeActivateProviderAsync()).IsError.Should().BeFalse();
        (await sqlite.DeActivateProviderAsync()).IsError.Should().BeFalse();
        var holon=new Holon {Id=Guid.NewGuid(),Name="owned-inherited-policy"};
        var request=new StorageOperationRequest {Operation="SaveHolon",Payload=holon,SaveChildren=false,PreferredProvider=mongoPrimary?types[0]:types[1]};
        await policy.RunAsync(async()=>{
            var saved=synchronous?router.RouteRequest<IHolon>(request):await router.RouteRequestAsync<IHolon>(request);
            saved.IsError.Should().BeFalse(saved.Message);request.PreparedExecution.Should().BeSameAs(policy);
            foreach(var type in types){
                var typed=new LoadHolonRequest {HolonId=holon.Id,LoadChildren=false,PreferredProvider=type};
                var loaded=synchronous?router.RouteRequestToProvider<IHolon>(typed,type):await router.RouteRequestToProviderAsync<IHolon>(typed,type);
                loaded.IsError.Should().BeFalse(loaded.Message);loaded.Result.Name.Should().Be(holon.Name);
            }
            return true;
        });
        mongo.IsProviderActivated.Should().BeTrue();sqlite.IsProviderActivated.Should().BeTrue();
        var restored=await new MongoUsageOutboxStore(uri,database).GetAsync(execution.OperationId,default);
        restored.ProviderAttempts.Should().HaveCount(4);
        restored.ProviderAttempts.Take(2).Select(attempt=>attempt.Provider).Should().Equal(
            mongoPrimary?new[]{types[0].ToString(),types[1].ToString()}:new[]{types[1].ToString(),types[0].ToString()});
        restored.ProviderAttempts.Select(attempt=>attempt.ClientRequestId).Distinct().Should().HaveCount(4);
        restored.ProviderAttempts.Should().OnlyContain(attempt=>attempt.NativePreparation==null&&attempt.ProviderRequestId==null);
        restored.Receipts.Should().BeEmpty(); // Attempts are not invented measurements or settled native fees.
        var denied=new PreparedStorageExecution(execution,"denied-routing",new Dictionary<ProviderType,string>(),Array.Empty<ProviderType>());
        await denied.RunAsync(async()=>{
            var typed=new LoadHolonRequest {HolonId=holon.Id,LoadChildren=false};
            var result=synchronous?router.RouteRequestToProvider<IHolon>(typed,types[0]):await router.RouteRequestToProviderAsync<IHolon>(typed,types[0]);
            result.IsError.Should().BeTrue();result.ErrorCode.Should().Be(PreparedStorageExecution.RejectedCode);
            return true;
        });
        (await store.GetAsync(execution.OperationId,default)).ProviderAttempts.Should().HaveCount(4);
        var bounded=await CreatePreparedRoutingAsync(uri,database,"bounded-routing",1);
        var boundedHolon=new Holon {Id=Guid.NewGuid(),Name="owned-primary-only-budget"};
        var boundedRequest=new StorageOperationRequest {Operation="SaveHolon",Payload=boundedHolon,SaveChildren=false,PreferredProvider=mongoPrimary?types[0]:types[1]};
        await bounded.Policy.RunAsync(async()=>{
            var saved=synchronous?router.RouteRequest<IHolon>(boundedRequest):await router.RouteRequestAsync<IHolon>(boundedRequest);
            saved.IsError.Should().BeFalse(saved.Message);saved.IsSaved.Should().BeTrue();saved.IsWarning.Should().BeTrue();
            saved.Exception.Should().BeOfType<UsageProtocolException>().Which.Code.Should().Be("PROVIDER_ATTEMPT_BUDGET_EXCEEDED");
            return true;
        });
        var primary=mongoPrimary?(IOASISStorageProvider)mongo:sqlite;
        var replica=mongoPrimary?(IOASISStorageProvider)sqlite:mongo;
        var committed=await primary.LoadHolonAsync(boundedHolon.Id,loadChildren:false);
        committed.IsError.Should().BeFalse(committed.Message);committed.Result.Name.Should().Be(boundedHolon.Name);
        var absent=await replica.LoadHolonAsync(boundedHolon.Id,loadChildren:false);
        absent.Result.Should().BeNull();
        var boundedStored=await new MongoUsageOutboxStore(uri,database).GetAsync(bounded.Execution.OperationId,default);
        boundedStored.Policy.MaximumProviderAttempts.Should().Be(1);
        boundedStored.ProviderAttempts.Should().ContainSingle().Which.Provider.Should().Be(primary.ProviderType.Value.ToString());
        boundedStored.Receipts.Should().BeEmpty();
    }

    private static async Task<NextGenSoftware.OASIS.Common.OASISResult<IHolon>> VerifyInheritedPreparedFallbackAsync(string uri,string database,OASISHyperDrive router,StorageOperationRequest request,bool synchronous)
    {
        var (store,execution,policy)=await CreatePreparedRoutingAsync(uri,database,"real-fallback");
        var result=await policy.RunAsync(async()=>synchronous?router.RouteRequest<IHolon>(request):await router.RouteRequestAsync<IHolon>(request));
        result.IsError.Should().BeFalse(result.Message);request.PreparedExecution.Should().BeSameAs(policy);
        var restored=await new MongoUsageOutboxStore(uri,database).GetAsync(execution.OperationId,default);
        restored.ProviderAttempts.Select(attempt=>attempt.Provider).Should().Equal(ProviderType.SQLLiteDBOASIS.ToString(),ProviderType.MongoDBOASIS.ToString());
        restored.ProviderAttempts.Select(attempt=>attempt.ClientRequestId).Distinct().Should().HaveCount(2);
        restored.Receipts.Should().BeEmpty();
        return result;
    }
}
