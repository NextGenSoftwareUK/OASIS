using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System.Data.Common;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Serialization;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Context;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Repositories;
using NextGenSoftware.OASIS.Common;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests;

public partial class SQLLiteDBOASISIntegrationTests
{
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task CommittedReplicaPreservesActualSourceCommitsAndDurableHeads(bool synchronous)
    {
        var sourcePath=Path.GetFullPath(Path.Combine(Path.GetDirectoryName(_databasePath)!,"replica-source-current.db"));
        File.Exists(sourcePath).Should().BeFalse("a retained owned source fixture requires inspection");
        using var source=new SQLLiteDBOASIS($"Data Source={sourcePath};Pooling=False");
        using var target=new SQLLiteDBOASIS(TestConnectionString);
        var sourceId="owned-sqlite-primary-"+Guid.NewGuid().ToString("N");var actor=Guid.NewGuid();var complete=false;
        static Holon Clone(IHolon value){var clone=StoredRecordJson.Project<Holon>(value);clone.Children=value.Children.Select(Clone).Cast<IHolon>().ToList();return clone;}
        static IEnumerable<IHolon> Nodes(IHolon value){yield return value;foreach(var child in value.Children)foreach(var item in Nodes(child))yield return item;}
        async Task<(IHolon Value,Guid Operation)> Commit(IHolon value,bool create=false,ConditionalHolonOperation kind=ConditionalHolonOperation.Save,bool children=true)
        {
            var op=Guid.NewGuid();var selected=children ? Nodes(value):new[]{value};
            var request=new ConditionalHolonWriteRequest(new ConditionalHolonWrite(op,actor,ProviderType.SQLLiteDBOASIS,
                selected.Select(item=>new HolonWriteCondition(item.Id,create ? null:CosmicConcurrency.GetProviderRevisionId(item,ProviderType.SQLLiteDBOASIS)))),kind,new[]{value},saveChildren:children);
            var result=synchronous ? source.ExecuteConditionalHolonWrite(request):await source.ExecuteConditionalHolonWriteAsync(request);
            result.IsError.Should().BeFalse(result.Message+result.Exception);return(result.Result.Single(),op);
        }
        CommittedHolonReplicaRequest Delivery((IHolon Value,Guid Operation) value,ConditionalHolonOperation kind=ConditionalHolonOperation.Save,bool children=true,string? origin=null)
            =>new(value.Operation,origin ?? sourceId,ProviderType.SQLLiteDBOASIS,actor,kind,new[]{value.Value},saveChildren:children);
        async Task<OASISResult<IEnumerable<IHolon>>> Apply(CommittedHolonReplicaRequest request)
            =>synchronous ? target.ApplyCommittedHolonReplica(request):await target.ApplyCommittedHolonReplicaAsync(request);
        string Snapshot(Guid id){using var control=new DataContext(TestConnectionString);return StoredRecordJson.Serialize(control.Holons.AsNoTracking().Single(row=>row.Id==id.ToString()));}
        long Count(string table,string field,Guid id)
        {
            using var connection=new SqliteConnection(TestConnectionString);connection.Open();using var command=connection.CreateCommand();
            command.CommandText="SELECT COUNT(*) FROM "+table+" WHERE "+field+"=$id";command.Parameters.AddWithValue("$id",id.ToString());return (long)command.ExecuteScalar()!;
        }
        try
        {
            (await source.ActivateProviderAsync()).IsError.Should().BeFalse();(await target.ActivateProviderAsync()).IsError.Should().BeFalse();
            var input=PersistentValueFixture();input.Children=new List<IHolon>{new Holon{Id=Guid.NewGuid(),Name="owned replica child"}};
            var first=await Commit(input,create:true);var original=StoredRecordJson.Serialize(first.Value);
            var initial=await Apply(Delivery(first));initial.IsError.Should().BeFalse(initial.Message+initial.Exception);initial.SavedCount.Should().Be(2);
            var ack=initial.Result.Single();ack.VersionId.Should().Be(first.Value.VersionId);ack.CreatedDate.Should().Be(first.Value.CreatedDate);
            ack.CreatedByAvatarId.Should().Be(actor);ack.ParentHolonId2.Should().Be(first.Value.ParentHolonId2);
            ((Holon)ack).CustomKey.Should().Be(((Holon)first.Value).CustomKey);
            ack.ProviderMetaData[ProviderType.Neo4jOASIS].Should().BeEquivalentTo(first.Value.ProviderMetaData[ProviderType.Neo4jOASIS]);
            ack.ProviderMetaData[ProviderType.SQLLiteDBOASIS].ContainsKey("local-note").Should().BeFalse("target local namespace is not imported");
            var token=CosmicConcurrency.GetProviderRevisionId(ack,ProviderType.SQLLiteDBOASIS);
            token.Should().HaveValue();token!.Value.Should().NotBe(CosmicConcurrency.GetProviderRevisionId(first.Value,ProviderType.SQLLiteDBOASIS)!.Value);
            StoredRecordJson.Serialize(first.Value).Should().Be(original);
            var secondInput=Clone(first.Value);secondInput.Name="owned replica second";secondInput.Children.Single().Name="owned child second";
            var second=await Commit(secondInput);var thirdInput=Clone(second.Value);thirdInput.Description="owned third";var third=await Commit(thirdInput);
            var before=Snapshot(input.Id);
            (await Apply(Delivery(third))).ErrorCode.Should().Be("COSMIC_REPLICA_LINEAGE_CONFLICT");Snapshot(input.Id).Should().Be(before);
            Count("CosmicWriteReceipt","OperationId",third.Operation).Should().Be(0);
            var appliedSecond=await Apply(Delivery(second));appliedSecond.IsError.Should().BeFalse(appliedSecond.Message+appliedSecond.Exception);
            appliedSecond.Result.Single().VersionId.Should().Be(second.Value.VersionId);var secondState=Snapshot(input.Id);
            using(var restarted=new SQLLiteDBOASIS(TestConnectionString))
            {
                (await restarted.ActivateProviderAsync()).IsError.Should().BeFalse();
                var replay=await restarted.ApplyCommittedHolonReplicaAsync(Delivery(first));replay.IsError.Should().BeFalse(replay.Message+replay.Exception);
                CosmicConcurrency.GetProviderRevisionId(replay.Result.Single(),ProviderType.SQLLiteDBOASIS).Should().Be(token);
                replay.Result.Single().IsNewHolon.Should().BeFalse();Snapshot(input.Id).Should().Be(secondState);
            }
            var oldName=first.Value.Name;first.Value.Name="changed intent";
            (await Apply(Delivery(first))).ErrorCode.Should().Be("COSMIC_OPERATION_ID_REUSED");first.Value.Name=oldName;
            (await Apply(Delivery(third,origin:sourceId+"-other"))).ErrorCode.Should().Be("COSMIC_REPLICA_LINEAGE_CONFLICT");
            (await Apply(Delivery(third))).IsError.Should().BeFalse();
            var childId=third.Value.Children.Single().Id;var childState=Snapshot(childId);
            var soft=await Commit(Clone(third.Value),kind:ConditionalHolonOperation.SoftDelete,children:false);
            var softened=await Apply(Delivery(soft,ConditionalHolonOperation.SoftDelete,children:false));softened.IsError.Should().BeFalse(softened.Message+softened.Exception);
            softened.IsDeleted.Should().BeTrue();softened.Result.Single().VersionId.Should().Be(soft.Value.VersionId);Snapshot(childId).Should().Be(childState);
            var hard=await Commit(Clone(soft.Value),kind:ConditionalHolonOperation.HardDelete,children:false);
            var deleted=await Apply(Delivery(hard,ConditionalHolonOperation.HardDelete,children:false));deleted.IsError.Should().BeFalse(deleted.Message+deleted.Exception);
            Count("Holon","Id",input.Id).Should().Be(0);Count("CosmicReplicaHead","HolonId",input.Id).Should().Be(1);Snapshot(childId).Should().Be(childState);
            (await Apply(Delivery(hard,ConditionalHolonOperation.HardDelete,children:false))).IsError.Should().BeFalse();
            (await Apply(Delivery(first))).IsError.Should().BeFalse();Count("Holon","Id",input.Id).Should().Be(0);
            var recreate=await Commit(new Holon{Id=input.Id,Name="owned new lineage"},create:true,children:false);
            (await Apply(Delivery(recreate,children:false))).ErrorCode.Should().Be("COSMIC_REPLICA_LINEAGE_CONFLICT");

            var localSource=await Commit(new Holon{Id=Guid.NewGuid(),Name="owned local conflict"},create:true,children:false);
            (await Apply(Delivery(localSource,children:false))).IsError.Should().BeFalse();
            var loaded=await target.LoadHolonAsync(localSource.Value.Id,loadChildren:false);loaded.Result.Name="owned local raw edit";
            var localWrite=await target.SaveHolonAsync(loaded.Result,saveChildren:false);localWrite.IsError.Should().BeFalse();localWrite.Result.VersionId.Should().Be(localSource.Value.VersionId);
            var localState=Snapshot(localSource.Value.Id);var nextInput=Clone(localSource.Value);nextInput.Name="must conflict";var next=await Commit(nextInput,children:false);
            (await Apply(Delivery(next,children:false))).ErrorCode.Should().Be("COSMIC_REPLICA_LINEAGE_CONFLICT");Snapshot(localSource.Value.Id).Should().Be(localState);

            var raced=await Commit(new Holon{Id=Guid.NewGuid(),Name="owned competing worker"},create:true,children:false);
            using(var other=new SQLLiteDBOASIS(TestConnectionString))
            {
                (await other.ActivateProviderAsync()).IsError.Should().BeFalse();var delivery=Delivery(raced,children:false);
                var races=await Task.WhenAll(Task.Run(()=>target.ApplyCommittedHolonReplica(delivery)),Task.Run(()=>other.ApplyCommittedHolonReplica(delivery)));
                races.Should().OnlyContain(value=>!value.IsError);
                CosmicConcurrency.GetProviderRevisionId(races[0].Result.Single(),ProviderType.SQLLiteDBOASIS).Should()
                    .Be(CosmicConcurrency.GetProviderRevisionId(races[1].Result.Single(),ProviderType.SQLLiteDBOASIS));
                Count("CosmicWriteReceipt","OperationId",raced.Operation).Should().Be(1);
            }

            var corrupt=await Commit(new Holon{Id=Guid.NewGuid(),Name="owned corrupt head"},create:true,children:false);
            (await Apply(Delivery(corrupt,children:false))).IsError.Should().BeFalse();var corruptState=Snapshot(corrupt.Value.Id);
            using(var control=new DataContext(TestConnectionString))control.Database.ExecuteSqlRaw("UPDATE CosmicReplicaHead SET SnapshotJson={0} WHERE HolonId={1}","{}",corrupt.Value.Id.ToString());
            var corruptInput=Clone(corrupt.Value);corruptInput.Name="must not overwrite";var corruptNext=await Commit(corruptInput,children:false);
            var rejected=await Apply(Delivery(corruptNext,children:false));rejected.IsError.Should().BeTrue();rejected.Exception.Message.Should().Contain("checksum");
            Snapshot(corrupt.Value.Id).Should().Be(corruptState);Count("CosmicWriteReceipt","OperationId",corruptNext.Operation).Should().Be(0);
            complete=true;
        }
        finally
        {
            source.Dispose();
            if(complete)foreach(var path in new[]{sourcePath,sourcePath+"-wal",sourcePath+"-shm"})if(File.Exists(path))File.Delete(path);
        }
    }

    private sealed class MutateCallerOnRead(Action mutate):DbCommandInterceptor
    {
        private bool used;
        public override InterceptionResult<DbDataReader> ReaderExecuting(DbCommand command,CommandEventData eventData,InterceptionResult<DbDataReader> result)
        {if(!used){used=true;mutate();}return result;}
    }
    private sealed class ReplicaProbeContext(string connection,IInterceptor interceptor):DataContext(connection)
    {
        protected override void OnConfiguring(DbContextOptionsBuilder options){base.OnConfiguring(options);options.AddInterceptors(interceptor);}
    }
    private sealed class ReplicaLostCommit(Exception cause):DbTransactionInterceptor
    {
        public int Commits;
        public override void TransactionCommitted(DbTransaction transaction,TransactionEndEventData eventData){Commits++;throw cause;}
    }
    private sealed class ReplicaCancelAfterSave(CancellationTokenSource cancellation):SaveChangesInterceptor
    {
        public override int SavedChanges(SaveChangesCompletedEventData eventData,int result){cancellation.Cancel();return result;}
    }
    private sealed class ReplicaPassThrough:DbCommandInterceptor { }
    private sealed class ReplicaCleanupFailure(Exception cause):DbConnectionInterceptor
    {
        public int Closures;
        public override void ConnectionClosed(DbConnection connection,ConnectionEndEventData eventData){Closures++;throw cause;}
    }
    [Theory]
    [InlineData("lost-commit")] [InlineData("cancel-late")] [InlineData("head-reject")] [InlineData("receipt-reject")] [InlineData("cleanup")]
    public async Task CommittedReplicaFaultsKeepNativeGraphHeadAndReceiptUnderOneOwner(string mode)
    {
        var sourcePath=Path.GetFullPath(Path.Combine(Path.GetDirectoryName(_databasePath)!,"replica-fault-source-current.db"));
        File.Exists(sourcePath).Should().BeFalse("a retained source fixture requires inspection");
        using var source=new SQLLiteDBOASIS($"Data Source={sourcePath};Pooling=False");var complete=false;
        try
        {
            (await source.ActivateProviderAsync()).IsError.Should().BeFalse();
            using(var initialize=new SQLLiteDBOASIS(TestConnectionString))(await initialize.ActivateProviderAsync()).IsError.Should().BeFalse();
            var input=new Holon{Id=Guid.NewGuid(),Name="owned fault source",CustomKey="owned fault key"};var operation=Guid.NewGuid();var actor=Guid.NewGuid();
            var committed=source.ExecuteConditionalHolonWrite(new ConditionalHolonWriteRequest(new ConditionalHolonWrite(operation,actor,ProviderType.SQLLiteDBOASIS,
                new[]{new HolonWriteCondition(input.Id,null)}),ConditionalHolonOperation.Save,new[]{input},saveChildren:false));
            committed.IsError.Should().BeFalse(committed.Message+committed.Exception);
            var request=new CommittedHolonReplicaRequest(operation,"owned-fault-origin",ProviderType.SQLLiteDBOASIS,actor,
                ConditionalHolonOperation.Save,committed.Result,saveChildren:false);
            var cause=new IOException("owned lost application acknowledgement after real SQLite commit");
            var commitProbe=new ReplicaLostCommit(cause);using var cancellation=new CancellationTokenSource();
            var cleanupProbe=new ReplicaCleanupFailure(cause);
            using var context=new ReplicaProbeContext(TestConnectionString,mode=="lost-commit" ? commitProbe
                :mode=="cancel-late" ? new ReplicaCancelAfterSave(cancellation):mode=="cleanup" ? cleanupProbe:new ReplicaPassThrough());
            if(mode is "head-reject" or "receipt-reject")
            {
                using var setup=new DataContext(TestConnectionString);
                var table=mode=="head-reject" ? "CosmicReplicaHead":"CosmicWriteReceipt";
                setup.Database.ExecuteSqlRaw("CREATE TRIGGER owned_replica_reject BEFORE INSERT ON "+table+" BEGIN SELECT RAISE(ABORT,'owned rejection'); END");
            }
            var repository=new HolonRepository(context);
            var result=repository.ApplyCommittedHolonReplica(request,mode=="cancel-late" ? cancellation.Token:default);
            using var inspection=new DataContext(TestConnectionString);
            long Count(string table)
            {
                using var connection=new SqliteConnection(TestConnectionString);connection.Open();using var command=connection.CreateCommand();
                command.CommandText="SELECT COUNT(*) FROM "+table;return (long)command.ExecuteScalar()!;
            }
            result.IsError.Should().Be(mode!="cleanup");
            if(mode=="cleanup")
            {
                result.IsSaved.Should().BeTrue();result.IsWarning.Should().BeTrue();result.Exception.Should().BeSameAs(cause);
                result.SavedCount.Should().Be(1);result.Result.Single().VersionId.Should().Be(committed.Result.Single().VersionId);
                Count("Holon").Should().Be(1);Count("CosmicReplicaHead").Should().Be(1);Count("CosmicWriteReceipt").Should().Be(1);
                cleanupProbe.Closures.Should().Be(1);
                NextGenSoftware.OASIS.API.Core.Helpers.NativeMutationReconciliation.IsRequired(result.Exception).Should().BeFalse();
            }
            else if(mode=="lost-commit")
            {
                result.ErrorCode.Should().Be("COSMIC_WRITE_RECONCILIATION_REQUIRED");
                var evidence=result.Exception.Should().BeOfType<SqliteReplicaReconciliationException>().Subject;
                evidence.OperationId.Should().Be(operation);evidence.SourceId.Should().Be(request.SourceId);evidence.InnerException.Should().BeSameAs(cause);
                evidence.OriginalTransactionId.Should().BeEmpty();commitProbe.Commits.Should().Be(1);
                Count("Holon").Should().Be(1);Count("CosmicReplicaHead").Should().Be(1);Count("CosmicWriteReceipt").Should().Be(1);
                inspection.Holons.AsNoTracking().Single().VersionId.Should().Be(committed.Result.Single().VersionId);
                var replay=repository.ApplyCommittedHolonReplica(request,default);replay.IsError.Should().BeFalse(replay.Message+replay.Exception);
                replay.Result.Single().VersionId.Should().Be(committed.Result.Single().VersionId);commitProbe.Commits.Should().Be(1);
            }
            else
            {
                NextGenSoftware.OASIS.API.Core.Helpers.NativeMutationReconciliation.IsRequired(result.Exception).Should().BeFalse();
                Count("Holon").Should().Be(0);Count("CosmicReplicaHead").Should().Be(0);Count("CosmicWriteReceipt").Should().Be(0);
                if(mode=="cancel-late")result.ErrorCode.Should().Be("COSMIC_REPLICA_WRITE_CANCELLED");
                else
                {
                    result.ErrorCode.Should().Be("COSMIC_REPLICA_WRITE_FAILED");
                    inspection.Database.ExecuteSqlRaw("DROP TRIGGER owned_replica_reject");
                    var retry=repository.ApplyCommittedHolonReplica(request,default);retry.IsError.Should().BeFalse(retry.Message+retry.Exception);
                    Count("Holon").Should().Be(1);Count("CosmicReplicaHead").Should().Be(1);Count("CosmicWriteReceipt").Should().Be(1);
                }
            }
            complete=true;
        }
        finally{source.Dispose();if(complete)foreach(var file in new[]{sourcePath,sourcePath+"-wal",sourcePath+"-shm"})if(File.Exists(file))File.Delete(file);}
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task CommittedReplicaHeadMigrationRefusesToDiscardRetainedLineage(bool populated)
    {
        const string previous="20261019210000_AddHolonPersistentValues";
        const string current="20261019220000_AddCosmicReplicaHeads";
        using var context=new DataContext(TestConnectionString);await context.Database.MigrateAsync();
        var migrator=context.GetService<Microsoft.EntityFrameworkCore.Migrations.IMigrator>();
        if(populated)
        {
            var record=new Holon{Id=Guid.NewGuid(),Name="owned retained head",Version=1,VersionId=Guid.NewGuid(),
                CreatedProviderType=new NextGenSoftware.Utilities.EnumValue<ProviderType>(ProviderType.Neo4jOASIS),
                CreatedOASISType=new NextGenSoftware.Utilities.EnumValue<OASISType>((OASISType)0)};
            CosmicConcurrency.SetProviderRevisionId(record,ProviderType.Neo4jOASIS,Guid.NewGuid());
            using var target=new SQLLiteDBOASIS(TestConnectionString);(await target.ActivateProviderAsync()).IsError.Should().BeFalse();
            var result=await target.ApplyCommittedHolonReplicaAsync(new CommittedHolonReplicaRequest(Guid.NewGuid(),"owned schema origin",ProviderType.Neo4jOASIS,
                Guid.Empty,ConditionalHolonOperation.Save,new[]{record},saveChildren:false));
            result.IsError.Should().BeFalse(result.Message+result.Exception);
            Func<Task> down=()=>migrator.MigrateAsync(previous);await down.Should().ThrowAsync<SqliteException>();
            (await context.Database.GetAppliedMigrationsAsync()).Should().Contain(current);
            await context.Database.OpenConnectionAsync();
            try
            {
                using var command=context.Database.GetDbConnection().CreateCommand();command.CommandText="SELECT COUNT(*) FROM CosmicReplicaHead";
                Convert.ToInt64(await command.ExecuteScalarAsync()).Should().Be(1);
                command.CommandText="SELECT COUNT(*) FROM __EFMigrationsLock";Convert.ToInt64(await command.ExecuteScalarAsync()).Should().Be(0);
            }
            finally{await context.Database.CloseConnectionAsync();}
        }
        else
        {
            await migrator.MigrateAsync(previous);(await context.Database.GetAppliedMigrationsAsync()).Should().NotContain(current);
            await migrator.MigrateAsync();(await context.Database.GetAppliedMigrationsAsync()).Should().Contain(current);
        }
    }
    [Fact]
    public async Task ConditionalGraphCapturesIntentAndTopologyBeforeNativeIo()
    {
        using(var initialize=new SQLLiteDBOASIS(TestConnectionString))(await initialize.ActivateProviderAsync()).IsError.Should().BeFalse();
        var child=new Holon{Id=Guid.NewGuid(),Name="captured child"};var root=new Holon{Id=Guid.NewGuid(),Name="captured root",Children=new List<IHolon>{child}};
        var op=Guid.NewGuid();var actor=Guid.NewGuid();
        using var context=new ReplicaProbeContext(TestConnectionString,
            new MutateCallerOnRead(()=>{root.Name="pending root edit";child.Name="pending child edit";root.Children.Clear();}));
        var repository=new HolonRepository(context);
        ConditionalHolonWriteRequest Request(IHolon value)=>new(new ConditionalHolonWrite(op,actor,ProviderType.SQLLiteDBOASIS,
            new[]{new HolonWriteCondition(root.Id,null),new HolonWriteCondition(child.Id,null)}),ConditionalHolonOperation.Save,new[]{value});
        var saved=repository.ExecuteConditionalHolonWrite(Request(root));saved.IsError.Should().BeFalse(saved.Message+saved.Exception);
        saved.Result.Single().Name.Should().Be("captured root");saved.Result.Single().Children.Single().Name.Should().Be("captured child");saved.SavedCount.Should().Be(2);
        root.Name.Should().Be("pending root edit");root.Children.Should().BeEmpty();child.Name.Should().Be("pending child edit");
        var original=new Holon{Id=root.Id,Name="captured root",Children=new List<IHolon>{new Holon{Id=child.Id,Name="captured child"}}};
        var replay=repository.ExecuteConditionalHolonWrite(Request(original));replay.IsError.Should().BeFalse(replay.Message+replay.Exception);
        replay.Result.Single().VersionId.Should().Be(saved.Result.Single().VersionId);
        replay.Result.Single().Children.Single().Name.Should().Be("captured child");
    }
}
