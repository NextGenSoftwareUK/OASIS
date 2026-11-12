using System;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Core.Serialization;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.HoloOASIS;

public partial class HoloOASIS : ICosmicQueryStorageProvider, ICosmicReadOnlyQueryActivation
{
    private BoundedPublicKeyValueQuery _query;
    private Guid _publicQueryProbeId;
    private bool _probeVerified;

    public void ConfigurePublicQuery(Guid probeId, IReadOnlySet<Guid> admittedIds)
    {
        ArgumentNullException.ThrowIfNull(admittedIds);
        if (probeId == Guid.Empty || !admittedIds.Contains(probeId))
            throw new ArgumentException("The HoloOASIS probe must belong to the reviewed catalogue.", nameof(probeId));
        if (_query != null)
            throw new InvalidOperationException("The HoloOASIS public query catalogue is already configured.");

        _query = new(QueryProviderType, ReadHolonAsync, 1, admittedIds);
        _publicQueryProbeId = probeId;
    }

    protected void InvalidatePublicQueryProbe() => _probeVerified = false;

    public virtual async Task<OASISResult<bool>> ActivateQuerySourceAsync(CancellationToken cancellationToken)
    {
        InvalidatePublicQueryProbe();
        cancellationToken.ThrowIfCancellationRequested();
        if (_query == null)
            return new() { IsError = true, ErrorCount = 1, ErrorCode = "CQL_CAPABILITY_UNSUPPORTED",
                Message = "A reviewed HoloOASIS query catalogue is required." };

        if (!IsProviderActivated || HoloNETClientAppAgent?.State != WebSocketState.Open)
        {
            var activation = await ActivateProviderAsync();
            cancellationToken.ThrowIfCancellationRequested();
            if (activation.IsError || !activation.Result)
                return activation;
        }

        return await VerifyPublicQueryProbeAsync(cancellationToken);
    }

    protected async Task<OASISResult<bool>> VerifyPublicQueryProbeAsync(CancellationToken cancellationToken)
    {
        var probe = await _query.VerifyProbeAsync(_publicQueryProbeId, cancellationToken);
        _probeVerified = !probe.IsError && probe.Result;
        return probe;
    }

    public ProviderType QueryProviderType => global::NextGenSoftware.OASIS.API.Core.Enums.ProviderType.HoloOASIS;
    public CosmicQueryCapabilities QueryCapabilities => _query == null ? null : BoundedIdentityCqlCompiler.Capabilities;

    public Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(CosmicQueryPlan<T> plan,
        int maximumResults = 100, CancellationToken cancellationToken = default) where T : IHolon =>
        _query == null
            ? Task.FromResult(new OASISResult<IReadOnlyList<IHolon>> { IsError = true, ErrorCount = 1,
                ErrorCode = "CQL_CAPABILITY_UNSUPPORTED", Message = "A reviewed HoloOASIS query catalogue is required." })
            : _query.ExecuteAsync(plan, maximumResults, _probeVerified && IsProviderActivated &&
            HoloNETClientAppAgent?.State == WebSocketState.Open, versioned: false, cancellationToken);

    private async Task<string> ReadHolonAsync(Guid id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var result = await LoadHolonAsync(id, loadChildren: false, recursive: false);
        cancellationToken.ThrowIfCancellationRequested();
        if (result.Result == null && result.Exception == null &&
            result.ErrorCode == OASISResultErrorCodes.RecordNotFound)
            return null;
        if (result.IsError)
            throw new IOException("The canonical HoloOASIS holon read failed.");

        return result.Result == null ? null : StoredRecordJson.Serialize<IHolon>(result.Result);
    }
}
