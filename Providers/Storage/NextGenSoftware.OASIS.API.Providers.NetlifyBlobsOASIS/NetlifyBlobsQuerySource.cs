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

namespace NextGenSoftware.OASIS.API.Providers.NetlifyBlobsOASIS;

/// <summary>Bounded public reads from one reviewed Netlify site store.</summary>
public sealed class NetlifyBlobsQuerySource : NetlifyBlobsOASIS, ICosmicQueryStorageProvider, ICosmicReadOnlyQueryActivation, IDisposable
{
    private readonly Guid probeId;
    private readonly BoundedPublicKeyValueQuery query;

    public NetlifyBlobsQuerySource(Guid siteId, string readerToken, string store, Uri objectOrigin, Guid probeId)
        : this(siteId, readerToken, store, objectOrigin, probeId, null) { }

    internal NetlifyBlobsQuerySource(Guid siteId, string readerToken, string store, Uri objectOrigin, Guid probeId, HttpMessageHandler handler)
        : base(CreateBackend(siteId, readerToken, store, objectOrigin, probeId, handler))
    {
        this.probeId = probeId;
        query = new(QueryProviderType, (id, token) => Backend.GetAsync(RecordKey("holon", id), token));
    }

    private static NetlifyBlobsBackend CreateBackend(Guid siteId, string token, string store, Uri origin, Guid probe, HttpMessageHandler handler)
    {
        if (siteId == Guid.Empty || probe == Guid.Empty || token is not { Length: > 0 and <= 1024 } ||
            token.Any(character => character <= ' ' || character > '~') ||
            store is not { Length: > 0 and <= 64 } || store.Any(character => !char.IsAsciiLetterOrDigit(character) && character is not ('-' or '_')) ||
            origin is not { IsAbsoluteUri: true, Scheme: "https", HostNameType: UriHostNameType.Dns, IsLoopback: false,
                IsDefaultPort: true, AbsolutePath: "/", UserInfo.Length: 0, Query.Length: 0, Fragment.Length: 0 })
            throw new ArgumentException("A reviewed Netlify site, teaching store, object origin, reader and probe are required.");
        return new NetlifyBlobsBackend(siteId.ToString("D"), token, store, "https://api.netlify.com", handler, origin);
    }

    public ProviderType QueryProviderType => Core.Enums.ProviderType.NetlifyBlobsOASIS;
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
        ((NetlifyBlobsBackend)Backend).Dispose();
    }
}
