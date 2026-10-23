using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.OracleDBOASIS;

public partial class OracleDBOASIS
{
    private static void RecordMissing<T>(OASISResult<T> result,string message)
    {
        OASISErrorHandling.HandleError(ref result,message);
        result.ErrorCode=OASISResultErrorCodes.RecordNotFound;
    }

    private static void RecordLoaded<T>(NextGenSoftware.OASIS.Common.OASISResult<T> result,T value,int count=1)
    {
        result.Result=value;result.IsError=false;result.IsLoaded=true;result.LoadedCount=count;
    }

    private static void RecordsLoaded<T>(NextGenSoftware.OASIS.Common.OASISResult<IEnumerable<T>> result,IEnumerable<T> values)
    {
        var rows=values as IReadOnlyCollection<T>??values.ToArray();
        RecordLoaded<IEnumerable<T>>(result,rows,rows.Count);
    }

    private readonly record struct NativeHolonProjection(int Type,int Deleted,Guid Parent);
    private static NativeHolonProjection ReadNativeHolonProjection(System.Data.Common.DbDataReader reader,int offset)
        =>new(reader.GetInt32(offset),reader.GetInt32(offset+1),reader.IsDBNull(offset+2)?Guid.Empty:new Guid((byte[])reader.GetValue(offset+2)));

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
