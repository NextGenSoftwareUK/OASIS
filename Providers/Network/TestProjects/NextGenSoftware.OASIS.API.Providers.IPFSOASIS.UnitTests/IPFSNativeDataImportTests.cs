using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Providers.IPFSOASIS;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.IPFSOASIS.UnitTests
{
    /// <summary>The IPFS native reader's paging, keys and path allowlist, with directory listings supplied by the test.</summary>
    public class IPFSNativeDataImportTests
    {
        private const string Directory = "/ipfs/QmPeople";

        private static IPFSOASIS Provider(string paths = Directory, params (string Name, string Cid, long Size)[] entries)
        {
            var listing = entries.Select(entry => new IPFSOASIS.NativeIpfsEntry(entry.Name, entry.Cid, entry.Size)).ToList();
            return new IPFSOASIS(new NextGenSoftware.OASIS.API.DNA.OASISDNA())
            {
                NativeImportPaths = paths,
                NativeDirectoryListing = (path, _) => Task.FromResult<IReadOnlyList<IPFSOASIS.NativeIpfsEntry>>(listing)
            };
        }

        [Fact]
        public async Task ListsConfiguredDirectoriesWithEntryCounts()
        {
            var sources = await Provider(Directory + "/", ("ada.json", "QmA", 10), ("grace.json", "QmG", 20)).ListNativeDataSourcesAsync();
            Assert.False(sources.IsError, sources.Message);
            var source = Assert.Single(sources.Result);
            Assert.Equal(Directory, source.Name);
            Assert.Equal(2, source.EstimatedRecordCount);
        }

        [Fact]
        public async Task PagesEntriesInNameOrderKeyedByName()
        {
            var provider = Provider(Directory, ("c.txt", "QmC", 3), ("a.txt", "QmA", 1), ("b.txt", "QmB", 2));

            var first = await provider.ReadNativeRecordsAsync(Directory, null, 2);
            Assert.False(first.IsError, first.Message);
            Assert.Equal(new[] { "a.txt", "b.txt" }, first.Result.Records.Select(record => record.NativeKey));
            Assert.Equal("b.txt", first.Result.ContinuationToken);
            Assert.Equal("QmA", first.Result.Records[0].Fields["cid"]);
            Assert.Equal(1L, first.Result.Records[0].Fields["size"]);

            var second = await provider.ReadNativeRecordsAsync(Directory, "b.txt", 2);
            Assert.Equal("c.txt", Assert.Single(second.Result.Records).NativeKey);
            Assert.Null(second.Result.ContinuationToken);
        }

        [Fact]
        public async Task InvalidUnlistedAndDuplicateNamedSourcesAreRefused()
        {
            Assert.Equal("NATIVE_IMPORT_UNAVAILABLE", (await Provider(null).ListNativeDataSourcesAsync()).ErrorCode);
            Assert.Equal("NATIVE_IMPORT_UNAVAILABLE", (await Provider("QmNoPrefix").ListNativeDataSourcesAsync()).ErrorCode);
            Assert.Equal("NATIVE_IMPORT_UNAVAILABLE", (await Provider("/ipfs/Qm/../Other").ListNativeDataSourcesAsync()).ErrorCode);
            Assert.Equal("NATIVE_IMPORT_READ_FAILED", (await Provider().ReadNativeRecordsAsync("/ipfs/QmOther", null, 5)).ErrorCode);

            var duplicate = await Provider(Directory, ("a", "Qm1", 1), ("a", "Qm2", 2)).ReadNativeRecordsAsync(Directory, null, 5);
            Assert.True(duplicate.IsError);
            Assert.Equal("NATIVE_IMPORT_READ_FAILED", duplicate.ErrorCode);
        }

        [Fact]
        public async Task OasisOwnLookupIndexIsRefused()
        {
            var dna = new NextGenSoftware.OASIS.API.DNA.OASISDNA();
            dna.OASIS.StorageProviders = new NextGenSoftware.OASIS.API.DNA.StorageProviderSettings
            { IPFSOASIS = new NextGenSoftware.OASIS.API.DNA.IPFSOASISSettings { LookUpIPFSAddress = "QmOasisIndex" } };

            var provider = new IPFSOASIS(dna) { NativeImportPaths = "/ipfs/QmOasisIndex" };
            var result = await provider.ListNativeDataSourcesAsync();
            Assert.Equal("NATIVE_IMPORT_UNAVAILABLE", result.ErrorCode);
            Assert.Contains("own lookup index", result.Message);
        }
    }
}
