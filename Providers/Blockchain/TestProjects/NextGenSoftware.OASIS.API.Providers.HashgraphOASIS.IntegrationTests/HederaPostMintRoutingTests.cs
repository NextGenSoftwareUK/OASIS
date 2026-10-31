using System.Reflection;
using NextGenSoftware.OASIS.API.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.HashgraphOASIS.IntegrationTests;

[TestClass]
public class HederaPostMintRoutingTests
{
    private sealed class ControlledBackend(Exception? failure):IKeyValueBackend
    {
        public int Writes;
        public Task VerifyAsync(CancellationToken token=default)=>Task.CompletedTask;
        public Task<string> GetAsync(string key,CancellationToken token=default)=>Task.FromResult<string>(null!);
        public Task PutAsync(string key,string value,CancellationToken token=default){Writes++;if(failure!=null)throw failure;return Task.CompletedTask;}
        public Task DeleteAsync(string key,CancellationToken token=default)=>Task.CompletedTask;
        public Task<IReadOnlyList<string>> ListKeysAsync(string prefix,CancellationToken token=default)=>Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());
    }
    private sealed class ControlledProvider:KeyValueStorageProviderBase
    {
        public ControlledProvider(IKeyValueBackend backend,ProviderType type):base(backend){
            ProviderName="Controlled Hedera routing evidence fixture "+type;
            ProviderType=new EnumValue<ProviderType>(type);
            ProviderCategory=new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
        }
    }
    [DataTestMethod]
    [DataRow(false,false)] [DataRow(false,true)] [DataRow(true,false)] [DataRow(true,true)]
    public async Task ActualPostMintEvidenceStopsV2AlternateWrites(bool asynchronous,bool deliveryRequested)
    {
        const string mint="0.0.1001@1700000000.000000001";const string delivery="0.0.1001@1700000001.000000001";
        var method=typeof(HashgraphOASIS).GetMethod("RetainHederaPostMintEvidence",BindingFlags.Static|BindingFlags.NonPublic)!;
        var evidence=(Exception)method.Invoke(null,new object?[]{new IOException("controlled delivery failure"),mint,deliveryRequested?delivery:null})!;
        var primaryBackend=new ControlledBackend(evidence);var alternateBackend=new ControlledBackend(null);
        var primary=new ControlledProvider(primaryBackend,Core.Enums.ProviderType.HashgraphOASIS);var alternate=new ControlledProvider(alternateBackend,Core.Enums.ProviderType.LocalFileOASIS);
        Assert.IsFalse((await primary.ActivateProviderAsync()).IsError);Assert.IsFalse((await alternate.ActivateProviderAsync()).IsError);
        try{
            var dna=new OASISDNA();dna.OASIS.HyperDriveMode=HyperDriveModes.V2;dna.OASIS.SubscriptionConfig.PlanType="Gold";
            var manager=new ProviderManager(null,dna){IsAutoFailOverEnabled=true,IsAutoLoadBalanceEnabled=false,IsAutoReplicationEnabled=false};
            manager.RegisterProvider(primary);manager.RegisterProvider(alternate);
            Assert.IsFalse(manager.SetAndReplaceAutoFailOverListForProviders(new[]{new EnumValue<ProviderType>(Core.Enums.ProviderType.LocalFileOASIS)}).IsError);
            var router=new OASISHyperDrive(manager);
            var request=new StorageOperationRequest{Operation="SaveHolon",Payload=new Holon{Id=Guid.NewGuid()},PreferredProvider=Core.Enums.ProviderType.HashgraphOASIS};
            var result=asynchronous?await router.RouteRequestAsync<IHolon>(request):router.RouteRequest<IHolon>(request);
            Assert.IsTrue(result.IsError);Assert.AreSame(evidence,result.Exception);Assert.AreEqual(1,primaryBackend.Writes);Assert.AreEqual(0,alternateBackend.Writes);
            Assert.IsInstanceOfType<HederaNftPostMintReconciliationException>(result.Exception);
            var retained=(HederaNftPostMintReconciliationException)result.Exception!;
            Assert.AreEqual(mint,retained.ConfirmedMintTransactionId);Assert.AreEqual(deliveryRequested?delivery:null,retained.RequestedDeliveryTransactionId);
        }finally{await primary.DeActivateProviderAsync();await alternate.DeActivateProviderAsync();}
    }
}
