using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive;
using NextGenSoftware.OASIS.API.Providers.LocalFileOASIS;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Context;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Repositories;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using Xunit;
namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;
public sealed partial class RealProviderRoutingTests
{
    private sealed class CommittedReplyLossContext:DataContext
    {
        public readonly IOException Cause=new("Owned V2 post-real-SQLite-commit acknowledgement loss.");
        public int Commits;
        public CommittedReplyLossContext(string connection):base(connection) {}
        public override int SaveChanges() {base.SaveChanges();Commits++;throw Cause;}
        public override async Task<int> SaveChangesAsync(CancellationToken token=default) {await base.SaveChangesAsync(token);Commits++;throw Cause;}
    }
    private sealed class NativeReplyLossProvider:SQLLiteDBOASIS
    {
        private readonly CommittedReplyLossContext _context;
        public NativeReplyLossProvider(string connection,CommittedReplyLossContext context):base(connection) {_context=context;}
        public override OASISResult<IAvatar> SaveAvatar(IAvatar avatar)=>new AvatarRepository(_context).SaveAvatar(avatar);
        public override Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar)=>new AvatarRepository(_context).SaveAvatarAsync(avatar);
        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail detail)=>new AvatarDetailRepository(_context).SaveAvatarDetail(detail);
        public override Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail detail)=>new AvatarDetailRepository(_context).SaveAvatarDetailAsync(detail);
        public override OASISResult<IHolon> SaveHolon(IHolon holon,bool saveChildren=true,bool recursive=true,int maxChildDepth=0,bool continueOnError=true,bool saveChildrenOnProvider=false)
            =>new HolonRepository(_context).SaveHolon(holon,saveChildren,recursive,maxChildDepth,continueOnError);
        public override Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon,bool saveChildren=true,bool recursive=true,int maxChildDepth=0,bool continueOnError=true,bool saveChildrenOnProvider=false)
            =>new HolonRepository(_context).SaveHolonAsync(holon,saveChildren,recursive,maxChildDepth,continueOnError);
    }

    [Theory]
    [InlineData(false,0)] [InlineData(true,0)] [InlineData(false,1)] [InlineData(true,1)] [InlineData(false,2)] [InlineData(true,2)]
    public async Task GeneratedCommitUncertaintyNeverReplaysOrReplicates(bool synchronous,int kind)
    {
        Directory.CreateDirectory(_root);var connection=$"Data Source={SqlitePath};Pooling=False";
        using var context=new CommittedReplyLossContext(connection);using var sqlite=new NativeReplyLossProvider(connection,context);
        var local=new LocalFileOASIS(storageDirectory:FilesPath);
        (await sqlite.ActivateProviderAsync()).IsError.Should().BeFalse();(await local.ActivateProviderAsync()).IsError.Should().BeFalse();
        try
        {
            var manager=CreateManager(autoFailover:true,autoReplication:true);manager.RegisterProvider(sqlite);manager.RegisterProvider(local);
            var types=new[] {new EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS),new EnumValue<ProviderType>(ProviderType.LocalFileOASIS)};
            manager.SetAndReplaceAutoFailOverListForProviders(types).IsError.Should().BeFalse();
            manager.SetAndReplaceAutoReplicationListForProviders(types).IsError.Should().BeFalse();
            var router=new OASISHyperDrive(manager);var before=manager.PerformanceMonitor.GetMetrics(ProviderType.LocalFileOASIS)?.TotalRequests ?? 0;
            var profile=new Avatar {Username="owned-uncertain",AvatarType=new EnumValue<AvatarType>(AvatarType.User)};
            var detail=new AvatarDetail {Username="owned-uncertain",XP=777};
            var root=new Holon {Name="owned-uncertain-root"};var child=new Holon {Name="owned-uncertain-child"};root.Children=new List<IHolon> {child};
            var request=new StorageOperationRequest {Operation=kind==0 ? "SaveAvatar":kind==1 ? "SaveAvatarDetail":"SaveHolon",
                Payload=kind==0 ? profile:kind==1 ? detail:root,PreferredProvider=ProviderType.SQLLiteDBOASIS};
            _preserveFixture=true;
            SqliteMutationReconciliationException Pending<T>(OASISResult<T> result)
            {
                result.IsError.Should().BeTrue();result.IsSaved.Should().BeFalse();result.Result.Should().BeNull();
                return result.Exception.Should().BeOfType<SqliteMutationReconciliationException>().Which;
            }
            var marker=kind==0 ? Pending(synchronous ? router.RouteRequest<IAvatar>(request):await router.RouteRequestAsync<IAvatar>(request))
                :kind==1 ? Pending(synchronous ? router.RouteRequest<IAvatarDetail>(request):await router.RouteRequestAsync<IAvatarDetail>(request))
                :Pending(synchronous ? router.RouteRequest<IHolon>(request):await router.RouteRequestAsync<IHolon>(request));
            marker.InnerException.Should().BeSameAs(context.Cause);context.Commits.Should().Be(1);
            (manager.PerformanceMonitor.GetMetrics(ProviderType.LocalFileOASIS)?.TotalRequests ?? 0).Should().Be(before);
            profile.Id.Should().BeEmpty();detail.Id.Should().BeEmpty();root.Id.Should().BeEmpty();child.Id.Should().BeEmpty();child.ParentHolonId.Should().BeEmpty();
            using var native=new DataContext(connection);
            var ids=kind==0 ? native.Avatars.AsNoTracking().Select(row=>row.Id).ToList():kind==1 ? native.AvatarDetails.AsNoTracking().Select(row=>row.Id).ToList():native.Holons.AsNoTracking().Select(row=>row.Id).ToList();
            ids.Should().HaveCount(kind==2 ? 2:1);ids.Select(Guid.Parse).Should().BeEquivalentTo(marker.RecordIds);marker.RecordIds.Should().NotContain(Guid.Empty);
            if(kind==0) (await local.LoadAllAvatarsAsync()).Result.Should().BeEmpty();
            else if(kind==1) (await local.LoadAllAvatarDetailsAsync()).Result.Should().BeEmpty();
            else (await local.LoadAllHolonsAsync(loadChildren:false)).Result.Should().BeEmpty();
            _preserveFixture=false;
        }
        finally {await local.DeActivateProviderAsync();}
    }
}
