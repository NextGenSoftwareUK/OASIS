using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.TursoOASIS;

/// <summary>Bounded public teaching reads through Turso's native libSQL pipeline.</summary>
public sealed class TursoQuerySource : KeyValueStorageProviderBase, ICosmicQueryStorageProvider, ICosmicReadOnlyQueryActivation, IDisposable
{
    private readonly TeachingBackend backend;
    private readonly BoundedPublicKeyValueQuery query;
    private readonly Guid probeId;

    public TursoQuerySource(Uri origin, string readerToken, Guid probeId)
        : this(origin, readerToken, probeId, null) { }

    internal TursoQuerySource(Uri origin, string readerToken, Guid probeId, HttpMessageHandler? handler)
        : this(CreateBackend(origin, readerToken, probeId, handler), probeId) { }

    private TursoQuerySource(TeachingBackend backend, Guid probeId) : base(backend, "turso:")
    {
        this.backend = backend;
        this.probeId = probeId;
        ProviderType = new EnumValue<ProviderType>(QueryProviderType);
        ProviderName = QueryProviderType.ToString();
        ProviderDescription = "Read-only Turso teaching holons";
        ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
        query = new(QueryProviderType, backend.ReadAsync);
    }

    public static bool IsValidConfiguration(Uri origin, string readerToken, Guid probeId) =>
        origin is { IsAbsoluteUri: true, Scheme: "https", HostNameType: UriHostNameType.Dns, IsDefaultPort: true } &&
        origin.Host.EndsWith(".turso.io", StringComparison.Ordinal) && origin.AbsolutePath == "/" &&
        origin.UserInfo.Length == 0 && origin.Query.Length == 0 && origin.Fragment.Length == 0 &&
        probeId != Guid.Empty && readerToken is { Length: > 0 and <= 8192 } &&
        readerToken.All(c => c > ' ' && c <= '~');

    private static TeachingBackend CreateBackend(Uri origin, string readerToken, Guid probeId, HttpMessageHandler? handler)
    {
        if (!IsValidConfiguration(origin, readerToken, probeId))
            throw new ArgumentException("A reviewed Turso HTTPS database, reader token and existing public probe are required.");
        if (handler == null)
        {
            var transport = (SocketsHttpHandler)NativeConnectionGuard.CreateGuardedHttpHandler();
            transport.UseProxy = false;
            transport.UseCookies = false;
            handler = transport;
        }
        return new TeachingBackend(new TursoOASIS(origin.AbsoluteUri, readerToken, handler, queryOnly: true));
    }

    public ProviderType QueryProviderType => Core.Enums.ProviderType.TursoOASIS;
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

    private sealed class TeachingBackend(TursoOASIS transport) : IKeyValueBackend, IDisposable
    {
        public Task<string> ReadAsync(Guid id, CancellationToken cancellationToken) =>
            transport.ReadExactTeachingHolonAsync(id, cancellationToken);

        public Task VerifyAsync(CancellationToken cancellationToken = default) => Unsupported();
        public Task<string> GetAsync(string key, CancellationToken cancellationToken = default) =>
            Task.FromException<string>(new NotSupportedException("Use an admitted Turso CQL plan."));
        public Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken cancellationToken = default) =>
            Task.FromException<IReadOnlyList<string>>(new NotSupportedException("Turso teaching scans are unavailable."));
        public Task PutAsync(string key, string value, CancellationToken cancellationToken = default) => Unsupported();
        public Task DeleteAsync(string key, CancellationToken cancellationToken = default) => Unsupported();
        private static Task Unsupported() => Task.FromException(new NotSupportedException("The Turso teaching profile exposes exact CQL reads."));
        public void Dispose() => transport.DisposeTransport();
    }
}
