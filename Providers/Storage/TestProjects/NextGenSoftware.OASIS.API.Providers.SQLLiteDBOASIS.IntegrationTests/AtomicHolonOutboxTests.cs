using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Context;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests;

public partial class SQLLiteDBOASISIntegrationTests
{
    [Theory]
    [InlineData(false,false)] [InlineData(true,false)] [InlineData(false,true)] [InlineData(true,true)]
    public async Task ManagerSavePublishesOnlyAfterAtomicOutboxEnrollment(bool synchronous,bool failOutbox)
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);(await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        if(failOutbox)
        {
            using var control=new DataContext(TestConnectionString);
            control.Database.ExecuteSqlRaw("CREATE TRIGGER owned_manager_outbox_fault BEFORE INSERT ON CosmicHolonOutbox BEGIN SELECT RAISE(ABORT,'owned manager outbox fault'); END");
        }
        var dna=new OASISDNA{OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS{HyperDriveMode=HyperDriveModes.V2,
            StorageProviders=new StorageProviderSettings{LogSwitchingProviders=false}}};
        var providers=new ProviderManager(null,dna){IsAutoReplicationEnabled=true,IsAutoFailOverEnabled=true,IsAutoLoadBalanceEnabled=false};
        providers.RegisterProvider(provider);
        var manager=new HolonManager(null,dna,providers);var input=new Holon{Name="manager committed"};
        var enrollment=new HolonReplicationEnrollment("source",new[]{"target-a","target-b"});
        var operation=Guid.NewGuid();var options=new CosmicWriteOptions(operation,expectedRevisions:null,replicationEnrollment:enrollment);
        var result=synchronous ? manager.SaveHolon(input,Guid.NewGuid(),options,saveChildren:false,providerType:ProviderType.SQLLiteDBOASIS)
            :await manager.SaveHolonAsync(input,Guid.NewGuid(),options,saveChildren:false,providerType:ProviderType.SQLLiteDBOASIS);
        if(failOutbox)
        {
            result.IsError.Should().BeTrue();input.Id.Should().BeEmpty();input.Version.Should().Be(0);input.HasHolonChanged().Should().BeTrue();
            using var control=new DataContext(TestConnectionString);control.Holons.AsNoTracking().Should().BeEmpty();
            using var inspect=new SqliteConnection(TestConnectionString);inspect.Open();using var count=inspect.CreateCommand();
            foreach(var table in new[]{"CosmicHolonOutbox","CosmicWriteReceipt"})
            {count.CommandText="SELECT COUNT(*) FROM "+table;Convert.ToInt64(count.ExecuteScalar()).Should().Be(0);}
            return;
        }
        result.IsError.Should().BeFalse(result.Message+result.Exception);input.Id.Should().NotBeEmpty();input.Version.Should().Be(1);
        input.HasHolonChanged().Should().BeFalse();
        using var connection=new SqliteConnection(TestConnectionString);connection.Open();using var command=connection.CreateCommand();
        command.CommandText="SELECT PayloadJson FROM CosmicHolonOutbox ORDER BY Sequence LIMIT 1";
        var frozen=CommittedHolonReplicaPayload.Read((string)command.ExecuteScalar()!);
        frozen.OperationId.Should().Be(operation);frozen.Roots[0].VersionId.Should().Be(input.VersionId);frozen.Roots[0].Id.Should().Be(input.Id);
        var unchanged=await manager.SaveHolonAsync(input,Guid.NewGuid(),new CosmicWriteOptions(Guid.NewGuid(),expectedRevisions:null,replicationEnrollment:enrollment),
            saveChildren:false,providerType:ProviderType.SQLLiteDBOASIS);
        unchanged.IsError.Should().BeFalse();unchanged.IsSaved.Should().BeFalse();
        command.CommandText="SELECT COUNT(*) FROM CosmicHolonOutbox";Convert.ToInt64(command.ExecuteScalar()).Should().Be(2);
    }

    [Fact]
    public async Task PreCancelledAtomicEnrollmentDoesNotCreateAnyDurableState()
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);(await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        using var cancellation=new CancellationTokenSource();cancellation.Cancel();
        Func<Task> save=()=>provider.ExecuteReplicatingHolonWriteAsync(OutboxCreate(new Holon{Id=Guid.NewGuid()}),
            new HolonReplicationEnrollment("source",new[]{"target"}),cancellation.Token);
        await save.Should().ThrowAsync<OperationCanceledException>();
        using var control=new DataContext(TestConnectionString);control.Holons.AsNoTracking().Should().BeEmpty();
        using var connection=new SqliteConnection(TestConnectionString);connection.Open();using var count=connection.CreateCommand();
        count.CommandText="SELECT COUNT(*) FROM CosmicHolonOutbox";Convert.ToInt64(count.ExecuteScalar()).Should().Be(0);
    }

    private static ConditionalHolonWriteRequest OutboxCreate(Holon root,Guid? operation=null)
    {
        var nodes=new[]{(IHolon)root}.Concat(root.Children).ToArray();
        return new(new ConditionalHolonWrite(operation ?? Guid.NewGuid(),Guid.NewGuid(),ProviderType.SQLLiteDBOASIS,
            nodes.Select(row=>new HolonWriteCondition(row.Id,null))),ConditionalHolonOperation.Save,new[]{root});
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task AtomicOutboxCommitsFrozenGraphForEveryTargetAndReplaysWithoutDuplicates(bool synchronous)
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var child=new Holon{Id=Guid.NewGuid(),Name="child"};
        var input=new Holon{Id=Guid.NewGuid(),Name="original",Children=new List<IHolon>{child}};
        var request=OutboxCreate(input);var targets=new List<string>{"target-b","target-a"};
        var enrollment=new HolonReplicationEnrollment("owned-source",targets);targets.Clear();
        var result=synchronous ? provider.ExecuteReplicatingHolonWrite(request,enrollment)
            :await provider.ExecuteReplicatingHolonWriteAsync(request,enrollment);
        result.IsError.Should().BeFalse(result.Message+result.Exception);
        var committed=result.Result.Single();
        using var connection=new SqliteConnection(TestConnectionString);connection.Open();
        using var query=connection.CreateCommand();query.CommandText="SELECT TargetId,PayloadJson,PayloadDigest FROM CosmicHolonOutbox ORDER BY Sequence";
        var payloads=new List<string>();
        using(var reader=query.ExecuteReader())
        {
            foreach(var target in new[]{"target-a","target-b"})
            {
                reader.Read().Should().BeTrue();reader.GetString(0).Should().Be(target);
                var payload=reader.GetString(1);payloads.Add(payload);
                reader.GetString(2).Should().Be(Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload))));
                var delivery=CommittedHolonReplicaPayload.Read(payload);
                delivery.OperationId.Should().Be(request.Write.OperationId);delivery.SourceId.Should().Be("owned-source");
                delivery.Roots.Single().VersionId.Should().Be(committed.VersionId);
                delivery.Roots.Single().Children.Single().VersionId.Should().Be(committed.Children.Single().VersionId);
            }
            reader.Read().Should().BeFalse();
        }
        payloads.Distinct().Should().HaveCount(1);
        var replay=await provider.ExecuteReplicatingHolonWriteAsync(request,new HolonReplicationEnrollment("owned-source",new[]{"target-a","target-b"}));
        replay.IsError.Should().BeFalse(replay.Message+replay.Exception);replay.Result.Single().VersionId.Should().Be(committed.VersionId);
        query.CommandText="SELECT COUNT(*) FROM CosmicHolonOutbox";Convert.ToInt64(query.ExecuteScalar()).Should().Be(2);
        input.Name="later";input.Children.Clear();
        CommittedHolonReplicaPayload.Read(payloads[0]).Roots[0].Name.Should().Be("original");
    }

    [Theory]
    [InlineData("outbox")] [InlineData("receipt")]
    public async Task AtomicOutboxFailureRollsBackGraphAllTargetsAndReceipt(string fault)
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);(await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        using var control=new DataContext(TestConnectionString);
        control.Database.ExecuteSqlRaw(fault=="outbox"
            ?"CREATE TRIGGER owned_outbox_fault BEFORE INSERT ON CosmicHolonOutbox WHEN NEW.TargetId='target-b' BEGIN SELECT RAISE(ABORT,'owned outbox fault'); END"
            :"CREATE TRIGGER owned_receipt_fault BEFORE INSERT ON CosmicWriteReceipt BEGIN SELECT RAISE(ABORT,'owned receipt fault'); END");
        var root=new Holon{Id=Guid.NewGuid(),Name="not committed"};var request=OutboxCreate(root);
        var result=await provider.ExecuteReplicatingHolonWriteAsync(request,new HolonReplicationEnrollment("owned-source",new[]{"target-a","target-b"}));
        result.IsError.Should().BeTrue();root.VersionId.Should().BeEmpty();
        control.Holons.AsNoTracking().Should().BeEmpty();
        using var connection=new SqliteConnection(TestConnectionString);connection.Open();using var command=connection.CreateCommand();
        foreach(var table in new[]{"CosmicWriteReceipt","CosmicHolonOutbox"})
        {command.CommandText="SELECT COUNT(*) FROM "+table;Convert.ToInt64(command.ExecuteScalar()).Should().Be(0);}
    }

    [Theory]
    [InlineData("source")] [InlineData("target")] [InlineData("plain")]
    public async Task OperationReplayCannotChangeReplicationObligations(string changed)
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);(await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var request=OutboxCreate(new Holon{Id=Guid.NewGuid(),Name="bound"});
        (await provider.ExecuteReplicatingHolonWriteAsync(request,new HolonReplicationEnrollment("source",new[]{"target-a"}))).IsError.Should().BeFalse();
        var result=changed=="plain" ? await provider.ExecuteConditionalHolonWriteAsync(request)
            :await provider.ExecuteReplicatingHolonWriteAsync(request,new HolonReplicationEnrollment(changed=="source" ? "other":"source",
                new[]{changed=="target" ? "target-b":"target-a"}));
        result.IsError.Should().BeTrue();result.ErrorCode.Should().Be("COSMIC_OPERATION_ID_REUSED");
        using var connection=new SqliteConnection(TestConnectionString);connection.Open();using var command=connection.CreateCommand();
        command.CommandText="SELECT COUNT(*) FROM CosmicHolonOutbox";Convert.ToInt64(command.ExecuteScalar()).Should().Be(1);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task AtomicOutboxRetainsOrderedCommittedUpdatesAndDeletionTombstones(bool synchronous)
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);(await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var enrollment=new HolonReplicationEnrollment("source",new[]{"target"});
        var create=OutboxCreate(new Holon{Id=Guid.NewGuid(),Name="first"});
        var first=await provider.ExecuteReplicatingHolonWriteAsync(create,enrollment);first.IsError.Should().BeFalse();
        var current=first.Result.Single();
        foreach(var operation in new[]{ConditionalHolonOperation.Save,ConditionalHolonOperation.SoftDelete,ConditionalHolonOperation.HardDelete})
        {
            var request=new ConditionalHolonWriteRequest(new ConditionalHolonWrite(Guid.NewGuid(),create.Write.ActorId,ProviderType.SQLLiteDBOASIS,
                new[]{new HolonWriteCondition(current.Id,CosmicConcurrency.GetProviderRevisionId(current,ProviderType.SQLLiteDBOASIS))}),
                operation,new[]{current},saveChildren:false);
            var result=synchronous ? provider.ExecuteReplicatingHolonWrite(request,enrollment):await provider.ExecuteReplicatingHolonWriteAsync(request,enrollment);
            result.IsError.Should().BeFalse(result.Message+result.Exception);current=result.Result.Single();
        }
        using var connection=new SqliteConnection(TestConnectionString);connection.Open();using var command=connection.CreateCommand();
        command.CommandText="SELECT PayloadJson FROM CosmicHolonOutbox ORDER BY Sequence";
        var deliveries=new List<CommittedHolonReplicaRequest>();
        using(var reader=command.ExecuteReader())while(reader.Read())deliveries.Add(CommittedHolonReplicaPayload.Read(reader.GetString(0)));
        deliveries.Should().HaveCount(4);
        for(var index=0;index<4;index++)
        {
            var record=deliveries[index].Roots.Single();record.Version.Should().Be(index+1);
            if(index>0)record.PreviousVersionId.Should().Be(deliveries[index-1].Roots[0].VersionId);
            record.IsDeleted.Should().Be(index>=2);
        }
        deliveries[3].Operation.Should().Be(ConditionalHolonOperation.HardDelete);
        (await provider.LoadHolonAsync(current.Id,loadChildren:false)).Result.Should().BeNull();
        var replay=await provider.ExecuteReplicatingHolonWriteAsync(create,enrollment);
        replay.IsError.Should().BeFalse();replay.Result.Single().VersionId.Should().Be(first.Result.Single().VersionId);
        command.CommandText="SELECT COUNT(*) FROM CosmicHolonOutbox";Convert.ToInt64(command.ExecuteScalar()).Should().Be(4);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task OutboxMigrationPreservesExistingObligationsOrRefusesDowngrade(bool populated)
    {
        using(var initial=new DataContext(TestConnectionString))await initial.Database.MigrateAsync();
        if(populated)
        {
            using var provider=new SQLLiteDBOASIS(TestConnectionString);(await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
            (await provider.ExecuteReplicatingHolonWriteAsync(OutboxCreate(new Holon{Id=Guid.NewGuid()}),
                new HolonReplicationEnrollment("source",new[]{"target"}))).IsError.Should().BeFalse();
        }
        using var context=new DataContext(TestConnectionString);var migrator=context.GetService<IMigrator>();
        const string previous="20261019230000_AddHolonCreationAuditPresence";
        if(populated)
        {
            Func<Task> downgrade=()=>migrator.MigrateAsync(previous);await downgrade.Should().ThrowAsync<SqliteException>();
            (await context.Database.GetAppliedMigrationsAsync()).Should().Contain("20261019233000_AddCosmicHolonOutbox");
        }
        else{await migrator.MigrateAsync(previous);await migrator.MigrateAsync();}
        using var connection=new SqliteConnection(TestConnectionString);connection.Open();using var command=connection.CreateCommand();
        command.CommandText="SELECT COUNT(*) FROM CosmicHolonOutbox";Convert.ToInt64(command.ExecuteScalar()).Should().Be(populated ? 1:0);
    }

    [Fact]
    public async Task ConcurrentClientsReplayOneAtomicOutboxEnrollment()
    {
        using var first=new SQLLiteDBOASIS(TestConnectionString);using var second=new SQLLiteDBOASIS(TestConnectionString);
        (await first.ActivateProviderAsync()).IsError.Should().BeFalse();(await second.ActivateProviderAsync()).IsError.Should().BeFalse();
        var request=OutboxCreate(new Holon{Id=Guid.NewGuid(),Name="one logical commit"});
        var enrollment=new HolonReplicationEnrollment("source",new[]{"target-a","target-b"});
        using var barrier=new Barrier(2);
        var results=await Task.WhenAll(new[]{first,second}.Select(provider=>Task.Run(()=>
        {barrier.SignalAndWait(TimeSpan.FromSeconds(10)).Should().BeTrue();return provider.ExecuteReplicatingHolonWrite(request,enrollment);})));
        results.Should().OnlyContain(result=>!result.IsError);results.Select(result=>result.Result.Single().VersionId).Distinct().Should().HaveCount(1);
        using var connection=new SqliteConnection(TestConnectionString);connection.Open();using var command=connection.CreateCommand();
        command.CommandText="SELECT COUNT(*) FROM CosmicHolonOutbox";Convert.ToInt64(command.ExecuteScalar()).Should().Be(2);
        command.CommandText="SELECT COUNT(*) FROM CosmicWriteReceipt";Convert.ToInt64(command.ExecuteScalar()).Should().Be(1);
    }
}
