using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MessagePack;
using NextGenSoftware.Holochain.HoloNET.Client.Interfaces;
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
                var receipt = response.Records?.Count == 1 ? response.Records[0] : null;
                if (!ReceiptMatchesMutation(receipt, item))
                {
                    OASISErrorHandling.HandleError(ref result,
                        "Holochain did not return the native receipt for the submitted mutation.");
                    result.ErrorCode = "HOLO_HYPERDRIVE_RECEIPT_INVALID";
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

        private static bool ReceiptMatchesMutation(IRecord receipt, HostedSyncFanOutItem item)
        {
            if (string.IsNullOrWhiteSpace(receipt?.ActionHash) || receipt.Bytes is not { Length: > 0 })
                return false;

            try
            {
                // Use the native entry so a null payload cannot match a JSON string containing "null".
                var options = MessagePackSerializerOptions.Standard.WithSecurity(MessagePackSecurity.UntrustedData);
                using var document = JsonDocument.Parse(MessagePackSerializer.ConvertToJson(receipt.Bytes, options));
                var entry = document.RootElement;
                return entry.ValueKind == JsonValueKind.Object &&
                    ReceiptTextMatches(entry, "operation_id", item.OperationId.ToString("D")) &&
                    ReceiptTextMatches(entry, "avatar_id", item.AvatarId.ToString("D")) &&
                    ReceiptTextMatches(entry, "entity_id", item.EntityId.ToString("D")) &&
                    ReceiptTextMatches(entry, "entity_type", item.EntityType) &&
                    ReceiptTextMatches(entry, "version_id", item.VersionId.ToString("D")) &&
                    entry.TryGetProperty("kind", out var kind) && kind.ValueKind == JsonValueKind.Number &&
                    kind.TryGetByte(out var operation) && operation == (byte)item.Kind &&
                    ReceiptTextMatches(entry, "payload_json", item.PayloadJson);
            }
            catch (MessagePackSerializationException) { return false; }
            catch (JsonException) { return false; }
        }

        private static bool ReceiptTextMatches(JsonElement entry, string key, string expected)
        {
            if (!entry.TryGetProperty(key, out var value))
                return false;

            return expected == null
                ? value.ValueKind == JsonValueKind.Null
                : value.ValueKind == JsonValueKind.String && string.Equals(value.GetString(), expected, StringComparison.Ordinal);
        }
    }
}
