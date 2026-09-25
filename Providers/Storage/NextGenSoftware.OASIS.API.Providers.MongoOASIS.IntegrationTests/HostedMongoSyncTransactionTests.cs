using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.Synchronization;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.MongoOASIS.IntegrationTests;

public sealed class HostedMongoSyncTransactionTests
{
    [Fact]
    public async Task PublicDefinitionsAreVisibleGloballyWhilePrivateHolonsRemainAvatarScoped()
    {
        string connectionString = RequiredConnectionString();
        string databaseName = NewDatabaseName("audience");
        var client = new MongoClient(connectionString);
        try
        {
            var provider = new NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS(
                connectionString, databaseName);
            var ownerId = Guid.NewGuid();
            var ownerDeviceId = Guid.NewGuid();
            var questId = Guid.NewGuid();
            var publicQuest = CreateOperation(ownerId, ownerDeviceId,
                $"{{\"holonId\":\"{questId:D}\",\"holonType\":{(int)HolonType.Quest},\"name\":\"public quest\"}}");
            publicQuest.EntityId = questId;
            publicQuest.EntityType = HyperDriveEntityTypes.Quest;
            var privateHolon = CreateOperation(ownerId, ownerDeviceId, "{\"name\":\"private state\"}");
            privateHolon.DeviceSequence = 2;

            var apply = await provider.ApplyOperationsAsync(ownerId, ownerDeviceId,
                new[] { publicQuest, privateHolon }, default);
            Assert.False(apply.IsError, apply.Message);

            var strangerChanges = await provider.ReadChangesAsync(Guid.NewGuid(), Guid.NewGuid(), null,
                Guid.Empty, 0, 20, default);
            Assert.False(strangerChanges.IsError, strangerChanges.Message);
            var visible = Assert.Single(strangerChanges.Result.Changes);
            Assert.Equal(HyperDriveEntityTypes.Quest, visible.EntityType);
            Assert.Equal(questId, visible.EntityId);

            var ownerChanges = await provider.ReadChangesAsync(ownerId, Guid.NewGuid(), null,
                Guid.Empty, 0, 20, default);
            Assert.False(ownerChanges.IsError, ownerChanges.Message);
            Assert.Equal(2, ownerChanges.Result.Changes.Count);
        }
        finally
        {
            await client.DropDatabaseAsync(databaseName);
        }
    }

    [Theory]
    [InlineData(HyperDriveEntityTypes.Quest, HolonType.Quest)]
    [InlineData(HyperDriveEntityTypes.InventoryItem, HolonType.InventoryItem)]
    [InlineData(HyperDriveEntityTypes.GeoNft, HolonType.Web5GeoNFT)]
    [InlineData(HyperDriveEntityTypes.GeoNftCollection, HolonType.Web5GeoNFTCollection)]
    public async Task TypedHolonMutationUsesItsAuthoritativeDomainCodec(string entityType, HolonType holonType)
    {
        string connectionString = RequiredConnectionString();
        string databaseName = NewDatabaseName("typed_holon");
        var client = new MongoClient(connectionString);
        try
        {
            var provider = new NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS(
                connectionString, databaseName);
            var activation = await provider.ActivateProviderAsync();
            Assert.False(activation.IsError, activation.Message);
            var avatarId = Guid.NewGuid();
            var entityId = Guid.NewGuid();
            var mutation = new HostedSyncFanOutItem
            {
                OperationId = Guid.NewGuid(), AvatarId = avatarId, EntityId = entityId,
                EntityType = entityType, Kind = SyncOperationKind.Upsert, VersionId = Guid.NewGuid(),
                PayloadJson = $"{{\"holonId\":\"{entityId:D}\",\"holonType\":{(int)holonType},\"name\":\"typed\"}}"
            };

            var applied = await provider.ApplyDomainMutationAsync(mutation, default);
            Assert.False(applied.IsError, applied.Message);
            var stored = await client.GetDatabase(databaseName).GetCollection<BsonDocument>("Holon")
                .Find(Builders<BsonDocument>.Filter.Eq("HolonId", entityId)).SingleAsync();
            Assert.Equal((int)holonType, stored["HolonType"].AsInt32);

            mutation.OperationId = Guid.NewGuid();
            mutation.EntityId = Guid.NewGuid();
            mutation.VersionId = Guid.NewGuid();
            mutation.PayloadJson = $"{{\"holonId\":\"{mutation.EntityId:D}\",\"holonType\":{(int)HolonType.Game}}}";
            var mismatch = await provider.ApplyDomainMutationAsync(mutation, default);
            Assert.True(mismatch.IsError);
            Assert.Equal("HOSTED_DOMAIN_TYPE_MISMATCH", mismatch.ErrorCode);
        }
        finally
        {
            await client.DropDatabaseAsync(databaseName);
        }
    }

    [Fact]
    public async Task ExistingHolonsAreBackfilledIntoInitialEdgeChangeFeed()
    {
        string connectionString = RequiredConnectionString();
        string databaseName = NewDatabaseName("domain_backfill");
        var client = new MongoClient(connectionString);
        try
        {
            var provider = new NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS(
                connectionString, databaseName);
            var activation = await provider.ActivateProviderAsync();
            Assert.False(activation.IsError, activation.Message);
            var avatarId = Guid.NewGuid();
            var holonId = Guid.NewGuid();
            var versionId = Guid.NewGuid();
            var saved = await provider.SaveHolonAsync(new NextGenSoftware.OASIS.API.Core.Holons.Holon
            {
                Id = holonId, VersionId = versionId, Name = "pre-existing holon",
                CreatedByAvatarId = avatarId, CreatedDate = DateTime.UtcNow, IsNewHolon = true
            }, saveChildren: false);
            Assert.False(saved.IsError, saved.Message);

            var backfill = await provider.BackfillDomainStateAsync(10, default);
            Assert.False(backfill.IsError, backfill.Message);
            Assert.True(backfill.Result.CaptureInitialized);
            Assert.Equal(1, backfill.Result.ProjectedCount);

            var changes = await provider.ReadChangesAsync(avatarId, Guid.NewGuid(), null, Guid.Empty,
                0, 20, default);
            Assert.False(changes.IsError, changes.Message);
            var change = Assert.Single(changes.Result.Changes);
            Assert.Equal(holonId, change.EntityId);
            Assert.Equal(versionId, change.VersionId);
            Assert.Equal("backfill:", change.ChangeId.Substring(0, "backfill:".Length));
        }
        finally
        {
            await client.DropDatabaseAsync(databaseName);
        }
    }

    [Fact]
    public async Task OrdinaryHolonSaveIsCapturedExactlyOnceForItsOwningAvatar()
    {
        string connectionString = RequiredConnectionString();
        string databaseName = NewDatabaseName("domain_capture");
        var client = new MongoClient(connectionString);
        var database = client.GetDatabase(databaseName);
        try
        {
            var provider = new NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS(
                connectionString, databaseName);
            var activation = await provider.ActivateProviderAsync();
            Assert.False(activation.IsError, activation.Message);
            var avatarId = Guid.NewGuid();
            var holonId = Guid.NewGuid();
            var versionId = Guid.NewGuid();
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            var captureTask = provider.CaptureNextDomainChangesAsync(20, TimeSpan.FromSeconds(10), timeout.Token);
            await WaitForDocumentAsync(database, "HyperDriveDomainCaptureState", timeout.Token);

            var saved = await provider.SaveHolonAsync(
                new NextGenSoftware.OASIS.API.Core.Holons.Holon
                {
                    Id = holonId,
                    VersionId = versionId,
                    Name = "REST-created holon",
                    CreatedByAvatarId = avatarId,
                    CreatedDate = DateTime.UtcNow,
                    IsNewHolon = true
                }, saveChildren: false);
            Assert.False(saved.IsError, saved.Message);

            var capture = await captureTask;
            Assert.False(capture.IsError, capture.Message);
            Assert.Equal(1, capture.Result.CapturedCount);
            Assert.Equal(0, capture.Result.RejectedCount);

            var changes = await provider.ReadChangesAsync(avatarId, Guid.NewGuid(), null, Guid.Empty,
                0, 20, timeout.Token);
            Assert.False(changes.IsError, changes.Message);
            var change = Assert.Single(changes.Result.Changes);
            Assert.Equal(holonId, change.EntityId);
            Assert.Equal(versionId, change.VersionId);
            Assert.Equal(HyperDriveEntityTypes.Holon, change.EntityType);
            Assert.Equal(1, await database.GetCollection<BsonDocument>("HyperDriveSyncChanges")
                .CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
        }
        finally
        {
            await client.DropDatabaseAsync(databaseName);
        }
    }

    [Fact]
    public async Task AuthoritativeDomainMutationReplayIsIdempotentAndRejectsOperationIdReuse()
    {
        string connectionString = RequiredConnectionString();
        string databaseName = NewDatabaseName("domain_replay");
        var client = new MongoClient(connectionString);
        try
        {
            var provider = new NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS(
                connectionString, databaseName);
            var mutation = new HostedSyncFanOutItem
            {
                OperationId = Guid.NewGuid(), AvatarId = Guid.NewGuid(), EntityId = Guid.NewGuid(),
                EntityType = HyperDriveEntityTypes.Holon, Kind = SyncOperationKind.Upsert,
                VersionId = Guid.NewGuid(), PayloadJson = "{\"name\":\"offline holon\"}"
            };

            var first = await provider.ApplyDomainMutationAsync(mutation, default);
            Assert.False(first.IsError, first.Message);
            Assert.False(first.Result.AlreadyApplied);

            var replay = await provider.ApplyDomainMutationAsync(mutation, default);
            Assert.False(replay.IsError, replay.Message);
            Assert.True(replay.Result.AlreadyApplied);
            Assert.Equal(1, await client.GetDatabase(databaseName).GetCollection<BsonDocument>("Holon")
                .CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));

            mutation.PayloadJson = "{\"name\":\"different content\"}";
            var mismatch = await provider.ApplyDomainMutationAsync(mutation, default);
            Assert.True(mismatch.IsError);
            Assert.Equal("HOSTED_DOMAIN_OPERATION_REUSE_MISMATCH", mismatch.ErrorCode);
        }
        finally
        {
            await client.DropDatabaseAsync(databaseName);
        }
    }

    [Fact]
    [Trait("Category", "ExternalPrimaryTermination")]
    public async Task AcknowledgedOperationRemainsIdempotentAfterPrimaryProcessTermination()
    {
        string coordinationDirectory = Environment.GetEnvironmentVariable(
            "OASIS_MONGO_PROCESS_KILL_COORDINATION_DIRECTORY") ??
            throw new InvalidOperationException(
                "OASIS_MONGO_PROCESS_KILL_COORDINATION_DIRECTORY is required for the externally coordinated primary-termination test.");
        Directory.CreateDirectory(coordinationDirectory);
        string readyPath = Path.Combine(coordinationDirectory, "ready");
        string continuePath = Path.Combine(coordinationDirectory, "continue");
        File.Delete(readyPath);
        File.Delete(continuePath);

        string connectionString = RequiredConnectionString();
        string databaseName = NewDatabaseName("process_loss");
        var avatarId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();
        var operation = CreateOperation(avatarId, deviceId, "{\"processLoss\":true}");
        var client = new MongoClient(connectionString);
        try
        {
            var provider = new NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS(
                connectionString, databaseName);
            var first = await provider.ApplyOperationsAsync(avatarId, deviceId, new[] { operation }, default);
            Assert.False(first.IsError, first.Message);
            await AssertCommittedShapeAsync(client.GetDatabase(databaseName));

            await File.WriteAllTextAsync(readyPath, operation.OperationId.ToString("D"));
            await WaitForFileAsync(continuePath, TimeSpan.FromSeconds(90));
            await WaitForWritablePrimaryAsync(client, TimeSpan.FromSeconds(60));

            var replay = await provider.ApplyOperationsAsync(avatarId, deviceId, new[] { operation }, default);
            Assert.False(replay.IsError, replay.Message);
            Assert.Single(replay.Result.OperationResults);
            await AssertCommittedShapeAsync(client.GetDatabase(databaseName));
        }
        finally
        {
            await client.DropDatabaseAsync(databaseName);
        }
    }

    [Fact]
    public async Task AcknowledgedOperationRemainsIdempotentAcrossPrimaryElection()
    {
        string connectionString = RequiredConnectionString();
        string databaseName = NewDatabaseName("election");
        var avatarId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();
        var operation = CreateOperation(avatarId, deviceId, "{\"election\":true}");
        var client = new MongoClient(connectionString);
        try
        {
            var provider = new NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS(
                connectionString, databaseName);
            var first = await provider.ApplyOperationsAsync(avatarId, deviceId, new[] { operation }, default);
            Assert.False(first.IsError, first.Message);

            try
            {
                await client.GetDatabase("admin").RunCommandAsync<BsonDocument>(new BsonDocument
                {
                    { "replSetStepDown", 10 }, { "force", true }
                });
            }
            catch (MongoException)
            {
                // The old primary is allowed to close the command connection while stepping down.
            }
            await WaitForWritablePrimaryAsync(client, TimeSpan.FromSeconds(60));

            var replay = await provider.ApplyOperationsAsync(avatarId, deviceId, new[] { operation }, default);
            Assert.False(replay.IsError, replay.Message);
            Assert.Single(replay.Result.OperationResults);
            await AssertCommittedShapeAsync(client.GetDatabase(databaseName));
        }
        finally
        {
            await client.DropDatabaseAsync(databaseName);
        }
    }

    [Theory]
    [InlineData(HostedMongoSyncTransactionBoundary.DomainMutationWritten)]
    [InlineData(HostedMongoSyncTransactionBoundary.EntityWritten)]
    [InlineData(HostedMongoSyncTransactionBoundary.ChangeFeedWritten)]
    [InlineData(HostedMongoSyncTransactionBoundary.FanOutWritten)]
    [InlineData(HostedMongoSyncTransactionBoundary.OperationResultWritten)]
    [InlineData(HostedMongoSyncTransactionBoundary.DeviceSequenceWritten)]
    [InlineData(HostedMongoSyncTransactionBoundary.BatchReadyToCommit)]
    public async Task InjectedFailureRollsBackEveryHostedSyncDocument(
        HostedMongoSyncTransactionBoundary boundary)
    {
        string connectionString = RequiredConnectionString();
        string databaseName = NewDatabaseName("fault");
        var avatarId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();
        var operation = CreateOperation(avatarId, deviceId, "{\"hosted\":true}");
        var client = new MongoClient(connectionString);
        try
        {
            var failing = new NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS(
                connectionString, databaseName, new ThrowAtBoundary(boundary));
            var failed = await failing.ApplyOperationsAsync(avatarId, deviceId, new[] { operation }, default);
            Assert.True(failed.IsError);
            Assert.Equal("MONGO_HOSTED_SYNC_APPLY_FAILED", failed.ErrorCode);
            await AssertAllTransactionalCollectionsEmptyAsync(client.GetDatabase(databaseName));

            var retrying = new NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS(
                connectionString, databaseName);
            var retry = await retrying.ApplyOperationsAsync(avatarId, deviceId, new[] { operation }, default);
            Assert.False(retry.IsError, retry.Message);
            Assert.Equal(SyncOperationDisposition.Accepted, Assert.Single(retry.Result.OperationResults).Disposition);
            await AssertCommittedShapeAsync(client.GetDatabase(databaseName));
        }
        finally
        {
            await client.DropDatabaseAsync(databaseName);
        }
    }

    [Fact]
    public async Task CommandIsQueuedIdempotentlyAndClaimedInDeviceOrder()
    {
        string connectionString = RequiredConnectionString();
        string databaseName = NewDatabaseName("commands");
        var avatarId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();
        var first = CreateCommand(avatarId, deviceId, 1);
        var second = CreateCommand(avatarId, deviceId, 2);
        var client = new MongoClient(connectionString);
        try
        {
            var provider = new NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS(connectionString, databaseName);
            var applied = await provider.ApplyOperationsAsync(avatarId, deviceId, new[] { first, second }, default);
            Assert.False(applied.IsError, applied.Message);
            Assert.All(applied.Result.OperationResults, x => Assert.Equal("COMMAND_QUEUED", x.Code));
            var replay = await provider.ApplyOperationsAsync(avatarId, deviceId, new[] { first, second }, default);
            Assert.False(replay.IsError, replay.Message);
            Assert.All(replay.Result.OperationResults, x => Assert.Equal(SyncOperationDisposition.AlreadyApplied, x.Disposition));

            var firstClaim = await provider.ClaimCommandsAsync("worker-a", 2,
                DateTime.UtcNow.AddMinutes(1), default);
            Assert.False(firstClaim.IsError, firstClaim.Message);
            var firstClaimed = Assert.Single(firstClaim.Result.Items);
            Assert.Equal(1, firstClaimed.DeviceSequence);

            var completed = await provider.CompleteCommandAsync(firstClaimed.OperationId, "worker-a",
                new HyperDriveCommandOutcome
                {
                    OperationId = firstClaimed.OperationId,
                    Succeeded = true,
                    Code = "COMMAND_COMPLETED",
                    Message = "first command completed",
                    ResultJson = "{}",
                    CompletedUtc = DateTime.UtcNow
                }, default);
            Assert.False(completed.IsError, completed.Message);

            var secondClaim = await provider.ClaimCommandsAsync("worker-a", 2,
                DateTime.UtcNow.AddMinutes(1), default);
            Assert.False(secondClaim.IsError, secondClaim.Message);
            var secondClaimed = Assert.Single(secondClaim.Result.Items);
            Assert.Equal(2, secondClaimed.DeviceSequence);
            Assert.True(firstClaimed.CommandSequence < secondClaimed.CommandSequence);
        }
        finally { await client.DropDatabaseAsync(databaseName); }
    }

    [Fact]
    public async Task DeferredHeadCommandBlocksLaterCommandsAndExpiredProcessingIsReclaimed()
    {
        string connectionString = RequiredConnectionString();
        string databaseName = NewDatabaseName("command_order");
        var avatarId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();
        var client = new MongoClient(connectionString);
        try
        {
            var provider = new NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS(connectionString, databaseName);
            await provider.ApplyOperationsAsync(avatarId, deviceId,
                new[] { CreateCommand(avatarId, deviceId, 1), CreateCommand(avatarId, deviceId, 2) }, default);
            var first = await provider.ClaimCommandsAsync("worker-a", 1, DateTime.UtcNow.AddMilliseconds(200), default);
            Assert.Single(first.Result.Items);
            await Task.Delay(300);
            var reclaimed = await provider.ClaimCommandsAsync("worker-b", 1, DateTime.UtcNow.AddMinutes(1), default);
            Assert.Single(reclaimed.Result.Items);
            Assert.Equal(1, reclaimed.Result.Items[0].DeviceSequence);
            var deferred = await provider.FailCommandAttemptAsync(reclaimed.Result.Items[0].OperationId,
                "worker-b", "TRANSIENT", "retry", DateTime.UtcNow.AddMinutes(1), default);
            Assert.False(deferred.IsError, deferred.Message);
            var blocked = await provider.ClaimCommandsAsync("worker-b", 1, DateTime.UtcNow.AddMinutes(1), default);
            Assert.True(blocked.Result.LeaseAcquired);
            Assert.Empty(blocked.Result.Items);
        }
        finally { await client.DropDatabaseAsync(databaseName); }
    }

    [Fact]
    public async Task CommandCompletionPublishesPrivateResultAndRejectsLostLease()
    {
        string connectionString = RequiredConnectionString();
        string databaseName = NewDatabaseName("command_result");
        var avatarId = Guid.NewGuid();
        var otherAvatarId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();
        var operation = CreateCommand(avatarId, deviceId, 1);
        var client = new MongoClient(connectionString);
        try
        {
            var provider = new NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS(connectionString, databaseName);
            await provider.ApplyOperationsAsync(avatarId, deviceId, new[] { operation }, default);
            var claim = await provider.ClaimCommandsAsync("worker-a", 1, DateTime.UtcNow.AddMinutes(1), default);
            var wrongRenewal = await provider.RenewCommandLeaseAsync(operation.OperationId, "worker-b",
                DateTime.UtcNow.AddMinutes(2), default);
            Assert.True(wrongRenewal.IsError);
            var renewal = await provider.RenewCommandLeaseAsync(operation.OperationId, "worker-a",
                DateTime.UtcNow.AddMinutes(2), default);
            Assert.False(renewal.IsError, renewal.Message);
            var outcome = new HyperDriveCommandOutcome
            {
                OperationId = operation.OperationId, Succeeded = true, Code = "COMMAND_COMPLETED",
                Message = "done", ResultJson = "{\"ok\":true}", CompletedUtc = DateTime.UtcNow
            };
            var lost = await provider.CompleteCommandAsync(operation.OperationId, "worker-b", outcome, default);
            Assert.True(lost.IsError);
            var completed = await provider.CompleteCommandAsync(operation.OperationId, "worker-a", outcome, default);
            Assert.False(completed.IsError, completed.Message);
            var ownerFeed = await provider.ReadChangesAsync(avatarId, deviceId, null, Guid.Empty, 0, 100, default);
            Assert.Contains(ownerFeed.Result.Changes, x => x.EntityType == HyperDriveEntityTypes.CommandResult &&
                x.EntityId == operation.OperationId);
            var otherFeed = await provider.ReadChangesAsync(otherAvatarId, Guid.NewGuid(), null, Guid.Empty, 0, 100, default);
            Assert.DoesNotContain(otherFeed.Result.Changes, x => x.EntityType == HyperDriveEntityTypes.CommandResult);
        }
        finally { await client.DropDatabaseAsync(databaseName); }
    }

    [Fact]
    public async Task AvatarBackfillPublishesOnlyPrivateWhitelistedEdgeProjections()
    {
        string connectionString = RequiredConnectionString();
        string databaseName = NewDatabaseName("avatar_projection");
        var ownerId = Guid.NewGuid();
        var client = new MongoClient(connectionString);
        var database = client.GetDatabase(databaseName);
        try
        {
            await database.GetCollection<NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities.Avatar>("Avatar")
                .InsertOneAsync(new NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities.Avatar
                {
                    Id = ObjectId.GenerateNewId().ToString(), HolonId = ownerId, VersionId = Guid.NewGuid(),
                    Username = "edge-owner", FirstName = "Edge", LastName = "Owner",
                    Password = "must-never-sync", JwtToken = "must-never-sync",
                    RefreshToken = "must-never-sync", VerificationToken = "must-never-sync",
                    CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow
                });
            await database.GetCollection<NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities.AvatarDetail>("AvatarDetail")
                .InsertOneAsync(new NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities.AvatarDetail
                {
                    Id = ObjectId.GenerateNewId().ToString(), HolonId = ownerId, VersionId = Guid.NewGuid(),
                    Username = "edge-owner", Email = "private@example.test", Karma = 12, XP = 34,
                    ActiveQuestId = Guid.NewGuid(), ActiveObjectiveId = Guid.NewGuid(),
                    CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow,
                    Inventory = new[] { new NextGenSoftware.OASIS.API.Core.Objects.InventoryItem
                        { Id = Guid.NewGuid(), Name = "Anorak", Quantity = 1 } }
                });

            var provider = new NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS(
                connectionString, databaseName);
            var backfill = await provider.BackfillDomainStateAsync(10, default);
            Assert.False(backfill.IsError, backfill.Message);
            Assert.True(backfill.Result.CaptureInitialized);
            Assert.Equal(2, backfill.Result.ProjectedCount);

            var ownerFeed = await provider.ReadChangesAsync(ownerId, Guid.NewGuid(), null, Guid.Empty, 0, 100, default);
            Assert.False(ownerFeed.IsError, ownerFeed.Message);
            var avatar = Assert.Single(ownerFeed.Result.Changes.Where(x => x.EntityType == HyperDriveEntityTypes.Avatar));
            var detail = Assert.Single(ownerFeed.Result.Changes.Where(x => x.EntityType == HyperDriveEntityTypes.AvatarDetail));
            using var avatarJson = JsonDocument.Parse(avatar.PayloadJson);
            using var detailJson = JsonDocument.Parse(detail.PayloadJson);
            Assert.Equal("edge-owner", avatarJson.RootElement.GetProperty("Username").GetString());
            Assert.False(avatarJson.RootElement.TryGetProperty("Password", out _));
            Assert.False(avatarJson.RootElement.TryGetProperty("JwtToken", out _));
            Assert.False(avatarJson.RootElement.TryGetProperty("RefreshToken", out _));
            Assert.False(avatarJson.RootElement.TryGetProperty("VerificationToken", out _));
            Assert.False(detailJson.RootElement.TryGetProperty("Email", out _));
            Assert.Equal(12, detailJson.RootElement.GetProperty("Karma").GetInt64());
            Assert.Single(detailJson.RootElement.GetProperty("Inventory").EnumerateArray());

            var otherFeed = await provider.ReadChangesAsync(Guid.NewGuid(), Guid.NewGuid(), null,
                Guid.Empty, 0, 100, default);
            Assert.DoesNotContain(otherFeed.Result.Changes, x => x.EntityType == HyperDriveEntityTypes.Avatar ||
                x.EntityType == HyperDriveEntityTypes.AvatarDetail);
        }
        finally { await client.DropDatabaseAsync(databaseName); }
    }

    [Fact]
    public async Task AvatarDetailChangeStreamResumesAndPublishesAuthoritativeGameplayState()
    {
        string connectionString = RequiredConnectionString();
        string databaseName = NewDatabaseName("avatar_cdc");
        var ownerId = Guid.NewGuid();
        var client = new MongoClient(connectionString);
        var database = client.GetDatabase(databaseName);
        try
        {
            var provider = new NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS(
                connectionString, databaseName);
            var initialized = await provider.BackfillDomainStateAsync(10, default);
            Assert.False(initialized.IsError, initialized.Message);
            var detail = new NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities.AvatarDetail
            {
                Id = ObjectId.GenerateNewId().ToString(), HolonId = ownerId, VersionId = Guid.NewGuid(),
                Username = "cdc-owner", XP = 10, CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow
            };
            var collection = database.GetCollection<NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities.AvatarDetail>(
                "AvatarDetail");
            await collection.InsertOneAsync(detail);
            var inserted = await provider.CaptureNextDomainChangesAsync(30, TimeSpan.FromSeconds(3), default);
            Assert.False(inserted.IsError, inserted.Message);
            Assert.Equal(1, inserted.Result.CapturedCount);

            Guid firstVersion = detail.VersionId;
            detail.PreviousVersionId = firstVersion;
            detail.VersionId = Guid.NewGuid();
            detail.XP = 42;
            detail.ModifiedDate = DateTime.UtcNow;
            await collection.ReplaceOneAsync(x => x.HolonId == ownerId, detail);
            var updated = await provider.CaptureNextDomainChangesAsync(30, TimeSpan.FromSeconds(3), default);
            Assert.False(updated.IsError, updated.Message);
            Assert.Equal(1, updated.Result.CapturedCount);

            var feed = await provider.ReadChangesAsync(ownerId, Guid.NewGuid(), null, Guid.Empty, 0, 100, default);
            var changes = feed.Result.Changes.Where(x => x.EntityType == HyperDriveEntityTypes.AvatarDetail).ToArray();
            Assert.Equal(2, changes.Length);
            Assert.Equal(firstVersion, changes[1].PreviousVersionId);
            using var payload = JsonDocument.Parse(changes[1].PayloadJson);
            Assert.Equal(42, payload.RootElement.GetProperty("Xp").GetInt32());
        }
        finally { await client.DropDatabaseAsync(databaseName); }
    }

    [Theory]
    [InlineData(HyperDriveEntityTypes.Avatar)]
    [InlineData(HyperDriveEntityTypes.AvatarDetail)]
    [InlineData(HyperDriveEntityTypes.QuestProgress)]
    public async Task ServerAuthoritativeProjectionRejectsDirectEdgeOverwrite(string entityType)
    {
        string connectionString = RequiredConnectionString();
        string databaseName = NewDatabaseName("authority");
        var avatarId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();
        var client = new MongoClient(connectionString);
        try
        {
            var provider = new NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS(connectionString, databaseName);
            var operation = CreateOperation(avatarId, deviceId, "{\"attempted\":true}");
            operation.EntityType = entityType;
            operation.EntityId = avatarId;
            var applied = await provider.ApplyOperationsAsync(avatarId, deviceId, new[] { operation }, default);
            Assert.False(applied.IsError, applied.Message);
            var rejected = Assert.Single(applied.Result.OperationResults);
            Assert.Equal(SyncOperationDisposition.Rejected, rejected.Disposition);
            Assert.Equal("HOSTED_DOMAIN_SERVER_AUTHORITATIVE", rejected.Code);
            Assert.Equal(0, await client.GetDatabase(databaseName).GetCollection<BsonDocument>(
                entityType == HyperDriveEntityTypes.Avatar ? "Avatar" : "AvatarDetail")
                .CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
        }
        finally { await client.DropDatabaseAsync(databaseName); }
    }

    private static string RequiredConnectionString() =>
        Environment.GetEnvironmentVariable("OASIS_MONGO_REPLICA_SET_CONNECTION") ??
        throw new InvalidOperationException("OASIS_MONGO_REPLICA_SET_CONNECTION is required; standalone MongoDB cannot prove transaction safety.");

    private static string NewDatabaseName(string scenario)
    {
        string name = $"hd_{scenario}_{Guid.NewGuid():N}";
        if (name.Length > 63)
            throw new ArgumentOutOfRangeException(nameof(scenario), scenario,
                "MongoDB database names cannot exceed 63 characters.");
        return name;
    }

    private static SyncOperation CreateOperation(Guid avatarId, Guid deviceId, string payloadJson) => new()
    {
        OperationId = Guid.NewGuid(), AvatarId = avatarId, DeviceId = deviceId, DeviceSequence = 1,
        EntityId = Guid.NewGuid(), EntityType = HyperDriveEntityTypes.Holon, Kind = SyncOperationKind.Upsert,
        BaseVersionId = Guid.Empty, VersionId = Guid.NewGuid(), PayloadJson = payloadJson,
        CreatedUtc = DateTime.UtcNow
    };

    private static SyncOperation CreateCommand(Guid avatarId, Guid deviceId, long deviceSequence) => new()
    {
        OperationId = Guid.NewGuid(), AvatarId = avatarId, DeviceId = deviceId,
        DeviceSequence = deviceSequence, EntityId = Guid.NewGuid(),
        EntityType = HyperDriveEntityTypes.QuestProgress, Kind = SyncOperationKind.Command,
        VersionId = Guid.NewGuid(), PayloadJson = "{}", CreatedUtc = DateTime.UtcNow
    };

    private static async Task WaitForFileAsync(string path, TimeSpan timeout)
    {
        DateTime deadline = DateTime.UtcNow.Add(timeout);
        while (DateTime.UtcNow < deadline)
        {
            if (File.Exists(path)) return;
            await Task.Delay(250);
        }
        throw new TimeoutException($"External MongoDB fault coordinator did not create '{path}' within the integration-test deadline.");
    }

    private static async Task WaitForDocumentAsync(IMongoDatabase database, string collection,
        CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            if (await database.GetCollection<BsonDocument>(collection)
                .CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty, cancellationToken: cancellationToken) > 0)
                return;
            await Task.Delay(25, cancellationToken);
        }
        cancellationToken.ThrowIfCancellationRequested();
    }

    private static async Task WaitForWritablePrimaryAsync(MongoClient client, TimeSpan timeout)
    {
        DateTime deadline = DateTime.UtcNow.Add(timeout);
        Exception? lastError = null;
        while (DateTime.UtcNow < deadline)
        {
            try
            {
                var hello = await client.GetDatabase("admin").WithReadPreference(ReadPreference.Primary)
                    .RunCommandAsync<BsonDocument>(new BsonDocument("hello", 1));
                if (hello.TryGetValue("isWritablePrimary", out var writable) && writable.ToBoolean()) return;
            }
            catch (MongoException ex) { lastError = ex; }
            await Task.Delay(250);
        }
        throw new TimeoutException("The MongoDB replica set did not elect a writable primary within the integration-test deadline.", lastError);
    }

    private static async Task AssertAllTransactionalCollectionsEmptyAsync(IMongoDatabase database)
    {
        foreach (string collection in new[]
        {
            "Holon",
            "HyperDriveDomainMutationReceipts",
            "HyperDriveSyncEntities", "HyperDriveSyncChanges", "HyperDriveSyncFanOut",
            "HyperDriveSyncOperations", "HyperDriveSyncDevices", "HyperDriveSyncCounters"
        })
            Assert.Equal(0, await database.GetCollection<BsonDocument>(collection)
                .CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
    }

    private static async Task AssertCommittedShapeAsync(IMongoDatabase database)
    {
        foreach (string collection in new[]
        {
            "Holon",
            "HyperDriveDomainMutationReceipts",
            "HyperDriveSyncEntities", "HyperDriveSyncChanges", "HyperDriveSyncFanOut",
            "HyperDriveSyncOperations", "HyperDriveSyncDevices", "HyperDriveSyncCounters"
        })
            Assert.Equal(1, await database.GetCollection<BsonDocument>(collection)
                .CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
    }

    private sealed class ThrowAtBoundary : IHostedMongoSyncFaultInjector
    {
        private readonly HostedMongoSyncTransactionBoundary _boundary;
        public ThrowAtBoundary(HostedMongoSyncTransactionBoundary boundary) => _boundary = boundary;
        public Task OnBoundaryAsync(HostedMongoSyncTransactionBoundary boundary, CancellationToken cancellationToken)
        {
            if (boundary == _boundary) throw new InvalidOperationException($"Injected failure at {boundary}.");
            return Task.CompletedTask;
        }
    }
}
