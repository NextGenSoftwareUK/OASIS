using System;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.Holochain.HoloNET.Client;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using CanonicalHoloOasis = NextGenSoftware.OASIS.API.Providers.HoloOASIS.HoloOASIS;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

internal sealed class HoloOasisReadSource : CanonicalHoloOasis, IDisposable
{
    private readonly HoloNETClientAppAgent _client;
    private readonly Uri _endpoint;
    private readonly string _appId;

    public HoloOasisReadSource(HoloNETClientAppAgent client, HoloOasisReadSigner signer,
        Uri endpoint, string appId, Guid probeId, IReadOnlySet<Guid> admittedIds)
        : base(null, client, useHoloNetwork: false)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(signer);
        client.ZomeCallSigner = signer;
        _client = client;
        _endpoint = endpoint;
        _appId = appId;
        ConfigurePublicQuery(probeId, admittedIds);
    }

    public override Task<OASISResult<bool>> ActivateProviderAsync() => ActivateQuerySourceAsync(default);

    public override async Task<OASISResult<bool>> ActivateQuerySourceAsync(CancellationToken cancellationToken)
    {
        InvalidatePublicQueryProbe();
        cancellationToken.ThrowIfCancellationRequested();
        if (_client.State != WebSocketState.Open)
        {
            var connected = await _client.ConnectAsync(_appId, _endpoint,
                retrieveAgentPubKeyAndDnaHashFromSandbox: false,
                automaticallyAttemptToRetrieveFromSandBoxIfConductorFails: false);
            cancellationToken.ThrowIfCancellationRequested();
            if (connected == null || connected.IsError || !connected.IsConnected)
                return new() { IsError = true, ErrorCount = 1, ErrorCode = "CQL_SOURCE_UNAVAILABLE",
                    Message = "The reviewed HoloOASIS app endpoint could not be connected." };
        }

        if (!IsProviderActivated)
        {
            var connection = await base.ActivateProviderAsync();
            cancellationToken.ThrowIfCancellationRequested();
            if (connection.IsError || !connection.Result)
                return connection;
        }

        return await VerifyPublicQueryProbeAsync(cancellationToken);
    }

    public void Dispose()
    {
        InvalidatePublicQueryProbe();
        if (_client.State != WebSocketState.Open && !IsProviderActivated)
            return;

        var result = DeActivateProviderAsync().GetAwaiter().GetResult();
        if (result.IsError)
            throw new IOException("The HoloOASIS CQL app connection could not be closed.");
    }

}
