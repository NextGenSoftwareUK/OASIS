using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.Synchronization;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.UnitTests;

[TestClass]
public class HolonReplicationConfigurationTests
{
    private static readonly DateTime Now=new(2026,10,19,12,0,0,DateTimeKind.Utc);
    private sealed class Clock : IHyperDriveClock{public DateTime Value=Now;public DateTime UtcNow=>Value;}
    public interface CompleteProvider : IOASISStorageProvider,IAtomicHolonReplicationStorageProvider,IHolonReplicationOutboxSource,ICommittedHolonReplicaStorageProvider {}
    public class Probe : DispatchProxy
    {
        public ProviderType Type;public bool Active,ActivationFails,CaptureFails;public int Activations,Captures,Deactivations;
        public Action OnActivate;public Store Outbox=new();
        protected override object Invoke(MethodInfo method,object[] args)
        {
            switch(method.Name)
            {
                case "get_ProviderType":return new EnumValue<ProviderType>(Type);
                case "get_IsProviderActivated":return Active;
                case "ActivateProviderAsync":Activations++;OnActivate?.Invoke();Active=!ActivationFails;return Task.FromResult(new OASISResult<bool>(Active){IsError=ActivationFails});
                case "GetHolonReplicationOutbox":Captures++;return new OASISResult<IHolonReplicationReadinessOutbox>(CaptureFails ? null:Outbox){IsError=CaptureFails};
                case "DeActivateProviderAsync":Deactivations++;Active=false;return Task.FromResult(new OASISResult<bool>(true));
                default:throw new InvalidOperationException("Unexpected provider call: "+method.Name);
            }
        }
    }
    public sealed class Store : IHolonReplicationReadinessOutbox
    {
        public bool Fail;public int Peeks,Claims;
        public Task<OASISResult<HolonDeliveryInspection>> PeekNextAsync(string target,CancellationToken token=default)
        {token.ThrowIfCancellationRequested();Peeks++;return Task.FromResult(new OASISResult<HolonDeliveryInspection>{IsError=Fail});}
        public Task<OASISResult<HolonDeliveryClaim>> ClaimNextAsync(string target,string worker,TimeSpan lease,CancellationToken token=default)
        {Claims++;return Task.FromResult(new OASISResult<HolonDeliveryClaim>());}
        public Task<OASISResult<bool>> RenewAsync(HolonDeliveryClaim claim,TimeSpan lease,CancellationToken token=default)=>throw new NotSupportedException();
        public Task<OASISResult<bool>> FinishAsync(HolonDeliveryClaim claim,HolonDeliveryOutcome outcome,string errorCode=null,string message=null,DateTime? nextAttemptUtc=null,CancellationToken cancellationToken=default)=>throw new NotSupportedException();
        public Task<OASISResult<bool>> FinishWithRecoveryAsync(HolonDeliveryClaim claim,HolonDeliveryOutcome outcome,HolonDeliveryRecoveryEvidence evidence,string errorCode=null,string message=null,DateTime? nextAttemptUtc=null,CancellationToken cancellationToken=default)=>throw new NotSupportedException();
        public Task<OASISResult<HolonDeliveryInspection>> InspectAsync(long sequence,CancellationToken cancellationToken=default)=>throw new NotSupportedException();
    }
    private static CompleteProvider Provider(ProviderType type)
    {var value=DispatchProxy.Create<CompleteProvider,Probe>();((Probe)value).Type=type;return value;}
    private static JObject Document(DateTime? expiry=null)=>new(){["enabled"]=true,["workerId"]="worker",["catalogs"]=new JArray(new JObject{
        ["id"]="catalog",["version"]="cc157ef4-0a97-49d4-a7bc-f4eedc6fc969",["sourceId"]="source",["storeIdentity"]="database",
        ["sourceProvider"]="SQLLiteDBOASIS",["targets"]=new JArray(new JObject{["name"]="mongo",["endpointIdentity"]="mongo-eu",
            ["provider"]="MongoDBOASIS",["actors"]=new JArray("65f7c683-57e0-44ae-8dba-312bd2ee96e0"),["expiresAtUtc"]=(expiry ?? Now.AddHours(1)).ToString("O")})})};
    private static HolonReplicationConfiguration Configuration(Clock clock=null)=>HolonReplicationConfiguration.Parse(Document().ToString(),clock ?? new Clock());

    [DataTestMethod]
    [DataRow("unknown")] [DataRow("missing")] [DataRow("worker")] [DataRow("default")] [DataRow("numeric")]
    [DataRow("self")] [DataRow("expired")] [DataRow("offset")] [DataRow("actor")] [DataRow("empty")] [DataRow("version")] [DataRow("type")]
    public void InvalidConfigurationIsRejectedBeforeRegistrationOrResolution(string fault)
    {
        var root=Document();var catalog=(JObject)root["catalogs"][0];var target=(JObject)catalog["targets"][0];
        switch(fault)
        {
            case "unknown":root["typo"]=true;break;case "missing":root.Remove("workerId");break;case "worker":root["workerId"]=" ";break;
            case "default":catalog["sourceProvider"]="Default";break;case "numeric":catalog["sourceProvider"]=1;break;
            case "self":target["provider"]="SQLLiteDBOASIS";break;case "expired":target["expiresAtUtc"]=Now.ToString("O");break;
            case "offset":target["expiresAtUtc"]="2026-10-19T13:00:00+00:00";break;case "actor":target["actors"]=new JArray(Guid.Empty.ToString());break;
            case "empty":catalog["targets"]=new JArray();break;case "version":catalog["version"]=Guid.Empty.ToString();break;
            default:root["enabled"]="true";break;
        }
        Assert.ThrowsException<System.IO.InvalidDataException>(()=>HolonReplicationConfiguration.Parse(root.ToString(),new Clock()));
    }
    [TestMethod]
    public void DisabledConfigurationCannotHideOtherOptionsAndDuplicateFieldsAreRejected()
    {
        Assert.IsFalse(HolonReplicationConfiguration.Parse(null).Enabled);Assert.IsFalse(HolonReplicationConfiguration.Parse("{\"enabled\":false}").Enabled);
        Assert.ThrowsException<System.IO.InvalidDataException>(()=>HolonReplicationConfiguration.Parse("{\"enabled\":false,\"workerId\":\"hidden\"}"));
        Assert.ThrowsException<System.IO.InvalidDataException>(()=>HolonReplicationConfiguration.Parse("{\"enabled\":false,\"enabled\":true}"));
        Assert.ThrowsException<System.IO.InvalidDataException>(()=>HolonReplicationConfiguration.Parse("{\"enabled\":false} {}"));
    }
    [TestMethod]
    public async Task FactoryPinsAllCapabilitiesBeforeActivationAndReusesCapturedStore()
    {
        var source=Provider(ProviderType.SQLLiteDBOASIS);var target=Provider(ProviderType.MongoDBOASIS);var calls=new List<ProviderType>();
        var configured=Configuration();var catalogs=await HolonReplicationCatalogFactory.BuildAsync(configured,type=>{calls.Add(type);return type==ProviderType.SQLLiteDBOASIS ? source:target;});
        Assert.AreEqual(2,calls.Count);Assert.AreEqual(1,((Probe)source).Activations);Assert.AreEqual(1,((Probe)target).Activations);
        Assert.AreEqual(1,((Probe)source).Captures);Assert.AreSame(((Probe)source).Outbox,catalogs.Single().Store);Assert.AreEqual(1,((Probe)source).Outbox.Peeks);
        Assert.AreEqual(0,((Probe)source).Outbox.Claims);Assert.AreEqual(0,((Probe)source).Deactivations);
        var equivalent=await HolonReplicationCatalogFactory.BuildAsync(configured,type=>type==ProviderType.SQLLiteDBOASIS ? source:target);
        Assert.AreEqual(1,((Probe)source).Activations);CollectionAssert.AreEqual(catalogs[0].TargetIds.ToArray(),equivalent[0].TargetIds.ToArray());
    }
    [DataTestMethod]
    [DataRow("missing")] [DataRow("wrong")] [DataRow("capability")] [DataRow("expired")]
    public async Task InvalidBindingsOrExpiredGrantsCannotActivateAnyProvider(string fault)
    {
        var clock=new Clock();var config=Configuration(clock);var source=Provider(ProviderType.SQLLiteDBOASIS);var target=Provider(ProviderType.MongoDBOASIS);
        if(fault=="wrong")((Probe)target).Type=ProviderType.Neo4jOASIS;if(fault=="expired")clock.Value=Now.AddHours(1);
        var plain=DispatchProxy.Create<IOASISStorageProvider,Probe>();((Probe)plain).Type=ProviderType.MongoDBOASIS;
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(()=>HolonReplicationCatalogFactory.BuildAsync(config,type=>type==ProviderType.SQLLiteDBOASIS ? source
            :fault=="missing" ? null:fault=="capability" ? plain:target));
        Assert.AreEqual(0,((Probe)source).Activations);Assert.AreEqual(0,((Probe)target).Activations);Assert.AreEqual(0,((Probe)source).Captures);
    }
    [DataTestMethod]
    [DataRow("activation")] [DataRow("capture")] [DataRow("peek")] [DataRow("expiry-during")] [DataRow("authority-drift")]
    public async Task NativeStartupFailuresRemainFailuresWithoutDefaultFallbackOrQueueMutation(string fault)
    {
        var clock=new Clock();var source=Provider(ProviderType.SQLLiteDBOASIS);var target=Provider(ProviderType.MongoDBOASIS);var probe=(Probe)source;
        probe.ActivationFails=fault=="activation";probe.CaptureFails=fault=="capture";probe.Outbox.Fail=fault=="peek";
        if(fault=="expiry-during")probe.OnActivate=()=>clock.Value=Now.AddHours(1);
        if(fault=="authority-drift")probe.OnActivate=()=>probe.Type=ProviderType.Neo4jOASIS;
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(()=>HolonReplicationCatalogFactory.BuildAsync(Configuration(clock),type=>type==ProviderType.SQLLiteDBOASIS ? source:target));
        Assert.AreEqual(0,probe.Outbox.Claims);Assert.AreEqual(0,probe.Deactivations);
    }
    [TestMethod]
    public async Task ConfiguredServiceFailsStartupEvenWhenBackgroundFailurePolicyIgnoresExceptions()
    {
        var services=new ServiceCollection();services.Configure<HostOptions>(options=>options.BackgroundServiceExceptionBehavior=BackgroundServiceExceptionBehavior.Ignore);
        services.AddConfiguredCosmicHolonReplication(Document(DateTime.UtcNow.AddHours(1)).ToString(),_=>null);
        using var provider=services.BuildServiceProvider();var hosted=provider.GetRequiredService<ConfiguredHolonReplicationHostedService>();
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(()=>hosted.StartAsync(default));
        Assert.IsFalse(provider.GetRequiredService<HolonReplicationHostState>().IsReady);
    }
    [TestMethod]
    public async Task ConfiguredHostStartsTheFactoryAndJoinsItsWorkerWithoutOwningSharedProviders()
    {
        var source=Provider(ProviderType.SQLLiteDBOASIS);var target=Provider(ProviderType.MongoDBOASIS);
        using var host=new HostBuilder().ConfigureServices(services=>services.AddConfiguredCosmicHolonReplication(Document(DateTime.UtcNow.AddHours(1)).ToString(),
            type=>type==ProviderType.SQLLiteDBOASIS ? source:target)).Build();
        await host.StartAsync();
        try
        {
            var state=host.Services.GetRequiredService<HolonReplicationHostState>();var deadline=DateTime.UtcNow.AddSeconds(5);
            while(!state.IsReady){Assert.IsTrue(DateTime.UtcNow<deadline);await Task.Delay(10);}
            Assert.AreEqual(1,host.Services.GetRequiredService<ConfiguredHolonReplicationHostedService>().Catalogs.Count);
        }
        finally{using var stop=new CancellationTokenSource(TimeSpan.FromSeconds(5));await host.StopAsync(stop.Token);}
        Assert.AreEqual(0,((Probe)source).Deactivations);Assert.AreEqual(0,((Probe)target).Deactivations);
    }
    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public void ProgrammaticAndConfiguredRegistrationCannotCreateTwoWorkerOwners(bool configuredFirst)
    {
        var target=Provider(ProviderType.MongoDBOASIS);var services=new ServiceCollection();
        var catalog=new HolonReplicationCatalog("manual",Guid.NewGuid(),"source","database",ProviderType.SQLLiteDBOASIS,new Store(),
            new[]{new HolonReplicationDestination("mongo","endpoint",ProviderType.MongoDBOASIS,target,
                new HolonReplicationActorGrant(new[]{Guid.NewGuid()},DateTime.UtcNow.AddMinutes(1)))});
        if(configuredFirst)services.AddConfiguredCosmicHolonReplication(Document(DateTime.UtcNow.AddMinutes(1)).ToString(),_=>null);
        else services.AddCosmicHolonReplication(new HolonReplicationHostOptions("worker"),catalog);
        var count=services.Count;
        if(configuredFirst)Assert.ThrowsException<InvalidOperationException>(()=>services.AddCosmicHolonReplication(new HolonReplicationHostOptions("worker"),catalog));
        else Assert.ThrowsException<InvalidOperationException>(()=>services.AddConfiguredCosmicHolonReplication(Document(DateTime.UtcNow.AddMinutes(1)).ToString(),_=>null));
        Assert.AreEqual(count,services.Count);
    }

    [TestMethod]
    public async Task SharedInitializationIsAwaitedBeforeAnyRegistryLookup()
    {
        var started=new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);var release=new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var source=Provider(ProviderType.SQLLiteDBOASIS);var target=Provider(ProviderType.MongoDBOASIS);var lookups=0;
        var services=new ServiceCollection();services.AddConfiguredCosmicHolonReplicationFromServices(Document(DateTime.UtcNow.AddMinutes(1)).ToString(),_=>
            new HolonReplicationProviderResolver(type=>{Interlocked.Increment(ref lookups);return type==ProviderType.SQLLiteDBOASIS ? source:target;},
                async token=>{started.TrySetResult(true);await release.Task.WaitAsync(token);}));
        using var provider=services.BuildServiceProvider();var service=provider.GetRequiredService<ConfiguredHolonReplicationHostedService>();
        var start=service.StartAsync(default);await started.Task.WaitAsync(TimeSpan.FromSeconds(5));Assert.AreEqual(0,lookups);Assert.AreEqual(0,((Probe)source).Activations);
        release.TrySetResult(true);await start.WaitAsync(TimeSpan.FromSeconds(5));
        using var stop=new CancellationTokenSource(TimeSpan.FromSeconds(5));await service.StopAsync(stop.Token);Assert.AreEqual(2,lookups);
    }

}
