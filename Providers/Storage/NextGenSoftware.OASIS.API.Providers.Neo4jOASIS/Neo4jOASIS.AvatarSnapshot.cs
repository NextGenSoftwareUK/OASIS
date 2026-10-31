using System;
using System.IO;
using System.Threading.Tasks;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS;

public partial class Neo4jOASIS
{
    public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar source)
    {
        var result=new OASISResult<IAvatar>();var archiveVersions=IsVersionControlEnabled;
        try
        {
            var staged=StageAvatar(source);
            result=await WriteIdentitySnapshotAsync<IAvatar>("Avatar",staged,ReadAvatarNode,
                "SET a.Username=$username, a.Email=$email, a.ProviderKey=$id, a.SnapshotJson=$snapshot, a.CreatedDate=$createdDate, a.ModifiedDate=$modifiedDate RETURN a",row=>new System.Collections.Generic.Dictionary<string,object>{{"username",row.Username},{"email",row.Email},{"createdDate",row.CreatedDate},{"modifiedDate",row.ModifiedDate}},archiveVersions).ConfigureAwait(false);
            if(result.IsError)return result;
            var acknowledged=result.Result;
            source.Id=acknowledged.Id;
            source.ProviderUniqueStorageKey??=new System.Collections.Generic.Dictionary<Core.Enums.ProviderType,string>();
            source.ProviderUniqueStorageKey[Core.Enums.ProviderType.Neo4jOASIS]=acknowledged.Id.ToString();source.IsNewHolon=false;
            if(archiveVersions){source.Version=acknowledged.Version;source.VersionId=acknowledged.VersionId;source.PreviousVersionId=acknowledged.PreviousVersionId;}
        }
        catch(Exception cause)
        {
            result.Exception=result.Exception==null?cause:new AggregateException(result.Exception,cause);result.Message=result.Exception.Message;
            if(result.IsSaved){result.IsWarning=true;result.WarningCount++;}
            else {result.IsError=true;result.ErrorCount++;result.ErrorCode=cause is ArgumentException or InvalidDataException?"HYPERDRIVE_PAYLOAD_INVALID":"COSMIC_WRITE_FAILED";}
        }
        return result;
    }

    internal static Avatar StageAvatar(IAvatar source)
    {
        ArgumentNullException.ThrowIfNull(source);
        var staged = OasisJson.Project<Avatar>(source);
        if (staged == null) throw new InvalidDataException("Neo4j avatar snapshot is null.");
        if (staged.Id == Guid.Empty) staged.Id = Guid.NewGuid();
        staged.ProviderUniqueStorageKey[Core.Enums.ProviderType.Neo4jOASIS] = staged.Id.ToString();
        staged.IsNewHolon = false;
        return staged;
    }

    internal static Avatar ReadAvatarSnapshot(string json, string indexedId)
    {
        if (string.IsNullOrWhiteSpace(json)) throw new InvalidDataException("Neo4j avatar canonical snapshot is missing.");
        var avatar = OasisJson.DeserializeStored<Avatar>(json);
        if (avatar == null || !Guid.TryParse(indexedId, out var id) || id == Guid.Empty || avatar.Id != id)
            throw new InvalidDataException("Neo4j avatar snapshot identity differs from its index.");
        return avatar;
    }

    private static Avatar ReadAvatarNode(Neo4j.Driver.INode node)
    {
        if (!node.Properties.TryGetValue("SnapshotJson", out var json))
            throw new InvalidDataException("Neo4j avatar canonical snapshot is missing.");
        var avatar=ReadAvatarSnapshot(json as string, ReadNativeIdentity(node).ToString());
        ValidateNativeLookupProjection(node,"Username",avatar.Username);
        ValidateNativeLookupProjection(node,"Email",avatar.Email);
        return avatar;
    }

    private static void ValidateNativeLookupProjection(Neo4j.Driver.INode node,string field,string canonical)
    {
        if(!node.Properties.TryGetValue(field,out var value))
        {
            if(canonical==null) return; // Neo4j represents a null property by its absence.
            throw new InvalidDataException("Neo4j native lookup projection is missing: "+field);
        }
        if(value is not string text || !string.Equals(text,canonical,StringComparison.Ordinal))
            throw new InvalidDataException("Neo4j native lookup projection differs from its canonical record: "+field);
    }

    private static Guid ReadNativeIdentity(Neo4j.Driver.INode node)
    {
        if(!node.Properties.TryGetValue("Id",out var value) || value is not string text
            || !Guid.TryParse(text,out var id) || id==Guid.Empty)
            throw new InvalidDataException("Neo4j native record identity is missing or invalid.");
        return id;
    }
}
