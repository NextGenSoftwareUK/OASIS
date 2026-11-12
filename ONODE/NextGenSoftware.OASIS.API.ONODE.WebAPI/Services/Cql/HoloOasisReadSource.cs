using System;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.Holochain.HoloNET.Client;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Core.Serialization;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using CanonicalHoloOasis = NextGenSoftware.OASIS.API.Providers.HoloOASIS.HoloOASIS;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

internal sealed class HoloOasisReadSource : CanonicalHoloOasis, ICosmicQueryStorageProvider, ICosmicReadOnlyQueryActivation
{
    private readonly Guid _probeId;
    private readonly BoundedPublicKeyValueQuery _query;
    private readonly HoloNETClientAppAgent _client;
    private readonly Uri _endpoint;
    private readonly string _appId;
    private bool _probeVerified;

    public HoloOasisReadSource(HoloNETClientAppAgent client, HoloOasisReadSigner signer,
        Uri endpoint, string appId, Guid probeId, IReadOnlySet<Guid> admittedIds)
        : base(null, client, useHoloNetwork: false)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(signer);
        ArgumentNullException.ThrowIfNull(admittedIds);
        if (probeId == Guid.Empty || !admittedIds.Contains(probeId))
            throw new ArgumentException("The HoloOASIS probe must belong to the reviewed catalogue.", nameof(probeId));

        client.ZomeCallSigner = signer;
        _client = client;
        _endpoint = endpoint;
        _appId = appId;
        _probeId = probeId;
        _query = new(QueryProviderType, ReadHolonAsync, 1, admittedIds);
    }

    public ProviderType QueryProviderType => global::NextGenSoftware.OASIS.API.Core.Enums.ProviderType.HoloOASIS;
    public CosmicQueryCapabilities QueryCapabilities => BoundedIdentityCqlCompiler.Capabilities;

    public override Task<OASISResult<bool>> ActivateProviderAsync() => ActivateQuerySourceAsync(default);

    public async Task<OASISResult<bool>> ActivateQuerySourceAsync(CancellationToken cancellationToken)
    {
        _probeVerified = false;
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

        var probe = await _query.VerifyProbeAsync(_probeId, cancellationToken);
        _probeVerified = !probe.IsError && probe.Result;
        return probe;
    }

    public Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(CosmicQueryPlan<T> plan,
        int maximumResults = 100, CancellationToken cancellationToken = default) where T : IHolon =>
        _query.ExecuteAsync(plan, maximumResults, _probeVerified && IsProviderActivated &&
            _client.State == WebSocketState.Open, versioned: false, cancellationToken);

    private async Task<string> ReadHolonAsync(Guid id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var result = await base.LoadHolonAsync(id, loadChildren: false, recursive: false);
        cancellationToken.ThrowIfCancellationRequested();
        if (result.IsError)
            throw new IOException("The canonical HoloOASIS holon read failed.");

        return result.Result == null ? null : StoredRecordJson.Serialize<IHolon>(result.Result);
    }
}
