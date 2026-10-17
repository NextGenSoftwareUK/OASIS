using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.Holochain.HoloNET.Client.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.HoloOASIS.Edge
{
    /// <summary>HoloNET implementation of the lightweight authenticated app-interface boundary.</summary>
    public sealed class HoloNetEdgeAppClient : IHoloEdgeAppClient
    {
        private readonly IHoloNETClientAppAgent _client;

        public HoloNetEdgeAppClient(IHoloNETClientAppAgent client) =>
            _client = client ?? throw new ArgumentNullException(nameof(client));

        public async Task<OASISResult<bool>> CallAsync(string zome, string function,
            IReadOnlyDictionary<string, object> payload, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(zome) || string.IsNullOrWhiteSpace(function))
                return Error("HOLO_EDGE_ZOME_CALL_INVALID", "A zome and function are required.");
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                // HoloNET owns the signed Holochain 0.7 call-zome envelope. Cancellation is
                // intentionally checked before dispatch; once signed and sent, the idempotent
                // operation id is the only safe completion/retry boundary.
                var response = await _client.CallZomeFunctionAsync(zome, function, payload)
                    .ConfigureAwait(false);
                if (response == null)
                    return Error("HOLO_EDGE_ZOME_RESPONSE_MISSING", "HoloNET returned no zome response.");
                if (response.IsError)
                    return Error("HOLO_EDGE_ZOME_CALL_FAILED", response.Message ?? "The Holochain zome call failed.");
                return new OASISResult<bool>(true) { IsSaved = true };
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                return Error("HOLO_EDGE_ZOME_CALL_FAILED", ex.Message);
            }
        }

        public async ValueTask DisposeAsync()
        {
            var disconnected = await _client.DisconnectAsync().ConfigureAwait(false);
            if (disconnected == null || !disconnected.IsDisconnected)
                throw new InvalidOperationException(disconnected?.Message ?? "HoloNET did not disconnect cleanly.");
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
