using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

/// <summary>Bounded public reads from one co-located teaching store.</summary>
public abstract class LoopbackPublicHolonQuerySource : KeyValueStorageProviderBase,
    ICosmicQueryStorageProvider, ICosmicReadOnlyQueryActivation, IDisposable
{
    private readonly TeachingBackend backend;
    private readonly BoundedPublicKeyValueQuery query;
    private readonly Guid probeId;

    protected LoopbackPublicHolonQuerySource(ProviderType provider, Uri sidecar, string readerToken, Guid probeId)
        : this(provider, CreateBackend(provider, sidecar, readerToken, probeId), probeId) { }

    private LoopbackPublicHolonQuerySource(ProviderType provider, TeachingBackend backend, Guid probeId)
        : base(backend, provider.ToString() + ":")
    {
        this.backend = backend;
        this.probeId = probeId;
        QueryProviderType = provider;
        ProviderType = new EnumValue<ProviderType>(provider);
        ProviderName = QueryProviderType.ToString();
        ProviderDescription = $"Public exact-key reads from a local {provider} teaching store";
        ProviderCategory = new EnumValue<ProviderCategory>(NextGenSoftware.OASIS.API.Core.Enums.ProviderCategory.StorageLocalAndNetwork);
        query = new(QueryProviderType, backend.ReadAsync);
    }

    public static bool IsValidConfiguration(Uri sidecar, string readerToken, Guid probeId) =>
        sidecar is { IsAbsoluteUri: true, Scheme: "http", Host: "127.0.0.1", Port: >= 1024 } &&
        sidecar.AbsoluteUri == $"http://127.0.0.1:{sidecar.Port}/" && sidecar.OriginalString == sidecar.AbsoluteUri &&
        probeId != Guid.Empty && readerToken is { Length: >= 32 and <= 256 } &&
        readerToken.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_');

    private static TeachingBackend CreateBackend(ProviderType provider, Uri sidecar, string readerToken, Guid probeId)
    {
        if (!IsValidConfiguration(sidecar, readerToken, probeId))
            throw new ArgumentException("A canonical loopback endpoint, dedicated reader and public probe are required.");
        var handler = new SocketsHttpHandler { AllowAutoRedirect = false, UseProxy = false, UseCookies = false };
        return new TeachingBackend(new HttpClient(handler), sidecar, readerToken, provider.ToString());
    }

    public ProviderType QueryProviderType { get; }
    public CosmicQueryCapabilities QueryCapabilities => BoundedIdentityCqlCompiler.Capabilities;
    public override Task<OASISResult<bool>> ActivateProviderAsync() => ActivateQuerySourceAsync();

    public async Task<OASISResult<bool>> ActivateQuerySourceAsync(CancellationToken cancellationToken = default)
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
        backend.Dispose();
    }

    private sealed class TeachingBackend(HttpClient client, Uri sidecar, string readerToken, string provider) : IKeyValueBackend, IDisposable
    {
        public async Task<string> ReadAsync(Guid id, CancellationToken cancellationToken)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(sidecar, $"/oasis/cql/holon?id={id:D}"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", readerToken);
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();
            if (response.StatusCode != HttpStatusCode.OK || response.Content.Headers.ContentType?.MediaType != "application/json")
                throw new InvalidDataException("The teaching companion returned an unexpected response.");
            if (!response.Headers.TryGetValues("X-OASIS-Teaching-Provider", out var identity) ||
                !identity.SequenceEqual(new[] { provider }, StringComparer.Ordinal))
                throw new InvalidDataException("The teaching companion differs from the admitted provider.");
            await response.Content.LoadIntoBufferAsync(1024 * 1024, cancellationToken);
            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            return json == "null" ? null! : json;
        }

        public Task VerifyAsync(CancellationToken cancellationToken = default) => Unsupported();
        public Task<string> GetAsync(string key, CancellationToken cancellationToken = default) =>
            Task.FromException<string>(new NotSupportedException("Use an admitted CQL plan."));
        public Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken cancellationToken = default) =>
            Task.FromException<IReadOnlyList<string>>(new NotSupportedException("Teaching scans are unavailable."));
        public Task PutAsync(string key, string value, CancellationToken cancellationToken = default) => Unsupported();
        public Task DeleteAsync(string key, CancellationToken cancellationToken = default) => Unsupported();
        private static Task Unsupported() => Task.FromException(new NotSupportedException("The teaching companion exposes exact CQL reads."));
        public void Dispose() => client.Dispose();
    }
}
