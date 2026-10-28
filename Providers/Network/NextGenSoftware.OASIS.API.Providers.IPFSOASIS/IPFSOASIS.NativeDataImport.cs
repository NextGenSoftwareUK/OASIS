using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
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
    /// Reads directories other applications published to IPFS so NativeDataImportManager can holonize them. Each path listed in
    /// <see cref="NativeImportPaths"/> (<c>/ipfs/&lt;cid&gt;</c> or <c>/ipns/&lt;name&gt;</c>) is a source and each entry of that
    /// directory a record keyed by its name, paged in name order. IPFS holds files rather than rows, so a record carries the
    /// entry's name, CID and size; the content itself stays on IPFS and is reachable through the cid field.
    /// </summary>
    public partial class IPFSOASIS : INativeDataImportProvider
    {
        private static readonly Regex NativePathPattern = new Regex("^/(ipfs|ipns)/[A-Za-z0-9._-]{1,256}(/[^/\\s]+)*$", RegexOptions.CultureInvariant);

        /// <summary>Comma-separated /ipfs/&lt;cid&gt; or /ipns/&lt;name&gt; directories native import may read (DNA IPFSOASIS.NativeImportPaths).</summary>
        public string NativeImportPaths
        {
            get => _nativeImportPaths ?? _OASISDNA?.OASIS?.StorageProviders?.IPFSOASIS?.NativeImportPaths;
            set => _nativeImportPaths = value;
        }

        private string _nativeImportPaths;

        /// <summary>Lists a directory's entries. Replaced in tests; by default it asks the activated IPFS daemon.</summary>
        internal Func<string, CancellationToken, Task<IReadOnlyList<NativeIpfsEntry>>> NativeDirectoryListing { get; set; }

        internal readonly struct NativeIpfsEntry
        {
            public NativeIpfsEntry(string name, string cid, long size) { Name = name; Cid = cid; Size = size; }
            public string Name { get; }
            public string Cid { get; }
            public long Size { get; }
        }

        public API.Core.Enums.ProviderType NativeDataProviderType => API.Core.Enums.ProviderType.IPFSOASIS;
public string DescribeNativeConnection(NativeConnection connection)        {            NativeConnectionGuard.RefuseUnsupported(connection, "IPFS");            return null;        }

        public async Task<OASISResult<IReadOnlyList<NativeDataSource>>> ListNativeDataSourcesAsync(NativeConnection connection = null, CancellationToken cancellationToken = default)
        {
            var result = new OASISResult<IReadOnlyList<NativeDataSource>>();
            try
            {
                NativeConnectionGuard.RefuseUnsupported(connection, "IPFS");
                var sources = new List<NativeDataSource>();
                foreach (var path in AllowedNativePaths())
                    sources.Add(new NativeDataSource(path, "directory", (await ListDirectoryAsync(path, cancellationToken)).Count));
                result.Result = sources;
                result.Message = $"{sources.Count} allowed IPFS directories.";
            }
            catch (Exception cause) when (cause is InvalidOperationException or IOException or ArgumentException)
            {
                OASISErrorHandling.HandleError(ref result, $"Listing native IPFS directories failed: {cause.Message}", cause);
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
                NativeConnectionGuard.RefuseUnsupported(connection, "IPFS");
                if (pageSize < 1) throw new ArgumentException("Page size must be at least 1.");
                if (!AllowedNativePaths().Contains(sourceName, StringComparer.Ordinal))
                    throw new ArgumentException($"'{sourceName}' is not a path listed in NativeImportPaths.");

                var entries = (await ListDirectoryAsync(sourceName, cancellationToken))
                    .Where(entry => continuationToken == null || string.CompareOrdinal(entry.Name, continuationToken) > 0)
                    .OrderBy(entry => entry.Name, StringComparer.Ordinal).Take(pageSize + 1).ToList();

                var records = entries.Take(pageSize).Select(entry => new NativeRecord(sourceName, entry.Name, new Dictionary<string, object>
                { ["name"] = entry.Name, ["cid"] = entry.Cid, ["size"] = entry.Size })).ToList();
                result.Result = new NativeRecordPage(records, entries.Count > pageSize ? records[^1].NativeKey : null);
            }
            catch (Exception cause) when (cause is InvalidOperationException or IOException or ArgumentException)
            {
                OASISErrorHandling.HandleError(ref result, $"Reading native IPFS directory '{sourceName}' failed: {cause.Message}", cause);
                result.ErrorCode = cause is InvalidOperationException ? "NATIVE_IMPORT_UNAVAILABLE" : "NATIVE_IMPORT_READ_FAILED";
            }
            return result;
        }

        private async Task<IReadOnlyList<NativeIpfsEntry>> ListDirectoryAsync(string path, CancellationToken cancellationToken)
        {
            var listing = NativeDirectoryListing ?? DaemonListingAsync;
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

        private IReadOnlyList<string> AllowedNativePaths()
        {
            var paths = (NativeImportPaths ?? "").Split(',').Select(path => path.Trim().TrimEnd('/')).Where(path => path.Length > 0)
                .Distinct(StringComparer.Ordinal).ToList();
            if (paths.Count == 0)
                throw new InvalidOperationException("Set NativeImportPaths to the IPFS directories other applications own before importing native data.");
            var invalid = paths.FirstOrDefault(path => !NativePathPattern.IsMatch(path) || path.Split('/').Contains(".."));
            if (invalid != null)
                throw new InvalidOperationException($"'{invalid}' is not a valid /ipfs/<cid> or /ipns/<name> path.");

            var own = _OASISDNA?.OASIS?.StorageProviders?.IPFSOASIS?.LookUpIPFSAddress?.Trim().TrimEnd('/');
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
