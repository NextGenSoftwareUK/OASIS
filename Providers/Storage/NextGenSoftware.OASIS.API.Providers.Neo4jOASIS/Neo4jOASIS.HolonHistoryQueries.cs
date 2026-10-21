using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS;

public partial class Neo4jOASIS
{
    private async Task<OASISResult<IEnumerable<IHolon>>> ReadHistoricalHolonQueryAsync(int version,
        Func<IHolon,bool> predicate,bool children,bool recursive,int maximumDepth,long currentDepth,bool continueOnError)
    {
        var response=new OASISResult<IEnumerable<IHolon>>();
        var failures=new List<string>();
        try
        {
            if(version<=0 || maximumDepth<0 || currentDepth<0)throw new ArgumentOutOfRangeException(nameof(version));
            var records=await ReadHistoricalRecordsAsync<Holon>("Holon",version,ReadHolonNode,json=>OasisJson.DeserializeStored<Holon>(json));
            if(records.IsError)return new OASISResult<IEnumerable<IHolon>>{IsError=true,ErrorCode=records.ErrorCode,Exception=records.Exception,Message=records.Message};
            var selected=records.Result.Where(predicate).Cast<IHolon>().ToList();
            if(children)
            {
                using var session=_driver.AsyncSession();
                foreach(var root in selected)
                    await HydrateNativeChildrenAsync(session,(Holon)root,recursive,maximumDepth,currentDepth,new HashSet<Guid>{root.Id},continueOnError,failures);
            }
            response.Result=selected;response.IsLoaded=true;
        }
        catch(Exception cause){response.IsError=true;response.Exception=cause;response.Message=cause.Message;}
        return MarkNativePartialRead(response,failures);
    }
}
