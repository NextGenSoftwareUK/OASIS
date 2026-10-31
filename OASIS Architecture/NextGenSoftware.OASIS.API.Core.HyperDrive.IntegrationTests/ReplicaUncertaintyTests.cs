using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive;
using NextGenSoftware.OASIS.API.Providers.LocalFileOASIS;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public sealed partial class RealProviderRoutingTests
{
    private sealed class OwnedReplicaUncertain : IOException,INativeMutationReconciliationRequired
    { public string OriginalTransactionId=>""; }

    private sealed class CommittedUnknownLocalFile(string path) : LocalFileOASIS(storageDirectory:path)
    {
        public int AcknowledgedWrites;public readonly Exception Cause=new OwnedReplicaUncertain();
        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon,bool saveChildren=true,bool recursive=true,int maxChildDepth=0,bool continueOnError=true,bool saveChildrenOnProvider=false)
        {
            var committed=await base.SaveHolonAsync(holon,saveChildren,recursive,maxChildDepth,continueOnError,saveChildrenOnProvider);
            if(committed.IsError) return committed;
            AcknowledgedWrites++;
            return new OASISResult<IHolon>{IsError=true,Exception=Cause,Message="Controlled lost acknowledgement after actual owned file write."};
        }
    }

    private sealed class UnsubmittedMongo() : MongoDBOASIS("mongodb://127.0.0.1:1","owned_unsubmitted_replica")
    {
        public int Activations;
        public override OASISResult<bool> ActivateProvider()
        {Activations++;return new OASISResult<bool>{IsError=true,Message="Owned guard forbids third-provider activation."};}
        public override Task<OASISResult<bool>> ActivateProviderAsync()=>Task.FromResult(ActivateProvider());
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task AutomaticReplicationStopsAfterActualFileCommitWithControlledUnknownAcknowledgement(bool synchronous)
    {
        Directory.CreateDirectory(_root);using var sqlite=new SQLLiteDBOASIS($"Data Source={SqlitePath};Pooling=False");
        var local=new CommittedUnknownLocalFile(FilesPath);var blocked=new UnsubmittedMongo();
        var manager=CreateManager(autoReplication:true);manager.RegisterProvider(sqlite);manager.RegisterProvider(local);manager.RegisterProvider(blocked);
        manager.SetAndReplaceAutoReplicationListForProviders(new[]{new EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS),new EnumValue<ProviderType>(ProviderType.LocalFileOASIS),new EnumValue<ProviderType>(ProviderType.MongoDBOASIS)}).IsError.Should().BeFalse();
        try
        {
            var holon=new Holon{Id=Guid.NewGuid(),Name="owned committed primary and replica",IsActive=true};
            var router=new OASISHyperDrive(manager);var request=new StorageOperationRequest{Operation="SaveHolon",Payload=holon,SaveChildren=false,PreferredProvider=ProviderType.SQLLiteDBOASIS};
            var result=synchronous ? router.RouteRequest<IHolon>(request):await router.RouteRequestAsync<IHolon>(request);
            result.IsError.Should().BeFalse(result.Message);result.IsSaved.Should().BeTrue();result.IsWarning.Should().BeTrue();result.Exception.Should().BeSameAs(local.Cause);
            local.AcknowledgedWrites.Should().Be(1);blocked.Activations.Should().Be(0);
            router.LastReplicationDiagnostic.Attempts.Should().ContainSingle().Which.ReconciliationRequired.Should().BeTrue();
            (await sqlite.LoadHolonAsync(holon.Id,loadChildren:false)).Result.Name.Should().Be(holon.Name);
            (await local.LoadHolonAsync(holon.Id,loadChildren:false)).Result.Name.Should().Be(holon.Name);
            Core.Helpers.NativeMutationReconciliation.OriginalTransactionIds(result.Exception).Should().BeEmpty("file paths/entity IDs are not transaction identities");
        }
        finally {await local.DeActivateProviderAsync();}
    }
}
