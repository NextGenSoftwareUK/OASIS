using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Providers.QdrantOASIS;

public partial class QdrantOASIS
{
    protected virtual Task<ulong> PersistHolonAsync(Guid id, Guid parentId, int type, bool deleted, string json)
        => UpsertPointAsync("oasis_holons", id.ToString("D"), new Dictionary<string, object?> {
            ["parent_holon_id"] = parentId.ToString("D"), ["holon_type"] = type,
            ["is_deleted"] = deleted, ["data_json"] = json });
}
