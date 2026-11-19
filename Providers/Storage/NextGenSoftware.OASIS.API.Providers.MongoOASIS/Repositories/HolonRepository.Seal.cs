using System;
using NextGenSoftware.OASIS.Common;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Repositories;

public partial class HolonRepository
{
    /// <summary>The stored document exactly as persisted (current version plus its embedded archive), for seal verification. Read only.</summary>
    internal async Task<BsonDocument> GetRawHolonDocumentAsync(Guid holonId)
    {
        var typed = _dbContext.Holon;
        var raw = typed.Database.GetCollection<BsonDocument>(typed.CollectionNamespace.CollectionName, typed.Settings);
        var rendered = Builders<Holon>.Filter.Where(x => x.HolonId == holonId)
            .Render(new RenderArgs<Holon>(typed.DocumentSerializer, typed.Settings.SerializerRegistry));
        var rows = await raw.Find(rendered).Limit(2).ToListAsync().ConfigureAwait(false);
        if (rows.Count > 1) throw new StorageIdentityAmbiguousException();
        return rows.SingleOrDefault();
    }
}
