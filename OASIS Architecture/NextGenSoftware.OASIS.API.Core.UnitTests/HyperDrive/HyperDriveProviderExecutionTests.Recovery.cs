using FluentAssertions;
using Moq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.UnitTests.HyperDrive;

public sealed partial class HyperDriveProviderExecutionTests
{
    [Fact]
    public async Task PredictiveRecoveryAllowsOneConcurrentProbeAndIgnoresStaleCompletion()
    {
        var dna=CreateDna(NextGenSoftware.OASIS.API.DNA.HyperDriveModes.V2);
        dna.OASIS.OASISHyperDriveConfig=new() {HealthCheckIntervalMs=1000};
        var engine=PredictiveFailoverEngine.ForProviderManager(new ProviderManager(null,dna));
        var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
        var now=DateTime.UtcNow;
        typeof(PredictiveFailoverEngine).GetField("_utcNow",flags)!.SetValue(engine,(Func<DateTime>)(()=>now));
        var set=typeof(PredictiveFailoverEngine).GetMethod("ExecutePreventiveFailoverAsync",flags)!;
        var acquire=typeof(PredictiveFailoverEngine).GetMethod("GetFailoverOverride",flags)!;
        await (Task)set.Invoke(engine,new object[] {ProviderType.LocalFileOASIS,ProviderType.SQLLiteDBOASIS})!;
        now=now.AddSeconds(1);
        var probes=new System.Collections.Concurrent.ConcurrentBag<Action<bool>>();
        Parallel.For(0,32,_=>{
            object[] args={ProviderType.LocalFileOASIS,null!};
            var selected=(ProviderType)acquire.Invoke(engine,args)!;
            if(args[1] is Action<bool> complete) {selected.Should().Be(ProviderType.Default);probes.Add(complete);}
            else selected.Should().Be(ProviderType.SQLLiteDBOASIS);
        });
        probes.Count.Should().Be(1);
        // A newer prediction owns its own state; old in-flight success cannot remove it.
        await (Task)set.Invoke(engine,new object[] {ProviderType.LocalFileOASIS,ProviderType.AzureStorageOASIS})!;
        probes.Single()(true);
        engine.GetFailoverOverride(ProviderType.LocalFileOASIS).Should().Be(ProviderType.AzureStorageOASIS);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task PredictiveRecoveryRetriesAfterIntervalAndClearsOnlyAfterSuccess(bool asynchronous)
    {
        var id=Guid.NewGuid();
        var primary=CreateActiveProvider(ProviderType.LocalFileOASIS,"recovering");
        var alternate=CreateActiveProvider(ProviderType.SQLLiteDBOASIS,"alternate");
        var healthy=false;
        OASISResult<IAvatar> PrimaryResult()=>healthy ? new(new Avatar {Id=id,Username="primary"})
            : new() {IsError=true,Message="still unavailable"};
        primary.Setup(p=>p.LoadAvatar(id,0)).Returns(PrimaryResult);
        primary.Setup(p=>p.LoadAvatarAsync(id,0)).ReturnsAsync(PrimaryResult);
        alternate.Setup(p=>p.LoadAvatar(id,0)).Returns(new OASISResult<IAvatar>(new Avatar {Id=id,Username="alternate"}));
        alternate.Setup(p=>p.LoadAvatarAsync(id,0)).ReturnsAsync(new OASISResult<IAvatar>(new Avatar {Id=id,Username="alternate"}));
        var dna=CreateDna(NextGenSoftware.OASIS.API.DNA.HyperDriveModes.V2);
        dna.OASIS.OASISHyperDriveConfig=new() {HealthCheckIntervalMs=1000};
        var manager=new ProviderManager(null,dna) {IsAutoFailOverEnabled=false};
        manager.RegisterProvider(primary.Object);manager.RegisterProvider(alternate.Object);
        var engine=PredictiveFailoverEngine.ForProviderManager(manager);
        var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
        var now=DateTime.UtcNow;
        typeof(PredictiveFailoverEngine).GetField("_utcNow",flags)!.SetValue(engine,(Func<DateTime>)(()=>now));
        await (Task)typeof(PredictiveFailoverEngine).GetMethod("ExecutePreventiveFailoverAsync",flags)!
            .Invoke(engine,new object[] {ProviderType.LocalFileOASIS,ProviderType.SQLLiteDBOASIS})!;
        var router=new OASISHyperDrive(manager);
        var request=new StorageOperationRequest {Operation="LoadAvatar",AvatarId=id,PreferredProvider=ProviderType.LocalFileOASIS};
        Task<OASISResult<IAvatar>> Read()=>asynchronous ? router.RouteRequestAsync<IAvatar>(request)
            : Task.FromResult(router.RouteRequest<IAvatar>(request));
        (await Read()).Result.Username.Should().Be("alternate");
        now=now.AddSeconds(1);
        (await Read()).IsError.Should().BeTrue();
        (await Read()).Result.Username.Should().Be("alternate");
        healthy=true;now=now.AddSeconds(1);
        (await Read()).Result.Username.Should().Be("primary");
        engine.GetFailoverOverride(ProviderType.LocalFileOASIS).Should().Be(ProviderType.Default);
        (await Read()).Result.Username.Should().Be("primary");
    }
}
