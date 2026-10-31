using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.Synchronization;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Context;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests;

public partial class SQLLiteDBOASISIntegrationTests
{
    private sealed class OutboxClock : IHyperDriveClock
    { public DateTime UtcNow {get;set;}=new(2026,10,19,12,0,0,DateTimeKind.Utc); }

    private sealed class ReservationCheckingClock(string connectionString) : IHyperDriveClock
    {
        public int Reads;
        public DateTime UtcNow
        {
            get
            {
                using var probe=new SqliteConnection(new SqliteConnectionStringBuilder(connectionString){DefaultTimeout=1}.ToString());probe.Open();
                Action acquire=()=>{using var transaction=probe.BeginTransaction(deferred:false);};
                acquire.Should().Throw<SqliteException>().Which.SqliteErrorCode.Should().Be(5);
                Reads++;return new DateTime(2026,10,19,12,0,0,DateTimeKind.Utc);
            }
        }
    }

    private async Task<Guid> SeedOrderedOutboxAsync()
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);(await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var enrollment=new HolonReplicationEnrollment("source",new[]{"a","b"});
        var request=OutboxCreate(new Holon{Id=Guid.NewGuid(),Name="first"});
        var first=await provider.ExecuteReplicatingHolonWriteAsync(request,enrollment);first.IsError.Should().BeFalse();
        var row=first.Result.Single();row.Name="second";
        var update=new ConditionalHolonWriteRequest(new ConditionalHolonWrite(Guid.NewGuid(),request.Write.ActorId,ProviderType.SQLLiteDBOASIS,
            new[]{new HolonWriteCondition(row.Id,CosmicConcurrency.GetProviderRevisionId(row,ProviderType.SQLLiteDBOASIS))}),
            ConditionalHolonOperation.Save,new[]{row},saveChildren:false);
        (await provider.ExecuteReplicatingHolonWriteAsync(update,enrollment)).IsError.Should().BeFalse();return request.Write.OperationId;
    }

    [Fact]
    public async Task DeliveryLeaseFencesExpiredOwnersIncludingReusedWorkerNames()
    {
        var firstOperation=await SeedOrderedOutboxAsync();var clock=new OutboxClock();
        var first=new SqliteHolonReplicationOutbox(TestConnectionString,clock);var restarted=new SqliteHolonReplicationOutbox(TestConnectionString,clock);
        var a=await first.ClaimNextAsync("a","worker",TimeSpan.FromMinutes(1));a.IsError.Should().BeFalse();a.Result.OperationId.Should().Be(firstOperation);
        (await restarted.ClaimNextAsync("a","worker",TimeSpan.FromMinutes(1))).Result.Should().BeNull();
        var b=await first.ClaimNextAsync("b","worker",TimeSpan.FromMinutes(1));b.Result.OperationId.Should().Be(firstOperation);
        clock.UtcNow=clock.UtcNow.AddSeconds(50);
        (await first.RenewAsync(a.Result,TimeSpan.FromMinutes(1))).Result.Should().BeTrue();
        clock.UtcNow=clock.UtcNow.AddSeconds(20);
        (await first.FinishAsync(a.Result,HolonDeliveryOutcome.Completed)).Result.Should().BeTrue();
        var newer=await restarted.ClaimNextAsync("b","worker",TimeSpan.FromMinutes(1));newer.IsError.Should().BeFalse();
        newer.Result.LeaseToken.Should().NotBe(b.Result.LeaseToken);newer.Result.OperationId.Should().Be(firstOperation);newer.Result.AttemptCount.Should().Be(2);
        (await first.RenewAsync(b.Result,TimeSpan.FromMinutes(1))).ErrorCode.Should().Be("COSMIC_OUTBOX_LEASE_LOST");
        (await first.FinishAsync(b.Result,HolonDeliveryOutcome.Completed)).ErrorCode.Should().Be("COSMIC_OUTBOX_LEASE_LOST");
        (await restarted.FinishAsync(newer.Result,HolonDeliveryOutcome.Completed)).Result.Should().BeTrue();
        var second=await first.ClaimNextAsync("a","worker",TimeSpan.FromMinutes(1));second.Result.OperationId.Should().NotBe(firstOperation);
        (await first.FinishAsync(second.Result,HolonDeliveryOutcome.Completed)).Result.Should().BeTrue();
        (await first.ClaimNextAsync("a","worker",TimeSpan.FromMinutes(1))).Result.Should().BeNull();
    }

    [Fact]
    public async Task RetryRetainsOriginalIntentAndBlocksLaterVersionsOnlyForItsTarget()
    {
        await SeedOrderedOutboxAsync();var clock=new OutboxClock();var store=new SqliteHolonReplicationOutbox(TestConnectionString,clock);
        var claim=(await store.ClaimNextAsync("a","one",TimeSpan.FromMinutes(1))).Result;
        (await store.FinishAsync(claim,HolonDeliveryOutcome.Retry,"TARGET_OFFLINE","owned test",clock.UtcNow.AddMinutes(5))).Result.Should().BeTrue();
        (await store.ClaimNextAsync("a","two",TimeSpan.FromMinutes(1))).Result.Should().BeNull();
        (await store.ClaimNextAsync("b","two",TimeSpan.FromMinutes(1))).Result.Should().NotBeNull();
        clock.UtcNow=clock.UtcNow.AddMinutes(5);
        var retry=(await store.ClaimNextAsync("a","two",TimeSpan.FromMinutes(1))).Result;
        retry.OperationId.Should().Be(claim.OperationId);retry.PayloadJson.Should().Be(claim.PayloadJson);retry.AttemptCount.Should().Be(2);
        retry.LeaseToken.Should().NotBe(claim.LeaseToken);
        (await store.FinishAsync(retry,HolonDeliveryOutcome.Completed)).Result.Should().BeTrue();
        (await store.ClaimNextAsync("a","two",TimeSpan.FromMinutes(1))).Result.OperationId.Should().NotBe(claim.OperationId);
    }

    [Fact]
    public async Task ReconciliationOutcomeBlocksAutomaticReplayAndPreservesDiagnostics()
    {
        await SeedOrderedOutboxAsync();var clock=new OutboxClock();var store=new SqliteHolonReplicationOutbox(TestConnectionString,clock);
        var claim=(await store.ClaimNextAsync("a","one",TimeSpan.FromMinutes(1))).Result;
        (await store.FinishAsync(claim,HolonDeliveryOutcome.ReconciliationRequired,"COMMIT_UNKNOWN","inspect original receipt")).Result.Should().BeTrue();
        clock.UtcNow=clock.UtcNow.AddDays(1);
        var blocked=await store.ClaimNextAsync("a","two",TimeSpan.FromMinutes(1));blocked.Result.Should().BeNull();blocked.Message.Should().Contain("reconciliation");
        using var connection=new SqliteConnection(TestConnectionString);connection.Open();using var command=connection.CreateCommand();
        command.CommandText="SELECT LastErrorCode,LastErrorMessage FROM CosmicHolonOutbox WHERE Sequence=$id";command.Parameters.AddWithValue("$id",claim.Sequence);
        using var reader=command.ExecuteReader();reader.Read().Should().BeTrue();reader.GetString(0).Should().Be("COMMIT_UNKNOWN");reader.GetString(1).Should().Be("inspect original receipt");
    }

    [Theory]
    [InlineData("payload")] [InlineData("digest")] [InlineData("enrollment")]
    public async Task CorruptDeliveryIntentFailsBeforeClaimMutation(string fault)
    {
        await SeedOrderedOutboxAsync();using var control=new DataContext(TestConnectionString);
        if(fault=="payload")control.Database.ExecuteSqlRaw("UPDATE CosmicHolonOutbox SET PayloadJson={0} WHERE Sequence=1","{}");
        else control.Database.ExecuteSqlRaw(fault=="digest" ? "UPDATE CosmicHolonOutbox SET PayloadDigest='bad' WHERE Sequence=1":"UPDATE CosmicHolonOutbox SET SourceId='other' WHERE Sequence=1");
        var store=new SqliteHolonReplicationOutbox(TestConnectionString,new OutboxClock());
        var result=await store.ClaimNextAsync("a","worker",TimeSpan.FromMinutes(1));result.IsError.Should().BeTrue();result.ErrorCode.Should().Be("COSMIC_OUTBOX_CORRUPT");
        using var connection=new SqliteConnection(TestConnectionString);connection.Open();using var command=connection.CreateCommand();
        command.CommandText="SELECT AttemptCount,DeliveryState FROM CosmicHolonOutbox WHERE Sequence=1";
        using var reader=command.ExecuteReader();reader.Read().Should().BeTrue();reader.GetInt32(0).Should().Be(0);reader.GetString(1).Should().Be("Pending");
    }

    [Fact]
    public async Task CompetingClientsCannotClaimTheSameTargetObligation()
    {
        await SeedOrderedOutboxAsync();var clock=new OutboxClock();using var barrier=new Barrier(2);
        var results=await Task.WhenAll(new[]{"one","two"}.Select(worker=>Task.Run(async()=>{
            barrier.SignalAndWait(TimeSpan.FromSeconds(10)).Should().BeTrue();
            return await new SqliteHolonReplicationOutbox(TestConnectionString,clock).ClaimNextAsync("a",worker,TimeSpan.FromMinutes(1));
        })));
        results.Should().OnlyContain(value=>!value.IsError);results.Count(value=>value.Result!=null).Should().Be(1);
    }

    [Fact]
    public async Task MissingDatabaseIsNotCreatedByDeliveryWorker()
    {
        var result=await new SqliteHolonReplicationOutbox(TestConnectionString,new OutboxClock()).ClaimNextAsync("a","worker",TimeSpan.FromMinutes(1));
        result.IsError.Should().BeTrue();File.Exists(new SqliteConnectionStringBuilder(TestConnectionString).DataSource).Should().BeFalse();
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task DeliveryLeaseMigrationPreservesV1ObligationsAndRefusesLoss(bool populated)
    {
        using var context=new DataContext(TestConnectionString);var migrator=context.GetService<IMigrator>();
        const string previous="20261019233000_AddCosmicHolonOutbox";
        await migrator.MigrateAsync(previous);
        var operation=Guid.NewGuid();var actor=Guid.NewGuid();
        var row=new Holon{Id=Guid.NewGuid(),Version=1,VersionId=Guid.NewGuid()};
        CosmicConcurrency.SetProviderRevisionId(row,ProviderType.SQLLiteDBOASIS,Guid.NewGuid());
        var payload=CommittedHolonReplicaPayload.Capture(new CommittedHolonReplicaRequest(operation,"source",ProviderType.SQLLiteDBOASIS,
            actor,ConditionalHolonOperation.Save,new[]{row},saveChildren:false));
        if(populated)context.Database.ExecuteSqlRaw("INSERT INTO CosmicHolonOutbox(OperationId,TargetId,SourceId,SourceAuthority,ActorId,PayloadJson,PayloadDigest,CreatedAtUtc) VALUES ({0},'a','source','SQLLiteDBOASIS',{1},{2},{3},{4})",
            operation.ToString("D"),actor.ToString("D"),payload,Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(payload))),DateTime.UtcNow.ToString("O"));
        await migrator.MigrateAsync();
        if(populated)
        {
            var claim=await new SqliteHolonReplicationOutbox(TestConnectionString,new OutboxClock()).ClaimNextAsync("a","worker",TimeSpan.FromMinutes(1));
            claim.IsError.Should().BeFalse(claim.Message+claim.Exception);claim.Result.PayloadJson.Should().Be(payload);claim.Result.OperationId.Should().Be(operation);
            Func<Task> down=()=>migrator.MigrateAsync(previous);await down.Should().ThrowAsync<SqliteException>();
            (await context.Database.GetAppliedMigrationsAsync()).Should().Contain("20261019234500_AddCosmicHolonDeliveryLeases");
        }
        else{await migrator.MigrateAsync(previous);await migrator.MigrateAsync();}
    }

    [Fact]
    public async Task FailedClaimMutationDoesNotConsumeAttemptOrLoseIntent()
    {
        await SeedOrderedOutboxAsync();using var context=new DataContext(TestConnectionString);
        context.Database.ExecuteSqlRaw("CREATE TRIGGER owned_delivery_fault BEFORE UPDATE OF DeliveryState ON CosmicHolonOutbox BEGIN SELECT RAISE(ABORT,'owned lease mutation fault'); END");
        var result=await new SqliteHolonReplicationOutbox(TestConnectionString,new OutboxClock()).ClaimNextAsync("a","worker",TimeSpan.FromMinutes(1));
        result.IsError.Should().BeTrue();result.Exception.Should().BeOfType<SqliteException>();
        using var connection=new SqliteConnection(TestConnectionString);connection.Open();using var query=connection.CreateCommand();
        query.CommandText="SELECT DeliveryState,AttemptCount,LeaseToken FROM CosmicHolonOutbox WHERE Sequence=1";
        using var reader=query.ExecuteReader();reader.Read().Should().BeTrue();reader.GetString(0).Should().Be("Pending");reader.GetInt32(1).Should().Be(0);reader.GetString(2).Should().BeEmpty();
    }

    [Fact]
    public async Task ExpiryIsCheckedAfterWriterReservationForClaimRenewAndCompletion()
    {
        await SeedOrderedOutboxAsync();var clock=new ReservationCheckingClock(TestConnectionString);
        var store=new SqliteHolonReplicationOutbox(TestConnectionString,clock);
        var claimed=await store.ClaimNextAsync("a","worker",TimeSpan.FromMinutes(1));claimed.IsError.Should().BeFalse(claimed.Message+claimed.Exception);
        (await store.RenewAsync(claimed.Result,TimeSpan.FromMinutes(1))).Result.Should().BeTrue();
        (await store.FinishAsync(claimed.Result,HolonDeliveryOutcome.Completed)).Result.Should().BeTrue();clock.Reads.Should().Be(3);
    }

    [Fact]
    public async Task ExpiredLeaseCannotCompleteOrRenewEvenBeforeAnotherWorkerReclaims()
    {
        await SeedOrderedOutboxAsync();var clock=new OutboxClock();var store=new SqliteHolonReplicationOutbox(TestConnectionString,clock);
        var claim=(await store.ClaimNextAsync("a","worker",TimeSpan.FromMinutes(1))).Result;clock.UtcNow=claim.LeaseUntilUtc;
        (await store.RenewAsync(claim,TimeSpan.FromMinutes(1))).ErrorCode.Should().Be("COSMIC_OUTBOX_LEASE_LOST");
        (await store.FinishAsync(claim,HolonDeliveryOutcome.Completed)).ErrorCode.Should().Be("COSMIC_OUTBOX_LEASE_LOST");
        var replacement=(await store.ClaimNextAsync("a","worker",TimeSpan.FromMinutes(1))).Result;
        replacement.OperationId.Should().Be(claim.OperationId);replacement.LeaseToken.Should().NotBe(claim.LeaseToken);
    }
}
