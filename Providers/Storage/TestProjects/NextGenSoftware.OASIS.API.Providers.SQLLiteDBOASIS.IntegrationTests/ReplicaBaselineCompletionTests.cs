using System.Text.Json;
using Microsoft.Data.Sqlite;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests;

public partial class SQLLiteDBOASISIntegrationTests
{
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task SqliteSourceBaselineCompletionIsFencedDurableAndAllowsOrdinaryNextDelivery(bool durableApplication)
    {
        await WithVersionFourSource(async(source,committed,actor)=>{
            var root=new DirectoryInfo(AppContext.BaseDirectory);
            while(root!=null && !File.Exists(Path.Combine(root.FullName,"Scripts/TestHosts/Invoke-WithProviderBuildLease.ps1")))root=root.Parent;
            Assert.NotNull(root);
            var state=Path.Combine(root!.FullName,"LocalState/COSMIC/SqliteBaselineCompletionCurrent");Directory.CreateDirectory(state);
            var manifest=Path.Combine(state,"fixture.json");var journal=Path.Combine(state,"application.json");
            if(File.Exists(manifest))
            {
                using var previous=JsonDocument.Parse(await File.ReadAllTextAsync(manifest));
                Assert.True(previous.RootElement.GetProperty("cleaned").GetBoolean(),"Preserve unresolved original fixture.");
            }
            foreach(var path in new[]{journal,journal+".lock",journal+".tmp"})Assert.False(File.Exists(path),"Preserve original application outcome.");
            var initialization=Guid.NewGuid();var owner=Guid.NewGuid();var complete=false;
            var sourcePath=Path.Combine(Path.GetDirectoryName(_databasePath)!,"initial-baseline-source-current.db");
            void Record(bool cleaned)=>File.WriteAllText(manifest,JsonSerializer.Serialize(new{owner,actor,root=committed.Id,
                initialization,sourcePath,targetPath=_databasePath,cleaned}));
            Record(false);
            using var target=new SQLLiteDBOASIS(TestConnectionString);Assert.False((await target.ActivateProviderAsync()).IsError);
            var outbox=(SqliteHolonReplicationOutbox)source.GetHolonReplicationOutbox().Result;
            async Task SaveNext(int version)
            {
                var selected=new[]{committed}.Concat(committed.Children).ToArray();
                var request=new ConditionalHolonWriteRequest(new ConditionalHolonWrite(Guid.NewGuid(),actor,ProviderType.SQLLiteDBOASIS,
                    selected.Select(row=>new HolonWriteCondition(row.Id,CosmicConcurrency.GetProviderRevisionId(row,ProviderType.SQLLiteDBOASIS)))),
                    ConditionalHolonOperation.Save,new[]{committed});
                var saved=await source.ExecuteReplicatingHolonWriteAsync(request,new HolonReplicationEnrollment("prepared-source",new[]{"prepared-target"}));
                Assert.False(saved.IsError,saved.Message+saved.Exception);committed=Assert.Single(saved.Result);Assert.Equal(version,committed.Version);
            }
            string Snapshot()
            {
                using var connection=new SqliteConnection($"Data Source={sourcePath};Pooling=False");connection.Open();
                using var command=connection.CreateCommand();
                command.CommandText="SELECT json_object('state',DeliveryState,'at',CompletedAtUtcTicks,'attempt',AttemptCount,'payload',PayloadJson,'digest',PayloadDigest,'error',LastErrorCode,'message',LastErrorMessage,'recovery',RecoveryEvidenceJson,'recoveryDigest',RecoveryEvidenceDigest) FROM CosmicHolonOutbox ORDER BY Sequence";
                using var reader=command.ExecuteReader();var rows=new List<string>();while(reader.Read())rows.Add(reader.GetString(0));
                return JsonSerializer.Serialize(rows);
            }
            try
            {
                await SaveNext(5);
                var dispatcher=new HolonOutboxDispatcher(outbox,new[]{new CommittedHolonReplicaTarget("prepared-target",target,
                    ProviderType.SQLLiteDBOASIS,"prepared-source",ProviderType.SQLLiteDBOASIS)},"baseline-completion-worker");
                Assert.Equal(1,(await dispatcher.DispatchOnceAsync("prepared-target")).Result.ReconciliationCount);
                var held=(await outbox.PeekNextAsync("prepared-target")).Result;
                var preparation=new HolonReplicaInitializationPreparation("source-completion",Guid.NewGuid(),"prepared-source","owned-source-file",
                    ProviderType.SQLLiteDBOASIS,outbox,"prepared-target","owned-target-file",ProviderType.SQLLiteDBOASIS,new OwnedPreparationPolicy(actor,committed.Id));
                var prepared=await preparation.PrepareAsync(actor,held.Sequence,initialization);
                Assert.False(prepared.IsError,prepared.Message+prepared.Exception);var intent=prepared.Result;
                var original=CommittedHolonReplicaPayload.Read((await outbox.ReadObligationAsync(held.Sequence)).Result.PayloadJson);
                var baseline=new CommittedHolonReplicaRequest(initialization,original.SourceId,original.SourceAuthority,original.ActorId,
                    original.Operation,original.Roots,original.SaveChildren,original.Recursive,original.MaxChildDepth);
                var absent=(await target.ReadReplicaInitializationOutcomeAsync(baseline)).Result;
                Assert.True((await outbox.CompleteInitializedBaselineAsync(intent,absent)).IsError);
                Assert.Equal(HolonDeliveryState.ReconciliationRequired,(await outbox.InspectAsync(held.Sequence)).Result.State);
                if(durableApplication)
                {
                    var application=new HolonReplicaInitializationApplication(preparation,target,outbox.CompleteInitializedBaselineAsync);
                    var applied=await application.ExecuteAsync(actor,held.Sequence,initialization,journal);
                    Assert.False(applied.IsError,applied.Message+applied.Exception);
                    var bytes=await File.ReadAllBytesAsync(journal);
                    Assert.True((await application.ExecuteAsync(actor,held.Sequence,initialization,journal)).Result);
                    Assert.Equal(bytes,await File.ReadAllBytesAsync(journal));
                }
                else Assert.False((await target.InitializeCommittedHolonReplicaAsync(baseline)).IsError);
                var receipt=(await target.ReadReplicaInitializationOutcomeAsync(baseline)).Result;
                if(!durableApplication)
                {
                    // Change only this owned native fixture after review: stale
                    // attempts must not complete or consume the original obligation.
                    using(var connection=new SqliteConnection($"Data Source={sourcePath};Pooling=False"))
                    {
                        connection.Open();using var change=connection.CreateCommand();
                        change.CommandText="UPDATE CosmicHolonOutbox SET AttemptCount=$attempt WHERE Sequence=$sequence";
                        change.Parameters.AddWithValue("$attempt",intent.AttemptCount+1);change.Parameters.AddWithValue("$sequence",held.Sequence);
                        Assert.Equal(1,change.ExecuteNonQuery());
                        Assert.True((await outbox.CompleteInitializedBaselineAsync(intent,receipt)).IsError);
                        Assert.Equal(HolonDeliveryState.ReconciliationRequired,(await outbox.InspectAsync(held.Sequence)).Result.State);
                        change.Parameters["$attempt"].Value=intent.AttemptCount;Assert.Equal(1,change.ExecuteNonQuery());
                    }
                    var untouched=Snapshot();
                    using(var cancelled=new CancellationTokenSource())
                    {
                        cancelled.Cancel();
                        await Assert.ThrowsAnyAsync<OperationCanceledException>(()=>outbox.CompleteInitializedBaselineAsync(intent,receipt,cancelled.Token));
                    }
                    Assert.Equal(untouched,Snapshot());
                    using var barrier=new Barrier(2);
                    var competing=await Task.WhenAll(Enumerable.Range(0,2).Select(_=>Task.Run(async()=>{
                        Assert.True(barrier.SignalAndWait(TimeSpan.FromSeconds(5)));
                        return await new SqliteHolonReplicationOutbox($"Data Source={sourcePath};Pooling=False")
                            .CompleteInitializedBaselineAsync(intent,receipt);
                    })));
                    Assert.All(competing,result=>Assert.True(result.Result,result.Message+result.Exception));
                }
                Assert.True((await outbox.CompleteInitializedBaselineAsync(intent,receipt)).Result);
                var acknowledged=Snapshot();
                var completed=(await outbox.InspectAsync(held.Sequence)).Result;
                Assert.Equal(held.ErrorCode,completed.ErrorCode);Assert.Equal(held.Message,completed.Message);
                Assert.True((await new SqliteHolonReplicationOutbox($"Data Source={sourcePath};Pooling=False").CompleteInitializedBaselineAsync(intent,receipt)).Result);
                Assert.Equal(acknowledged,Snapshot());
                var changed=new HolonReplicaInitializationOutcome(initialization,ProviderType.SQLLiteDBOASIS,
                    ConditionalHolonReceiptState.Committed,receipt.BaselineFingerprint,new string('0',64));
                Assert.True((await outbox.CompleteInitializedBaselineAsync(intent,changed)).IsError);
                Assert.Equal(acknowledged,Snapshot());
                using(var connection=new SqliteConnection($"Data Source={sourcePath};Pooling=False"))
                {
                    connection.Open();using(var count=connection.CreateCommand())
                    {
                        count.CommandText="SELECT COUNT(*) FROM CosmicHolonOutboxInitializationCompletion";
                        Assert.Equal(1L,(long)count.ExecuteScalar()!);
                    }
                    // A downgrade cannot silently discard the exact recovery ACK.
                    using(var transaction=connection.BeginTransaction())
                    {
                        using var command=connection.CreateCommand();command.Transaction=transaction;
                        var migration=new Migrations.AddReplicaInitializationCompletion();
                        command.CommandText=Assert.IsType<Microsoft.EntityFrameworkCore.Migrations.Operations.SqlOperation>(Assert.Single(migration.DownOperations)).Sql;
                        Assert.Throws<SqliteException>(()=>command.ExecuteNonQuery());
                        transaction.Rollback();
                    }
                }
                Assert.Equal(acknowledged,Snapshot());
                committed.Name="ordinary post-baseline update";await SaveNext(6);
                Assert.Equal(1,(await dispatcher.DispatchOnceAsync("prepared-target")).Result.CompletedCount);
                Assert.Equal(6,(await target.LoadHolonAsync(committed.Id,loadChildren:false)).Result.Version);
                Assert.Null((await outbox.PeekNextAsync("prepared-target")).Result);
                complete=true;
            }
            finally
            {
                if(complete)
                {
                    foreach(var path in new[]{journal,journal+".tmp",journal+".lock"})if(File.Exists(path))File.Delete(path);
                    Record(true);
                }
            }
        });
    }
}
