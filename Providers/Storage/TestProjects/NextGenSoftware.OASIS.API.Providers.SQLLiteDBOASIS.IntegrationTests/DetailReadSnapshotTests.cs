using System.Data.Common;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Avatar;
using NextGenSoftware.OASIS.API.Core.Objects.Avatar;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Context;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Repositories;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests;

public partial class SQLLiteDBOASISIntegrationTests
{
    private sealed class BetweenDetailQueries(Action write):DbCommandInterceptor
    {
        public int Writes {get;private set;}
        private void Before(DbCommand command)
        {
            if(Writes==0 && command.CommandText.Contains("HeartRateEntry",StringComparison.Ordinal))
            {
                write();Writes++;
            }
        }
        public override InterceptionResult<DbDataReader> ReaderExecuting(DbCommand command,CommandEventData data,InterceptionResult<DbDataReader> result)
        {Before(command);return result;}
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,CommandEventData data,InterceptionResult<DbDataReader> result,CancellationToken cancellationToken=default)
        {Before(command);return ValueTask.FromResult(result);}
    }

    private sealed class DetailReadContext(string connection,DbCommandInterceptor interceptor):DataContext(connection)
    {
        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {base.OnConfiguring(options);options.AddInterceptors(interceptor);}
    }

    public static IEnumerable<object[]> DetailSnapshotCases()
    {
        foreach(var synchronous in new[] {true,false})
        foreach(var selector in new[] {"id","username","email","all"}) yield return new object[] {synchronous,selector};
    }

    [Theory]
    [MemberData(nameof(DetailSnapshotCases))]
    public async Task SplitDetailReadCannotCombineTwoCommittedVersions(bool synchronous,string selector)
    {
        var id=Guid.NewGuid();
        using var setup=new SQLLiteDBOASIS(TestConnectionString);
        (await setup.ActivateProviderAsync()).IsError.Should().BeFalse();
        var owner=new AvatarDetail {Id=id,Username="snapshot-owner",Email="snapshot@test.invalid",Karma=100,
            HeartRateData=new List<IHeartRateEntry> {new HeartRateEntry {HeartRateValue=70,TimeStamp=new DateTime(2026,10,16,1,0,0,DateTimeKind.Utc)}}};
        owner.Inventory.Add(new InventoryItem {Id=Guid.NewGuid(),Name="snapshot inventory",Quantity=31});
        (await setup.SaveAvatarDetailAsync(owner)).IsError.Should().BeFalse();
        var other=new AvatarDetail {Id=Guid.NewGuid(),Username="unrelated-snapshot-owner",Karma=17,
            HeartRateData=new List<IHeartRateEntry> {new HeartRateEntry {HeartRateValue=83,TimeStamp=new DateTime(2026,10,16,2,0,0,DateTimeKind.Utc)}}};
        (await setup.SaveAvatarDetailAsync(other)).IsError.Should().BeFalse();
        using var writer=new DataContext(TestConnectionString+";Default Timeout=3");
        writer.Database.OpenConnection();
        using(var mode=writer.Database.GetDbConnection().CreateCommand())
        {mode.CommandText="PRAGMA journal_mode=WAL";mode.ExecuteScalar()!.ToString().Should().Be("wal");}
        var hook=new BetweenDetailQueries(()=>
        {
            using var transaction=writer.Database.BeginTransaction();
            writer.Database.ExecuteSqlRaw("UPDATE AvatarDetail SET Karma=200 WHERE Id={0}",id.ToString());
            writer.Database.ExecuteSqlRaw("UPDATE HeartRateEntry SET HeartRateValue=99 WHERE AvatarId={0}",id.ToString());
            writer.Database.ExecuteSqlRaw("UPDATE AvatarInventory SET Quantity=99 WHERE AvatarId={0}",id.ToString());
            transaction.Commit();
        });
        using var context=new DetailReadContext(TestConnectionString+";Default Timeout=3",hook);
        var repository=new AvatarDetailRepository(context);IAvatarDetail actual;
        if(selector=="all")
        {
            var result=synchronous ? repository.LoadAllAvatarDetails():await repository.LoadAllAvatarDetailsAsync();
            result.IsError.Should().BeFalse(result.Message);actual=result.Result.Single(row=>row.Id==id);
        }
        else
        {
            var result=selector switch {
                "username"=>synchronous ? repository.LoadAvatarDetailByUsername(owner.Username):await repository.LoadAvatarDetailByUsernameAsync(owner.Username),
                "email"=>synchronous ? repository.LoadAvatarDetailByEmail(owner.Email):await repository.LoadAvatarDetailByEmailAsync(owner.Email),
                _=>synchronous ? repository.LoadAvatarDetail(id):await repository.LoadAvatarDetailAsync(id)};
            result.IsError.Should().BeFalse(result.Message);actual=result.Result;
        }
        hook.Writes.Should().Be(1);
        actual.Karma.Should().Be(100);actual.HeartRateData.Single().HeartRateValue.Should().Be(70,"root and collections must share one read snapshot");
        actual.Inventory.Single().Quantity.Should().Be(31);
        var latest=(await setup.LoadAvatarDetailAsync(id)).Result;latest.Karma.Should().Be(200);latest.HeartRateData.Single().HeartRateValue.Should().Be(99);
        latest.Inventory.Single().Quantity.Should().Be(99);
        var unrelated=(await setup.LoadAvatarDetailAsync(other.Id)).Result;
        unrelated.Karma.Should().Be(17);unrelated.HeartRateData.Single().HeartRateValue.Should().Be(83);
    }
}
