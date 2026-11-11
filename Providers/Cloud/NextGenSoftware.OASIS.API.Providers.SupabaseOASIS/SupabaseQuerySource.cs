using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.SupabaseOASIS;

public sealed class SupabaseQuerySource : SupabaseOASIS, ICosmicReadOnlyQueryActivation
{
    private readonly Guid _probeId;

    public SupabaseQuerySource(string origin, string gatewayKey, string readerToken, HttpClient client, Guid probeId)
        : base(origin, gatewayKey, client, "/rest/v1", readerToken)
    {
        if (probeId == Guid.Empty)
            throw new ArgumentException("An existing public teaching probe is required.", nameof(probeId));
        _probeId = probeId;
    }

    public Task<OASISResult<bool>> ActivateQuerySourceAsync(CancellationToken cancellationToken)
        => VerifyPublicQueryProbeAsync(_probeId, cancellationToken);
}
