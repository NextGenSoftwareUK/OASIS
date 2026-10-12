using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Providers.TRONOASIS;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using NextGenSoftware.Utilities;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public class TronReconciliationRoutingTests
{
    private sealed class ControlledBackend(Exception? failure) : IKeyValueBackend
    {
        public int Writes;
        private readonly Dictionary<string,string> records=new();
        public Task VerifyAsync(CancellationToken token=default)=>Task.CompletedTask;
        public Task<string> GetAsync(string key,CancellationToken token=default)=>Task.FromResult(records.GetValueOrDefault(key)!);
        public Task PutAsync(string key,string value,CancellationToken token=default){Writes++;if(failure!=null)throw failure;records[key]=value;return Task.CompletedTask;}
        public Task DeleteAsync(string key,CancellationToken token=default){records.Remove(key);return Task.CompletedTask;}
        public Task<IReadOnlyList<string>> ListKeysAsync(string prefix,CancellationToken token=default)=>Task.FromResult<IReadOnlyList<string>>(records.Keys.Where(key=>key.StartsWith(prefix,StringComparison.Ordinal)).ToArray());
    }
    private sealed class ControlledProvider : KeyValueStorageProviderBase
    {
        public ControlledProvider(IKeyValueBackend backend,ProviderType type):base(backend){
            ProviderName="Controlled TRON reconciliation routing fixture "+type;
            ProviderType=new EnumValue<ProviderType>(type);
            ProviderCategory=new EnumValue<ProviderCategory>(API.Core.Enums.ProviderCategory.StorageAndNetwork);
        }
    }
    public static IEnumerable<object[]> Cases()=>
        from kind in Enumerable.Range(0,4) from asynchronous in new[]{false,true} from wrapping in Enumerable.Range(0,3)
        select new object[]{kind,asynchronous,wrapping};

    [Theory,MemberData(nameof(Cases))]
    public async Task ShippingTronEvidenceSurvivesStorageResultAndStopsV2Failover(int kind,bool asynchronous,int wrapping)
    {
        var cause=new IOException("controlled native interruption");var id=new string('a',64);var chain=new string('b',64);
        Exception evidence=kind switch{
            0=>new TronCommittedReadbackException(id,chain,cause),
            1=>new TronEscrowReconciliationException(id,chain,cause),
            2=>new TronBroadcastOutcomeUnknownException(id,chain,1,2,cause),
            _=>cause
        };
        evidence=wrapping switch{1=>new InvalidOperationException("controlled provider wrapper",evidence),2=>new AggregateException(new IOException("other failure"),evidence),_=>evidence};
        await VerifyRouter(evidence,ProviderType.TRONOASIS,asynchronous,kind!=3);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task ActualSuiCommittedMintReadbackEvidenceStopsV2Failover(bool asynchronous)
    {
        var root=new DirectoryInfo(AppContext.BaseDirectory);
        const string relative="Providers/Blockchain/TestProjects/NextGenSoftware.OASIS.API.Providers.SuiOASIS.UnitTests/Fixtures/unlock-readback-failure.mjs";
        while(root!=null&&!File.Exists(Path.Combine(root.FullName,relative)))root=root.Parent;
        Assert.NotNull(root);
        var previous=Environment.GetEnvironmentVariable("OASIS_SUI_SDK_BRIDGE");
        Environment.SetEnvironmentVariable("OASIS_SUI_SDK_BRIDGE",Path.Combine(root.FullName,relative));
        try{
            using var provider=new API.Providers.SuiOASIS.SuiOASIS("http://127.0.0.1:1","localnet",privateKey:"test-only-secret");
            var mint=await provider.MintNFTAsync(new API.Core.Objects.NFT.Requests.MintWeb3NFTRequest{Title="controlled router readback",NumberToMint=1});
            Assert.True(mint.IsError);var evidence=Assert.IsType<API.Providers.SuiOASIS.SuiNativeMutationReconciliationException>(mint.Exception);
            Assert.Equal(new string('1',32),evidence.OriginalTransactionId);Assert.True(evidence.ExecutionSucceeded);
            await VerifyRouter(evidence,ProviderType.SuiOASIS,asynchronous,true);
        }finally{Environment.SetEnvironmentVariable("OASIS_SUI_SDK_BRIDGE",previous);}
    }

    private static async Task VerifyRouter(Exception evidence,ProviderType type,bool asynchronous,bool reconciliationRequired)
    {
        var firstBackend=new ControlledBackend(evidence);var alternateBackend=new ControlledBackend(null);
        var first=new ControlledProvider(firstBackend,type);var alternate=new ControlledProvider(alternateBackend,ProviderType.LocalFileOASIS);
        Assert.False((await first.ActivateProviderAsync()).IsError);Assert.False((await alternate.ActivateProviderAsync()).IsError);
        try{
            var dna=new OASISDNA();dna.OASIS.HyperDriveMode=HyperDriveModes.V2;dna.OASIS.SubscriptionConfig.PlanType="Gold";
            var manager=new ProviderManager(null,dna){IsAutoFailOverEnabled=true,IsAutoReplicationEnabled=false,IsAutoLoadBalanceEnabled=false};
            manager.RegisterProvider(first);manager.RegisterProvider(alternate);
            Assert.False(manager.SetAndReplaceAutoFailOverListForProviders(new[]{new EnumValue<ProviderType>(ProviderType.LocalFileOASIS)}).IsError);
            var router=new OASISHyperDrive(manager);var holon=new Holon{Id=Guid.NewGuid(),Name="controlled reconciliation routing"};
            var request=new StorageOperationRequest{Operation="SaveHolon",Payload=holon,PreferredProvider=type};
            var result=asynchronous?await router.RouteRequestAsync<IHolon>(request):router.RouteRequest<IHolon>(request);
            Assert.Equal(reconciliationRequired,result.IsError);Assert.Equal(1,firstBackend.Writes);
            if(reconciliationRequired){Assert.Same(evidence,result.Exception);Assert.Equal(0,alternateBackend.Writes);}
            else{Assert.True(alternateBackend.Writes>0);Assert.Equal(holon.Id,result.Result.Id);}
        }finally{await first.DeActivateProviderAsync();await alternate.DeActivateProviderAsync();}
    }
}
