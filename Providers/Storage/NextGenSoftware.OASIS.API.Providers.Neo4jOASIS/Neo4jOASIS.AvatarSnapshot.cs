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
        var result = new OASISResult<IAvatar>();
        var dispatched = false;
        var archiveVersions = IsVersionControlEnabled;
        try
        {
            var staged = StageAvatar(source);
            if (!IsProviderActivated || _driver == null) throw new InvalidOperationException("Neo4j provider is not activated.");
            Avatar acknowledged;
            using (var session = _driver.AsyncSession())
            {
                using var transaction = await session.BeginTransactionAsync();
                dispatched = true;
                var expected=archiveVersions ? await StampRecordHistoryAsync(transaction,"Avatar",staged,ReadAvatarNode):null;
                var query=SnapshotWritePrefix("Avatar",archiveVersions);
                query+="SET a.Username=$username, a.Email=$email, a.ProviderKey=$id, a.SnapshotJson=$snapshot, a.CreatedDate=$createdDate, a.ModifiedDate=$modifiedDate RETURN a";
                var rows = await (await transaction.RunAsync(query, new { id = staged.Id.ToString(), username = staged.Username,
                    email = staged.Email, snapshot = OasisJson.Serialize(staged), createdDate = staged.CreatedDate, modifiedDate = staged.ModifiedDate,
                    expected,writeId=Guid.NewGuid().ToString("N") })).ToListAsync();
                if (rows.Count != 1) throw new InvalidDataException("Neo4j avatar save did not acknowledge exactly one record.");
                acknowledged = ReadAvatarNode(rows[0]["a"].As<Neo4j.Driver.INode>());
                if (acknowledged.Id != staged.Id) throw new InvalidDataException("Neo4j avatar save acknowledged another identity.");
                await transaction.CommitAsync();
            }
            source.Id = acknowledged.Id;
            source.ProviderUniqueStorageKey[Core.Enums.ProviderType.Neo4jOASIS] = acknowledged.Id.ToString();
            source.IsNewHolon = false;
            if(archiveVersions){source.Version=acknowledged.Version;source.VersionId=acknowledged.VersionId;source.PreviousVersionId=acknowledged.PreviousVersionId;}
            result.Result = acknowledged; result.IsSaved = true;
        }
        catch (Exception cause)
        {
            result.IsError = true;
            result.Exception = dispatched ? new Neo4jMutationReconciliationException(cause) : cause;
            result.Message = result.Exception.Message;
            if (!dispatched && (cause is ArgumentException || cause is InvalidDataException)) result.ErrorCode = "HYPERDRIVE_PAYLOAD_INVALID";
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
