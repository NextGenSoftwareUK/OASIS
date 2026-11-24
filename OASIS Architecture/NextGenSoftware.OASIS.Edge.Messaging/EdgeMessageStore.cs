using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace NextGenSoftware.OASIS.Edge.Messaging
{
    /// <summary>
    /// Durable local message log. With a path it persists atomically (write temp, then replace) so a crash or
    /// power loss never leaves a half-written file; with no path it is in-memory (tests).
    /// </summary>
    public sealed class EdgeMessageStore
    {
        private sealed class Snapshot
        {
            public List<EdgeMessage> Messages { get; set; } = new List<EdgeMessage>();
            public DateTime? ServerCursor { get; set; }
        }

        private readonly object _gate = new object();
        private readonly string _path;
        private readonly Dictionary<Guid, EdgeMessage> _messages = new Dictionary<Guid, EdgeMessage>();
        private DateTime? _cursor;

        public EdgeMessageStore(string path = null)
        {
            _path = path;
            if (path != null && File.Exists(path))
            {
                var snapshot = JsonSerializer.Deserialize<Snapshot>(File.ReadAllText(path)) ??
                    throw new InvalidDataException($"The message store '{path}' is empty or corrupt.");
                foreach (var m in snapshot.Messages) _messages[m.Id] = m;
                _cursor = snapshot.ServerCursor;
            }
        }

        public DateTime? ServerCursor { get { lock (_gate) return _cursor; } }

        public void SetServerCursor(DateTime? cursor)
        {
            lock (_gate) { _cursor = cursor; Persist(); }
        }

        public EdgeMessage Get(Guid id)
        {
            lock (_gate) return _messages.TryGetValue(id, out var m) ? Clone(m) : null;
        }

        /// <summary>
        /// Inserts a message or merges into the stored one. Content is immutable once stored (first write
        /// wins); state only moves forward. Returns true when the message was new to this device.
        /// </summary>
        public bool Upsert(EdgeMessage incoming)
        {
            lock (_gate)
            {
                var isNew = !_messages.TryGetValue(incoming.Id, out var existing);
                if (isNew)
                    _messages[incoming.Id] = Clone(incoming);
                else
                {
                    if (Advances(existing.State, incoming.State))
                        existing.State = incoming.State;
                    existing.ServerReceivedAt = existing.ServerReceivedAt ?? incoming.ServerReceivedAt;
                }
                Persist();
                return isNew;
            }
        }

        public bool MarkReceiptQueued(IEnumerable<Guid> ids)
        {
            lock (_gate)
            {
                foreach (var id in ids)
                    if (_messages.TryGetValue(id, out var m)) m.ReceiptQueued = true;
                Persist();
                return true;
            }
        }

        public bool SetState(Guid id, EdgeMessageState state)
        {
            lock (_gate)
            {
                if (!_messages.TryGetValue(id, out var m)) return false;
                if (Advances(m.State, state) || state == EdgeMessageState.Failed) m.State = state;
                Persist();
                return true;
            }
        }

        public IReadOnlyList<EdgeMessage> GetConversation(Guid conversationId, int limit = 100, int offset = 0)
        {
            lock (_gate)
                return _messages.Values.Where(m => m.ConversationId == conversationId)
                    .OrderBy(m => m.ClientTimestamp).ThenBy(m => m.Id).Skip(offset).Take(limit).Select(Clone).ToList();
        }

        public IReadOnlyList<EdgeConversation> GetConversations(Guid myAvatarId)
        {
            lock (_gate)
                return _messages.Values.GroupBy(m => m.ConversationId).Select(g =>
                {
                    var last = g.OrderByDescending(m => m.ClientTimestamp).ThenBy(m => m.Id).First();
                    return new EdgeConversation
                    {
                        ConversationId = g.Key,
                        OtherAvatarId = last.FromAvatarId == myAvatarId ? last.ToAvatarId : last.FromAvatarId,
                        LastMessage = Clone(last),
                        UnreadCount = g.Count(m => m.ToAvatarId == myAvatarId && m.State == EdgeMessageState.Received)
                    };
                }).OrderByDescending(c => c.LastMessage.ClientTimestamp).ToList();
        }

        private static bool Advances(EdgeMessageState from, EdgeMessageState to) =>
            from != EdgeMessageState.Failed && (int)to > (int)from && to != EdgeMessageState.Failed;

        private static EdgeMessage Clone(EdgeMessage m) => new EdgeMessage
        {
            Id = m.Id, ConversationId = m.ConversationId, FromAvatarId = m.FromAvatarId, ToAvatarId = m.ToAvatarId,
            Content = m.Content, MessageType = m.MessageType, ClientTimestamp = m.ClientTimestamp,
            ServerReceivedAt = m.ServerReceivedAt, State = m.State, ReceiptQueued = m.ReceiptQueued
        };

        private void Persist()
        {
            if (_path == null) return;
            var directory = Path.GetDirectoryName(Path.GetFullPath(_path));
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            var temp = _path + ".tmp";
            File.WriteAllText(temp, JsonSerializer.Serialize(new Snapshot { Messages = _messages.Values.ToList(), ServerCursor = _cursor }));
            if (File.Exists(_path)) File.Replace(temp, _path, null);
            else File.Move(temp, _path);
        }
    }
}
