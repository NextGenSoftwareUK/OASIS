using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.BigQueryOASIS;

public partial class BigQueryOASIS
{
    private static void ValidateGraphDepth(int maximum,int initial=0)
    {
        if(maximum<0)throw new ArgumentOutOfRangeException(nameof(maximum));
        if(initial<0)throw new ArgumentOutOfRangeException(nameof(initial));
    }

    private async Task<List<IHolon>> ProjectGraphRootsAsync(IEnumerable<IHolon> roots,bool children,bool recursive,int maximum,int initial,bool fromProvider,bool rootsAreChildren=false,IEnumerable<IHolon>? providerPool=null)
    {
        var selected=roots.ToList();
        var pool=children&&fromProvider?providerPool??(await GetAllDocsAsync<Holon>(CollHolons)).Where(row=>!row.IsDeleted):selected;
        var project=HolonGraphProjection.Create(pool,children,recursive,maximum,initial,fromProvider,
            source=>Core.Serialization.StoredRecordJson.Project<Holon>(source),rootsAreChildren);
        return selected.Select(project).ToList();
    }
}
