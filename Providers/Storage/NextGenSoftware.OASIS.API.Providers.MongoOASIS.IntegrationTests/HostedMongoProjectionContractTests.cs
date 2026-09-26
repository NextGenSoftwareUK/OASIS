using System;
using System.Text.Json;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.Synchronization;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.MongoOASIS.IntegrationTests;

public sealed class HostedMongoProjectionContractTests
{
    [Fact]
    public async Task HostedInventoryChangesAlwaysUseTheEdgeInventoryContract()
    {
        string connectionString = RequiredConnectionString();
        string databaseName = $"hd_projection_{Guid.NewGuid():N}"[..38];
        var client = new MongoClient(connectionString);
        var avatarId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();
        try
        {
            var provider = new NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS(connectionString, databaseName);
            var activated = await provider.ActivateProviderAsync();
            Assert.False(activated.IsError, activated.Message);

            var definitionId = Guid.NewGuid();
            await client.GetDatabase(databaseName).GetCollection<Holon>("Holon").InsertOneAsync(new Holon
            {
                Id = ObjectId.GenerateNewId().ToString(), HolonId = definitionId,
                HolonType = HolonType.InventoryItem, Name = "Projected inventory definition",
                Description = "Edge contract probe", CreatedByAvatarId = avatarId.ToString("D"),
                VersionId = Guid.NewGuid(), CreatedDate = DateTime.UtcNow
            });
            var backfill = await provider.BackfillDomainStateAsync(100, default);
            Assert.False(backfill.IsError, $"{backfill.ErrorCode}: {backfill.Message} {backfill.Exception}");

            var entityId = Guid.NewGuid();
            var operation = new SyncOperation
            {
                OperationId = Guid.NewGuid(), AvatarId = avatarId, DeviceId = deviceId, DeviceSequence = 1,
                EntityId = entityId, EntityType = HyperDriveEntityTypes.InventoryItem,
                Kind = SyncOperationKind.Command, VersionId = Guid.NewGuid(), PayloadJson = "{}",
                CreatedUtc = DateTime.UtcNow
            };
            var applied = await provider.ApplyOperationsAsync(avatarId, deviceId, new[] { operation }, default);
            Assert.False(applied.IsError, applied.Message);
            var claim = await provider.ClaimCommandsAsync("projection-worker", 1, DateTime.UtcNow.AddMinutes(1), default);
            Assert.False(claim.IsError, claim.Message);
            Assert.Single(claim.Result.Items);
            var completed = await provider.CompleteCommandAsync(operation.OperationId, "projection-worker",
                new HyperDriveCommandOutcome
                {
                    OperationId = operation.OperationId, Succeeded = true, Code = "COMMAND_COMPLETED",
                    Message = "done", ResultJson = JsonSerializer.Serialize(new HyperDriveInventoryItemProjection
                    { Id = entityId, Name = "Command item", Quantity = 2 }), CompletedUtc = DateTime.UtcNow
                }, default);
            Assert.False(completed.IsError, completed.Message);

            var feed = await provider.ReadChangesAsync(avatarId, deviceId, null, Guid.Empty, 0, 100, default);
            Assert.False(feed.IsError, feed.Message);
            Assert.Contains(feed.Result.Changes, x => x.EntityType == HyperDriveEntityTypes.CommandResult &&
                x.EntityId == operation.OperationId);
            var definition = Assert.Single(feed.Result.Changes, x =>
                x.EntityType == HyperDriveEntityTypes.InventoryItem && x.EntityId == definitionId);
            var commandEntity = Assert.Single(feed.Result.Changes, x =>
                x.EntityType == HyperDriveEntityTypes.InventoryItem && x.EntityId == entityId);
            Assert.Equal(definitionId,
                JsonSerializer.Deserialize<HyperDriveInventoryItemProjection>(definition.PayloadJson)!.Id);
            Assert.Equal(entityId,
                JsonSerializer.Deserialize<HyperDriveInventoryItemProjection>(commandEntity.PayloadJson)!.Id);
        }
        finally { await client.DropDatabaseAsync(databaseName); }
    }

    private static string RequiredConnectionString() =>
        Environment.GetEnvironmentVariable("OASIS_MONGO_REPLICA_SET_CONNECTION") ??
        throw new InvalidOperationException("OASIS_MONGO_REPLICA_SET_CONNECTION is required.");
}
