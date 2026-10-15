using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Entities.Models;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Extensions;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS;

public partial class SolanaOASIS
{
    private async Task<OASISResult<IEnumerable<IHolon>>> ReadHolonMetadataAsync(Dictionary<string,string> pairs,
        Core.Enums.MetaKeyValuePairMatchMode mode,Core.Enums.HolonType type,bool loadChildren,bool recursive,
        int maxDepth,int initialDepth,bool fromProvider,int version)
    {
        var result=new OASISResult<IEnumerable<IHolon>>();
        try
        {
            if(pairs == null || pairs.Count==0 || pairs.Keys.Any(string.IsNullOrWhiteSpace))
                throw new ArgumentException("Nonempty metadata pairs with nonblank keys are required.");
            if(mode is not (Core.Enums.MetaKeyValuePairMatchMode.All or Core.Enums.MetaKeyValuePairMatchMode.Any))
                throw new ArgumentOutOfRangeException(nameof(mode));
            var requested=pairs.ToArray();
            bool Match(IHolon row,KeyValuePair<string,string> pair) => row.MetaData != null
                && row.MetaData.TryGetValue(pair.Key,out var stored)
                && string.Equals(stored == null ? null:Convert.ToString(stored,System.Globalization.CultureInfo.InvariantCulture),pair.Value,StringComparison.Ordinal);
            return await ReadHolonCollectionAsync(row=>(type==Core.Enums.HolonType.All || row.HolonType==type)
                && (mode==Core.Enums.MetaKeyValuePairMatchMode.All ? requested.All(pair=>Match(row,pair)):requested.Any(pair=>Match(row,pair))),
                loadChildren,recursive,maxDepth,initialDepth,fromProvider,version);
        }
        catch(Exception cause) {OASISErrorHandling.HandleError(ref result,"Solana metadata query failed: "+cause.Message,cause);}
        return result;
    }

    private async Task<OASISResult<IEnumerable<IHolon>>> ReadHolonCollectionAsync(Func<IHolon,bool> select,
        bool loadChildren,bool recursive,int maxDepth,int initialDepth,bool fromProvider,int version)
    {
        var result=new OASISResult<IEnumerable<IHolon>>();
        try
        {
            if(version!=0) throw new NotSupportedException("Solana collection reads currently require latest version (0).");
            if(maxDepth<0 || initialDepth<0) throw new ArgumentOutOfRangeException(nameof(maxDepth));
            if(!IsProviderActivated)
            {
                var activation=await ActivateProviderAsync();
                if(activation.IsError) throw new InvalidOperationException(activation.Message,activation.Exception);
            }
            var stored=await _solanaRepository.GetAllAsync<SolanaHolonDto>();
            var records=new List<IHolon>();
            foreach(var entry in stored)
            {
                var holon=entry.Record.GetHolon();
                holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.SolanaOASIS]=entry.Signature;
                records.Add(holon);
            }
            var project=HolonGraphProjection.Create(records,loadChildren,recursive,maxDepth,initialDepth,fromProvider,
                source=>OasisJson.Deserialize<Core.Holons.Holon>(OasisJson.Serialize(source)));
            result.Result=records.Where(select).Select(project).ToList();
            result.IsLoaded=true;
        }
        catch(Exception cause) {OASISErrorHandling.HandleError(ref result,"Solana finalized holon collection failed: "+cause.Message,cause);}
        return result;
    }
}
