using System;
using System.IO;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using NextGenSoftware.OASIS.API.Core.Serialization;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.UnitTests")]

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;

// Provider-owned query state. Never accept a projection supplied by metadata or
// copy an older observation over the manifest in the final replacement record.
internal static class MongoCqlMembership
{
    internal const string Field = "CosmicCqlMembership";
    internal const int SchemaVersion = 1;

    internal static BsonValue ValidObservation()
    {
        var root = "$" + Field + ".";
        BsonValue Equal(BsonValue left, BsonValue right) => new BsonDocument("$eq", new BsonArray { left, right });
        var source = new BsonDocument("$ifNull", new BsonArray { "$ChildRoleManifestJson", BsonNull.Value });
        return new BsonDocument("$and", new BsonArray
        {
            Equal(root + "schema", SchemaVersion),
            Equal(root + "owner", new BsonDocument("$toLower", "$HolonId")),
            Equal(root + "source", source),
            Equal(root + "declared", new BsonDocument("$ne", new BsonArray { source, BsonNull.Value })),
            new BsonDocument("$isArray", root + "members")
        });
    }

    internal static BsonDocument Build(Guid ownerId, string manifest)
    {
        try
        {
            var members = Array.Empty<Guid>();
            if (manifest != null)
            {
                var observed = HolonChildRoleManifestJson.Deserialize(manifest);
                if (observed.ParentId != ownerId)
                    throw new InvalidDataException("Mongo CQL membership manifest belongs to another holon.");
                members = observed.Roles.SelectMany(role => role.ChildIds).ToArray();
            }

            return new BsonDocument
            {
                { "schema", SchemaVersion },
                { "owner", ownerId.ToString("D") },
                // Exact source observation permits stale-projection detection.
                { "source", manifest == null ? BsonNull.Value : new BsonString(manifest) },
                { "declared", manifest != null },
                { "members", new BsonArray(members.Select(id => new BsonString(id.ToString("D")))) }
            };
        }
        catch (InvalidDataException cause)
        {
            throw new BsonSerializationException("Invalid canonical Mongo CQL membership manifest.", cause);
        }
    }
}
