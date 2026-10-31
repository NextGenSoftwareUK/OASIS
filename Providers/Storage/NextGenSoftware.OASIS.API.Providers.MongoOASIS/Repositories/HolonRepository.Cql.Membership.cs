using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Repositories;

public partial class HolonRepository
{
    internal async Task UpgradeCqlMembershipAsync()
    {
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var records = _dbContext.MongoDB.GetCollection<BsonDocument>("Holon");
        var version = new BsonDocument("$ifNull", new BsonArray { "$CosmicCqlMembership.schema", 0 });
        var filter = new BsonDocument("$expr", new BsonDocument("$ne", new BsonArray { version, MongoCqlMembership.SchemaVersion }));
        using var cursor = await records.FindAsync(filter, new FindOptions<BsonDocument>
        { BatchSize = 64, MaxTime = TimeSpan.FromSeconds(30) }, deadline.Token).ConfigureAwait(false);
        while (await cursor.MoveNextAsync(deadline.Token).ConfigureAwait(false))
            foreach (var observed in cursor.Current)
            {
                var schema = observed.GetValue(MongoCqlMembership.Field, BsonNull.Value);
                if (!schema.IsBsonNull)
                {
                    if (!schema.IsBsonDocument || !schema.AsBsonDocument.TryGetValue("schema", out var value) ||
                        !value.IsInt32 || value.AsInt32 != 0)
                        throw new InvalidDataException("Unsupported Mongo CQL membership projection schema.");
                }
                var canonical = BsonSerializer.Deserialize<Entities.Holon>(observed);
                var projection = MongoCqlMembership.Build(canonical.HolonId, canonical.ChildRoleManifestJson);
                // A concurrent save must not receive membership from an older observation.
                var condition = new BsonDocument { { "_id", observed["_id"] },
                    { "$expr", new BsonDocument("$eq", new BsonArray { "$$ROOT", new BsonDocument("$literal", observed) }) } };
                var updated = await records.UpdateOneAsync(condition,
                    new BsonDocument("$set", new BsonDocument(MongoCqlMembership.Field, projection)),
                    cancellationToken: deadline.Token).ConfigureAwait(false);
                if (updated.MatchedCount != 1)
                    throw new InvalidDataException("Mongo record changed during CQL membership upgrade; activation was not published.");
            }
    }

    private async Task ValidateCqlMembershipAsync(IClientSessionHandle session,
        Func<AggregateOptions> options, CancellationToken cancellation)
    {
        var pipeline = new[] { new BsonDocument("$match", new BsonDocument("$expr",
            new BsonDocument("$not", new BsonArray { MongoCqlMembership.ValidObservation() }))),
            new BsonDocument("$limit", 1), new BsonDocument("$project", new BsonDocument("_id", 1)) };
        using var cursor = await _dbContext.Holon.AggregateAsync<BsonDocument>(session, pipeline,
            options(), cancellation).ConfigureAwait(false);
        if (await cursor.MoveNextAsync(cancellation).ConfigureAwait(false) && System.Linq.Enumerable.Any(cursor.Current))
            throw new InvalidDataException("Mongo CQL membership projection is missing, stale or invalid; activate a compatible provider to upgrade older records.");
    }
}
