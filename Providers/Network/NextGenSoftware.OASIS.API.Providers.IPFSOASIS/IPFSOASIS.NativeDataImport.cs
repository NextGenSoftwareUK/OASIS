using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using NextGenSoftware.OASIS.Common;

[assembly: InternalsVisibleTo("NextGenSoftware.OASIS.API.Providers.IPFSOASIS.UnitTests")]

namespace NextGenSoftware.OASIS.API.Providers.IPFSOASIS
{
    /// <summary>
    /// Reads directories other applications published to IPFS so NativeDataImportManager can holonize them. Each path
    /// (<c>/ipfs/&lt;cid&gt;</c> or <c>/ipns/&lt;name&gt;</c>) is a source and each entry of that directory a record keyed by its
    /// name, paged in name order. IPFS holds files rather than rows, so a record carries the entry's name, CID and size; the
    /// content itself stays on IPFS and is reachable through the cid field.
    /// <para>
    /// Given a <see cref="NativeConnection"/> (<c>apiUrl</c>, the HTTP API of an IPFS node, and <c>paths</c>) it lists the
    /// caller's directories through that node. With none it reads <see cref="NativeImportPaths"/> through this node's daemon,
    /// and OASIS's own lookup index is refused.
    /// </para>
    /// </summary>
    public partial class IPFSOASIS : INativeDataImportProvider
    {
        private static readonly Regex NativePathPattern = new Regex("^/(ipfs|ipns)/[A-Za-z0-9._-]{1,256}(/[^/\\s]+)*$", RegexOptions.CultureInvariant);
        private static readonly string[] ConnectionSettings = { "apiUrl", "paths" };
        private static int MaximumListingBytes => NativeImportOptions.MegabytesToBytes(NativeImportOptions.Current.MaximumIpfsListingMegabytes, "MaximumIpfsListingMegabytes");

        /// <summary>Comma-separated /ipfs/&lt;cid&gt; or /ipns/&lt;name&gt; directories native import may read (DNA IPFSOASIS.NativeImportPaths).</summary>
        public string NativeImportPaths
        {
            get => _nativeImportPaths ?? _OASISDNA?.OASIS?.StorageProviders?.IPFSOASIS?.NativeImportPaths;
            set => _nativeImportPaths = value;
        }

        private string _nativeImportPaths;

        /// <summary>Lists a directory's entries. Replaced in tests; by default it asks the activated daemon, or the caller's node.</summary>
        internal Func<string, CancellationToken, Task<IReadOnlyList<NativeIpfsEntry>>> NativeDirectoryListing { get; set; }

        /// <summary>The handler used to reach a caller's node. Replaced in tests; by default it checks every address before connecting.</summary>
        internal Func<HttpMessageHandler> NativeConnectionHandler { get; set; } = NativeConnectionGuard.CreateGuardedHttpHandler;

        internal readonly struct NativeIpfsEntry
        {
            public NativeIpfsEntry(string name, string cid, long size) { Name = name; Cid = cid; Size = size; }
            public string Name { get; }
            public string Cid { get; }
            public long Size { get; }
        }

        public API.Core.Enums.ProviderType NativeDataProviderType => API.Core.Enums.ProviderType.IPFSOASIS;

        public string DescribeNativeConnection(NativeConnection connection)
        {
            if (connection == null) return null;
            var (api, _) = ParseNativeConnection(connection);
            return "ipfs://" + api.Authority.ToLowerInvariant();
        }

        public async Task<OASISResult<IReadOnlyList<NativeDataSource>>> ListNativeDataSourcesAsync(NativeConnection connection = null,
            CancellationToken cancellationToken = default)
        {
            var result = new OASISResult<IReadOnlyList<NativeDataSource>>();
            try
            {
                var sources = new List<NativeDataSource>();
                foreach (var path in AllowedNativePaths(connection))
                    sources.Add(new NativeDataSource(path, "directory", (await ListDirectoryAsync(path, connection, cancellationToken)).Count));
                result.Result = sources;
                result.Message = $"{sources.Count} allowed IPFS directories.";
            }
            catch (Exception cause) when (cause is InvalidOperationException or IOException or ArgumentException or HttpRequestException or JsonException)
            {
                OASISErrorHandling.HandleError(ref result, $"Listing native IPFS directories failed: {NativeConnectionGuard.Explain(cause)}", cause);
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
                if (pageSize < 1) throw new ArgumentException("Page size must be at least 1.");
                if (!AllowedNativePaths(connection).Contains(sourceName, StringComparer.Ordinal))
                    throw new ArgumentException($"'{sourceName}' is not a path of this connection.");

                var entries = (await ListDirectoryAsync(sourceName, connection, cancellationToken))
                    .Where(entry => continuationToken == null || string.CompareOrdinal(entry.Name, continuationToken) > 0)
                    .OrderBy(entry => entry.Name, StringComparer.Ordinal).Take(pageSize + 1).ToList();

                var records = entries.Take(pageSize).Select(entry => new NativeRecord(sourceName, entry.Name, new Dictionary<string, object>
                { ["name"] = entry.Name, ["cid"] = entry.Cid, ["size"] = entry.Size })).ToList();
                result.Result = new NativeRecordPage(records, entries.Count > pageSize ? records[^1].NativeKey : null);
            }
            catch (Exception cause) when (cause is InvalidOperationException or IOException or ArgumentException or HttpRequestException or JsonException)
            {
                OASISErrorHandling.HandleError(ref result, $"Reading native IPFS directory '{sourceName}' failed: {NativeConnectionGuard.Explain(cause)}", cause);
                result.ErrorCode = cause is InvalidOperationException ? "NATIVE_IMPORT_UNAVAILABLE" : "NATIVE_IMPORT_READ_FAILED";
            }
            return result;
        }

        private async Task<IReadOnlyList<NativeIpfsEntry>> ListDirectoryAsync(string path, NativeConnection connection, CancellationToken cancellationToken)
        {
            var listing = NativeDirectoryListing
                ?? (connection != null ? (listed, token) => ListThroughCallersNodeAsync(connection, listed, token) : DaemonListingAsync);
            var entries = await listing(path, cancellationToken);
            var duplicate = entries.GroupBy(entry => entry.Name, StringComparer.Ordinal).FirstOrDefault(group => group.Count() > 1);
            if (duplicate != null) throw new IOException($"{path} lists the name '{duplicate.Key}' twice, so it has no stable record keys.");
            return entries;
        }

        private async Task<IReadOnlyList<NativeIpfsEntry>> DaemonListingAsync(string path, CancellationToken cancellationToken)
        {
            if (!IsProviderActivated || _ipfsClient == null)
                throw new InvalidOperationException("The IPFS provider must be activated before importing native data.");
            var node = await _ipfsClient.FileSystem.ListFileAsync(path, cancellationToken);
            if (!node.IsDirectory)
                throw new IOException($"{path} is a file, not a directory.");
            return node.Links.Select(link => new NativeIpfsEntry(link.Name, link.Id.ToString(), link.Size)).ToList();
        }

        /// <summary>
        /// Lists a directory through the caller's own node with a plain HTTP call (<c>POST /api/v0/ls</c>), not the shared IPFS client,
        /// so the guarded handler checks every address and no redirect is followed.
        /// </summary>
        private async Task<IReadOnlyList<NativeIpfsEntry>> ListThroughCallersNodeAsync(NativeConnection connection, string path, CancellationToken cancellationToken)
        {
            var (api, _) = ParseNativeConnection(connection);
            using var http = new HttpClient(NativeConnectionHandler(), disposeHandler: true) { Timeout = NativeImportOptions.ReadTimeout };
            using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(api, "api/v0/ls?arg=" + Uri.EscapeDataString(path)));
            using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();
            if (response.Content.Headers.ContentLength > MaximumListingBytes)
                throw new IOException($"The node's listing of {path} is larger than {MaximumListingBytes} bytes.");
            var body = await response.Content.ReadAsByteArrayAsync();
            if (body.Length > MaximumListingBytes)
                throw new IOException($"The node's listing of {path} is larger than {MaximumListingBytes} bytes.");

            using var json = JsonDocument.Parse(body);
            if (!json.RootElement.TryGetProperty("Objects", out var objects) || objects.ValueKind != JsonValueKind.Array || objects.GetArrayLength() != 1)
                throw new IOException($"The node did not return one directory for {path}.");
            if (!objects[0].TryGetProperty("Links", out var links) || links.ValueKind != JsonValueKind.Array)
                return new List<NativeIpfsEntry>();
            return links.EnumerateArray().Select(link => new NativeIpfsEntry(
                link.GetProperty("Name").GetString(), link.GetProperty("Hash").GetString(),
                link.TryGetProperty("Size", out var size) && size.TryGetInt64(out var bytes) ? bytes : 0L)).ToList();
        }

        private static (Uri Api, string Paths) ParseNativeConnection(NativeConnection connection)
        {
            connection.RequireOnly(ConnectionSettings);
            if (!Uri.TryCreate(connection.Require("apiUrl"), UriKind.Absolute, out var api) || (api.Scheme != Uri.UriSchemeHttps && api.Scheme != Uri.UriSchemeHttp)
                || !string.IsNullOrEmpty(api.UserInfo) || !string.IsNullOrEmpty(api.Query) || !string.IsNullOrEmpty(api.Fragment))
                throw new NativeConnectionRefusedException("apiUrl must be the http(s) address of your IPFS node's API, for example https://ipfs.example.com:5001/.");
            return (new Uri(api.GetLeftPart(UriPartial.Authority) + "/"), connection.Require("paths"));
        }

        /// <summary>The paths that can be read: the caller's own, or the configured ones minus OASIS's own lookup index.</summary>
        private IReadOnlyList<string> AllowedNativePaths(NativeConnection connection)
        {
            var configured = connection == null ? NativeImportPaths : ParseNativeConnection(connection).Paths;
            var paths = (configured ?? "").Split(',').Select(path => path.Trim().TrimEnd('/')).Where(path => path.Length > 0)
                .Distinct(StringComparer.Ordinal).ToList();
            if (paths.Count == 0)
                throw new InvalidOperationException("Set NativeImportPaths to the IPFS directories other applications own before importing native data, or send a connection.");
            var invalid = paths.FirstOrDefault(path => !NativePathPattern.IsMatch(path) || path.Split('/').Contains(".."));
            if (invalid != null)
                throw new InvalidOperationException($"'{invalid}' is not a valid /ipfs/<cid> or /ipns/<name> path.");

            var own = connection == null ? _OASISDNA?.OASIS?.StorageProviders?.IPFSOASIS?.LookUpIPFSAddress?.Trim().TrimEnd('/') : null;
            if (!string.IsNullOrEmpty(own))
            {
                var ownPath = own.StartsWith("/", StringComparison.Ordinal) ? own : "/ipfs/" + own;
                var clash = paths.FirstOrDefault(path => string.Equals(path, ownPath, StringComparison.Ordinal));
                if (clash != null)
                    throw new InvalidOperationException($"'{clash}' is OASIS's own lookup index; native import will not read it.");
            }
            return paths;
        }
    }
}
