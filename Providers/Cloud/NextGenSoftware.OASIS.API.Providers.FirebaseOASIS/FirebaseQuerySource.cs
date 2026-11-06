using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.FirebaseOASIS;

public sealed class FirebaseQuerySource : FirebaseOASIS, ICosmicReadOnlyQueryActivation
{
    private readonly Guid _probeId;

    public FirebaseQuerySource(string origin, string readerToken, HttpClient client, Guid probeId)
        : base(origin, readerToken, client)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(readerToken);
        if (probeId == Guid.Empty)
            throw new ArgumentException("An existing public teaching probe is required.", nameof(probeId));
        _probeId = probeId;
    }

    public Task<OASISResult<bool>> ActivateQuerySourceAsync(CancellationToken cancellationToken)
        => VerifyPublicQueryProbeAsync(_probeId, cancellationToken);
}
