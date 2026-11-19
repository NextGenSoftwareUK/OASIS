using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.Edge.NearbySync
{
    /// <summary>
    /// Handles a request addressed to this node. The avatar id is the one bound to the verified sender key,
    /// never a value the sender claims about itself.
    /// </summary>
    public delegate Task<byte[]> NearbyRequestHandler(string senderNodeId, Guid senderAvatarId, byte[] payload, CancellationToken cancellationToken);

    /// <summary>
    /// A device on the nearby mesh: signs and sends requests, verifies and answers requests addressed to it,
    /// and relays everything else (flood with hop limit and duplicate suppression) so devices several hops
    /// apart can still sync. Works over any mix of links (BLE, Wi-Fi Direct, LAN).
    /// </summary>
    public sealed class NearbyNode : IDisposable
    {
        private sealed class Pending
        {
            public string Expected;
            public TaskCompletionSource<byte[]> Completion = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        private readonly NearbyIdentity _identity;
        private readonly NearbyNodeOptions _options;
        private readonly List<INearbyLink> _links;
        private readonly Dictionary<INearbyLink, Action<string, byte[]>> _subscriptions = new Dictionary<INearbyLink, Action<string, byte[]>>();
        private readonly Dictionary<INearbyLink, NearbyReassembler> _reassemblers = new Dictionary<INearbyLink, NearbyReassembler>();
        private readonly NearbyDedupeCache _seen;
        private readonly ConcurrentDictionary<Guid, Pending> _pending = new ConcurrentDictionary<Guid, Pending>();
        private readonly ConcurrentDictionary<string, (INearbyLink Link, string PeerId)> _routes =
            new ConcurrentDictionary<string, (INearbyLink, string)>();
        private readonly CancellationTokenSource _lifetime = new CancellationTokenSource();
        private long _messageCounter = DateTime.UtcNow.Ticks;

        public NearbyNode(NearbyIdentity identity, IEnumerable<INearbyLink> links, NearbyNodeOptions options = null)
        {
            _identity = identity ?? throw new ArgumentNullException(nameof(identity));
            _links = links?.ToList() ?? throw new ArgumentNullException(nameof(links));
            _options = options ?? new NearbyNodeOptions();
            _seen = new NearbyDedupeCache(_options.DedupeCapacity);
            foreach (var link in _links)
            {
                var captured = link;
                _reassemblers[link] = new NearbyReassembler();
                Action<string, byte[]> handler = (peerId, frame) => OnFrame(captured, peerId, frame);
                _subscriptions[link] = handler;
                link.FrameReceived += handler;
            }
        }

        public string NodeId => _identity.NodeId;

        private readonly ConcurrentDictionary<byte, NearbyRequestHandler> _handlers = new ConcurrentDictionary<byte, NearbyRequestHandler>();

        /// <summary>Registers the handler for a channel. Requests on a channel without a handler are ignored.</summary>
        public void RegisterHandler(byte channel, NearbyRequestHandler handler) => _handlers[channel] = handler ?? throw new ArgumentNullException(nameof(handler));

        public bool HasDirectConnection => _links.Any(l => l.ConnectedPeerIds.Count > 0);

        /// <summary>Sends a signed request to <paramref name="recipientNodeId"/> and awaits its signed response.</summary>
        public async Task<byte[]> RequestAsync(string recipientNodeId, byte channel, byte[] payload, TimeSpan? timeout, CancellationToken cancellationToken)
        {
            var framed = new byte[(payload?.Length ?? 0) + 1];
            framed[0] = channel;
            if (payload != null) Buffer.BlockCopy(payload, 0, framed, 1, payload.Length);
            var envelope = NewEnvelope(NearbyEnvelopeKind.Request, recipientNodeId, Guid.NewGuid(), framed);
            var pending = new Pending { Expected = recipientNodeId };
            _pending[envelope.CorrelationId] = pending;
            try
            {
                await SendAsync(envelope, null, null, cancellationToken).ConfigureAwait(false);
                using (var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _lifetime.Token))
                {
                    timeoutSource.CancelAfter(timeout ?? _options.DefaultRequestTimeout);
                    using (timeoutSource.Token.Register(() => pending.Completion.TrySetCanceled()))
                        return await pending.Completion.Task.ConfigureAwait(false);
                }
            }
            finally
            {
                _pending.TryRemove(envelope.CorrelationId, out _);
            }
        }

        private NearbyEnvelope NewEnvelope(NearbyEnvelopeKind kind, string recipient, Guid correlationId, byte[] payload)
        {
            var envelope = new NearbyEnvelope
            {
                Id = Guid.NewGuid(),
                CorrelationId = correlationId,
                Kind = kind,
                SenderNodeId = _identity.NodeId,
                RecipientNodeId = recipient,
                HopsRemaining = _options.MaximumHops,
                Payload = payload ?? Array.Empty<byte>()
            };
            envelope.Signature = _identity.Sign(envelope.GetSignedBytes());
            return envelope;
        }

        /// <summary>Sends along the learned route if there is one, otherwise floods every link except where it came from.</summary>
        private async Task SendAsync(NearbyEnvelope envelope, INearbyLink arrivedOn, string arrivedFrom, CancellationToken cancellationToken)
        {
            _seen.TryAdd(envelope.Id);
            var bytes = envelope.ToBytes();
            if (_routes.TryGetValue(envelope.RecipientNodeId, out var route) && route.Link.ConnectedPeerIds.Contains(route.PeerId))
            {
                await SendBytesAsync(route.Link, route.PeerId, bytes, cancellationToken).ConfigureAwait(false);
                return;
            }
            var targets = _links.SelectMany(l => l.ConnectedPeerIds.Select(p => (Link: l, PeerId: p)))
                .Where(t => !(ReferenceEquals(t.Link, arrivedOn) && t.PeerId == arrivedFrom)).ToList();
            if (targets.Count == 0)
                throw new InvalidOperationException("No nearby peers are connected.");
            var errors = new List<Exception>();
            var sent = 0;
            foreach (var target in targets)
            {
                try { await SendBytesAsync(target.Link, target.PeerId, bytes, cancellationToken).ConfigureAwait(false); sent++; }
                catch (Exception ex) when (!(ex is OperationCanceledException)) { errors.Add(ex); }
            }
            if (sent == 0)
                throw new AggregateException("Every nearby link failed to send.", errors);
        }

        private async Task SendBytesAsync(INearbyLink link, string peerId, byte[] bytes, CancellationToken cancellationToken)
        {
            var frames = NearbyFrameChunker.Split(bytes, link.MaximumFrameSize, Interlocked.Increment(ref _messageCounter));
            foreach (var frame in frames)
                await link.SendAsync(peerId, frame, cancellationToken).ConfigureAwait(false);
        }

        private void OnFrame(INearbyLink link, string peerId, byte[] frame)
        {
            byte[] message;
            lock (_reassemblers[link]) message = _reassemblers[link].Add(peerId, frame);
            if (message == null || !NearbyEnvelope.TryParse(message, out var envelope))
                return;
            if (!_seen.TryAdd(envelope.Id))
                return;
            _ = Task.Run(() => ProcessAsync(link, peerId, envelope));
        }

        private async Task ProcessAsync(INearbyLink link, string peerId, NearbyEnvelope envelope)
        {
            try
            {
                if (envelope.RecipientNodeId != _identity.NodeId)
                {
                    if (_options.RelayForOthers && envelope.HopsRemaining > 0)
                    {
                        envelope.HopsRemaining--;
                        await SendAsync(envelope, link, peerId, _lifetime.Token).ConfigureAwait(false);
                    }
                    return;
                }
                if (!_identity.TryVerify(envelope.SenderNodeId, envelope.GetSignedBytes(), envelope.Signature, out var avatarId))
                    return;
                _routes[envelope.SenderNodeId] = (link, peerId);

                if (envelope.Kind == NearbyEnvelopeKind.Response)
                {
                    if (_pending.TryGetValue(envelope.CorrelationId, out var pending) && pending.Expected == envelope.SenderNodeId)
                        pending.Completion.TrySetResult(envelope.Payload);
                    return;
                }

                if (envelope.Payload.Length == 0 || !_handlers.TryGetValue(envelope.Payload[0], out var handler))
                    return;
                var body = new byte[envelope.Payload.Length - 1];
                Buffer.BlockCopy(envelope.Payload, 1, body, 0, body.Length);
                var response = await handler(envelope.SenderNodeId, avatarId, body, _lifetime.Token).ConfigureAwait(false);
                var reply = NewEnvelope(NearbyEnvelopeKind.Response, envelope.SenderNodeId, envelope.CorrelationId, response);
                await SendAsync(reply, null, null, _lifetime.Token).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is OperationCanceledException || ex is InvalidOperationException ||
                                       ex is AggregateException || ex is ObjectDisposedException)
            {
                // The peer disappeared or the node is stopping. The requester times out and retries the idempotent exchange.
            }
        }

        public void Dispose()
        {
            _lifetime.Cancel();
            foreach (var pair in _subscriptions)
                pair.Key.FrameReceived -= pair.Value;
            foreach (var pending in _pending.Values)
                pending.Completion.TrySetCanceled();
            _lifetime.Dispose();
        }
    }
}
