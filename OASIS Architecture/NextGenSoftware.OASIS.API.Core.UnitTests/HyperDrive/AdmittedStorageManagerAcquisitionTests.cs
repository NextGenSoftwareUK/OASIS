using Moq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Helpers;

namespace NextGenSoftware.OASIS.API.Core.UnitTests.HyperDrive;

public class AdmittedStorageManagerAcquisitionTests
{
    private static PreparedStorageExecution Policy()=>new(new UsageExecutionHandle(new UsageOutboxOperation(),Mock.Of<IUsageOutboxStore>(),
        new Web4UsageLedgerOptions(),new SubscriptionAuthorizationResult(),Mock.Of<IUsageReservationAuthority>()),
        "acquisition",new Dictionary<ProviderType,string>(),Array.Empty<ProviderType>());
    private static OASISDNA Dna()=>new(){OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS {HyperDriveMode=HyperDriveModes.V2}};

    [Fact]
    public async Task RealKeyManagerAndNestedManagersDoNotActivateDuringAdmittedAcquisition()
    {
        var dna=Dna();var providers=new ProviderManager(null,dna);
        var storage=new Mock<IOASISStorageProvider>(MockBehavior.Strict);
        storage.SetupGet(p=>p.ProviderType).Returns(new EnumValue<ProviderType>(ProviderType.HashgraphOASIS));
        storage.SetupAdd(p=>p.OnStorageProviderError+=It.IsAny<NextGenSoftware.OASIS.API.Core.Events.EventDelegates.StorageProviderError>());
        await Policy().RunAsync(()=>{
            Assert.True(AdmittedStorageManagerAcquisition.TryCreate(()=>dna,()=>storage.Object,
                (selected,configuration)=>new KeyManager(selected,configuration,providers),out KeyManager manager));
            Assert.Same(dna,manager.OASISDNA);
            Assert.Same(storage.Object,providers.GetStorageProvider(ProviderType.HashgraphOASIS));
            return Task.FromResult(0);
        });
        storage.Verify(p=>p.ActivateProvider(),Times.Never);
        storage.Verify(p=>p.ActivateProviderAsync(),Times.Never);
    }

    [Fact]
    public void OrdinaryAcquisitionNeverEvaluatesAdmittedCallbacks()
    {
        Assert.False(AdmittedStorageManagerAcquisition.TryCreate<object>(()=>throw new Exception("DNA evaluated"),
            ()=>throw new Exception("provider evaluated"),(_,_)=>throw new Exception("factory evaluated"),out var result));
        Assert.Null(result);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task MissingOrNonV2DnaRejectsBeforeProviderSelection(bool missing)
    {
        await Policy().RunAsync(()=>{
            var dna=Dna();dna.OASIS.HyperDriveMode="unreviewed-mode";
            Assert.Equal(PreparedStorageExecution.RejectedCode,Assert.Throws<UsageProtocolException>(()=>
                AdmittedStorageManagerAcquisition.TryCreate<object>(()=>missing?null:dna,
                    ()=>throw new Exception("provider evaluated"),(_,_)=>throw new Exception("factory evaluated"),out _)).Code);
            return Task.FromResult(0);
        });
    }

    [Fact]
    public async Task AdmittedAcquisitionPassesExactConfigurationAndProviderOnce()
    {
        var dna=Dna();var provider=Mock.Of<IOASISStorageProvider>();var expected=new object();var selections=0;var creations=0;
        await Policy().RunAsync(()=>{
            Assert.True(AdmittedStorageManagerAcquisition.TryCreate(()=>dna,()=>{selections++;return provider;},(selected,configuration)=>{
                creations++;Assert.Same(provider,selected);Assert.Same(dna,configuration);return expected;
            },out var manager));
            Assert.Same(expected,manager);return Task.FromResult(0);
        });
        Assert.Equal(1,selections);Assert.Equal(1,creations);
    }

    [Fact]
    public async Task FailedFactoryIsNotAnOrdinaryAcquisitionFallback()
    {
        await Policy().RunAsync(()=>{
            var failure=new IOException("original factory failure");
            Assert.Same(failure,Assert.Throws<IOException>(()=>AdmittedStorageManagerAcquisition.TryCreate<object>(Dna,()=>null,(_,_)=>throw failure,out _)));
            Assert.Equal(PreparedStorageExecution.RejectedCode,Assert.Throws<UsageProtocolException>(()=>
                AdmittedStorageManagerAcquisition.TryCreate<object>(Dna,()=>null,(_,_)=>null,out _)).Code);
            return Task.FromResult(0);
        });
    }

    [Fact]
    public async Task ClosedInheritedAcquisitionRejectsBeforeAnyCallback()
    {
        var release=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);Task detached=null;
        await Policy().RunAsync(()=>{
            detached=Task.Run(async()=>{await release.Task;Assert.Equal(PreparedStorageExecution.RejectedCode,
                Assert.Throws<UsageProtocolException>(()=>AdmittedStorageManagerAcquisition.TryCreate<object>(
                    ()=>throw new Exception("DNA evaluated"),()=>throw new Exception("provider evaluated"),(_,_)=>throw new Exception("factory evaluated"),out _)).Code);});
            return Task.FromResult(0);
        });
        release.SetResult();await detached;
    }
}
