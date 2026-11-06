using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using NextGenSoftware.Utilities;
using PType = NextGenSoftware.OASIS.API.Core.Enums.ProviderType;

namespace NextGenSoftware.OASIS.API.Providers.DruidOASIS;

/// <summary>Exact-key reads from a dedicated public Druid teaching datasource.</summary>
public sealed class DruidPublicQuerySource : KeyValueStorageProviderBase, ICosmicQueryStorageProvider,
    ICosmicReadOnlyQueryActivation, IDisposable
{
    private readonly DruidBackend backend;
    private readonly BoundedPublicKeyValueQuery query;
    private readonly Guid probeId;

    public DruidPublicQuerySource(Uri origin, Guid probeId)
        : this(new DruidBackend(origin, probeId, handler: null), probeId) { }

    internal DruidPublicQuerySource(Uri origin, Guid probeId, HttpMessageHandler handler)
        : this(new DruidBackend(origin, probeId, handler), probeId) { }

    private DruidPublicQuerySource(DruidBackend backend, Guid probeId) : base(backend, "druid-public:")
    {
        this.backend = backend;
        this.probeId = probeId;
        ProviderType = new EnumValue<PType>(QueryProviderType);
        ProviderName = QueryProviderType.ToString();
        ProviderDescription = "Reviewed public Apache Druid teaching holons";
        ProviderCategory = new EnumValue<Core.Enums.ProviderCategory>(Core.Enums.ProviderCategory.StorageLocalAndNetwork);
        query = new(QueryProviderType, backend.ReadAsync);
    }

    public PType QueryProviderType => PType.DruidOASIS;
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

    private sealed class DruidBackend : IKeyValueBackend, IDisposable
    {
        private const string Sql = "SELECT \"doc\" FROM \"oasis_cql_public_holons\" WHERE \"id\" = ? AND \"is_public\" = 1 LIMIT 2";
        private readonly Uri endpoint;
        private readonly HttpClient client;

        public DruidBackend(Uri origin, Guid probeId, HttpMessageHandler? handler)
        {
            if (origin is not { IsAbsoluteUri: true } || origin.AbsolutePath != "/" ||
                origin.Query.Length != 0 || origin.Fragment.Length != 0 || origin.UserInfo.Length != 0 || probeId == Guid.Empty)
                throw new ArgumentException("A reviewed Druid origin and public probe are required.");

            var loopback = origin.Scheme == "http" && origin.Host == "127.0.0.1" && origin.Port >= 1024;
            var https = origin.Scheme == "https" && origin.HostNameType == UriHostNameType.Dns &&
                !origin.IsLoopback && origin.IsDefaultPort;
            if (!loopback && !https)
                throw new ArgumentException("Use a Druid HTTPS DNS origin or a fixed IPv4 loopback fixture.");

            if (handler == null)
            {
                var transport = loopback ? new SocketsHttpHandler { AllowAutoRedirect = false }
                    : (SocketsHttpHandler)NativeConnectionGuard.CreateGuardedHttpHandler();
                transport.UseCookies = false;
                transport.UseProxy = false;
                handler = transport;
            }
            endpoint = new Uri(origin, "/druid/v2/sql");
            client = new HttpClient(handler);
        }

        public async Task<string> ReadAsync(Guid id, CancellationToken token)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
            request.Headers.Accept.ParseAdd("application/json");
            request.Content = JsonContent.Create(new
            {
                query = Sql,
                resultFormat = "object",
                parameters = new[] { new { type = "VARCHAR", value = id.ToString("D") } },
                context = new { timeout = 10000 }
            });
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token);
            response.EnsureSuccessStatusCode();
            if (response.Content.Headers.ContentType?.MediaType != "application/json")
                throw new InvalidDataException("Unexpected Druid SQL content type.");
            await response.Content.LoadIntoBufferAsync(128 * 1024, token);
            using var json = JsonDocument.Parse(await response.Content.ReadAsByteArrayAsync(token),
                new JsonDocumentOptions { MaxDepth = 8 });
            var rows = json.RootElement;
            if (rows.ValueKind != JsonValueKind.Array || rows.GetArrayLength() > 1)
                throw new InvalidDataException("The reviewed Druid datasource requires unique public teaching IDs.");
            if (rows.GetArrayLength() == 0) return null!;
            var row = rows[0];
            if (row.ValueKind != JsonValueKind.Object || row.GetPropertyCount() != 1 ||
                !row.TryGetProperty("doc", out var doc) || doc.ValueKind != JsonValueKind.String)
                throw new InvalidDataException("A Druid row must contain one canonical holon document.");
            return doc.GetString()!;
        }

        public Task VerifyAsync(CancellationToken cancellationToken = default) => Unsupported();
        public Task<string> GetAsync(string key, CancellationToken cancellationToken = default) =>
            Task.FromException<string>(new NotSupportedException("Use an admitted Druid CQL plan."));
        public Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken cancellationToken = default) =>
            Task.FromException<IReadOnlyList<string>>(new NotSupportedException("Druid teaching scans are unavailable."));
        public Task PutAsync(string key, string value, CancellationToken cancellationToken = default) => Unsupported();
        public Task DeleteAsync(string key, CancellationToken cancellationToken = default) => Unsupported();
        private static Task Unsupported() => Task.FromException(new NotSupportedException("Druid teaching access is read-only."));
        public void Dispose() => client.Dispose();
    }
}
