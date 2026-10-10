using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.Synchronization;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.HoloOASIS.Edge
{
    /// <summary>
    /// Lightweight Holochain domain replication target. The hApp persists operation_id with the
    /// mutation and returns the original record on replay, providing the idempotency invariant.
    /// SQLite remains the atomic Edge journal until a Holo journal passes the same fault suite.
    /// </summary>
    public sealed class HoloEdgeMutationRepository : IHyperDriveIdempotentReplicationTarget,
        IHyperDriveLocalReplicationTarget
    {
        public const string DefaultZome = "oasis";
        public const string ApplyMutationFunction = "apply_hyperdrive_mutation";
        private IHoloEdgeAppClient _client;
        private readonly string _zome;
        private readonly SemaphoreSlim _clientGate = new SemaphoreSlim(1, 1);

        public string ReplicationTargetId => "HoloOASIS.Edge";
        public string LocalReplicationTargetId => ReplicationTargetId;

        public HoloEdgeMutationRepository(IHoloEdgeAppClient client, string zome = DefaultZome)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            if (string.IsNullOrWhiteSpace(zome)) throw new ArgumentException("A Holochain zome is required.", nameof(zome));
            _zome = zome;
        }

        public Task<OASISResult<bool>> ApplyReplicatedMutationAsync(HostedSyncFanOutItem item,
            CancellationToken cancellationToken) => ApplyAsync(item?.OperationId ?? Guid.Empty,
                item?.AvatarId ?? Guid.Empty, item?.EntityId ?? Guid.Empty, item?.EntityType,
                item?.Kind ?? SyncOperationKind.Command, item?.VersionId ?? Guid.Empty,
                item?.PayloadJson, cancellationToken);

        public Task<OASISResult<bool>> ApplyLocalMutationAsync(SyncOperation operation,
            CancellationToken cancellationToken) => ApplyAsync(operation?.OperationId ?? Guid.Empty,
                operation?.AvatarId ?? Guid.Empty, operation?.EntityId ?? Guid.Empty, operation?.EntityType,
                operation?.Kind ?? SyncOperationKind.Command, operation?.VersionId ?? Guid.Empty,
                operation?.PayloadJson, cancellationToken);

        private async Task<OASISResult<bool>> ApplyAsync(Guid operationId, Guid avatarId, Guid entityId,
            string entityType, SyncOperationKind kind, Guid versionId, string payloadJson,
            CancellationToken cancellationToken)
        {
            var error = Validate(operationId, avatarId, entityId, entityType, kind, versionId, payloadJson);
            if (error != null) return error;

            IReadOnlyDictionary<string, object> payload = new Dictionary<string, object>
            {
                ["operation_id"] = operationId.ToString("D"),
                ["avatar_id"] = avatarId.ToString("D"),
                ["entity_id"] = entityId.ToString("D"),
                ["entity_type"] = entityType,
                ["kind"] = (int)kind,
                ["version_id"] = versionId.ToString("D"),
                ["payload_json"] = kind == SyncOperationKind.Delete ? null : payloadJson
            };
            await _clientGate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (_client == null)
                    return Error("HOLO_EDGE_SESSION_UNAVAILABLE",
                        "The Holochain app session is suspended or disconnected; the durable projection remains queued.");
                return await _client.CallAsync(_zome, ApplyMutationFunction, payload, cancellationToken)
                    .ConfigureAwait(false);
            }
            finally { _clientGate.Release(); }
        }

        internal async Task AttachClientAsync(IHoloEdgeAppClient client, CancellationToken cancellationToken)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));
            await _clientGate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (_client != null) throw new InvalidOperationException("A Holochain app session is already attached.");
                _client = client;
            }
            finally { _clientGate.Release(); }
        }

        internal async Task DetachClientAsync()
        {
            await _clientGate.WaitAsync().ConfigureAwait(false);
            try
            {
                if (_client == null) return;
                await _client.DisposeAsync().ConfigureAwait(false);
                _client = null;
            }
            finally { _clientGate.Release(); }
        }

        private static OASISResult<bool> Validate(Guid operationId, Guid avatarId, Guid entityId,
            string entityType, SyncOperationKind kind, Guid versionId, string payloadJson)
        {
            if (operationId == Guid.Empty || avatarId == Guid.Empty || entityId == Guid.Empty ||
                versionId == Guid.Empty || string.IsNullOrWhiteSpace(entityType))
                return Error("HOLO_EDGE_MUTATION_INVALID", "Operation, avatar, entity, version and entity type are required.");
            if (kind == SyncOperationKind.Command)
                return Error("HOLO_EDGE_COMMAND_NOT_REPLICABLE",
                    "Hosted-authoritative commands cannot be persisted as Holochain domain mutations.");
            if (kind != SyncOperationKind.Upsert && kind != SyncOperationKind.Delete)
                return Error("HOLO_EDGE_MUTATION_KIND_INVALID", "Only upsert and delete domain mutations are supported.");
            if (kind == SyncOperationKind.Upsert && string.IsNullOrWhiteSpace(payloadJson))
                return Error("HOLO_EDGE_PAYLOAD_REQUIRED", "A Holochain upsert requires a payload.");
            return null;
        }

        private static OASISResult<bool> Error(string code, string message) => new OASISResult<bool>
        {
            IsError = true,
            ErrorCount = 1,
            ErrorCode = code,
            Message = message
        };
    }
}
