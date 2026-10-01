using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.ONET
{
    public enum ONETRequestResponseMessageKind { Request = 0, Response = 1 }

    public sealed class ONETRequestResponseEnvelope
    {
        public const int CurrentProtocolVersion = 1;
        public int ProtocolVersion { get; set; } = CurrentProtocolVersion;
        public Guid CorrelationId { get; set; }
        public ONETRequestResponseMessageKind Kind { get; set; }
        public string Operation { get; set; } = string.Empty;
        public string PayloadJson { get; set; } = string.Empty;
        public bool IsError { get; set; }
        public string ErrorCode { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
    }

    public sealed class ONETApplicationMessage
    {
        public string SourceNodeId { get; set; } = string.Empty;
        public string TargetNodeId { get; set; } = string.Empty;
        public ONETRequestResponseEnvelope Envelope { get; set; } = new ONETRequestResponseEnvelope();
    }

    public interface IONETApplicationMessageChannel
    {
        string LocalNodeId { get; }
        event EventHandler<ONETApplicationMessage> MessageReceived;
        Task<OASISResult<bool>> SendAsync(ONETApplicationMessage message, CancellationToken cancellationToken);
    }

    public sealed class ONETRequestContext
    {
        public string SourceNodeId { get; set; } = string.Empty;
        public string Operation { get; set; } = string.Empty;
        public string PayloadJson { get; set; } = string.Empty;
    }

    public sealed class ONETRequestResponseEndpoint : IDisposable
    {
        private sealed class PendingRequest
        {
            public string ExpectedSourceNodeId { get; set; } = string.Empty;
            public string ExpectedOperation { get; set; } = string.Empty;
            public TaskCompletionSource<ONETRequestResponseEnvelope> Completion { get; } =
                new TaskCompletionSource<ONETRequestResponseEnvelope>(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        private readonly IONETApplicationMessageChannel _channel;
        private readonly ConcurrentDictionary<Guid, PendingRequest> _pending = new ConcurrentDictionary<Guid, PendingRequest>();
        private readonly ConcurrentDictionary<string, Func<ONETRequestContext, CancellationToken, Task<OASISResult<string>>>> _handlers =
            new ConcurrentDictionary<string, Func<ONETRequestContext, CancellationToken, Task<OASISResult<string>>>>(StringComparer.Ordinal);
        private bool _disposed;

        public ONETRequestResponseEndpoint(IONETApplicationMessageChannel channel)
        {
            _channel = channel ?? throw new ArgumentNullException(nameof(channel));
            if (string.IsNullOrWhiteSpace(channel.LocalNodeId))
                throw new ArgumentException("The ONET channel must have a local node identifier.", nameof(channel));
            _channel.MessageReceived += OnMessageReceived;
        }

        public void RegisterHandler(string operation,
            Func<ONETRequestContext, CancellationToken, Task<OASISResult<string>>> handler)
        {
            ThrowIfDisposed();
            if (string.IsNullOrWhiteSpace(operation)) throw new ArgumentException("An operation is required.", nameof(operation));
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            if (!_handlers.TryAdd(operation, handler))
                throw new InvalidOperationException($"An ONET handler is already registered for '{operation}'.");
        }

        public async Task<OASISResult<string>> RequestAsync(string targetNodeId, string operation,
            string payloadJson, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            var result = new OASISResult<string>();
            if (string.IsNullOrWhiteSpace(targetNodeId)) return Failure(result, "ONET_TARGET_REQUIRED", "A target node is required.");
            if (string.IsNullOrWhiteSpace(operation)) return Failure(result, "ONET_OPERATION_REQUIRED", "An operation is required.");
            var correlationId = Guid.NewGuid();
            var pending = new PendingRequest { ExpectedSourceNodeId = targetNodeId, ExpectedOperation = operation };
            if (!_pending.TryAdd(correlationId, pending))
                return Failure(result, "ONET_CORRELATION_COLLISION", "Unable to allocate a unique request correlation identifier.");
            try
            {
                var sent = await _channel.SendAsync(new ONETApplicationMessage
                {
                    SourceNodeId = _channel.LocalNodeId, TargetNodeId = targetNodeId,
                    Envelope = new ONETRequestResponseEnvelope
                    {
                        CorrelationId = correlationId, Kind = ONETRequestResponseMessageKind.Request,
                        Operation = operation, PayloadJson = payloadJson ?? string.Empty
                    }
                }, cancellationToken).ConfigureAwait(false);
                if (sent == null || sent.IsError || !sent.Result)
                    return Failure(result, "ONET_REQUEST_SEND_FAILED", sent?.Message ?? "ONET did not accept the request for delivery.");
                var response = await AwaitWithCancellationAsync(pending.Completion.Task, cancellationToken).ConfigureAwait(false);
                if (response.IsError)
                    return Failure(result, string.IsNullOrWhiteSpace(response.ErrorCode) ? "ONET_REMOTE_ERROR" : response.ErrorCode,
                        string.IsNullOrWhiteSpace(response.ErrorMessage) ? "The remote ONET node rejected the request." : response.ErrorMessage);
                result.Result = response.PayloadJson;
                return result;
            }
            catch (OperationCanceledException)
            {
                return Failure(result, "ONET_REQUEST_CANCELLED", "The ONET request was cancelled before a response arrived.");
            }
            finally { _pending.TryRemove(correlationId, out _); }
        }

        private void OnMessageReceived(object sender, ONETApplicationMessage message)
        {
            if (_disposed || message?.Envelope == null ||
                !string.Equals(message.TargetNodeId, _channel.LocalNodeId, StringComparison.Ordinal) ||
                message.Envelope.ProtocolVersion != ONETRequestResponseEnvelope.CurrentProtocolVersion ||
                message.Envelope.CorrelationId == Guid.Empty) return;
            if (message.Envelope.Kind == ONETRequestResponseMessageKind.Response)
            {
                if (_pending.TryGetValue(message.Envelope.CorrelationId, out var pending) &&
                    string.Equals(message.SourceNodeId, pending.ExpectedSourceNodeId, StringComparison.Ordinal) &&
                    string.Equals(message.Envelope.Operation, pending.ExpectedOperation, StringComparison.Ordinal))
                    pending.Completion.TrySetResult(message.Envelope);
                return;
            }
            _ = DispatchRequestAsync(message);
        }

        private async Task DispatchRequestAsync(ONETApplicationMessage message)
        {
            OASISResult<string> handled;
            if (!_handlers.TryGetValue(message.Envelope.Operation, out var handler))
                handled = Failure(new OASISResult<string>(), "ONET_OPERATION_NOT_FOUND", $"No handler is registered for '{message.Envelope.Operation}'.");
            else
            {
                try
                {
                    handled = await handler(new ONETRequestContext
                    {
                        SourceNodeId = message.SourceNodeId, Operation = message.Envelope.Operation,
                        PayloadJson = message.Envelope.PayloadJson
                    }, CancellationToken.None).ConfigureAwait(false) ??
                        Failure(new OASISResult<string>(), "ONET_HANDLER_NO_RESULT", "The ONET operation handler returned no result.");
                }
                catch (Exception ex)
                {
                    handled = Failure(new OASISResult<string>(), "ONET_HANDLER_FAILED", ex.Message);
                }
            }
            await _channel.SendAsync(new ONETApplicationMessage
            {
                SourceNodeId = _channel.LocalNodeId, TargetNodeId = message.SourceNodeId,
                Envelope = new ONETRequestResponseEnvelope
                {
                    CorrelationId = message.Envelope.CorrelationId,
                    Kind = ONETRequestResponseMessageKind.Response, Operation = message.Envelope.Operation,
                    PayloadJson = handled.Result ?? string.Empty, IsError = handled.IsError,
                    ErrorCode = handled.ErrorCode ?? string.Empty, ErrorMessage = handled.Message ?? string.Empty
                }
            }, CancellationToken.None).ConfigureAwait(false);
        }

        private static async Task<T> AwaitWithCancellationAsync<T>(Task<T> task, CancellationToken cancellationToken)
        {
            var cancelled = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            using (cancellationToken.Register(() => cancelled.TrySetResult(true)))
            {
                if (task != await Task.WhenAny(task, cancelled.Task).ConfigureAwait(false))
                    throw new OperationCanceledException(cancellationToken);
            }
            return await task.ConfigureAwait(false);
        }

        private static OASISResult<string> Failure(OASISResult<string> result, string code, string message)
        { result.IsError = true; result.ErrorCount = 1; result.ErrorCode = code; result.Message = message; return result; }
        private void ThrowIfDisposed() { if (_disposed) throw new ObjectDisposedException(nameof(ONETRequestResponseEndpoint)); }
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true; _channel.MessageReceived -= OnMessageReceived;
            foreach (var pending in _pending.Values) pending.Completion.TrySetCanceled();
            _pending.Clear();
        }
    }
}
