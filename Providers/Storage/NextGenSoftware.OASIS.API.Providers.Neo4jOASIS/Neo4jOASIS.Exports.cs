using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using INode=Neo4j.Driver.INode;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS;

public partial class Neo4jOASIS
{
    private static void ValidateExportVersion(int version)
    {if(version < -1)throw new ArgumentOutOfRangeException(nameof(version),"Export version must be -1 (all stored versions), zero or positive.");}

    private async Task<OASISResult<IEnumerable<IHolon>>> ExportStoredHolonsAsync(int version)
    {
        var result=new OASISResult<IEnumerable<IHolon>>();
        try{
            ValidateExportVersion(version);
            if(version!=-1)return await LoadAllHolonsAsync(loadChildren:false,version:version);
            if(!IsProviderActivated||_driver==null)throw new InvalidOperationException("Neo4j provider is not activated.");
            using var session=_driver.AsyncSession();
            var currentRows=await(await session.RunAsync("MATCH (h:Holon) RETURN h")).ToListAsync();
            var identities=new HashSet<Guid>();var selected=new List<IHolon>();
            foreach(var row in currentRows){
                var current=ReadHolonNode(row["h"].As<INode>());
                if(!identities.Add(current.Id))throw new InvalidDataException("Neo4j export contains duplicate current identities.");
                var archives=await(await session.RunAsync("MATCH (v:OasisRecordVersion {Kind:$kind,RecordId:$id}) RETURN v",new{kind="Holon",id=current.Id.ToString()})).ToListAsync();
                selected.AddRange(await SelectStoredHolonVersionsAsync(current,archives.Select(archive=>archive["v"].As<INode>()).ToList()));
            }
            // Roots retain their stored payloads; this is not a point-in-time child graph.
            result.Result=selected;result.IsLoaded=true;result.LoadedCount=selected.Count;
        }catch(Exception cause){OASISErrorHandling.HandleError(ref result,"Neo4j export failed.",cause);
            if(cause is ArgumentException)result.ErrorCode="HYPERDRIVE_PAYLOAD_INVALID";
            else if(cause is InvalidDataException)result.ErrorCode=StorageLifecycleConflictException.ErrorCode;}
        return result;
    }

    internal static async Task<IReadOnlyList<IHolon>> SelectStoredHolonVersionsAsync(Holon current,IReadOnlyList<INode> archives)
    {
        ArgumentNullException.ThrowIfNull(current);ArgumentNullException.ThrowIfNull(archives);
        if(current.Id==Guid.Empty||current.Version<0)throw new InvalidDataException("Neo4j current export identity/version is invalid.");
        var versions=new SortedDictionary<int,string>();
        foreach(var archive in archives){
            var fields=archive?.Properties??throw new InvalidDataException("Neo4j archived export node is missing.");
            if(!fields.TryGetValue("Kind",out var kind)||kind is not string kindText||kindText!="Holon"
                ||!fields.TryGetValue("RecordId",out var id)||id is not string idText||idText!=current.Id.ToString()
                ||!fields.TryGetValue("Version",out var rawVersion)||rawVersion is not long nativeVersion||nativeVersion<=0||nativeVersion>=current.Version
                ||!fields.TryGetValue("SnapshotJson",out var snapshot)||snapshot is not string json||string.IsNullOrWhiteSpace(json)
                ||!versions.TryAdd(checked((int)nativeVersion),json))
                throw new InvalidDataException("Neo4j archive identity/version/snapshot is invalid or duplicated.");
        }
        var selected=new List<IHolon>();
        foreach(var pair in versions){
            var historical=await StoredRecordVersions.ReadAsync(current.Id,current,pair.Key,
                _=>Task.FromResult(OasisJson.DeserializeStored<Holon>(pair.Value)));
            if(historical==null)throw new InvalidDataException("Neo4j archived export payload is null.");
            if(!historical.IsDeleted)selected.Add(historical);
        }
        if(!current.IsDeleted)selected.Add(current);
        return selected;
    }
}
