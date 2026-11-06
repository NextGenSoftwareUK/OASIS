using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using NextGenSoftware.Utilities;

[assembly: InternalsVisibleTo("NextGenSoftware.OASIS.API.Providers.ConvexOASIS.UnitTests")]

namespace NextGenSoftware.OASIS.API.Providers.ConvexOASIS;

/// <summary>Bounded public reads from a reviewed Convex teaching companion.</summary>
public sealed class ConvexQuerySource : KeyValueStorageProviderBase,
    ICosmicQueryStorageProvider, ICosmicReadOnlyQueryActivation, IDisposable
{
    private readonly TeachingBackend backend;
    private readonly BoundedPublicKeyValueQuery query;
    private readonly Guid probeId;

    public ConvexQuerySource(Uri site, string readerToken, Guid probeId)
        : this(site, readerToken, probeId, null) { }

    internal ConvexQuerySource(Uri site, string readerToken, Guid probeId, HttpMessageHandler? handler)
        : this(CreateBackend(site, readerToken, probeId, handler), probeId) { }

    private ConvexQuerySource(TeachingBackend backend, Guid probeId) : base(backend, "convex:")
    {
        this.backend = backend;
        this.probeId = probeId;
        ProviderType = new EnumValue<ProviderType>(QueryProviderType);
        ProviderName = QueryProviderType.ToString();
        ProviderDescription = "Read-only Convex teaching holons";
        ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Storage);
        query = new(QueryProviderType, backend.ReadAsync);
    }

    public static bool IsValidConfiguration(Uri site, string readerToken, Guid probeId) =>
        site is { IsAbsoluteUri: true, Scheme: "https", HostNameType: UriHostNameType.Dns, IsDefaultPort: true } &&
        site.Host.Contains('.') && site.AbsolutePath == "/" && site.UserInfo.Length == 0 &&
        site.Query.Length == 0 && site.Fragment.Length == 0 && probeId != Guid.Empty &&
        readerToken is { Length: >= 32 and <= 256 } && readerToken.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_');

    private static TeachingBackend CreateBackend(Uri site, string readerToken, Guid probeId, HttpMessageHandler? testHandler)
    {
        if (!IsValidConfiguration(site, readerToken, probeId))
            throw new ArgumentException("A reviewed Convex HTTPS site, teaching reader and existing public probe are required.");
        if (testHandler != null) return new TeachingBackend(new HttpClient(testHandler, disposeHandler: false), site, readerToken);
        var handler = (SocketsHttpHandler)NativeConnectionGuard.CreateGuardedHttpHandler();
        handler.UseProxy = false;
        handler.UseCookies = false;
        return new TeachingBackend(new HttpClient(handler), site, readerToken);
    }

    public ProviderType QueryProviderType => Core.Enums.ProviderType.ConvexOASIS;
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

    private sealed class TeachingBackend(HttpClient client, Uri site, string readerToken) : IKeyValueBackend, IDisposable
    {
        public async Task<string> ReadAsync(Guid id, CancellationToken cancellationToken)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(site, $"/oasis/cql/holon?id={id:D}"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", readerToken);
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();
            if (response.StatusCode != System.Net.HttpStatusCode.OK ||
                response.Content.Headers.ContentType?.MediaType != "application/json")
                throw new InvalidDataException("The Convex teaching companion returned an unexpected response.");
            await response.Content.LoadIntoBufferAsync(1024 * 1024, cancellationToken);
            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            return json == "null" ? null! : json;
        }

        public Task VerifyAsync(CancellationToken cancellationToken = default) => Unsupported();
        public Task<string> GetAsync(string key, CancellationToken cancellationToken = default) =>
            Task.FromException<string>(new NotSupportedException("Use an admitted Convex CQL plan."));
        public Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken cancellationToken = default) =>
            Task.FromException<IReadOnlyList<string>>(new NotSupportedException("Convex teaching scans are unavailable."));
        public Task PutAsync(string key, string value, CancellationToken cancellationToken = default) => Unsupported();
        public Task DeleteAsync(string key, CancellationToken cancellationToken = default) => Unsupported();
        private static Task Unsupported() => Task.FromException(new NotSupportedException("The Convex teaching companion exposes exact CQL reads."));
        public void Dispose() => client.Dispose();
    }
}
