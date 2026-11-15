using System.Diagnostics;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using Xunit;
using Xunit.Abstractions;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public sealed class SqliteMetadataReadPerformanceTests(ITestOutputHelper output)
{
    [Fact]
    public async Task NativeSqliteFiltersMetadataBeforeHolonMapping()
    {
        var folder = Directory.CreateTempSubdirectory("oasis-sqlite-metadata-");
        try
        {
            using var provider = new SQLLiteDBOASIS($"Data Source={Path.Combine(folder.FullName, "metadata.db")};Pooling=False");
            var activated = await provider.ActivateProviderAsync();
            Assert.False(activated.IsError, activated.Message);

            var owner = Guid.NewGuid();
            var otherOwner = Guid.NewGuid();
            const int rowCount = 96;
            const string payload = "large file metadata";
            var roots = new List<Guid>();
            var otherOwnerFile = Guid.NewGuid();
            var childId = Guid.NewGuid();
            for (var i = 0; i < rowCount; i++)
            {
                var file = i < 3;
                var id = i == 2 ? otherOwnerFile : Guid.NewGuid();
                if (i < 2) roots.Add(id);
                var result = await provider.SaveHolonAsync(new Holon
                {
                    Id = id, Name = $"Owned metadata fixture {i}", IsActive = true,
                    MetaData = new Dictionary<string, object>
                    {
                        ["oasisFileType"] = file ? "file" : "record",
                        ["CreatedByAvatarId"] = (i == 2 ? otherOwner : owner).ToString(),
                        ["marker.with dot"] = i == 2 ? "other" : "caller",
                        ["enabled"] = file,
                        ["body"] = string.Concat(Enumerable.Repeat(payload, 512))
                    }
                }, saveChildren: false);
                Assert.True(!result.IsError && result.IsSaved, result.Message);
            }

            var child = await provider.SaveHolonAsync(new Holon
            {
                Id = childId, ParentHolonId = roots[0], Name = "Owned child fixture", IsActive = true
            }, saveChildren: false);
            Assert.True(!child.IsError && child.IsSaved, child.Message);

            var filter = new Dictionary<string, string>
            {
                ["oasisFileType"] = "file", ["marker.with dot"] = "caller", ["enabled"] = "True"
            };
            for (var iteration = 0; iteration < 6; iteration++)
            {
                var start = Stopwatch.GetTimestamp();
                var allRows = await provider.LoadAllHolonsAsync(loadChildren: false);
                Assert.False(allRows.IsError, allRows.Message);
                var baseline = allRows.Result.Where(row => StoredRecordSearch.MatchesMetadata(
                    row.MetaData, filter, MetaKeyValuePairMatchMode.All)).Select(row => row.Id).OrderBy(id => id).ToArray();
                var scanMs = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
                start = Stopwatch.GetTimestamp();
                var filtered = await provider.LoadHolonsByMetaDataAsync(filter, MetaKeyValuePairMatchMode.All, loadChildren: false);
                var nativeMs = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
                Assert.False(filtered.IsError, filtered.Message);
                Assert.Equal(roots.OrderBy(id => id).ToArray(), baseline);
                Assert.Equal(baseline, filtered.Result.Select(row => row.Id).OrderBy(id => id).ToArray());
                Assert.All(filtered.Result, row => Assert.Empty(row.Children ?? []));
                Assert.Equal(2, filtered.LoadedCount);
                if (iteration > 0)
                    output.WriteLine($"SQLITE_METADATA iteration={iteration} allRows={allRows.LoadedCount} selectedRows={filtered.LoadedCount} fullMappingMs={scanMs:F3} nativePredicateMs={nativeMs:F3}");
            }

            var eager = await provider.LoadHolonsByMetaDataAsync(filter, MetaKeyValuePairMatchMode.All, loadChildren: true);
            Assert.False(eager.IsError, eager.Message);
            Assert.Equal(childId, Assert.Single(eager.Result.Single(row => row.Id == roots[0]).Children).Id);
            var any = await provider.LoadHolonsByMetaDataAsync(new Dictionary<string, string>
                { ["marker.with dot"] = "other", ["unknown"] = "no-match" }, MetaKeyValuePairMatchMode.Any, loadChildren: false);
            Assert.False(any.IsError, any.Message);
            Assert.Equal(otherOwnerFile, Assert.Single(any.Result).Id);

            var dna = new OASISDNA { OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS
                { HyperDriveMode = HyperDriveModes.V2, StorageProviders = new StorageProviderSettings() } };
            var providers = new ProviderManager(null, dna)
                { IsAutoFailOverEnabled = false, IsAutoLoadBalanceEnabled = false, IsAutoReplicationEnabled = false };
            providers.RegisterProvider(provider);
            var files = new FilesManager(provider, dna, providers);
            var fileList = await files.GetAllFilesForAvatarAsync(owner);
            var count = await files.GetFileCountForAvatarAsync(owner);
            Assert.False(fileList.IsError, fileList.Message);
            Assert.False(count.IsError, count.Message);
            Assert.Equal(roots.OrderBy(id => id).ToArray(), fileList.Result.Select(file => file.Id).OrderBy(id => id).ToArray());
            Assert.Equal(fileList.Result.Count, count.Result);

            var otherCount = await files.GetFileCountForAvatarAsync(otherOwner);
            Assert.False(otherCount.IsError, otherCount.Message);
            Assert.Equal(1, otherCount.Result);
        }
        finally
        {
            folder.Delete(recursive: true);
        }
    }
}
