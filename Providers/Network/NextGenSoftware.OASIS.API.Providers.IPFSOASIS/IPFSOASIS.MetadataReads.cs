using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.IPFSOASIS;

public partial class IPFSOASIS
{
    private async Task<OASISResult<IEnumerable<IHolon>>> ReadIpfsMetadataAsync(Dictionary<string,string> pairs,
        MetaKeyValuePairMatchMode mode,HolonType type,bool loadChildren,bool recursive,int maxDepth,int initialDepth,bool fromProvider,int version)
    {
        var result=new OASISResult<IEnumerable<IHolon>>();
        try
        {
            if(pairs==null || pairs.Count==0 || pairs.Keys.Any(string.IsNullOrWhiteSpace))
                throw new ArgumentException("Nonempty metadata pairs with nonblank keys are required.");
            if(mode is not (MetaKeyValuePairMatchMode.All or MetaKeyValuePairMatchMode.Any)) throw new ArgumentOutOfRangeException(nameof(mode));
            if(maxDepth<0 || initialDepth<0) throw new ArgumentOutOfRangeException(nameof(maxDepth));
            var requested=pairs.ToArray();
            var records=await ReadIndexedCollectionAsync<Holon>("holon",row=>true,version);
            bool Match(Holon row,KeyValuePair<string,string> pair) => row.MetaData!=null
                && row.MetaData.TryGetValue(pair.Key,out var stored)
                && string.Equals(stored==null ? null:Convert.ToString(stored,CultureInfo.InvariantCulture),pair.Value,StringComparison.Ordinal);
            var project=HolonGraphProjection.Create(records,loadChildren,recursive,maxDepth,initialDepth,fromProvider,
                source=>OasisJson.Deserialize<Holon>(OasisJson.Serialize(source)));
            result.Result=records.Where(row=>(type==HolonType.All || row.HolonType==type)
                && (mode==MetaKeyValuePairMatchMode.All ? requested.All(pair=>Match(row,pair)):requested.Any(pair=>Match(row,pair))))
                .Select(project).ToList();
            result.IsLoaded=true;
        }
        catch(Exception cause)
        {
            OASISErrorHandling.HandleError(ref result,"IPFS metadata query failed: "+cause.Message,cause);
            if(cause is StorageIdentityAmbiguousException) result.ErrorCode=OASISResultErrorCodes.AmbiguousIdentity;
        }
        return result;
    }
}
