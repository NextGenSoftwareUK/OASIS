using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Context;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests;

public partial class SQLLiteDBOASISIntegrationTests
{
    [Fact]
    public async Task RecoveryEvidenceSurvivesIndependentStoreReconstructionWithoutTruncation()
    {
        await SeedOrderedOutboxAsync();var clock=new OutboxClock();var store=new SqliteHolonReplicationOutbox(TestConnectionString,clock);
        var claim=(await store.ClaimNextAsync("a","worker",TimeSpan.FromMinutes(1))).Result;
        var original=new string('x',5000)+"/original";var ids=new List<string>{original,"second",original};
        var evidence=new HolonDeliveryRecoveryEvidence(true,ids);ids.Clear();
        (await store.FinishWithRecoveryAsync(claim,HolonDeliveryOutcome.ReconciliationRequired,evidence,"COMMIT_UNKNOWN","inspect original receipts")).Result.Should().BeTrue();
        var inspected=await new SqliteHolonReplicationOutbox(TestConnectionString,clock).InspectAsync(claim.Sequence);
        inspected.IsError.Should().BeFalse(inspected.Message+inspected.Exception);
        inspected.Result.OperationId.Should().Be(claim.OperationId);inspected.Result.TargetId.Should().Be("a");
        inspected.Result.State.Should().Be(HolonDeliveryState.ReconciliationRequired);
        inspected.Result.RecoveryEvidence.RequiresNativeReconciliation.Should().BeTrue();
        inspected.Result.RecoveryEvidence.OriginalTransactionIds.Should().Equal(original,"second");
    }

    [Fact]
    public async Task FailedEvidenceWriteCannotCommitStateWithoutItsRecoveryIdentities()
    {
        await SeedOrderedOutboxAsync();var clock=new OutboxClock();var store=new SqliteHolonReplicationOutbox(TestConnectionString,clock);
        var claim=(await store.ClaimNextAsync("a","worker",TimeSpan.FromMinutes(1))).Result;
        using var control=new DataContext(TestConnectionString);
        control.Database.ExecuteSqlRaw("CREATE TRIGGER owned_recovery_fault BEFORE UPDATE OF RecoveryEvidenceJson ON CosmicHolonOutbox BEGIN SELECT RAISE(ABORT,'owned evidence fault'); END");
        var result=await store.FinishWithRecoveryAsync(claim,HolonDeliveryOutcome.ReconciliationRequired,
            new HolonDeliveryRecoveryEvidence(true,new[]{"original"}),"UNKNOWN","original native outcome unknown");
        result.IsError.Should().BeTrue();var inspected=(await store.InspectAsync(claim.Sequence)).Result;
        inspected.State.Should().Be(HolonDeliveryState.InFlight);inspected.RecoveryEvidence.Should().BeNull();
        control.Database.ExecuteSqlRaw("DROP TRIGGER owned_recovery_fault");
        (await store.FinishWithRecoveryAsync(claim,HolonDeliveryOutcome.ReconciliationRequired,
            new HolonDeliveryRecoveryEvidence(true,new[]{"original"}),"UNKNOWN")).Result.Should().BeTrue();
    }

    [Theory]
    [InlineData(HolonDeliveryOutcome.Completed)] [InlineData(HolonDeliveryOutcome.Retry)]
    public async Task NativeReconciliationEvidenceCannotAuthorizeCompletionOrAutomaticRetry(HolonDeliveryOutcome outcome)
    {
        await SeedOrderedOutboxAsync();var clock=new OutboxClock();var store=new SqliteHolonReplicationOutbox(TestConnectionString,clock);
        var claim=(await store.ClaimNextAsync("a","worker",TimeSpan.FromMinutes(1))).Result;
        var result=await store.FinishWithRecoveryAsync(claim,outcome,new HolonDeliveryRecoveryEvidence(true,Array.Empty<string>()),
            outcome==HolonDeliveryOutcome.Retry ? "UNKNOWN":null,nextAttemptUtc:outcome==HolonDeliveryOutcome.Retry ? clock.UtcNow.AddMinutes(1):null);
        result.IsError.Should().BeTrue();(await store.InspectAsync(claim.Sequence)).Result.State.Should().Be(HolonDeliveryState.InFlight);
    }

    [Fact]
    public async Task StaleOwnerCannotReplaceCurrentLeaseRecoveryEvidence()
    {
        await SeedOrderedOutboxAsync();var clock=new OutboxClock();var store=new SqliteHolonReplicationOutbox(TestConnectionString,clock);
        var old=(await store.ClaimNextAsync("a","worker",TimeSpan.FromMinutes(1))).Result;clock.UtcNow=old.LeaseUntilUtc;
        var current=(await store.ClaimNextAsync("a","worker",TimeSpan.FromMinutes(1))).Result;
        (await store.FinishWithRecoveryAsync(old,HolonDeliveryOutcome.ReconciliationRequired,
            new HolonDeliveryRecoveryEvidence(true,new[]{"stale"}),"UNKNOWN")).ErrorCode.Should().Be("COSMIC_OUTBOX_LEASE_LOST");
        (await store.FinishWithRecoveryAsync(current,HolonDeliveryOutcome.ReconciliationRequired,
            new HolonDeliveryRecoveryEvidence(true,new[]{"current"}),"UNKNOWN")).Result.Should().BeTrue();
        (await store.InspectAsync(current.Sequence)).Result.RecoveryEvidence.OriginalTransactionIds.Should().Equal("current");
    }

    [Theory]
    [InlineData("checksum")] [InlineData("state")] [InlineData("schema")] [InlineData("pair")]
    public async Task InvalidStoredRecoveryEvidenceFailsInspection(string fault)
    {
        await SeedOrderedOutboxAsync();var store=new SqliteHolonReplicationOutbox(TestConnectionString,new OutboxClock());
        var claim=(await store.ClaimNextAsync("a","worker",TimeSpan.FromMinutes(1))).Result;
        (await store.FinishWithRecoveryAsync(claim,HolonDeliveryOutcome.ReconciliationRequired,
            new HolonDeliveryRecoveryEvidence(true,new[]{"original"}),"UNKNOWN")).Result.Should().BeTrue();
        using var connection=new SqliteConnection(TestConnectionString);connection.Open();using var mutate=connection.CreateCommand();
        mutate.CommandText=fault=="checksum" ? "UPDATE CosmicHolonOutbox SET RecoveryEvidenceDigest='bad' WHERE Sequence=$id"
            :fault=="state" ? "UPDATE CosmicHolonOutbox SET DeliveryState='Completed' WHERE Sequence=$id"
            :fault=="pair" ? "UPDATE CosmicHolonOutbox SET RecoveryEvidenceDigest=NULL WHERE Sequence=$id"
            :"UPDATE CosmicHolonOutbox SET RecoveryEvidenceJson=$json,RecoveryEvidenceDigest=$digest WHERE Sequence=$id";
        mutate.Parameters.AddWithValue("$id",claim.Sequence);
        if(fault=="schema")
        {
            const string invalid="{\"format\":2,\"requiresNativeReconciliation\":true,\"originalTransactionIds\":[\"original\"]}";
            mutate.Parameters.AddWithValue("$json",invalid);mutate.Parameters.AddWithValue("$digest",Convert.ToHexString(
                System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(invalid))));
        }
        mutate.ExecuteNonQuery();var inspected=await store.InspectAsync(claim.Sequence);
        inspected.IsError.Should().BeTrue();inspected.ErrorCode.Should().Be("COSMIC_OUTBOX_CORRUPT");
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task RecoveryEvidenceMigrationPreservesLegacyStateAndGuardsRetainedEvidence(bool evidencePresent)
    {
        using(var initial=new DataContext(TestConnectionString))await initial.Database.MigrateAsync();
        await SeedOrderedOutboxAsync();var store=new SqliteHolonReplicationOutbox(TestConnectionString,new OutboxClock());
        if(evidencePresent)
        {
            var claim=(await store.ClaimNextAsync("a","worker",TimeSpan.FromMinutes(1))).Result;
            (await store.FinishWithRecoveryAsync(claim,HolonDeliveryOutcome.ReconciliationRequired,
                new HolonDeliveryRecoveryEvidence(true,new[]{"original"}),"UNKNOWN")).Result.Should().BeTrue();
        }
        using var context=new DataContext(TestConnectionString);var migrator=context.GetService<IMigrator>();
        const string previous="20261019234500_AddCosmicHolonDeliveryLeases";
        if(evidencePresent)
        {
            Func<Task> down=()=>migrator.MigrateAsync(previous);await down.Should().ThrowAsync<SqliteException>();
            (await context.Database.GetAppliedMigrationsAsync()).Should().Contain("20261020000000_AddCosmicHolonRecoveryEvidence");
        }
        else{await migrator.MigrateAsync(previous);await migrator.MigrateAsync();}
        var inspected=await store.InspectAsync(1);inspected.IsError.Should().BeFalse();
        if(evidencePresent)inspected.Result.RecoveryEvidence.OriginalTransactionIds.Should().Equal("original");
        else{inspected.Result.State.Should().Be(HolonDeliveryState.Pending);inspected.Result.RecoveryEvidence.Should().BeNull();}
    }
    [Fact]
    public async Task ReadOnlyHeadInspectionRetainsBlockedIdentityAndDoesNotConsumeAttempts()
    {
        await SeedOrderedOutboxAsync();var clock=new OutboxClock();var store=new SqliteHolonReplicationOutbox(TestConnectionString,clock);
        string Snapshot()
        {
            using var connection=new SqliteConnection(TestConnectionString);connection.Open();using var query=connection.CreateCommand();
            query.CommandText="SELECT group_concat(value,'|') FROM (SELECT Sequence||':'||DeliveryState||':'||AttemptCount||':'||LeaseToken||':'||LeaseUntilUtcTicks AS value FROM CosmicHolonOutbox ORDER BY Sequence)";
            return (string)query.ExecuteScalar()!;
        }
        var original=Snapshot();var pending=await store.PeekNextAsync("a");pending.IsError.Should().BeFalse();pending.Result.State.Should().Be(HolonDeliveryState.Pending);
        pending.Result.AttemptCount.Should().Be(0);Snapshot().Should().Be(original);
        var claim=(await store.ClaimNextAsync("a","worker",TimeSpan.FromMinutes(1))).Result;var leased=Snapshot();
        var inFlight=await store.PeekNextAsync("a");inFlight.Result.Sequence.Should().Be(claim.Sequence);inFlight.Result.State.Should().Be(HolonDeliveryState.InFlight);Snapshot().Should().Be(leased);
        (await store.FinishWithRecoveryAsync(claim,HolonDeliveryOutcome.ReconciliationRequired,new(true,new[]{"native-original"}),"NATIVE_UNKNOWN")).Result.Should().BeTrue();
        var retained=Snapshot();var blocked=await new SqliteHolonReplicationOutbox(TestConnectionString,clock).PeekNextAsync("a");
        blocked.Result.State.Should().Be(HolonDeliveryState.ReconciliationRequired);blocked.Result.Sequence.Should().Be(claim.Sequence);
        blocked.Result.AttemptCount.Should().Be(1);blocked.Result.RecoveryEvidence.OriginalTransactionIds.Should().Equal("native-original");Snapshot().Should().Be(retained);
        (await store.PeekNextAsync("missing")).Result.Should().BeNull();(await store.PeekNextAsync(" ")).IsError.Should().BeTrue();
        using var corrupt=new SqliteConnection(TestConnectionString);corrupt.Open();using var mutate=corrupt.CreateCommand();
        mutate.CommandText="UPDATE CosmicHolonOutbox SET PayloadDigest='owned corrupt' WHERE Sequence=$sequence";mutate.Parameters.AddWithValue("$sequence",claim.Sequence);mutate.ExecuteNonQuery();
        (await store.PeekNextAsync("a")).ErrorCode.Should().Be("COSMIC_OUTBOX_CORRUPT");
        (await store.InspectAsync(claim.Sequence)).Result.AttemptCount.Should().Be(1);
    }

    [Fact]
    public async Task ActivatedSourceCaptureUsesTheExistingDatabaseAndOwnsNoSchemaOrLease()
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);
        provider.GetHolonReplicationOutbox().ErrorCode.Should().Be("COSMIC_OUTBOX_SOURCE_NOT_ACTIVATED");
        File.Exists(_databasePath).Should().BeFalse();await SeedOrderedOutboxAsync();
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var captured=provider.GetHolonReplicationOutbox();captured.IsError.Should().BeFalse(captured.Message+captured.Exception);
        var head=await captured.Result.PeekNextAsync("a");head.IsError.Should().BeFalse(head.Message+head.Exception);head.Result.AttemptCount.Should().Be(0);
        using var control=new SqliteConnection(TestConnectionString);control.Open();using var query=control.CreateCommand();
        query.CommandText="SELECT COUNT(*) FROM CosmicHolonOutbox WHERE AttemptCount<>0";((long)query.ExecuteScalar()!).Should().Be(0L);
        await provider.DeActivateProviderAsync();provider.GetHolonReplicationOutbox().ErrorCode.Should().Be("COSMIC_OUTBOX_SOURCE_NOT_ACTIVATED");
    }

}
