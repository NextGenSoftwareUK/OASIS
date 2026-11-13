using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Providers.WeaviateOASIS;

public partial class WeaviateOASIS
{
    protected virtual Task PersistHolonAsync(Guid id, Guid parentId, int type, bool deleted, string json)
        => UpsertObjectAsync("OasisHolon", id.ToString("D"), new Dictionary<string, object?> {
            ["parentHolonId"] = parentId.ToString("D"), ["holonType"] = type,
            ["isDeleted"] = deleted, ["dataJson"] = json });
}
