using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS;

public sealed class Neo4jMutationReconciliationException : Exception, INativeMutationReconciliationRequired
{
    public string OriginalTransactionId => string.Empty; // Driver did not expose a transaction identity; never invent one.
    public Neo4jMutationReconciliationException(Exception cause)
        : base("Neo4j mutation outcome requires inspection before replay.", cause) { }
}

public partial class Neo4jOASIS
{
    internal static string AvatarDeleteSelection(string field)
    {
        if (field != "Id" && field != "ProviderKey" && field != "Username" && field != "Email")
            throw new ArgumentException("Unknown avatar selector.", nameof(field));
        return "MATCH (a:Avatar {" + field + ": $value}) RETURN a";
    }

    internal static (string Query, Dictionary<string, object> Parameters) PlanAvatarDelete(string json, string indexedId, bool softDelete, DateTime now)
    {
        var avatar = ReadAvatarSnapshot(json, indexedId);
        var parameters = new Dictionary<string, object> { ["id"] = indexedId, ["expected"] = json };
        // Compare the full observed record; never overwrite a concurrently changed snapshot.
        // Acquire the node write lock before reading the compare value (reserved temporary property).
        // https://neo4j.com/docs/operations-manual/current/database-internals/concurrent-data-access/
        const string prefix = "MATCH (a:Avatar {Id: $id}) WITH collect(a) AS candidates WHERE size(candidates) = 1 "
            + "UNWIND candidates AS a SET a.__oasisAvatarLock = true REMOVE a.__oasisAvatarLock "
            + "WITH a WHERE a.SnapshotJson = $expected ";
        if (!softDelete) return (prefix + "DETACH DELETE a RETURN 1 AS affected", parameters);
        avatar.IsActive = false;
        if (avatar.DeletedDate == DateTime.MinValue) avatar.DeletedDate = now;
        parameters["snapshot"] = OasisJson.Serialize(avatar);
        parameters["deletedDate"] = avatar.DeletedDate;
        return (prefix + "SET a.SnapshotJson = $snapshot, a.IsActive = false, a.DeletedDate = $deletedDate RETURN 1 AS affected", parameters);
    }

    private async Task<OASISResult<bool>> DeleteAvatarRecordAsync(string field, string value, bool softDelete)
    {
        var response = new OASISResult<bool>();
        var dispatched = false;
        Core.Objects.AvatarTombstone tombstone = null;
        try
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Avatar deletion selector is required.");
            if (!IsProviderActivated || _driver == null) throw new InvalidOperationException("Neo4j provider is not activated.");
            using (var session = _driver.AsyncSession())
            {
                var found = await (await session.RunAsync(AvatarDeleteSelection(field), new { value })).ToListAsync();
                if (found.Count == 0)
                    return new OASISResult<bool>(false) { IsError = true, ErrorCode = OASISResultErrorCodes.RecordNotFound, Message = "Avatar not found in Neo4j." };
                if (found.Count != 1) throw new StorageLifecycleConflictException("Neo4j avatar selector is not unique.");
                var node = found[0]["a"].As<Neo4j.Driver.INode>();
                if (!node.Properties.TryGetValue("SnapshotJson", out var json)) throw new InvalidDataException("Neo4j avatar canonical snapshot is missing.");
                var plan = PlanAvatarDelete(json as string, node["Id"].As<string>(), softDelete, DateTime.UtcNow);
                var detailRows = await (await session.RunAsync("MATCH (a:AvatarDetail {Id:$id}) RETURN a", new { id = node["Id"].As<string>() })).ToListAsync();
                if (detailRows.Count > 1) throw new StorageLifecycleConflictException("Neo4j avatar-detail identity is not unique.");
                Dictionary<string, object> detailParameters = null;
                if (softDelete)
                {
                    var deletedProfile = ReadAvatarSnapshot((string)plan.Parameters["snapshot"], node["Id"].As<string>());
                    tombstone = new Core.Objects.AvatarTombstone(deletedProfile.Id, deletedProfile.DeletedDate, deletedProfile.DeletedByAvatarId);
                }
                if (detailRows.Count == 1)
                {
                    var detailNode = detailRows[0]["a"].As<Neo4j.Driver.INode>();
                    var detail = ReadDetailNode(detailNode);
                    detailParameters = new Dictionary<string, object>(plan.Parameters) { ["expected"] = (string)detailNode["SnapshotJson"] };
                    if (softDelete)
                    {
                        if (detail.IsDeleted && detail.DeletedDate != tombstone.DeletedAtUtc)
                            throw new StorageLifecycleConflictException("Neo4j profile/detail have conflicting committed deletion timestamps.");
                        detail.IsActive = false; detail.DeletedDate = tombstone.DeletedAtUtc;
                        detail.DeletedByAvatarId = tombstone.DeletedByAvatarId;
                        detailParameters["snapshot"] = OasisJson.Serialize(detail);
                    }
                }
                using var transaction = await session.BeginTransactionAsync();
                dispatched = true;
                var changed = await (await transaction.RunAsync(plan.Query, plan.Parameters)).ToListAsync();
                if (changed.Count == 0)
                {
                    dispatched = false; // Acknowledged compare failure: no mutation took place.
                    throw new StorageLifecycleConflictException("Neo4j avatar changed before deletion.");
                }
                if (changed.Count != 1 || changed[0]["affected"].As<int>() != 1)
                    throw new InvalidDataException("Neo4j deletion acknowledgement is invalid.");
                if (detailParameters != null)
                {
                    var detailQuery = plan.Query.Replace("(a:Avatar {Id:", "(a:AvatarDetail {Id:", StringComparison.Ordinal);
                    var detailChanged = await (await transaction.RunAsync(detailQuery, detailParameters)).ToListAsync();
                    if (detailChanged.Count != 1 || detailChanged[0]["affected"].As<int>() != 1)
                        throw new StorageLifecycleConflictException("Neo4j avatar detail changed before paired deletion.");
                }
                await transaction.CommitAsync();
            }
            response.Result = true;
            response.IsSaved = true;
            response.IsDeleted = true;
            if (tombstone != null) AvatarTombstoneEvidence.Attach(response, tombstone);
            response.Message = softDelete ? "Avatar soft deleted in Neo4j." : "Avatar hard deleted in Neo4j.";
        }
        catch (Exception cause)
        {
            response.IsError = true;
            response.Result = false;
            response.Exception = dispatched ? new Neo4jMutationReconciliationException(cause) : cause;
            response.Message = response.Exception.Message;
            if (cause is StorageLifecycleConflictException) response.ErrorCode = StorageLifecycleConflictException.ErrorCode;
        }
        return response;
    }
}
