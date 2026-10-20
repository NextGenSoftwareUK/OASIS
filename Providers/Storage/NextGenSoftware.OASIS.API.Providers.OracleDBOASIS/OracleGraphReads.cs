using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.OracleDBOASIS;

public partial class OracleDBOASIS
{
    private static void ValidateNativeHolonProjection(IHolon row,int type,int deleted,Guid parent)
    {
        if((int)row.HolonType!=type||(row.IsDeleted?1:0)!=deleted||row.ParentHolonId!=parent)
            throw new System.IO.InvalidDataException("Oracle native projection is inconsistent (type/deletion/parent differs from its canonical record).");
    }

    private static void ValidateSelectedHolon(IHolon row,HolonType type,Guid? parent=null)
    {
        if((type!=HolonType.All&&row.HolonType!=type)||(parent.HasValue&&row.ParentHolonId!=parent.Value))
            throw new System.IO.InvalidDataException("Oracle indexed holon selection differs from its canonical record.");
    }
    private static void ValidateGraphRead(int maxDepth,int initialDepth,int version,HolonType? type=null)
    {
        if(version<0)throw new ArgumentOutOfRangeException(nameof(version));
        if(maxDepth<0||initialDepth<0)throw new ArgumentOutOfRangeException(nameof(maxDepth));
        if(type.HasValue&&!Enum.IsDefined(typeof(HolonType),type.Value))throw new ArgumentOutOfRangeException(nameof(type));
    }

    private async Task<List<IHolon>> ProjectHolonRootsAsync(IEnumerable<IHolon> roots,bool children,bool recursive,int maxDepth,int initialDepth,bool fromProvider,bool rootsAreChildren=false)
    {
        var selected=roots.ToList();
        var pool=children&&fromProvider?(await ReadRecordCollectionAsync<Holon>("OASIS_HOLONS",0)).Cast<IHolon>():selected;
        var project=HolonGraphProjection.Create(pool,children,recursive,maxDepth,initialDepth,fromProvider,
            source=>Core.Serialization.StoredRecordJson.Project<Holon>(source),rootsAreChildren:rootsAreChildren);
        return selected.Select(project).ToList();
    }
}
