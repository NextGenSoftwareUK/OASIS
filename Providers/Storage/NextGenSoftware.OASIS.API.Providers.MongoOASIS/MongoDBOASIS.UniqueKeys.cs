using System;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;

public partial class MongoDBOASIS : ICosmicUniqueKeyStorageProvider
{
    public async Task<OASISResult<Guid>> FindUniqueKeyHolderAsync(string scope, string value, CancellationToken cancellationToken = default)
    {
        try
        {
            var key = new CosmicUniqueKeyClaim(Guid.NewGuid(), scope, value).NativeKey;
            var held = await Database.MongoDB.GetCollection<BsonDocument>("CosmicUniqueKey")
                .WithReadConcern(ReadConcern.Majority)
                .Find(new BsonDocument("_id", key)).SingleOrDefaultAsync(cancellationToken).ConfigureAwait(false);
            return new OASISResult<Guid>(held == null ? Guid.Empty : Guid.Parse(held["HolonId"].AsString)) { IsLoaded = true };
        }
        catch (Exception cause)
        {
            return new OASISResult<Guid> { IsError = true, ErrorCount = 1, ErrorCode = "COSMIC_UNIQUE_KEY_LOOKUP_FAILED", Message = "The MongoDB unique key could not be read.", Exception = cause };
        }
    }
}
