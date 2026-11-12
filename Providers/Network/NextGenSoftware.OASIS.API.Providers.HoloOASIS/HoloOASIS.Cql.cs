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

public partial class HoloOASIS : ICosmicQueryStorageProvider
{
    private BoundedPublicKeyValueQuery _query;
    private bool _probeVerified;

    protected void ConfigurePublicQuery(IReadOnlySet<Guid> admittedIds) =>
        _query = new(QueryProviderType, ReadHolonAsync, 1, admittedIds);

    protected void InvalidatePublicQueryProbe() => _probeVerified = false;

    protected async Task<OASISResult<bool>> VerifyPublicQueryProbeAsync(Guid probeId, CancellationToken cancellationToken)
    {
        var probe = await _query.VerifyProbeAsync(probeId, cancellationToken);
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
