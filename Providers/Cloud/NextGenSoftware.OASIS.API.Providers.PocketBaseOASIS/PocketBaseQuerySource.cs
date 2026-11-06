using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.PocketBaseOASIS;

public sealed class PocketBaseQuerySource : PocketBaseOASIS, ICosmicReadOnlyQueryActivation
{
    private readonly Guid _probeId;

    public PocketBaseQuerySource(string origin, HttpClient httpClient, string queryToken, Guid probeId)
        : base(origin, httpClient, queryToken)
    {
        if (probeId == Guid.Empty)
            throw new ArgumentException("An existing public teaching probe is required.", nameof(probeId));
        _probeId = probeId;
    }

    public Task<OASISResult<bool>> ActivateQuerySourceAsync(CancellationToken cancellationToken)
        => VerifyPublicQueryProbeAsync(_probeId, cancellationToken);
}
