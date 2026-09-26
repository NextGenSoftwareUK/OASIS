using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.Synchronization;
using NextGenSoftware.OASIS.API.Providers.EdgeSQLiteOASIS;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.Edge.Runtime
{
    public interface IEdgePayloadSerializer
    {
        string Serialize<T>(T value);
        T Deserialize<T>(string json);
    }

    public sealed class EdgeEntityWriteRequest<T>
    {
        public Guid OperationId { get; set; }
        public Guid EntityId { get; set; }
        public string EntityType { get; set; }
        public Guid BaseVersionId { get; set; }
        public Guid VersionId { get; set; }
        public T Entity { get; set; }
        public DateTime CreatedUtc { get; set; }
    }

    public sealed class EdgeEntityDeleteRequest
    {
        public Guid OperationId { get; set; }
        public Guid EntityId { get; set; }
        public string EntityType { get; set; }
        public Guid BaseVersionId { get; set; }
        public Guid VersionId { get; set; }
        public DateTime CreatedUtc { get; set; }
    }

    public sealed class EdgeCommandRequest<T>
    {
        public Guid OperationId { get; set; }
        public Guid EntityId { get; set; }
        public string EntityType { get; set; }
        public Guid VersionId { get; set; }
        public T Payload { get; set; }
        public DateTime CreatedUtc { get; set; }
    }

    public sealed class EdgeEntityReadResult<T>
    {
        public Guid EntityId { get; set; }
        public string EntityType { get; set; }
        public Guid VersionId { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime ChangedUtc { get; set; }
        public T Entity { get; set; }
    }

    /// <summary>
    /// Typed entity boundary for Edge clients. Every write delegates to the runtime's atomic
    /// local-state/outbox transaction so an acknowledged offline write is always synchronizable.
    /// </summary>
    public sealed class EdgeEntityRepository
    {
        private readonly Guid _deviceId;
        private readonly Guid _avatarId;
        private readonly EdgeSQLiteSyncStateStore _store;
        private readonly Func<EdgeLocalMutation, CancellationToken, Task<OASISResult<SyncOperation>>> _save;
        private readonly IEdgePayloadSerializer _serializer;

        internal EdgeEntityRepository(Guid deviceId, Guid avatarId, EdgeSQLiteSyncStateStore store,
            Func<EdgeLocalMutation, CancellationToken, Task<OASISResult<SyncOperation>>> save,
            IEdgePayloadSerializer serializer)
        {
            _deviceId = deviceId;
            _avatarId = avatarId;
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _save = save ?? throw new ArgumentNullException(nameof(save));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        }

        public Task<OASISResult<SyncOperation>> SaveAsync<T>(EdgeEntityWriteRequest<T> request,
            CancellationToken cancellationToken = default)
        {
            var validation = Validate(request?.OperationId ?? Guid.Empty, request?.EntityId ?? Guid.Empty,
                request?.EntityType, request?.VersionId ?? Guid.Empty);
            if (validation != null) return Task.FromResult(Error<SyncOperation>("EDGE_ENTITY_WRITE_INVALID", validation));
            if (request.Entity == null)
                return Task.FromResult(Error<SyncOperation>("EDGE_ENTITY_PAYLOAD_REQUIRED", "An edge entity upsert requires a payload."));

            string payload;
            try { payload = _serializer.Serialize(request.Entity); }
            catch (Exception ex)
            {
                return Task.FromResult(Error<SyncOperation>("EDGE_ENTITY_SERIALIZATION_FAILED",
                    $"The edge entity payload could not be serialized: {ex.Message}"));
            }
            if (string.IsNullOrWhiteSpace(payload))
                return Task.FromResult(Error<SyncOperation>("EDGE_ENTITY_SERIALIZATION_EMPTY", "The edge entity serializer returned an empty payload."));

            return _save(ToMutation(request.OperationId, request.EntityId, request.EntityType,
                SyncOperationKind.Upsert, request.BaseVersionId, request.VersionId, payload, request.CreatedUtc), cancellationToken);
        }

        public Task<OASISResult<SyncOperation>> DeleteAsync(EdgeEntityDeleteRequest request,
            CancellationToken cancellationToken = default)
        {
            var validation = Validate(request?.OperationId ?? Guid.Empty, request?.EntityId ?? Guid.Empty,
                request?.EntityType, request?.VersionId ?? Guid.Empty);
            if (validation != null) return Task.FromResult(Error<SyncOperation>("EDGE_ENTITY_DELETE_INVALID", validation));
            return _save(ToMutation(request.OperationId, request.EntityId, request.EntityType,
                SyncOperationKind.Delete, request.BaseVersionId, request.VersionId, null, request.CreatedUtc), cancellationToken);
        }

        public Task<OASISResult<SyncOperation>> QueueCommandAsync<T>(EdgeCommandRequest<T> request,
            CancellationToken cancellationToken = default)
        {
            var validation = Validate(request?.OperationId ?? Guid.Empty, request?.EntityId ?? Guid.Empty,
                request?.EntityType, request?.VersionId ?? Guid.Empty);
            if (validation != null) return Task.FromResult(Error<SyncOperation>("EDGE_COMMAND_INVALID", validation));
            if (request.Payload == null)
                return Task.FromResult(Error<SyncOperation>("EDGE_COMMAND_PAYLOAD_REQUIRED", "An edge command requires a payload."));
            string payload;
            try { payload = _serializer.Serialize(request.Payload); }
            catch (Exception ex)
            {
                return Task.FromResult(Error<SyncOperation>("EDGE_COMMAND_SERIALIZATION_FAILED",
                    $"The edge command payload could not be serialized: {ex.Message}"));
            }
            return _save(ToMutation(request.OperationId, request.EntityId, request.EntityType,
                SyncOperationKind.Command, Guid.Empty, request.VersionId, payload, request.CreatedUtc), cancellationToken);
        }

        public Task<OASISResult<EdgeEntityReadResult<HyperDriveCommandOutcome>>> LoadCommandOutcomeAsync(
            Guid operationId, CancellationToken cancellationToken = default) =>
            LoadAsync<HyperDriveCommandOutcome>(HyperDriveEntityTypes.CommandResult, operationId, cancellationToken);

        public Task<OASISResult<SyncOperation>> QueueQuestProgressAsync(Guid operationId, Guid questId,
            HyperDriveQuestProgressCommand payload, CancellationToken cancellationToken = default) =>
            QueueCommandAsync(new EdgeCommandRequest<HyperDriveQuestProgressCommand>
            {
                OperationId = operationId, EntityId = questId,
                EntityType = HyperDriveEntityTypes.QuestProgress, VersionId = Guid.NewGuid(),
                Payload = payload, CreatedUtc = DateTime.UtcNow
            }, cancellationToken);

        public Task<OASISResult<SyncOperation>> QueueInventoryGrantAsync(Guid operationId, Guid itemId,
            HyperDriveInventoryGrantCommand payload, CancellationToken cancellationToken = default) =>
            QueueCommandAsync(new EdgeCommandRequest<HyperDriveInventoryGrantCommand>
            {
                OperationId = operationId, EntityId = itemId,
                EntityType = HyperDriveEntityTypes.InventoryItem, VersionId = Guid.NewGuid(),
                Payload = payload, CreatedUtc = DateTime.UtcNow
            }, cancellationToken);

        public Task<OASISResult<SyncOperation>> QueueGeoNftCollectionAsync(Guid operationId, Guid geoNftId,
            HyperDriveGeoNftCollectionCommand payload, CancellationToken cancellationToken = default) =>
            QueueCommandAsync(new EdgeCommandRequest<HyperDriveGeoNftCollectionCommand>
            {
                OperationId = operationId, EntityId = geoNftId,
                EntityType = HyperDriveEntityTypes.GeoNftCollection, VersionId = Guid.NewGuid(),
                Payload = payload, CreatedUtc = DateTime.UtcNow
            }, cancellationToken);

        public async Task<OASISResult<EdgeEntityReadResult<T>>> LoadAsync<T>(string entityType, Guid entityId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(entityType) || entityId == Guid.Empty)
                return Error<EdgeEntityReadResult<T>>("EDGE_ENTITY_READ_INVALID", "A non-empty entity type and entity id are required.");

            var stored = await _store.LoadEntityAsync(entityType, entityId, cancellationToken).ConfigureAwait(false);
            if (stored == null)
                return Error<EdgeEntityReadResult<T>>("EDGE_ENTITY_READ_FAILED", "The local edge store returned no result.");
            if (stored.IsError)
                return Error<EdgeEntityReadResult<T>>(stored.ErrorCode ?? "EDGE_ENTITY_READ_FAILED", stored.Message);
            if (stored.Result == null)
                return new OASISResult<EdgeEntityReadResult<T>> { IsLoaded = true, Message = "The edge entity was not found." };

            T entity = default;
            if (!stored.Result.IsDeleted)
            {
                try { entity = _serializer.Deserialize<T>(stored.Result.PayloadJson); }
                catch (Exception ex)
                {
                    return Error<EdgeEntityReadResult<T>>("EDGE_ENTITY_DESERIALIZATION_FAILED",
                        $"The stored edge entity payload could not be deserialized: {ex.Message}");
                }
            }
            return new OASISResult<EdgeEntityReadResult<T>>
            {
                IsLoaded = true,
                Result = new EdgeEntityReadResult<T>
                {
                    EntityId = stored.Result.EntityId,
                    EntityType = stored.Result.EntityType,
                    VersionId = stored.Result.VersionId,
                    IsDeleted = stored.Result.IsDeleted,
                    ChangedUtc = stored.Result.ChangedUtc,
                    Entity = entity
                }
            };
        }

        public async Task<OASISResult<IReadOnlyList<EdgeEntityReadResult<T>>>> LoadAllAsync<T>(string entityType,
            bool includeDeleted = false, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(entityType))
                return Error<IReadOnlyList<EdgeEntityReadResult<T>>>("EDGE_ENTITY_TYPE_REQUIRED", "An entity type is required.");
            var stored = await _store.LoadEntitiesAsync(entityType, includeDeleted, cancellationToken).ConfigureAwait(false);
            if (stored == null || stored.IsError)
                return Error<IReadOnlyList<EdgeEntityReadResult<T>>>(stored?.ErrorCode ?? "EDGE_ENTITIES_READ_FAILED",
                    stored?.Message ?? "The local edge store returned no result.");
            var entities = new List<EdgeEntityReadResult<T>>();
            foreach (var row in stored.Result ?? Array.Empty<EdgeStoredEntity>())
            {
                T entity = default;
                if (!row.IsDeleted)
                {
                    try { entity = _serializer.Deserialize<T>(row.PayloadJson); }
                    catch (Exception ex)
                    {
                        return Error<IReadOnlyList<EdgeEntityReadResult<T>>>("EDGE_ENTITY_DESERIALIZATION_FAILED",
                            $"Stored edge entity '{row.EntityId}' could not be deserialized: {ex.Message}");
                    }
                }
                entities.Add(new EdgeEntityReadResult<T>
                {
                    EntityId = row.EntityId, EntityType = row.EntityType, VersionId = row.VersionId,
                    IsDeleted = row.IsDeleted, ChangedUtc = row.ChangedUtc, Entity = entity
                });
            }
            return new OASISResult<IReadOnlyList<EdgeEntityReadResult<T>>>(entities) { IsLoaded = true };
        }

        private EdgeLocalMutation ToMutation(Guid operationId, Guid entityId, string entityType,
            SyncOperationKind kind, Guid baseVersionId, Guid versionId, string payloadJson, DateTime createdUtc) => new EdgeLocalMutation
        {
            OperationId = operationId,
            DeviceId = _deviceId,
            AvatarId = _avatarId,
            EntityId = entityId,
            EntityType = entityType,
            Kind = kind,
            BaseVersionId = baseVersionId,
            LocalVersionId = versionId,
            PayloadJson = payloadJson,
            CreatedUtc = createdUtc
        };

        private static string Validate(Guid operationId, Guid entityId, string entityType, Guid versionId)
        {
            if (operationId == Guid.Empty) return "A stable operation id is required for idempotent synchronization.";
            if (entityId == Guid.Empty) return "A non-empty entity id is required.";
            if (string.IsNullOrWhiteSpace(entityType)) return "A non-empty entity type is required.";
            if (versionId == Guid.Empty) return "A non-empty local version id is required.";
            return null;
        }

        private static OASISResult<T> Error<T>(string code, string message) => new OASISResult<T>
        { IsError = true, ErrorCount = 1, ErrorCode = code, Message = message };
    }
}
