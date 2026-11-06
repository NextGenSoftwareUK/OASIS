using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.Http.Headers;
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

namespace NextGenSoftware.OASIS.API.Providers.PinotOASIS;

/// <summary>Exact-key reads from a dedicated public Pinot teaching datasource.</summary>
public sealed class PinotPublicQuerySource : KeyValueStorageProviderBase, ICosmicQueryStorageProvider,
    ICosmicReadOnlyQueryActivation, IDisposable
{
    private readonly PinotBackend backend;
    private readonly BoundedPublicKeyValueQuery query;
    private readonly Guid probeId;

    public PinotPublicQuerySource(Uri origin, Guid probeId)
        : this(new PinotBackend(origin, probeId, handler: null), probeId) { }

    internal PinotPublicQuerySource(Uri origin, Guid probeId, HttpMessageHandler handler)
        : this(new PinotBackend(origin, probeId, handler), probeId) { }

    public PinotPublicQuerySource(Uri origin, Guid probeId, string username, string password)
        : this(origin, probeId, username, password, handler: null) { }

    internal PinotPublicQuerySource(Uri origin, Guid probeId, string username, string password, HttpMessageHandler? handler)
        : this(new PinotBackend(origin, probeId, handler,
            HostedBasicSqlQueryReader.CreateAuthorization(origin, probeId, username, password)), probeId) { }

    private PinotPublicQuerySource(PinotBackend backend, Guid probeId) : base(backend, "pinot-public:")
    {
        this.backend = backend;
        this.probeId = probeId;
        ProviderType = new EnumValue<PType>(QueryProviderType);
        ProviderName = QueryProviderType.ToString();
        ProviderDescription = "Reviewed public Apache Pinot teaching holons";
        ProviderCategory = new EnumValue<Core.Enums.ProviderCategory>(Core.Enums.ProviderCategory.StorageLocalAndNetwork);
        query = new(QueryProviderType, backend.ReadAsync);
    }

    public PType QueryProviderType => PType.PinotOASIS;
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

    private sealed class PinotBackend : IKeyValueBackend, IDisposable
    {
        private readonly Uri endpoint;
        private readonly HttpClient client;
        private readonly AuthenticationHeaderValue? authorization;

        public PinotBackend(Uri origin, Guid probeId, HttpMessageHandler? handler, AuthenticationHeaderValue? authorization = null)
        {
            if (origin is not { IsAbsoluteUri: true } || origin.AbsolutePath != "/" ||
                origin.Query.Length != 0 || origin.Fragment.Length != 0 || origin.UserInfo.Length != 0 || probeId == Guid.Empty)
                throw new ArgumentException("A reviewed Pinot origin and public probe are required.");

            var loopback = origin.Scheme == "http" && origin.Host == "127.0.0.1" && origin.Port >= 1024;
            var https = origin.Scheme == "https" && origin.HostNameType == UriHostNameType.Dns &&
                !origin.IsLoopback && origin.IsDefaultPort;
            if (!loopback && !https)
                throw new ArgumentException("Use a Pinot HTTPS DNS origin or a fixed IPv4 loopback fixture.");

            if (handler == null)
            {
                var transport = loopback ? new SocketsHttpHandler { AllowAutoRedirect = false }
                    : (SocketsHttpHandler)NativeConnectionGuard.CreateGuardedHttpHandler();
                transport.UseCookies = false;
                transport.UseProxy = false;
                handler = transport;
            }
            endpoint = new Uri(origin, "/query/sql");
            this.authorization = authorization;
            client = new HttpClient(handler);
        }

        public async Task<string> ReadAsync(Guid id, CancellationToken token)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
            request.Headers.Accept.ParseAdd("application/json");
            request.Headers.Authorization = authorization;
            // The only SQL literal comes from a strongly typed GUID, never request text.
            request.Content = JsonContent.Create(new
            {
                sql = $"SELECT doc FROM oasis_cql_public_holons WHERE id = '{id:D}' AND is_public = 1 LIMIT 2"
            });
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token);
            response.EnsureSuccessStatusCode();
            if (response.Content.Headers.ContentType?.MediaType != "application/json")
                throw new InvalidDataException("Unexpected Pinot broker content type.");
            await response.Content.LoadIntoBufferAsync(128 * 1024, token);
            using var json = JsonDocument.Parse(await response.Content.ReadAsByteArrayAsync(token),
                new JsonDocumentOptions { MaxDepth = 12 });
            var envelope = json.RootElement;
            if (envelope.ValueKind != JsonValueKind.Object ||
                envelope.GetProperty("exceptions").GetArrayLength() != 0 ||
                envelope.GetProperty("partialResult").GetBoolean() ||
                envelope.GetProperty("numServersQueried").GetInt32() != envelope.GetProperty("numServersResponded").GetInt32())
                throw new InvalidDataException("The Pinot broker did not return a complete query result.");

            var table = envelope.GetProperty("resultTable");
            var schema = table.GetProperty("dataSchema");
            if (!IsSingleValue(schema.GetProperty("columnNames"), "doc") ||
                !IsSingleValue(schema.GetProperty("columnDataTypes"), "STRING"))
                throw new InvalidDataException("A Pinot result must project the reviewed document column.");
            var rows = table.GetProperty("rows");
            if (rows.ValueKind != JsonValueKind.Array || rows.GetArrayLength() > 1)
                throw new InvalidDataException("The reviewed Pinot table requires unique public teaching IDs.");
            if (rows.GetArrayLength() == 0) return null!;
            var row = rows[0];
            if (row.ValueKind != JsonValueKind.Array || row.GetArrayLength() != 1 || row[0].ValueKind != JsonValueKind.String)
                throw new InvalidDataException("A Pinot row must contain one canonical holon document.");
            return row[0].GetString()!;
        }

        private static bool IsSingleValue(JsonElement values, string expected) =>
            values.ValueKind == JsonValueKind.Array && values.GetArrayLength() == 1 &&
            values[0].ValueKind == JsonValueKind.String && values[0].GetString() == expected;

        public Task VerifyAsync(CancellationToken cancellationToken = default) => Unsupported();
        public Task<string> GetAsync(string key, CancellationToken cancellationToken = default) =>
            Task.FromException<string>(new NotSupportedException("Use an admitted Pinot CQL plan."));
        public Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken cancellationToken = default) =>
            Task.FromException<IReadOnlyList<string>>(new NotSupportedException("Pinot teaching scans are unavailable."));
        public Task PutAsync(string key, string value, CancellationToken cancellationToken = default) => Unsupported();
        public Task DeleteAsync(string key, CancellationToken cancellationToken = default) => Unsupported();
        private static Task Unsupported() => Task.FromException(new NotSupportedException("Pinot teaching access is read-only."));
        public void Dispose() => client.Dispose();
    }
}
