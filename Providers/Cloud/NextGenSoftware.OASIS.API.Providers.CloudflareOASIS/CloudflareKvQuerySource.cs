using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

[assembly: InternalsVisibleTo("NextGenSoftware.OASIS.API.Providers.CloudflareOASIS.UnitTests")]

namespace NextGenSoftware.OASIS.API.Providers.CloudflareOASIS;

/// <summary>Bounded reads from a dedicated, public-only Workers KV teaching namespace.</summary>
public sealed class CloudflareKvQuerySource : CloudflareOASIS, ICosmicQueryStorageProvider, ICosmicReadOnlyQueryActivation, IDisposable
{
    private readonly Guid probeId;
    private readonly BoundedPublicKeyValueQuery query;

    public CloudflareKvQuerySource(string accountId, string namespaceId, string readerToken, Guid probeId)
        : this(accountId, namespaceId, readerToken, probeId, null) { }

    internal CloudflareKvQuerySource(string accountId, string namespaceId, string readerToken, Guid probeId, HttpMessageHandler? handler)
        : base(accountId, namespaceId, readerToken, CreateClient(accountId, namespaceId, readerToken, probeId, handler), queryOnly: true)
    {
        this.probeId = probeId;
        query = new(QueryProviderType, (id, token) => KvGetAsync(HolonKey(id), token));
    }

    private static HttpClient CreateClient(string accountId, string namespaceId, string readerToken, Guid probeId, HttpMessageHandler? handler)
    {
        static bool IsIdentifier(string value) => value is { Length: 32 } && value.All(char.IsAsciiHexDigit);
        if (!IsIdentifier(accountId) || !IsIdentifier(namespaceId) || probeId == Guid.Empty ||
            readerToken is not { Length: > 0 and <= 1024 } || readerToken.Any(c => c <= ' ' || c > '~'))
            throw new ArgumentException("A reviewed Cloudflare account, namespace, reader and teaching probe are required.");
        if (handler != null) return new HttpClient(handler, disposeHandler: false);
        var transport = (SocketsHttpHandler)NativeConnectionGuard.CreateGuardedHttpHandler();
        transport.UseProxy = false;
        transport.UseCookies = false;
        return new HttpClient(transport);
    }

    public ProviderType QueryProviderType => Core.Enums.ProviderType.CloudflareOASIS;
    public CosmicQueryCapabilities QueryCapabilities => BoundedIdentityCqlCompiler.Capabilities;
    public override Task<OASISResult<bool>> ActivateProviderAsync() => ActivateQuerySourceAsync();

    public async Task<OASISResult<bool>> ActivateQuerySourceAsync(CancellationToken cancellationToken = default)
    {
        IsProviderActivated = false;
        var result = await query.VerifyProbeAsync(probeId, cancellationToken);
        IsProviderActivated = !result.IsError && result.Result;
        return result;
    }

    public override Task<OASISResult<bool>> DeActivateProviderAsync()
    {
        IsProviderActivated = false;
        return base.DeActivateProviderAsync();
    }

    public Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(CosmicQueryPlan<T> plan,
        int maximumResults = 100, CancellationToken cancellationToken = default) where T : IHolon =>
        query.ExecuteAsync(plan, maximumResults, IsProviderActivated, IsVersionControlEnabled, cancellationToken);

    public void Dispose()
    {
        IsProviderActivated = false;
        DisposeTransport();
    }
}
