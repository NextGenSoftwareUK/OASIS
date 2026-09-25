using System;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.Synchronization;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.HoloOASIS
{
    public partial class HoloOASIS
    {
        private const string HYPERDRIVE_ZOME = "oasis";
        private const string APPLY_HYPERDRIVE_MUTATION = "apply_hyperdrive_mutation";

        public string ReplicationTargetId => "HoloOASIS";

        public async Task<OASISResult<bool>> ApplyReplicatedMutationAsync(HostedSyncFanOutItem item,
            CancellationToken cancellationToken)
        {
            var result = new OASISResult<bool>();
            try
            {
                if (item == null || item.OperationId == Guid.Empty || item.AvatarId == Guid.Empty ||
                    item.EntityId == Guid.Empty || item.VersionId == Guid.Empty ||
                    string.IsNullOrWhiteSpace(item.EntityType))
                    throw new ArgumentException("A complete hosted fan-out item is required.", nameof(item));
                if (item.Kind != SyncOperationKind.Upsert && item.Kind != SyncOperationKind.Delete)
                    throw new ArgumentException("Only entity upserts and deletes can be replicated.", nameof(item));
                if (item.Kind == SyncOperationKind.Upsert && string.IsNullOrWhiteSpace(item.PayloadJson))
                    throw new ArgumentException("A replicated upsert requires a payload.", nameof(item));
                cancellationToken.ThrowIfCancellationRequested();

                if (!IsProviderActivated)
                {
                    var activation = await ActivateProviderAsync().ConfigureAwait(false);
                    if (activation == null || activation.IsError || !activation.Result)
                    {
                        OASISErrorHandling.HandleError(ref result,
                            activation?.Message ?? "HoloOASIS could not be activated for replication.",
                            activation?.Exception);
                        result.ErrorCode = activation?.ErrorCode ?? "HOLO_HYPERDRIVE_ACTIVATION_FAILED";
                        return result;
                    }
                }

                cancellationToken.ThrowIfCancellationRequested();
                var response = await HoloNETClientAppAgent.CallZomeFunctionAsync(HYPERDRIVE_ZOME,
                    APPLY_HYPERDRIVE_MUTATION, new
                    {
                        operation_id = item.OperationId.ToString("D"),
                        avatar_id = item.AvatarId.ToString("D"),
                        entity_id = item.EntityId.ToString("D"),
                        entity_type = item.EntityType,
                        kind = (byte)item.Kind,
                        version_id = item.VersionId.ToString("D"),
                        payload_json = item.PayloadJson
                    }).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
                if (response == null || response.IsError)
                {
                    OASISErrorHandling.HandleError(ref result,
                        response?.Message ?? "The Holochain HyperDrive zome returned no response.",
                        response?.Exception);
                    result.ErrorCode = "HOLO_HYPERDRIVE_REPLICATION_FAILED";
                    return result;
                }

                result.Result = true;
                result.IsSaved = true;
                result.Message = "The HyperDrive mutation was durably applied by Holochain.";
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result,
                    $"The HyperDrive mutation was not applied by Holochain. Reason: {ex.Message}", ex);
                result.ErrorCode = "HOLO_HYPERDRIVE_REPLICATION_FAILED";
            }
            return result;
        }
    }
}
