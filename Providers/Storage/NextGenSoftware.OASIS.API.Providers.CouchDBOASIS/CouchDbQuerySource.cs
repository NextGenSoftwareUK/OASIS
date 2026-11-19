using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.CouchDBOASIS;

public sealed class CouchDbQuerySource : CouchDBOASIS, ICosmicReadOnlyQueryActivation
{
    private readonly Guid probeId;

    public CouchDbQuerySource(string origin, string username, string password, HttpClient client, Guid probeId)
        : base(origin, username, password, client)
    {
        if (probeId == Guid.Empty)
            throw new ArgumentException("An existing public teaching probe is required.", nameof(probeId));
        this.probeId = probeId;
    }

    public Task<OASISResult<bool>> ActivateQuerySourceAsync(CancellationToken cancellationToken)
        => VerifyPublicQueryProbeAsync(probeId, cancellationToken);
}
