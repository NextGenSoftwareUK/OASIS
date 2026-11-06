using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using Weaviate.Client;

namespace NextGenSoftware.OASIS.API.Providers.WeaviateOASIS;

/// <summary>Explicit query profile with caller-owned SDK and REST lifetimes.</summary>
public sealed class WeaviateQuerySource : WeaviateOASIS, ICosmicReadOnlyQueryActivation
{
    private readonly Guid _probeId;

    public WeaviateQuerySource(HttpClient rest, string origin, WeaviateClient queryClient, Guid probeId)
        : base(rest, origin, queryClient: queryClient ?? throw new ArgumentNullException(nameof(queryClient)))
    {
        if (probeId == Guid.Empty)
            throw new ArgumentException("An existing public teaching probe is required.", nameof(probeId));
        _probeId = probeId;
    }

    public Task<OASISResult<bool>> ActivateQuerySourceAsync(CancellationToken cancellationToken)
        => VerifyPublicQueryProbeAsync(_probeId, cancellationToken);
}
