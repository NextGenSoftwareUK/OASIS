using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using Xunit;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Context;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests;
public partial class SQLLiteDBOASISIntegrationTests
{
    [Theory]
    [InlineData(false,false,false)] [InlineData(false,false,true)]
    [InlineData(false,true,false)] [InlineData(false,true,true)]
    [InlineData(true,false,false)] [InlineData(true,false,true)]
    [InlineData(true,true,false)] [InlineData(true,true,true)]
    public async Task DeleteHistoryFollowsActualTransactionOutcome(bool synchronous,bool soft,bool committed)
    {
        var id=Guid.NewGuid();
        using(var setup=new SQLLiteDBOASIS(TestConnectionString))
        {
            (await setup.ActivateProviderAsync()).IsError.Should().BeFalse();
            (await setup.SaveHolonAsync(new Holon{Id=id,Name="retained",Version=1,IsActive=true})).IsError.Should().BeFalse();
        }
        using var fault=new LostSaveReplyContext(TestConnectionString,committed);
        var repository=new HolonRepository(fault,()=>true);
        var result=synchronous ? repository.DeleteHolon(id,soft):await repository.DeleteHolonAsync(id,soft);
        result.IsError.Should().BeTrue();result.IsDeleted.Should().BeFalse();
        if(committed)
        {
            result.ErrorCode.Should().Be("SQLITE_WRITE_RECONCILIATION_REQUIRED");
            result.Exception.Should().BeOfType<SqliteMutationReconciliationException>().Which.RecordId.Should().Be(id);
        }
        else {result.ErrorCode.Should().Be("SQLITE_DELETE_FAILED");result.Exception.Should().BeSameAs(fault.Cause);}
        fault.ChangeTracker.Entries<NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Entities.HolonModel>().Should().BeEmpty();
        using var native=new DataContext(TestConnectionString);
        var row=native.Holons.AsNoTracking().SingleOrDefault(value=>value.Id==id.ToString("D"));
        if(committed && !soft)row.Should().BeNull();
        else {row.Should().NotBeNull();row.Version.Should().Be(committed ? 2:1);row.IsActive.Should().Be(!committed);}
        native.Database.OpenConnection();using var count=native.Database.GetDbConnection().CreateCommand();
        count.CommandText="SELECT COUNT(*) FROM CosmicHolonRevision";
        Convert.ToInt32(count.ExecuteScalar()).Should().Be(committed ? 1:0);
        var observed=new HolonRepository(native).LoadHolon(id,loadChildren:false,version:1);
        observed.IsError.Should().BeFalse();observed.Result.Name.Should().Be("retained");observed.Result.IsActive.Should().BeTrue();
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task OrdinaryDeleteDoesNotCommitOrDiscardUnrelatedWork(bool externalTransaction)
    {
        using(var setup=new SQLLiteDBOASIS(TestConnectionString))
            (await setup.ActivateProviderAsync()).IsError.Should().BeFalse();
        using var context=new DataContext(TestConnectionString);
        var repository=new HolonRepository(context,()=>true);var target=new Holon{Name="target"};
        repository.SaveHolon(target).IsError.Should().BeFalse();
        using var transaction=externalTransaction ? context.Database.BeginTransaction():null;
        var pending=new NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Entities.HolonModel{Id=Guid.NewGuid().ToString("D"),Name="pending"};
        if(!externalTransaction)context.Holons.Add(pending);
        var result=repository.DeleteHolon(target.Id,false);
        result.IsError.Should().BeTrue();result.Exception.Should().BeOfType<InvalidOperationException>();
        if(externalTransaction)context.Database.CurrentTransaction.Should().BeSameAs(transaction);
        else context.Entry(pending).State.Should().Be(EntityState.Added);
        using var native=new DataContext(TestConnectionString);
        native.Holons.AsNoTracking().Single().Name.Should().Be("target");
    }

    [Theory]
    [InlineData(false,false,false)] [InlineData(false,false,true)]
    [InlineData(false,true,false)] [InlineData(false,true,true)]
    [InlineData(true,false,false)] [InlineData(true,false,true)]
    [InlineData(true,true,false)] [InlineData(true,true,true)]
    public async Task OrdinaryDeleteRetainsPreviousRevisionAtomically(bool synchronous,bool byKey,bool soft)
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString){IsVersionControlEnabled=true};
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var source=new Holon{Name="before-delete",Version=1,IsActive=true};
        const string key="delete-history-key";source.ProviderUniqueStorageKey[ProviderType.SQLLiteDBOASIS]=key;
        (await provider.SaveHolonAsync(source)).IsError.Should().BeFalse();
        var before=(await provider.LoadHolonAsync(source.Id,loadChildren:false)).Result;
        using var context=new DataContext(TestConnectionString);
        var repository=new HolonRepository(context,()=>true);
        var deleted=soft
            ? byKey ? synchronous ? repository.DeleteHolon(key,true):await repository.DeleteHolonAsync(key,true)
                : synchronous ? repository.DeleteHolon(source.Id,true):await repository.DeleteHolonAsync(source.Id,true)
            : byKey ? synchronous ? provider.DeleteHolon(key):await provider.DeleteHolonAsync(key)
                : synchronous ? provider.DeleteHolon(source.Id):await provider.DeleteHolonAsync(source.Id);
        deleted.IsError.Should().BeFalse(deleted.Message+deleted.Exception);deleted.IsDeleted.Should().BeTrue();
        var historical=await provider.LoadHolonAsync(source.Id,loadChildren:false,version:1);
        historical.IsError.Should().BeFalse(historical.Message+historical.Exception);
        historical.Result.Should().NotBeNull();historical.Result.Name.Should().Be("before-delete");
        historical.Result.IsActive.Should().BeTrue();historical.Result.DeletedDate.Should().Be(DateTime.MinValue);
        historical.Result.VersionId.Should().Be(before.VersionId);
        var byOldKey=await provider.LoadHolonAsync(key,loadChildren:false,version:1);
        byOldKey.IsError.Should().BeFalse();byOldKey.Result.Id.Should().Be(source.Id);
        if(soft)
        {
            var tombstone=(await provider.LoadHolonAsync(source.Id,loadChildren:false)).Result;
            tombstone.Version.Should().Be(2);tombstone.IsActive.Should().BeFalse();
        }
        else (await provider.LoadHolonAsync(source.Id,loadChildren:false)).Result.Should().BeNull();
    }
}
