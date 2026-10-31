using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS;

public partial class AzureCosmosDBOASIS
{
    private async Task<OASISResult<IEnumerable<T>>> ReadNearbyAsync<T>(long latitude,long longitude,int radius,Func<Task<OASISResult<IEnumerable<T>>>> load)
        where T:IHolonBase
    {
        var result=new OASISResult<IEnumerable<T>>();
        try
        {
            if(latitude < -90000000 || latitude > 90000000 || longitude < -180000000 || longitude > 180000000 || radius<0)
                throw new ArgumentOutOfRangeException(nameof(radius),"Nearby requests require valid microdegree coordinates and a nonnegative metre radius.");
            var loaded=await load();
            if(loaded.IsError) return loaded;
            bool IsNear(T row)
            {
                if(row.MetaData==null || !row.MetaData.TryGetValue("Latitude",out var lat) || !row.MetaData.TryGetValue("Longitude",out var lon)) return false;
                if(!double.TryParse(Convert.ToString(lat,CultureInfo.InvariantCulture),NumberStyles.Float,CultureInfo.InvariantCulture,out var latValue)
                    || !double.TryParse(Convert.ToString(lon,CultureInfo.InvariantCulture),NumberStyles.Float,CultureInfo.InvariantCulture,out var lonValue)
                    || !double.IsFinite(latValue) || !double.IsFinite(lonValue) || latValue < -90 || latValue > 90 || lonValue < -180 || lonValue > 180)
                    throw new InvalidDataException("Stored record has invalid Latitude/Longitude degree metadata.");
                return GeoHelper.CalculateDistance(latitude/1e6d,longitude/1e6d,latValue,lonValue)<=radius;
            }
            result.Result=loaded.Result.Where(IsNear).ToList();result.IsLoaded=true;result.LoadedCount=result.Result.Count();
        }
        catch(Exception cause) {OASISErrorHandling.HandleError(ref result,cause.Message,cause);}
        return result;
    }
}
