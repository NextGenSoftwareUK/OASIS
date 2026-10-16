using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.LocalFileOASIS;

public partial class LocalFileOASIS
{
    private async Task<OASISResult<IEnumerable<IHolon>>> ReadGraphAsync(Func<IHolon,bool> select,HolonType type,
        bool children,bool recursive,int depth,int initialDepth,int version,Guid? pointId=null)
    {
        var result=new OASISResult<IEnumerable<IHolon>>();
        try
        {
            if(version<0 || depth<0 || initialDepth<0) throw new ArgumentOutOfRangeException(nameof(version));
            if(!Enum.IsDefined(typeof(HolonType),type)) throw new ArgumentOutOfRangeException(nameof(type));
            if(!IsProviderActivated)
            {
                var active=await ActivateProviderAsync();
                if(active.IsError) {OASISErrorHandling.HandleError(ref result,active.Message,active.Exception);return result;}
            }
            var records=new List<IHolon>();
            var pointPath=pointId.HasValue ? Path.Combine(_holonDirectory,pointId.Value+".json"):null;
            IEnumerable<string> files=pointId.HasValue && !children ? File.Exists(pointPath) ? new[] {pointPath}:Array.Empty<string>():Directory.EnumerateFiles(_holonDirectory,"*.json").OrderBy(path=>path,StringComparer.Ordinal);
            foreach(var file in files)
            {
                var row=OasisJson.DeserializeStored<Holon>(await File.ReadAllTextAsync(file))
                    ?? throw new InvalidDataException("Stored local holon is null.");
                if(!Guid.TryParse(Path.GetFileNameWithoutExtension(file),out var id) || row.Id!=id || id==Guid.Empty)
                    throw new InvalidDataException("Local holon file identity differs from its canonical record.");
                if(version==0 || row.Version==version) records.Add(row);
            }
            // Canonical LocalFile snapshots store relationships by parent ID, never embedded navigation.
            var project=HolonGraphProjection.Create(records,children,recursive,depth,initialDepth,true,
                row=>OasisJson.Deserialize<Holon>(OasisJson.Serialize(row)));
            result.Result=records.Where(row=>(type==HolonType.All || row.HolonType==type) && select(row)).Select(project).ToList();
            result.IsLoaded=true;
        }
        catch(Exception cause) {result.Result=null;result.IsLoaded=false;OASISErrorHandling.HandleError(ref result,cause.Message,cause);}
        return result;
    }

    private async Task<OASISResult<IHolon>> ReadSingleGraphAsync(Func<IHolon,bool> select,bool children,bool recursive,int depth,int version,Guid? pointId=null)
    {
        var rows=await ReadGraphAsync(select,HolonType.All,children,recursive,depth,0,version,pointId);
        var result=OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IEnumerable<IHolon>,IHolon>(rows);
        if(rows.IsError) return result;
        var matches=rows.Result.ToList();
        if(matches.Count>1)
        {
            result.IsLoaded=false;result.ErrorCode=OASISResultErrorCodes.AmbiguousIdentity;
            OASISErrorHandling.HandleError(ref result,"Local holon selector is ambiguous.",new StorageIdentityAmbiguousException());return result;
        }
        result.Result=matches.SingleOrDefault();result.IsLoaded=result.Result!=null;
        return result;
    }

    private async Task<OASISResult<IEnumerable<IHolon>>> ReadParentGraphAsync(Guid parent,HolonType type,bool children,bool recursive,int depth,int initialDepth,bool continueOnError,bool fromProvider,int version)
    {
        var loaded=await LoadAllHolonsAsync(type,children,recursive,depth,initialDepth,continueOnError,fromProvider,version);
        if(loaded.IsError) return loaded;
        loaded.Result=loaded.Result.Where(row=>row.ParentHolonId==parent).ToList();return loaded;
    }

    private async Task<OASISResult<IEnumerable<IHolon>>> ReadGraphMetadataAsync(Dictionary<string,string> pairs,MetaKeyValuePairMatchMode mode,
        HolonType type,bool children,bool recursive,int depth,int initialDepth,int version)
    {
        var result=new OASISResult<IEnumerable<IHolon>>();
        try
        {
            if(pairs==null || pairs.Count==0 || pairs.Keys.Any(string.IsNullOrWhiteSpace)) throw new ArgumentException("Nonempty metadata keys are required.");
            if(mode!=MetaKeyValuePairMatchMode.All && mode!=MetaKeyValuePairMatchMode.Any) throw new ArgumentOutOfRangeException(nameof(mode));
            bool Matches(IHolon row)
            {
                bool Pair(KeyValuePair<string,string> pair)=>row.MetaData!=null && row.MetaData.TryGetValue(pair.Key,out var value) && value?.ToString()==pair.Value;
                return mode==MetaKeyValuePairMatchMode.All ? pairs.All(Pair):pairs.Any(Pair);
            }
            return await ReadGraphAsync(Matches,type,children,recursive,depth,initialDepth,version);
        }
        catch(Exception cause) {OASISErrorHandling.HandleError(ref result,cause.Message,cause);return result;}
    }
}
