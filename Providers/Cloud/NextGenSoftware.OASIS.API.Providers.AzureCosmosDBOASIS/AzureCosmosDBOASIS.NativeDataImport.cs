using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using NextGenSoftware.OASIS.Common;

[assembly: InternalsVisibleTo("NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.UnitTests")]

namespace NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS
{
    /// <summary>
    /// Reads documents other applications keep in Cosmos DB so NativeDataImportManager can holonize them. Each container
    /// listed in <see cref="NativeImportContainers"/> is a source and each document a record keyed by its id, paged in id
    /// order. Reads use <see cref="NativeImportDatabaseName"/>, and when that is the OASIS database the OASIS containers are
    /// refused, so native import can never read or re-import OASIS's own records.
    /// </summary>
    public partial class AzureCosmosDBOASIS : INativeDataImportProvider
    {
        private readonly object _nativeClientGate = new object();
        private CosmosClient _nativeClient;

        /// <summary>Database other applications own (DNA AzureCosmosDBOASIS.NativeImportDatabaseName).</summary>
        public string NativeImportDatabaseName { get; set; }

        /// <summary>Comma-separated containers native import may read (DNA AzureCosmosDBOASIS.NativeImportContainers).</summary>
        public string NativeImportContainers { get; set; }

        public API.Core.Enums.ProviderType NativeDataProviderType => API.Core.Enums.ProviderType.AzureCosmosDBOASIS;
        public string DescribeNativeConnection(NativeConnection connection)
        {
            if (connection == null) return null;
            var (endpoint, _, database, _) = ParseNativeConnection(connection);
            return "cosmos://" + endpoint.Authority.ToLowerInvariant() + "/" + database;
        }

        public async Task<OASISResult<IReadOnlyList<NativeDataSource>>> ListNativeDataSourcesAsync(NativeConnection connection = null, CancellationToken cancellationToken = default)
        {
            var result = new OASISResult<IReadOnlyList<NativeDataSource>>();
            try
            {
                using var native = await OpenNativeAsync(connection, cancellationToken);
                var sources = new List<NativeDataSource>();
                foreach (var name in native.Containers)
                {
                    var container = native.Client.GetContainer(native.Database, name);
                    using var iterator = container.GetItemQueryIterator<long>(new QueryDefinition("SELECT VALUE COUNT(1) FROM c"));
                    long total = 0;
                    while (iterator.HasMoreResults)
                        foreach (var count in await iterator.ReadNextAsync(cancellationToken)) total += count;
                    sources.Add(new NativeDataSource(name, "container", total));
                }
                result.Result = sources;
                result.Message = $"{sources.Count} Cosmos containers in {native.Database}.";
            }
            catch (Exception cause) when (cause is CosmosException or InvalidOperationException or FormatException)
            {
                OASISErrorHandling.HandleError(ref result, $"Listing native Cosmos containers failed: {(connection == null ? cause.Message : connection.Redact(NativeConnectionGuard.Explain(cause), "key"))}", cause);
                result.ErrorCode = cause is InvalidOperationException ? "NATIVE_IMPORT_UNAVAILABLE" : "NATIVE_IMPORT_LIST_FAILED";
            }
            return result;
        }

        public async Task<OASISResult<NativeRecordPage>> ReadNativeRecordsAsync(string sourceName, string continuationToken, int pageSize,
            NativeConnection connection = null, CancellationToken cancellationToken = default)
        {
            var result = new OASISResult<NativeRecordPage>();
            try
            {
                using var native = await OpenNativeAsync(connection, cancellationToken);
                if (pageSize < 1) throw new ArgumentException("Page size must be at least 1.");
                if (!native.Containers.Contains(sourceName, StringComparer.Ordinal))
                    throw new ArgumentException($"'{sourceName}' is not an importable container of this connection.");

                var container = native.Client.GetContainer(native.Database, sourceName);
                var documents = new List<JObject>();
                using (var iterator = container.GetItemQueryIterator<JObject>(NativePageQuery(continuationToken, pageSize),
                    requestOptions: new QueryRequestOptions { MaxItemCount = pageSize + 1 }))
                {
                    while (iterator.HasMoreResults && documents.Count < pageSize + 1)
                        documents.AddRange(await iterator.ReadNextAsync(cancellationToken));
                }

                var page = documents.Take(pageSize).Select(document => ToNativeRecord(sourceName, document)).ToList();
                result.Result = new NativeRecordPage(page, documents.Count > pageSize ? page[^1].NativeKey : null);
            }
            catch (Exception cause) when (cause is CosmosException or InvalidOperationException or ArgumentException or FormatException)
            {
                OASISErrorHandling.HandleError(ref result, $"Reading native Cosmos container '{sourceName}' failed: {(connection == null ? cause.Message : connection.Redact(NativeConnectionGuard.Explain(cause), "key"))}", cause);
                result.ErrorCode = cause is InvalidOperationException ? "NATIVE_IMPORT_UNAVAILABLE" : "NATIVE_IMPORT_READ_FAILED";
            }
            return result;
        }

        internal static QueryDefinition NativePageQuery(string after, int pageSize)
        {
            var text = after == null
                ? "SELECT TOP @take * FROM c ORDER BY c.id"
                : "SELECT TOP @take * FROM c WHERE c.id > @after ORDER BY c.id";
            var query = new QueryDefinition(text).WithParameter("@take", pageSize + 1);
            return after == null ? query : query.WithParameter("@after", after);
        }

        /// <summary>Top-level scalar properties become fields; nested values become JSON text; Cosmos system properties are dropped.</summary>
        internal static NativeRecord ToNativeRecord(string sourceName, JObject document)
        {
            var id = document["id"]?.Type == JTokenType.String ? document.Value<string>("id") : null;
            if (string.IsNullOrEmpty(id))
                throw new InvalidOperationException($"A document in '{sourceName}' has no string id, so it has no stable native key.");

            var fields = new Dictionary<string, object>(StringComparer.Ordinal);
            foreach (var property in document.Properties())
            {
                if (property.Name.Length > 0 && property.Name[0] == '_') continue;
                fields[property.Name] = ToScalar(property.Value);
            }
            return new NativeRecord(sourceName, id, fields);
        }

        private static object ToScalar(JToken value) => value.Type switch
        {
            JTokenType.Null or JTokenType.Undefined => null,
            JTokenType.String => value.Value<string>(),
            JTokenType.Boolean => value.Value<bool>(),
            JTokenType.Integer => value.Value<long>(),
            JTokenType.Float => value.Value<double>(),
            _ => value.ToString(Formatting.None)
        };

        private static readonly string[] ConnectionSettings = { "endpoint", "key", "database", "containers" };

        /// <summary>A resolved native source: a client, the database and its readable containers. A caller's client is disposed with it.</summary>
        private sealed class NativeLease : IDisposable
        {
            private readonly CosmosClient _ownedClient;
            public CosmosClient Client { get; }
            public string Database { get; }
            public IReadOnlyList<string> Containers { get; }

            public NativeLease(CosmosClient client, string database, IReadOnlyList<string> containers, CosmosClient ownedClient)
            { Client = client; Database = database; Containers = containers; _ownedClient = ownedClient; }

            public void Dispose() => _ownedClient?.Dispose();
        }

        private async Task<NativeLease> OpenNativeAsync(NativeConnection connection, CancellationToken cancellationToken)
        {
            if (connection == null)
            {
                var allowed = AllowedContainers(); // refuse an unconfigured or OASIS-owned source before any client is built
                return new NativeLease(NativeClient(), NativeImportDatabaseName, allowed, null);
            }

            var (endpoint, key, database, containers) = ParseNativeConnection(connection);
            await NativeConnectionGuard.EnsurePublicHostAsync(endpoint.Host, cancellationToken);
            CosmosClient client;
            try
            {
                client = new CosmosClient(endpoint.AbsoluteUri, key, new CosmosClientOptions
                {
                    ConnectionMode = ConnectionMode.Gateway,
                    RequestTimeout = NativeImportOptions.ReadTimeout,
                    // Gateway mode only: every connection resolves the host, checks every address and connects to those addresses.
                    HttpClientFactory = () => new HttpClient(NativeConnectionGuard.CreateGuardedHttpHandler())
                });
            }
            catch (FormatException)
            {
                throw new NativeConnectionRefusedException("The Cosmos account key is not valid (it must be the base64 key from the Azure portal).");
            }
            try
            {
                var names = containers;
                if (names == null)
                {
                    var found = new List<string>();
                    using var iterator = client.GetDatabase(database).GetContainerQueryIterator<ContainerProperties>();
                    while (iterator.HasMoreResults)
                        found.AddRange((await iterator.ReadNextAsync(cancellationToken)).Select(properties => properties.Id));
                    names = found.OrderBy(name => name, StringComparer.Ordinal).ToList();
                }
                return new NativeLease(client, database, names, client);
            }
            catch
            {
                client.Dispose();
                throw;
            }
        }

        /// <summary>Containers are optional: without them every container of the database is a source.</summary>
        private static (Uri Endpoint, string Key, string Database, IReadOnlyList<string> Containers) ParseNativeConnection(NativeConnection connection)
        {
            connection.RequireOnly(ConnectionSettings);
            if (!Uri.TryCreate(connection.Require("endpoint"), UriKind.Absolute, out var endpoint) || endpoint.Scheme != Uri.UriSchemeHttps || !string.IsNullOrEmpty(endpoint.UserInfo))
                throw new NativeConnectionRefusedException("endpoint must be the https address of your Cosmos account, for example https://myaccount.documents.azure.com:443/.");
            var containers = connection.Optional("containers")?.Split(',').Select(name => name.Trim()).Where(name => name.Length > 0)
                .Distinct(StringComparer.Ordinal).ToList();
            return (endpoint, connection.Require("key"), connection.Require("database"), containers);
        }

        private IReadOnlyList<string> AllowedContainers()
        {
            if (string.IsNullOrWhiteSpace(NativeImportDatabaseName))
                throw new InvalidOperationException("Set NativeImportDatabaseName to the Cosmos database other applications own before importing native data.");
            var containers = (NativeImportContainers ?? "").Split(',').Select(name => name.Trim()).Where(name => name.Length > 0)
                .Distinct(StringComparer.Ordinal).ToList();
            if (containers.Count == 0)
                throw new InvalidOperationException("Set NativeImportContainers to the Cosmos containers other applications own before importing native data.");
            if (string.Equals(NativeImportDatabaseName, databaseName, StringComparison.Ordinal))
            {
                var own = containers.FirstOrDefault(name => collectionNames != null && collectionNames.Contains(name, StringComparer.Ordinal));
                if (own != null)
                    throw new InvalidOperationException($"'{own}' is an OASIS container; native import will not read OASIS's own records.");
            }
            return containers;
        }

        private void ReleaseNativeImportClient()
        {
            CosmosClient client;
            lock (_nativeClientGate)
            {
                client = _nativeClient;
                _nativeClient = null;
            }
            client?.Dispose();
        }

        private CosmosClient NativeClient()
        {
            if (serviceEndpoint == null || string.IsNullOrWhiteSpace(authKey))
                throw new InvalidOperationException("The Cosmos provider needs an endpoint and key before importing native data.");
            lock (_nativeClientGate)
            {
                if (_nativeClient != null) return _nativeClient;
                var options = new CosmosClientOptions { ConnectionMode = ConnectionMode.Gateway };
                if (serviceEndpoint.IsLoopback)
                    options.HttpClientFactory = () => new HttpClient(new HttpClientHandler
                    { ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator });
                return _nativeClient = new CosmosClient(serviceEndpoint.ToString(), authKey, options);
            }
        }
    }
}
