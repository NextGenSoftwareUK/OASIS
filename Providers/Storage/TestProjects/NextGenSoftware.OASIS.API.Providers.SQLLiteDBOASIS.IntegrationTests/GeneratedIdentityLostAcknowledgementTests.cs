using Microsoft.EntityFrameworkCore;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Context;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Repositories;
using NextGenSoftware.OASIS.Common;
using Xunit;
using FluentAssertions;
namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests;
public partial class SQLLiteDBOASISIntegrationTests
{
    private sealed class LostSaveReplyContext:DataContext
    {
        public readonly IOException Cause=new("Owned acknowledgement loss after actual SQLite commit.");
        public int Commits;
        public LostSaveReplyContext(string connection):base(connection) {}
        public override int SaveChanges() {base.SaveChanges();Commits++;throw Cause;}
        public override async Task<int> SaveChangesAsync(CancellationToken token=default)
            {await base.SaveChangesAsync(token);Commits++;throw Cause;}
    }

    [Theory]
    [InlineData(false,0)] [InlineData(true,0)] [InlineData(false,1)] [InlineData(true,1)]
    [InlineData(false,2)] [InlineData(true,2)]
    public async Task CommittedGeneratedIdentityRemainsUnpublishedAndReconciliable(bool synchronous,int kind)
    {
        using(var setup=new SQLLiteDBOASIS(TestConnectionString)) (await setup.ActivateProviderAsync()).IsError.Should().BeFalse();
        using var fault=new LostSaveReplyContext(TestConnectionString);
        SqliteMutationReconciliationException marker;
        if(kind==0)
        {
            var source=new Avatar {Username="owned-lost-profile",IsActive=true,
                AvatarType=new NextGenSoftware.Utilities.EnumValue<Core.Enums.AvatarType>(Core.Enums.AvatarType.User)};
            var repository=new AvatarRepository(fault);
            var result=synchronous ? repository.SaveAvatar(source):await repository.SaveAvatarAsync(source);
            marker=Pending(result,fault);source.Id.Should().BeEmpty();
        }
        else if(kind==1)
        {
            var source=new AvatarDetail {Username="owned-lost-detail",XP=777,IsActive=true};
            var repository=new AvatarDetailRepository(fault);
            var result=synchronous ? repository.SaveAvatarDetail(source):await repository.SaveAvatarDetailAsync(source);
            marker=Pending(result,fault);source.Id.Should().BeEmpty();
        }
        else
        {
            var source=new Holon {Name="owned-lost-root"};var child=new Holon {Name="owned-lost-child"};source.Children=new List<IHolon> {child};
            var repository=new HolonRepository(fault);
            var result=synchronous ? repository.SaveHolon(source):await repository.SaveHolonAsync(source);
            marker=Pending(result,fault);source.Id.Should().BeEmpty();child.Id.Should().BeEmpty();child.ParentHolonId.Should().BeEmpty();
        }
        using var native=new DataContext(TestConnectionString);
        var ids=kind==0 ? native.Avatars.AsNoTracking().Select(row=>row.Id).ToList():kind==1
            ? native.AvatarDetails.AsNoTracking().Select(row=>row.Id).ToList():native.Holons.AsNoTracking().Select(row=>row.Id).ToList();
        ids.Should().HaveCount(kind==2 ? 2:1);ids.Select(Guid.Parse).Should().BeEquivalentTo(marker.RecordIds);
        marker.RecordIds.Should().NotContain(Guid.Empty);
        if(kind==1) native.AvatarDetails.AsNoTracking().Single().XP.Should().Be(777);
        if(kind==2) native.Holons.AsNoTracking().Single(row=>row.Name=="owned-lost-child").ParentHolonId.Should()
            .Be(native.Holons.AsNoTracking().Single(row=>row.Name=="owned-lost-root").Id);
    }

    private static SqliteMutationReconciliationException Pending<T>(OASISResult<T> result,LostSaveReplyContext fault)
    {
        result.IsError.Should().BeTrue();result.IsSaved.Should().BeFalse();result.Result.Should().BeNull();fault.Commits.Should().Be(1);
        var marker=result.Exception.Should().BeOfType<SqliteMutationReconciliationException>().Which;
        marker.InnerException.Should().BeSameAs(fault.Cause);marker.OriginalTransactionId.Should().BeEmpty();
        return marker;
    }
}
