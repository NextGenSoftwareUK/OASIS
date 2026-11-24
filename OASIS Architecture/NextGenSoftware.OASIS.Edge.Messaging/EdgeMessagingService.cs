using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.Synchronization;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Edge.NearbySync;

namespace NextGenSoftware.OASIS.Edge.Messaging
{
    /// <summary>
    /// Offline-first messaging for one avatar on one device.
    /// Durable path: every send is a HyperDrive command in the outbox, replayed idempotently by message id
    /// over whichever transport syncs (hosted HTTP, or a nearby gateway). Online reads use a server cursor.
    /// Nearby delivery is an additional direct route to a paired, reachable recipient; it never replaces the
    /// durable path, and the shared message id makes the two routes converge without duplicates.
    /// </summary>
    public sealed class EdgeMessagingService : IDisposable
    {
        public const int MaximumContentLength = 64 * 1024;
        private static readonly TimeSpan NearbyTimeout = TimeSpan.FromSeconds(5);

        private readonly Guid _avatarId;
        private readonly EdgeMessageStore _store;
        private readonly IEdgeMessageCommandQueue _queue;
        private readonly NearbyNode _node;
        private readonly NearbyIdentity _identity;

        public EdgeMessagingService(Guid avatarId, EdgeMessageStore store, IEdgeMessageCommandQueue queue,
            NearbyNode nearbyNode = null, NearbyIdentity nearbyIdentity = null)
        {
            if (avatarId == Guid.Empty) throw new ArgumentException("An avatar id is required.", nameof(avatarId));
            if ((nearbyNode == null) != (nearbyIdentity == null))
                throw new ArgumentException("Nearby delivery needs both the node and its identity, or neither.");
            _avatarId = avatarId;
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _queue = queue ?? throw new ArgumentNullException(nameof(queue));
            _node = nearbyNode;
            _identity = nearbyIdentity;
            _node?.RegisterHandler(NearbyChannels.Messaging, HandleNearbyAsync);
        }

        /// <summary>Raised when a message from someone else is stored for the first time.</summary>
        public event Action<EdgeMessage> MessageReceived;

        public IReadOnlyList<EdgeConversation> GetConversations() => _store.GetConversations(_avatarId);

        public IReadOnlyList<EdgeMessage> GetMessages(Guid otherAvatarId, int limit = 100, int offset = 0) =>
            _store.GetConversation(EdgeConversationId.Direct(_avatarId, otherAvatarId), limit, offset);

        public async Task<OASISResult<EdgeMessage>> SendAsync(Guid toAvatarId, string content, CancellationToken cancellationToken = default)
        {
            var result = new OASISResult<EdgeMessage>();
            if (toAvatarId == Guid.Empty || string.IsNullOrEmpty(content) || content.Length > MaximumContentLength)
                return Fail(result, "MESSAGE_INVALID", $"A recipient and content of 1 to {MaximumContentLength} characters are required.");

            var message = new EdgeMessage
            {
                Id = Guid.NewGuid(),
                ConversationId = EdgeConversationId.Direct(_avatarId, toAvatarId),
                FromAvatarId = _avatarId,
                ToAvatarId = toAvatarId,
                Content = content,
                ClientTimestamp = DateTime.UtcNow,
                State = EdgeMessageState.Pending
            };
            _store.Upsert(message);

            var queued = await _queue.QueueAsync(message.Id, new HyperDriveMessageCommand
            {
                Action = HyperDriveMessageAction.Send,
                ToAvatarId = toAvatarId,
                Content = content,
                MessageType = 0,
                ClientTimestamp = message.ClientTimestamp
            }, cancellationToken).ConfigureAwait(false);
            if (queued.IsError)
            {
                _store.SetState(message.Id, EdgeMessageState.Failed);
                return Fail(result, queued.ErrorCode ?? "MESSAGE_QUEUE_FAILED", queued.Message);
            }
            _store.SetState(message.Id, EdgeMessageState.Queued);

            await TryDeliverNearbyAsync(message, cancellationToken).ConfigureAwait(false);
            result.Result = _store.Get(message.Id);
            result.IsSaved = true;
            return result;
        }

        private async Task TryDeliverNearbyAsync(EdgeMessage message, CancellationToken cancellationToken)
        {
            if (_node == null || !_identity.TryFindNodeByAvatar(message.ToAvatarId, out var nodeId))
                return;
            try
            {
                var packet = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new NearbyMessagePacket { Message = message }));
                var reply = await _node.RequestAsync(nodeId, NearbyChannels.Messaging, packet, NearbyTimeout, cancellationToken).ConfigureAwait(false);
                var ack = JsonSerializer.Deserialize<NearbyMessageAck>(Encoding.UTF8.GetString(reply));
                if (ack != null && ack.Accepted)
                    _store.SetState(message.Id, EdgeMessageState.Delivered);
            }
            catch (Exception ex) when (ex is OperationCanceledException || ex is InvalidOperationException ||
                                       ex is AggregateException || ex is JsonException)
            {
                // Recipient not reachable right now. The message stays Queued and the outbox delivers it.
            }
        }

        private Task<byte[]> HandleNearbyAsync(string senderNodeId, Guid senderAvatarId, byte[] payload, CancellationToken cancellationToken)
        {
            NearbyMessageAck ack;
            try
            {
                var message = JsonSerializer.Deserialize<NearbyMessagePacket>(Encoding.UTF8.GetString(payload))?.Message;
                if (message == null || message.Id == Guid.Empty || string.IsNullOrEmpty(message.Content) || message.Content.Length > MaximumContentLength)
                    ack = new NearbyMessageAck { Reason = "invalid" };
                else if (message.FromAvatarId != senderAvatarId)
                    ack = new NearbyMessageAck { Reason = "sender-mismatch" };
                else if (message.ToAvatarId != _avatarId)
                    ack = new NearbyMessageAck { Reason = "wrong-recipient" };
                else
                {
                    message.ConversationId = EdgeConversationId.Direct(message.FromAvatarId, message.ToAvatarId);
                    message.State = EdgeMessageState.Received;
                    message.ServerReceivedAt = null;
                    message.ReceiptQueued = false;
                    if (_store.Upsert(message))
                        MessageReceived?.Invoke(_store.Get(message.Id));
                    ack = new NearbyMessageAck { Accepted = true };
                }
            }
            catch (JsonException)
            {
                ack = new NearbyMessageAck { Reason = "invalid" };
            }
            return Task.FromResult(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(ack)));
        }

        /// <summary>
        /// Pulls everything after the stored cursor, merges it, and queues delivery receipts for incoming messages
        /// the server has not yet seen as delivered. Returns the number of messages new to this device.
        /// </summary>
        public async Task<OASISResult<int>> SyncFromServerAsync(IEdgeMessagingApi api, CancellationToken cancellationToken = default)
        {
            var result = new OASISResult<int>();
            var newCount = 0;
            var needReceipts = new List<Guid>();
            while (true)
            {
                var page = await api.SyncAsync(_store.ServerCursor, 200, cancellationToken).ConfigureAwait(false);
                if (page.IsError)
                    return Fail(result, page.ErrorCode ?? "MESSAGING_SYNC_FAILED", page.Message);

                foreach (var server in page.Result.Messages)
                {
                    var incoming = server.ToAvatarId == _avatarId;
                    var message = new EdgeMessage
                    {
                        Id = server.Id,
                        ConversationId = EdgeConversationId.Direct(server.FromAvatarId, server.ToAvatarId),
                        FromAvatarId = server.FromAvatarId,
                        ToAvatarId = server.ToAvatarId,
                        Content = server.Content,
                        MessageType = server.MessageType,
                        ClientTimestamp = server.ClientTimestamp ?? server.Timestamp,
                        ServerReceivedAt = server.ReceivedAt,
                        State = server.DeliveryState >= 2 || server.IsRead ? EdgeMessageState.Read
                            : incoming ? EdgeMessageState.Received
                            : server.DeliveryState == 1 ? EdgeMessageState.Delivered : EdgeMessageState.Sent
                    };
                    var isNew = _store.Upsert(message);
                    if (isNew) { newCount++; if (incoming) MessageReceived?.Invoke(_store.Get(message.Id)); }
                    if (incoming && server.DeliveryState == 0 && !(_store.Get(message.Id)?.ReceiptQueued ?? false))
                        needReceipts.Add(message.Id);
                }
                if (page.Result.NextCursor.HasValue)
                    _store.SetServerCursor(page.Result.NextCursor);
                if (!page.Result.HasMore)
                    break;
            }

            if (needReceipts.Count > 0)
            {
                var queued = await _queue.QueueAsync(Guid.NewGuid(), new HyperDriveMessageCommand
                {
                    Action = HyperDriveMessageAction.MarkDelivered, MessageIds = needReceipts
                }, cancellationToken).ConfigureAwait(false);
                if (queued.IsError)
                    return Fail(result, queued.ErrorCode ?? "MESSAGE_RECEIPT_QUEUE_FAILED", queued.Message);
                _store.MarkReceiptQueued(needReceipts);
            }
            result.Result = newCount;
            return result;
        }

        /// <summary>Marks incoming messages read locally and queues the read receipt.</summary>
        public async Task<OASISResult<bool>> MarkReadAsync(IEnumerable<Guid> messageIds, CancellationToken cancellationToken = default)
        {
            var result = new OASISResult<bool>();
            var ids = (messageIds ?? Enumerable.Empty<Guid>()).Distinct()
                .Where(id => { var m = _store.Get(id); return m != null && m.ToAvatarId == _avatarId; }).ToList();
            if (ids.Count == 0) { result.Result = true; return result; }
            var queued = await _queue.QueueAsync(Guid.NewGuid(), new HyperDriveMessageCommand
            {
                Action = HyperDriveMessageAction.MarkRead, MessageIds = ids
            }, cancellationToken).ConfigureAwait(false);
            if (queued.IsError)
            {
                result.IsError = true; result.ErrorCode = queued.ErrorCode; result.Message = queued.Message;
                return result;
            }
            foreach (var id in ids) _store.SetState(id, EdgeMessageState.Read);
            result.Result = true;
            return result;
        }

        private static OASISResult<T> Fail<T>(OASISResult<T> r, string code, string message)
        {
            r.IsError = true; r.ErrorCode = code; r.Message = message;
            return r;
        }

        public void Dispose() { }
    }
}
