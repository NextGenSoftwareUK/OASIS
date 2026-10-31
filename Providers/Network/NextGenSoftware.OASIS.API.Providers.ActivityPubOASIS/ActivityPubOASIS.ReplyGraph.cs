using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.ActivityPubOASIS;

public partial class ActivityPubOASIS
{
    private async Task<OASISResult<IEnumerable<IHolon>>> ReadMastodonChildrenAsync(string key,
        Core.Enums.HolonType type,bool loadChildren,bool recursive,int maxDepth,int currentDepth,bool providerChildren,int version)
    {
        var result=new OASISResult<IEnumerable<IHolon>>();
        try
        {
            ObjectDisposedException.ThrowIf(_disposed,this);
            if(maxDepth<0||currentDepth<0)throw new ArgumentOutOfRangeException(nameof(maxDepth));
            if(!Enum.IsDefined(type))throw new ArgumentOutOfRangeException(nameof(type));
            var nativeDepth=1;
            if(loadChildren&&providerChildren&&(maxDepth==0||currentDepth<maxDepth))
                nativeDepth=!recursive?2:maxDepth==0?0:(int)Math.Min(int.MaxValue,(long)maxDepth-currentDepth+1);
            var parent=await ReadMastodonStatusAsync(key,true,true,nativeDepth,version,true);
            if(parent.IsError)return Core.Helpers.OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IHolon,IEnumerable<IHolon>>(parent);
            if(parent.Result==null)throw new InvalidDataException("Native parent status read returned no record.");
            var selected=parent.Result.Children.Where(child=>type==Core.Enums.HolonType.All||child.HolonType==type).ToArray();
            result.Result=selected;result.LoadedCount=selected.Length;result.IsLoaded=true;
        }
        catch(Exception error){OASISErrorHandling.HandleError(ref result,"Native Mastodon parent reply read failed.",error);}
        return result;
    }

    private async Task<IHolon> ReadMastodonReplyGraphAsync(Holon root,bool recursive,int depth)
    {
        var key=root.ProviderUniqueStorageKey[Core.Enums.ProviderType.ActivityPubOASIS];
        using var response=await _httpClient.GetAsync("/api/v1/statuses/"+Uri.EscapeDataString(key)+"/context");
        response.EnsureSuccessStatusCode();
        if(response.Headers.Contains("Mastodon-Async-Refresh"))throw new InvalidDataException("Native reply context is awaiting refresh; no incomplete tree returned.");
        using var json=JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        if(json.RootElement.ValueKind!=JsonValueKind.Object
            ||!json.RootElement.TryGetProperty("ancestors",out var ancestors)||ancestors.ValueKind!=JsonValueKind.Array
            ||!json.RootElement.TryGetProperty("descendants",out var descendants)||descendants.ValueKind!=JsonValueKind.Array)
            throw new InvalidDataException("Native context requires ancestor and descendant arrays.");
        if(descendants.GetArrayLength()>10000)throw new InvalidDataException("Native reply graph exceeds the10000-record safety bound.");
        var records=new Dictionary<Guid,IHolon>{{root.Id,root}};
        foreach(var value in descendants.EnumerateArray())
        {
            var child=ProjectMastodonStatus(value);
            if(!records.TryAdd(child.Id,child))throw new InvalidDataException("Duplicate status identity in native reply context.");
        }
        if(records.ContainsKey(root.ParentHolonId))throw new InvalidDataException("Native root reply parent cannot be its own descendant.");
        var levels=new Dictionary<Guid,int>{{root.Id,0}};
        foreach(var node in records.Values)
        {
            var path=new List<IHolon>();var seen=new HashSet<Guid>();var current=node;int level;
            while(!levels.TryGetValue(current.Id,out level))
            {
                if(!seen.Add(current.Id))throw new InvalidDataException("Native reply graph contains a cycle.");
                path.Add(current);
                if(!records.TryGetValue(current.ParentHolonId,out current))throw new InvalidDataException("Native reply context contains an orphan or unrelated status.");
            }
            for(var index=path.Count-1;index>=0;index--)levels[path[index].Id]=++level;
        }
        var requestedDepth=!recursive?1:depth==0?int.MaxValue:depth;
        var children=records.Values.Where(node=>node.Id!=root.Id).ToLookup(node=>node.ParentHolonId);
        foreach(var node in records.Values.Where(node=>levels[node.Id]<requestedDepth))
        {
            if(!node.MetaData.TryGetValue("MastodonRepliesCount",out var value)||value is not long count)
                throw new InvalidDataException("Native reply count is required to detect incomplete context.");
            if(children[node.Id].LongCount()!=count)throw new InvalidDataException("Native context is incomplete or changed; refusing truncated reply graph.");
        }
        // Same graph/depth implementation as storage providers, with detached canonical copies.
        return HolonGraphProjection.Create(records.Values,true,recursive,depth,0,true,
            node=>OasisJson.DeserializeStored<IHolon>(OasisJson.Serialize(node)))(root);
    }
}
