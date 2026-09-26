using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.Synchronization;
using AvatarDetail = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities.AvatarDetail;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS
{
    public partial class MongoDBOASIS : IHostedAvatarGameplayCommandStore
    {
        public async Task<OASISResult<HyperDriveAvatarDetailProjection>> ApplyAvatarGameplayCommandAsync(
            HostedSyncCommandItem command, CancellationToken cancellationToken)
        {
            if (command == null || command.OperationId == Guid.Empty || command.AvatarId == Guid.Empty ||
                command.EntityId != command.AvatarId || command.EntityType != HyperDriveEntityTypes.AvatarGameplay)
                return GameplayRejected("An avatar-scoped command with a stable operation identity is required.");
            HyperDriveAvatarGameplayCommand payload;
            try { payload = HyperDriveJson.Deserialize<HyperDriveAvatarGameplayCommand>(command.PayloadJson); }
            catch (System.Text.Json.JsonException ex) { return GameplayRejected(ex.Message); }
            await EnsureSyncInitializedAsync(cancellationToken).ConfigureAwait(false);
            using var session = await Database.MongoClient.StartSessionAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            session.StartTransaction();
            try
            {
                var collection = Database.MongoDB.GetCollection<AvatarDetail>("AvatarDetail");
                var detail = await collection.Find(session, x => x.HolonId == command.AvatarId)
                    .SingleOrDefaultAsync(cancellationToken).ConfigureAwait(false);
                if (detail == null) return GameplayRejected("The avatar detail was not found.");
                detail.MetaData ??= new Dictionary<string, object>();
                var receipts = detail.MetaData.TryGetValue(HyperDriveAvatarGameplay.ReceiptMetadataKey, out var value)
                    ? JsonConvert.DeserializeObject<Dictionary<Guid, string>>(value.ToString())
                    : new Dictionary<Guid, string>();
                var canonicalPayload = HyperDriveJson.Serialize(payload);
                if (receipts.TryGetValue(command.OperationId, out var original))
                    return original == canonicalPayload
                        ? new OASISResult<HyperDriveAvatarDetailProjection>(CreateEdgeAvatarDetailProjection(detail))
                        : GameplayRejected("The operation identity was reused with a different payload.");
                var applied = HyperDriveAvatarGameplay.Apply(CreateEdgeAvatarDetailProjection(detail), payload);
                if (applied.IsError) return applied;
                detail.XP = applied.Result.Xp;
                detail.ActiveQuestId = applied.Result.ActiveQuestId;
                detail.ActiveObjectiveId = applied.Result.ActiveObjectiveId;
                var quantities = applied.Result.Inventory.ToDictionary(x => x.Id, x => x.Quantity);
                if (detail.Inventory != null)
                {
                    detail.Inventory = detail.Inventory.Where(x => quantities.ContainsKey(x.Id)).ToList();
                    foreach (var item in detail.Inventory) item.Quantity = quantities[item.Id];
                }
                receipts.Add(command.OperationId, canonicalPayload);
                detail.MetaData[HyperDriveAvatarGameplay.ReceiptMetadataKey] = JsonConvert.SerializeObject(receipts);
                detail.VersionId = Guid.NewGuid();
                await collection.ReplaceOneAsync(session, x => x.Id == detail.Id, detail,
                    cancellationToken: cancellationToken).ConfigureAwait(false);
                // The domain change stream projects this document, including its receipt, as one version.
                await session.CommitTransactionAsync(cancellationToken).ConfigureAwait(false);
                return new OASISResult<HyperDriveAvatarDetailProjection>(CreateEdgeAvatarDetailProjection(detail));
            }
            finally
            {
                if (session.IsInTransaction) await session.AbortTransactionAsync(CancellationToken.None).ConfigureAwait(false);
            }
        }

        private static OASISResult<HyperDriveAvatarDetailProjection> GameplayRejected(string message) => new OASISResult<HyperDriveAvatarDetailProjection>
        { IsError = true, ErrorCode = "AVATAR_GAMEPLAY_REJECTED", Message = message };
    }
}
