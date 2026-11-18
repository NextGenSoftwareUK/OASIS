using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities;
using NextGenSoftware.OASIS.Common;
using Holon = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities.Holon;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Repositories;

public partial class HolonRepository
{
    private IMongoCollection<BsonDocument> RawHolons()
    {
        var typed = _dbContext.Holon;
        return typed.Database.GetCollection<BsonDocument>(typed.CollectionNamespace.CollectionName, typed.Settings);
    }

    // A holon is one document: the current version plus the embedded archive of every earlier version.
    internal static int CountStoredVersions(BsonDocument document)
    {
        var count = 1;
        if (document != null && document.TryGetValue(MongoNativeHolonHistory.Field, out var archive) && archive.IsBsonDocument
            && archive.AsBsonDocument.TryGetValue("records", out var records) && records.IsBsonArray)
            count += records.AsBsonArray.Count;
        return count;
    }

    /// <summary>Reads the stored record whether or not it is deleted (the ordinary delete only finds live holons). Read only; one indexed lookup by holon id.</summary>
    internal async Task<HolonPurgeSubject> GetPurgeSubjectAsync(Guid holonId, CancellationToken cancellationToken)
    {
        var filter = Builders<Holon>.Filter.Where(x => x.HolonId == holonId)
            .Render(new RenderArgs<Holon>(_dbContext.Holon.DocumentSerializer, _dbContext.Holon.Settings.SerializerRegistry));
        var rows = await RawHolons().Find(filter).Limit(2).ToListAsync(cancellationToken).ConfigureAwait(false);
        if (rows.Count > 1) throw new StorageIdentityAmbiguousException();
        if (rows.Count == 0) return new HolonPurgeSubject { Id = holonId, Exists = false };
        var holon = Helpers.DataHelper.ConvertMongoEntityToOASISHolon(BsonSerializer.Deserialize<Holon>(rows[0]), false);
        return new HolonPurgeSubject
        {
            Id = holonId, Exists = true, Holon = holon, CreatedByAvatarId = holon.CreatedByAvatarId,
            IsDeleted = holon.DeletedDate != DateTime.MinValue, DeletedDate = holon.DeletedDate,
            VersionCount = CountStoredVersions(rows[0])
        };
    }

    /// <summary>Permanently removes the holon's whole document: current version and the whole archive, with majority + journal write concern.</summary>
    internal async Task<HolonPurgeStoreOutcome> PurgeHolonAsync(Guid holonId, CancellationToken cancellationToken)
    {
        var raw = RawHolons().WithWriteConcern(WriteConcern.WMajority.With(journal: true));
        var filter = Builders<Holon>.Filter.Where(x => x.HolonId == holonId)
            .Render(new RenderArgs<Holon>(_dbContext.Holon.DocumentSerializer, _dbContext.Holon.Settings.SerializerRegistry));
        var ids = new List<BsonValue>();
        using (var cursor = await raw.Find(filter).Project(Builders<BsonDocument>.Projection.Include("_id")).Limit(50).ToCursorAsync(cancellationToken).ConfigureAwait(false))
            while (await cursor.MoveNextAsync(cancellationToken).ConfigureAwait(false))
                foreach (var d in cursor.Current) ids.Add(d["_id"]);
        var removed = 0;
        foreach (var id in ids)
        {
            var deleted = await raw.FindOneAndDeleteAsync(new BsonDocument("_id", id), cancellationToken: cancellationToken).ConfigureAwait(false);
            if (deleted != null) removed += CountStoredVersions(deleted);
        }
        return new HolonPurgeStoreOutcome
        {
            Store = "MongoDBOASIS", VersionsRemoved = removed,
            State = ids.Count == 0 ? HolonPurgeStoreState.NothingToPurge : HolonPurgeStoreState.Purged,
            Message = ids.Count == 0 ? "No record found." : "The record and its whole version archive were removed."
        };
    }
}
