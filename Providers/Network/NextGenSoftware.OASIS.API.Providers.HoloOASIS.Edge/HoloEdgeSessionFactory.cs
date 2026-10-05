using System;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.Holochain.HoloNET.Client;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Edge.Runtime.Holochain;

namespace NextGenSoftware.OASIS.API.Providers.HoloOASIS.Edge
{
    public interface IHoloEdgeSessionFactory
    {
        Task<OASISResult<IHoloEdgeAppClient>> ConnectAsync(string installedAppId,
            HolochainAndroidAppSession session, CancellationToken cancellationToken);
    }

    /// <summary>Creates an authenticated lightweight HoloNET app-interface connection.</summary>
    public sealed class HoloNetEdgeSessionFactory : IHoloEdgeSessionFactory
    {
        public async Task<OASISResult<IHoloEdgeAppClient>> ConnectAsync(string installedAppId,
            HolochainAndroidAppSession session, CancellationToken cancellationToken)
        {
            if (session == null || session.Port < 1 || session.Port > 65535)
                return Error("HOLO_EDGE_SESSION_INVALID", "The conductor session has no valid app-interface port.");
            if (session.AuthenticationToken == null || session.AuthenticationToken.Length == 0)
                return Error("HOLO_EDGE_SESSION_TOKEN_REQUIRED", "The conductor session has no authentication token.");
            if (string.IsNullOrWhiteSpace(installedAppId))
                return Error("HOLO_EDGE_APP_ID_REQUIRED", "The installed app id is required.");

            cancellationToken.ThrowIfCancellationRequested();
            var endpoint = new Uri($"ws://127.0.0.1:{session.Port}");
            var dna = new HoloNETDNA
            {
                HolochainConductorAppAgentURI = endpoint.AbsoluteUri,
                AppAuthenticationToken = session.AuthenticationToken,
                InstalledAppId = installedAppId,
                AutoStartHolochainConductor = false,
                AutoShutdownHolochainConductor = false
            };
            var client = new HoloNETClientAppAgent(installedAppId, null, dna);
            try
            {
                var connected = await client.ConnectAsync(installedAppId, endpoint,
                    retrieveAgentPubKeyAndDnaHashFromSandbox: false,
                    automaticallyAttemptToRetrieveFromSandBoxIfConductorFails: false)
                    .ConfigureAwait(false);
                if (connected == null || !connected.IsConnected)
                    return Error("HOLO_EDGE_CONNECT_FAILED", connected?.Message ?? "HoloNET did not connect.");
                return new OASISResult<IHoloEdgeAppClient>(new HoloNetEdgeAppClient(client));
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception error)
            {
                return Error("HOLO_EDGE_CONNECT_FAILED", error.Message);
            }
        }

        private static OASISResult<IHoloEdgeAppClient> Error(string code, string message) =>
            new OASISResult<IHoloEdgeAppClient>
            {
                IsError = true, ErrorCount = 1, ErrorCode = code, Message = message
            };
    }
}
