using System;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.Synchronization;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.MongoOASIS.IntegrationTests;

public sealed class HostedMongoConcurrentTransactionTests
{
    [Fact]
    public async Task ConcurrentWritersRetrySharedChangeSequenceTransactions()
    {
        string connectionString = Environment.GetEnvironmentVariable("OASIS_MONGO_REPLICA_SET_CONNECTION_STRING")
            ?? throw new InvalidOperationException("OASIS_MONGO_REPLICA_SET_CONNECTION_STRING is required.");
        string databaseName = "oasis_sync_concurrent_" + Guid.NewGuid().ToString("N")[..12];
        var client = new MongoClient(connectionString);
        try
        {
            var provider = new NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS(
                connectionString, databaseName);
            var avatarId = Guid.NewGuid();
            var tasks = Enumerable.Range(0, 32).Select(index =>
            {
                var deviceId = Guid.NewGuid();
                var entityId = Guid.NewGuid();
                return provider.ApplyOperationsAsync(avatarId, deviceId, new[]
                {
                    new SyncOperation
                    {
                        OperationId = Guid.NewGuid(), DeviceId = deviceId, AvatarId = avatarId,
                        DeviceSequence = 1, EntityId = entityId, EntityType = "concurrency-probe",
                        Kind = SyncOperationKind.Upsert, BaseVersionId = Guid.Empty,
                        VersionId = Guid.NewGuid(), PayloadJson = $"{{\"index\":{index}}}",
                        CreatedUtc = DateTime.UtcNow
                    }
                }, default);
            }).ToArray();

            var results = await Task.WhenAll(tasks);
            Assert.All(results, result => Assert.False(result.IsError,
                result.ErrorCode + ": " + result.Message + " " + result.Exception));

            var database = client.GetDatabase(databaseName);
            Assert.Equal(32, await database.GetCollection<MongoDB.Bson.BsonDocument>("HyperDriveSyncChanges")
                .CountDocumentsAsync(FilterDefinition<MongoDB.Bson.BsonDocument>.Empty));
            Assert.Equal(32, await database.GetCollection<MongoDB.Bson.BsonDocument>("HyperDriveSyncOperations")
                .CountDocumentsAsync(FilterDefinition<MongoDB.Bson.BsonDocument>.Empty));
        }
        finally
        {
            await client.DropDatabaseAsync(databaseName);
        }
    }
}
