using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.DynamoDBOASIS;

public partial class DynamoDBOASIS
{
    public OASISResult<IEnumerable<IAvatar>> GetAvatarsNearMe(long latitude,long longitude,int radiusInMeters)
        => ReadNearbyAsync(latitude,longitude,radiusInMeters,()=>LoadAllAvatarsAsync()).GetAwaiter().GetResult();

    public OASISResult<IEnumerable<IHolon>> GetHolonsNearMe(long latitude,long longitude,int radiusInMeters,HolonType type)
        => ReadNearbyAsync(latitude,longitude,radiusInMeters,()=>LoadAllHolonsAsync(HolonType.All,loadChildren:false),type).GetAwaiter().GetResult();

    private async Task<OASISResult<IEnumerable<T>>> ReadNearbyAsync<T>(long latitude,long longitude,int radius,
        Func<Task<OASISResult<IEnumerable<T>>>> load,HolonType type=HolonType.All) where T:IHolonBase
    {
        var result=new OASISResult<IEnumerable<T>>();
        try
        {
            var query=new StoredRecordLocationQuery(latitude,longitude,radius);
            if(!Enum.IsDefined(typeof(HolonType),type)) throw new ArgumentOutOfRangeException(nameof(type));
            var loaded=await load();if(loaded.IsError) return loaded;
            result=OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IEnumerable<T>,IEnumerable<T>>(loaded);
            result.Result=query.Select(loaded.Result,row=>row.IsActive && !row.IsDeleted && (type==HolonType.All || row.HolonType==type));
            result.IsLoaded=true;result.LoadedCount=System.Linq.Enumerable.Count(result.Result);
        }
        catch(Exception cause) {result.Result=null;result.IsLoaded=false;result.LoadedCount=0;OASISErrorHandling.HandleError(ref result,cause.Message,cause);}
        return result;
    }
}
