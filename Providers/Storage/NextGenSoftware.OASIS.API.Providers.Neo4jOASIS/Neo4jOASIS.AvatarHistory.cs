using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using INode = Neo4j.Driver.INode;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS;

public partial class Neo4jOASIS
{
    // Caller owns the transaction: archive and current replacement commit or roll back together.
    private async Task<string> StampAvatarHistoryAsync(IAsyncTransaction transaction, Avatar staged)
    {
        var rows=await (await transaction.RunAsync("MATCH (a:Avatar {Id:$id}) "
            + "SET a.__oasisAvatarLock=true REMOVE a.__oasisAvatarLock RETURN a",new{id=staged.Id.ToString()})).ToListAsync();
        if(rows.Count>1)throw new StorageIdentityAmbiguousException();
        var previous=rows.Count==0 ? null:ReadAvatarNode(rows[0]["a"].As<INode>());
        var expected=rows.Count==0 ? null:(string)rows[0]["a"].As<INode>()["SnapshotJson"];
        await StoredRecordVersions.StampAsync(staged.Id,staged,previous,async stored=>
        {
            var archived=await (await transaction.RunAsync("MERGE (v:OasisRecordVersion {Kind:'Avatar',RecordId:$id,Version:$version}) "
                + "ON CREATE SET v.SnapshotJson=$snapshot WITH v WHERE v.SnapshotJson=$snapshot RETURN v",
                new{id=stored.Id.ToString(),version=stored.Version,snapshot=OasisJson.Serialize(stored)})).ToListAsync();
            if(archived.Count!=1)throw new StorageLifecycleConflictException("Neo4j archived avatar version conflicts with its original snapshot.");
        });
        return expected;
    }

    private async Task<OASISResult<IEnumerable<IAvatar>>> ReadHistoricalAvatarsAsync(int version)
    {
        var result=new OASISResult<IEnumerable<IAvatar>>();
        try
        {
            if(version<=0)throw new ArgumentOutOfRangeException(nameof(version));
            if(!IsProviderActivated || _driver==null)throw new InvalidOperationException("Neo4j provider is not activated.");
            using var session=_driver.AsyncSession();
            var rows=await (await session.RunAsync("MATCH (a:Avatar) RETURN a")).ToListAsync();
            var selected=new List<IAvatar>();var ids=new HashSet<Guid>();
            foreach(var row in rows)
            {
                var current=ReadAvatarNode(row["a"].As<INode>());
                if(!ids.Add(current.Id))throw new StorageIdentityAmbiguousException();
                var stored=await StoredRecordVersions.ReadAsync(current.Id,current,version,async requested=>
                {
                    var history=await (await session.RunAsync("MATCH (v:OasisRecordVersion {Kind:'Avatar',RecordId:$id,Version:$version}) RETURN v",
                        new{id=current.Id.ToString(),version=requested})).ToListAsync();
                    if(history.Count==0)return null;
                    if(history.Count!=1)throw new InvalidDataException("Neo4j archived avatar identity/version is not unique.");
                    var node=history[0]["v"].As<INode>();
                    node.Properties.TryGetValue("SnapshotJson",out var json);
                    return ReadAvatarSnapshot(json as string,current.Id.ToString());
                });
                if(stored!=null)selected.Add(stored);
            }
            result.Result=selected;result.IsLoaded=true;
        }
        catch(Exception cause){result.IsError=true;result.Exception=cause;result.Message=cause.Message;
            if(cause is StorageIdentityAmbiguousException)result.ErrorCode=OASISResultErrorCodes.AmbiguousIdentity;
            else if(cause is InvalidDataException || cause is StorageLifecycleConflictException)result.ErrorCode=StorageLifecycleConflictException.ErrorCode;}
        return result;
    }

    private async Task<OASISResult<IAvatar>> ReadHistoricalAvatarAsync(string field,string value,int version)
    {
        var result=new OASISResult<IAvatar>();
        try
        {
            AvatarSelectionQuery(field); // Same closed selector vocabulary as current records.
            if(string.IsNullOrWhiteSpace(value) || field=="Id" && (!Guid.TryParse(value,out var id)||id==Guid.Empty))
                throw new ArgumentException("A nonempty avatar lookup identity is required.");
            var all=await ReadHistoricalAvatarsAsync(version);
            if(all.IsError)return new OASISResult<IAvatar>{IsError=true,Message=all.Message,Exception=all.Exception,ErrorCode=all.ErrorCode};
            var matches=all.Result.Where(avatar=>field switch{
                "Id"=>avatar.Id==Guid.Parse(value),
                "ProviderKey"=>avatar.ProviderUniqueStorageKey.TryGetValue(Core.Enums.ProviderType.Neo4jOASIS,out var key)&&key==value,
                "Username"=>avatar.Username==value,
                "Email"=>avatar.Email==value,
                _=>false}).ToList();
            if(matches.Count==0)return new OASISResult<IAvatar>{IsError=true,ErrorCode=OASISResultErrorCodes.RecordNotFound,Message="Requested Neo4j avatar version was not found."};
            if(matches.Count!=1)throw new StorageIdentityAmbiguousException();
            result.Result=matches[0];result.IsLoaded=true;
        }
        catch(Exception cause){result.IsError=true;result.Exception=cause;result.Message=cause.Message;
            if(cause is StorageIdentityAmbiguousException)result.ErrorCode=OASISResultErrorCodes.AmbiguousIdentity;}
        return result;
    }
}
