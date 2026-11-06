using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.VercelKVOASIS;

/// <summary>Bounded public reads from one reviewed Vercel KV store migrated to Upstash.</summary>
public sealed class VercelKvQuerySource : VercelKVOASIS, ICosmicQueryStorageProvider, ICosmicReadOnlyQueryActivation, IDisposable
{
    private readonly Guid probeId;
    private readonly BoundedPublicKeyValueQuery query;

    public VercelKvQuerySource(Uri endpoint, string readerToken, Guid probeId)
        : this(endpoint, readerToken, probeId, null) { }

    internal VercelKvQuerySource(Uri endpoint, string readerToken, Guid probeId, HttpMessageHandler handler)
        : base(CreateBackend(endpoint, readerToken, probeId, handler))
    {
        this.probeId = probeId;
        query = new(QueryProviderType, (id, token) => Backend.GetAsync(RecordKey("holon", id), token));
    }

    private static UpstashRestBackend CreateBackend(Uri endpoint, string readerToken, Guid probeId, HttpMessageHandler handler)
    {
        if (probeId == Guid.Empty || readerToken is not { Length: > 0 and <= 1024 } ||
            readerToken.Any(character => character <= ' ' || character > '~') ||
            endpoint is not { IsAbsoluteUri: true, Scheme: "https", HostNameType: UriHostNameType.Dns } ||
            !endpoint.IsDefaultPort || endpoint.UserInfo.Length != 0 || endpoint.AbsolutePath != "/" ||
            endpoint.Query.Length != 0 || endpoint.Fragment.Length != 0 ||
            !endpoint.IdnHost.EndsWith(".upstash.io", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("A reviewed migrated Upstash HTTPS endpoint, reader and existing teaching probe are required.");
        return new UpstashRestBackend(endpoint.AbsoluteUri, readerToken, handler, queryOnly: true);
    }

    public ProviderType QueryProviderType => Core.Enums.ProviderType.VercelKVOASIS;
    public CosmicQueryCapabilities QueryCapabilities => BoundedIdentityCqlCompiler.Capabilities;
    public override Task<OASISResult<bool>> ActivateProviderAsync() => ActivateQuerySourceAsync(default);

    public async Task<OASISResult<bool>> ActivateQuerySourceAsync(CancellationToken cancellationToken)
    {
        IsProviderActivated = false;
        var result = await query.VerifyProbeAsync(probeId, cancellationToken);
        IsProviderActivated = !result.IsError && result.Result;
        return result;
    }

    public Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(CosmicQueryPlan<T> plan,
        int maximumResults = 100, CancellationToken cancellationToken = default) where T : IHolon =>
        query.ExecuteAsync(plan, maximumResults, IsProviderActivated, IsVersionControlEnabled, cancellationToken);

    public void Dispose()
    {
        IsProviderActivated = false;
        ((UpstashRestBackend)Backend).Dispose();
    }
}
