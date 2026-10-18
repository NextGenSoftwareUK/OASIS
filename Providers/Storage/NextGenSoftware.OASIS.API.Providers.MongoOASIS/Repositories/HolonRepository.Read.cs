using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Repositories;

public partial class HolonRepository
{
    private Holon ReadNativeHolon(FilterDefinition<Holon> filter)
        => ProjectNativeHolon(_dbContext.Holon.Find(filter).Limit(2).ToList());

    private async Task<Holon> ReadNativeHolonAsync(FilterDefinition<Holon> filter)
        => ProjectNativeHolon(await _dbContext.Holon.Find(filter).Limit(2).ToListAsync());

    private static Holon ProjectNativeHolon(List<Holon> rows)
    {
        if(rows.Count>1) throw new StorageIdentityAmbiguousException();
        var row=rows.SingleOrDefault();
        if(row==null) return null;
        if(!ObjectId.TryParse(row.Id,out _)) throw new InvalidOperationException("Stored Mongo native holon identity is invalid.");
        // The returned projection exposes its real storage identity. A READ must never repair stored data.
        row.ProviderUniqueStorageKey=row.ProviderUniqueStorageKey==null ? new Dictionary<ProviderType,string>()
            :new Dictionary<ProviderType,string>(row.ProviderUniqueStorageKey);
        row.ProviderUniqueStorageKey[ProviderType.MongoDBOASIS]=row.Id;
        return row;
    }
}
