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

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS;

public partial class Neo4jOASIS
{
    private static AvatarDetail ReadDetailNode(Neo4j.Driver.INode node)
    {
        if (!node.Properties.TryGetValue("SnapshotJson", out var json) || json is not string snapshot || string.IsNullOrWhiteSpace(snapshot))
            throw new InvalidDataException("Neo4j avatar-detail canonical snapshot is missing.");
        var detail = OasisJson.DeserializeStored<AvatarDetail>(snapshot);
        if (detail == null || detail.Id != ReadNativeIdentity(node))
            throw new InvalidDataException("Neo4j avatar-detail snapshot identity differs from its index.");
        ValidateNativeLookupProjection(node,"Username",detail.Username);
        ValidateNativeLookupProjection(node,"Email",detail.Email);
        return detail;
    }

    public override async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail source)
    {
        var result = new OASISResult<IAvatarDetail>();
        var dispatched = false;
        try
        {
            ArgumentNullException.ThrowIfNull(source);
            if (!IsProviderActivated || _driver == null) throw new InvalidOperationException("Neo4j provider is not activated.");
            var staged = OasisJson.Deserialize<AvatarDetail>(OasisJson.Serialize(source));
            if (staged == null) throw new InvalidDataException("Neo4j detail snapshot is null.");
            if (staged.Id == Guid.Empty) staged.Id = Guid.NewGuid();
            staged.ProviderUniqueStorageKey[Core.Enums.ProviderType.Neo4jOASIS] = staged.Id.ToString();
            staged.IsNewHolon = false;
            AvatarDetail acknowledged;
            using (var session = _driver.AsyncSession())
            {
                using var transaction = await session.BeginTransactionAsync();
                var query = "MERGE (a:AvatarDetail {Id:$id}) SET a.Username=$username, a.Email=$email, a.ProviderKey=$id, a.SnapshotJson=$snapshot RETURN a";
                dispatched = true;
                var rows = await (await transaction.RunAsync(query, new { id = staged.Id.ToString(), username = staged.Username,
                    email = staged.Email, snapshot = OasisJson.Serialize(staged) })).ToListAsync();
                if (rows.Count != 1) throw new InvalidDataException("Neo4j detail save did not acknowledge exactly one record.");
                acknowledged = ReadDetailNode(rows[0]["a"].As<Neo4j.Driver.INode>());
                if (acknowledged.Id != staged.Id) throw new InvalidDataException("Neo4j detail save acknowledged another identity.");
                await transaction.CommitAsync();
            }
            source.Id = acknowledged.Id;
            source.ProviderUniqueStorageKey[Core.Enums.ProviderType.Neo4jOASIS] = acknowledged.Id.ToString();
            source.IsNewHolon = false;
            result.Result = acknowledged; result.IsSaved = true;
        }
        catch (Exception cause)
        {
            result.IsError = true;
            result.Exception = dispatched ? new Neo4jMutationReconciliationException(cause) : cause;
            result.Message = result.Exception.Message;
        }
        return result;
    }

    public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail source)
        => SaveAvatarDetailAsync(source).GetAwaiter().GetResult();

    private async Task<OASISResult<IEnumerable<IAvatarDetail>>> ReadDetailsAsync(string field = null, string value = null)
    {
        var result = new OASISResult<IEnumerable<IAvatarDetail>>();
        try
        {
            if (!IsProviderActivated || _driver == null) throw new InvalidOperationException("Neo4j provider is not activated.");
            if (field != null && field != "Id" && field != "Email" && field != "Username") throw new ArgumentException("Unknown detail selector.");
            var query = field == null ? "MATCH (a:AvatarDetail) RETURN a" : "MATCH (a:AvatarDetail {" + field + ":$value}) RETURN a";
            using (var session = _driver.AsyncSession())
            {
                var rows = await (await session.RunAsync(query, new { value })).ToListAsync();
                result.Result = rows.Select(row => (IAvatarDetail)ReadDetailNode(row["a"].As<Neo4j.Driver.INode>())).ToList();
            }
            result.IsLoaded = true;
        }
        catch (Exception cause) { result.IsError = true; result.Exception = cause; result.Message = cause.Message; }
        return result;
    }

    private async Task<OASISResult<IAvatarDetail>> ReadOneDetailAsync(string field, string value)
    {
        var read = await ReadDetailsAsync(field, value);
        if (read.IsError) return new OASISResult<IAvatarDetail> { IsError = true, Exception = read.Exception, Message = read.Message };
        var records = read.Result.ToList();
        if (records.Count == 0) return new OASISResult<IAvatarDetail> { IsError = true, ErrorCode = OASISResultErrorCodes.RecordNotFound, Message = "Avatar detail not found in Neo4j." };
        if (records.Count != 1) return new OASISResult<IAvatarDetail> { IsError = true, ErrorCode = StorageLifecycleConflictException.ErrorCode, Message = "Neo4j detail selector is not unique." };
        return new OASISResult<IAvatarDetail>(records[0]) { IsLoaded = true };
    }

    public override Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0) => ReadDetailsAsync();
    public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0) => LoadAllAvatarDetailsAsync(version).GetAwaiter().GetResult();
    public override Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0) => ReadOneDetailAsync("Id", id.ToString());
    public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0) => LoadAvatarDetailAsync(id, version).GetAwaiter().GetResult();
    public override Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string email, int version = 0) => ReadOneDetailAsync("Email", email);
    public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string email, int version = 0) => LoadAvatarDetailByEmailAsync(email, version).GetAwaiter().GetResult();
    public override Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string username, int version = 0) => ReadOneDetailAsync("Username", username);
}
