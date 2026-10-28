using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using NextGenSoftware.OASIS.Common;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests
{
    public class NativeDataImportTests
    {
        private static readonly Guid Owner = Guid.Parse("6f9619ff-8b86-d011-b42d-00c04fc964ff");

        private static NativeRecord Record(string key, params (string Name, object Value)[] fields)
            => new NativeRecord("people", key, fields.ToDictionary(field => field.Name, field => field.Value));

        [Fact]
        public void StableIdDependsOnlyOnProviderSourceAndKey()
        {
            var id = NativeRecordHolonMapper.StableHolonId(Owner, ProviderType.MongoDBOASIS, "people", "1");

            Assert.Equal(id, NativeRecordHolonMapper.StableHolonId(Owner, ProviderType.MongoDBOASIS, "people", "1"));
            Assert.NotEqual(id, NativeRecordHolonMapper.StableHolonId(Owner, ProviderType.SQLLiteDBOASIS, "people", "1"));
            Assert.NotEqual(id, NativeRecordHolonMapper.StableHolonId(Owner, ProviderType.MongoDBOASIS, "orders", "1"));
            Assert.NotEqual(id, NativeRecordHolonMapper.StableHolonId(Owner, ProviderType.MongoDBOASIS, "people", "2"));
            Assert.NotEqual(id, NativeRecordHolonMapper.StableHolonId(Guid.NewGuid(), ProviderType.MongoDBOASIS, "people", "1"));
            Assert.Throws<ArgumentException>(() => NativeRecordHolonMapper.StableHolonId(Guid.Empty, ProviderType.MongoDBOASIS, "people", "1"));
            Assert.Equal('8', id.ToString()[14]);
        }

        [Fact]
        public void ConventionMapsNameDescriptionFieldsAndLineage()
        {
            var created = NativeRecordHolonMapper.CreateHolon(Owner, ProviderType.MongoDBOASIS,
                Record("k1", ("Title", "Ada"), ("summary", "Engineer"), ("age", 36L)), null);

            Assert.False(created.IsError, created.Message);
            var holon = created.Result;
            Assert.Equal("Ada", holon.Name);
            Assert.Equal("Engineer", holon.Description);
            Assert.Equal(36L, holon.MetaData["age"]);
            Assert.Equal("MongoDBOASIS", holon.MetaData[NativeRecordHolonMapper.ProviderMetaDataKey]);
            Assert.Equal("people", holon.MetaData[NativeRecordHolonMapper.SourceMetaDataKey]);
            Assert.Equal("k1", holon.MetaData[NativeRecordHolonMapper.KeyMetaDataKey]);
            Assert.Equal(NativeRecordHolonMapper.StableHolonId(Owner, ProviderType.MongoDBOASIS, "people", "k1"), holon.Id);
        }

        [Fact]
        public void RecordWithoutNamingFieldsIsNamedBySourceAndKey()
        {
            var holon = NativeRecordHolonMapper.CreateHolon(Owner, ProviderType.MongoDBOASIS, Record("k9", ("count", 3)), null).Result;
            Assert.Equal("people k9", holon.Name);
            Assert.Null(holon.Description);
        }

        [Fact]
        public void MappingOverridesNameRenamesAndExcludes()
        {
            var mapping = NativeDataMapping.ForSource("people");
            mapping.HolonType = HolonType.Avatar;
            mapping.NameField = "handle";
            mapping.FieldRenames["dob"] = "birthDate";
            mapping.ExcludedFields.Add("passwordHash");

            var holon = NativeRecordHolonMapper.CreateHolon(Owner, ProviderType.MongoDBOASIS,
                Record("k2", ("name", "ignored"), ("handle", "ada"), ("dob", "1815-12-10"), ("passwordHash", "x")), mapping).Result;

            Assert.Equal(HolonType.Avatar, holon.HolonType);
            Assert.Equal("ada", holon.Name);
            Assert.Equal("1815-12-10", holon.MetaData["birthDate"]);
            Assert.False(holon.MetaData.ContainsKey("dob"));
            Assert.False(holon.MetaData.ContainsKey("passwordHash"));
        }

        [Fact]
        public void UnsupportedValuesAndReservedKeysAreRefused()
        {
            var nested = NativeRecordHolonMapper.CreateHolon(Owner, ProviderType.MongoDBOASIS, Record("k3", ("tags", new[] { "a" })), null);
            Assert.True(nested.IsError);
            Assert.Contains("tags", nested.Message);

            var reserved = NativeRecordHolonMapper.CreateHolon(Owner, ProviderType.MongoDBOASIS, Record("k4", ("oasis.native.key", "spoof")), null);
            Assert.True(reserved.IsError);
        }

        [Fact]
        public void ApplyReplacesStaleFieldsOnAPreviouslyImportedHolon()
        {
            var holon = NativeRecordHolonMapper.CreateHolon(Owner, ProviderType.MongoDBOASIS, Record("k5", ("name", "Old"), ("removed", 1)), null).Result;

            var applied = NativeRecordHolonMapper.Apply(Owner, ProviderType.MongoDBOASIS, Record("k5", ("name", "New")), null, holon);

            Assert.False(applied.IsError, applied.Message);
            Assert.Equal("New", holon.Name);
            Assert.False(holon.MetaData.ContainsKey("removed"));
        }

        [Fact]
        public void ApplyRefusesAHolonThatIsNotTheRecordsTarget()
        {
            var other = NativeRecordHolonMapper.CreateHolon(Owner, ProviderType.MongoDBOASIS, Record("a"), null).Result;
            Assert.True(NativeRecordHolonMapper.Apply(Owner, ProviderType.MongoDBOASIS, Record("b"), null, other).IsError);
        }

        private sealed class FakeProvider : INativeDataImportProvider
        {
            public readonly List<NativeRecordPage> Pages = new List<NativeRecordPage>();
            public OASISResult<NativeRecordPage> Failure;
            public readonly List<(string Token, int Size)> Reads = new List<(string, int)>();

            public ProviderType NativeDataProviderType => ProviderType.MongoDBOASIS;

            public Task<OASISResult<IReadOnlyList<NativeDataSource>>> ListNativeDataSourcesAsync(CancellationToken cancellationToken = default)
                => Task.FromResult(new OASISResult<IReadOnlyList<NativeDataSource>>(new[] { new NativeDataSource("people", "collection", 3) }));

            public Task<OASISResult<NativeRecordPage>> ReadNativeRecordsAsync(string sourceName, string continuationToken, int pageSize,
                CancellationToken cancellationToken = default)
            {
                Reads.Add((continuationToken, pageSize));
                if (Failure != null) return Task.FromResult(Failure);
                var index = continuationToken == null ? 0 : int.Parse(continuationToken);
                return Task.FromResult(new OASISResult<NativeRecordPage>(Pages[index]));
            }
        }

        private sealed class Store
        {
            public readonly Dictionary<Guid, IHolon> Holons = new Dictionary<Guid, IHolon>();
            public Guid? FailSaveFor;

            public NativeDataImportManager Manager() => new NativeDataImportManager(
                (id, _) => Task.FromResult(new OASISResult<IHolon>(Holons.TryGetValue(id, out var found) ? found : null)),
                (holon, _, _) =>
                {
                    if (holon.Id == FailSaveFor)
                        return Task.FromResult(new OASISResult<IHolon> { IsError = true, Message = "disk full" });
                    Holons[holon.Id] = holon;
                    return Task.FromResult(new OASISResult<IHolon>(holon) { IsSaved = true, HasAnyHolonsChanged = true });
                });
        }

        private static FakeProvider TwoPages() => new FakeProvider
        {
            Pages =
            {
                new NativeRecordPage(new[] { Record("1", ("name", "Ada")), Record("2", ("name", "Alan")) }, "1"),
                new NativeRecordPage(new[] { Record("3", ("name", "Grace")) }, null)
            }
        };

        [Fact]
        public async Task ImportPagesThroughTheWholeSourceAndReimportUpdatesInsteadOfDuplicating()
        {
            var provider = TwoPages();
            var store = new Store();
            var manager = store.Manager();

            var first = await manager.ImportSourceAsync(provider, "people", Owner, pageSize: 2);
            Assert.False(first.IsError, first.Message);
            Assert.Equal(3, first.Result.RecordsRead);
            Assert.Equal(3, first.Result.HolonsCreated);
            Assert.False(first.Result.Truncated);
            Assert.Equal(new[] { ((string)null, 2), ("1", 2) }, provider.Reads);

            var second = await manager.ImportSourceAsync(provider, "people", Owner, pageSize: 2);
            Assert.Equal(0, second.Result.HolonsCreated);
            Assert.Equal(3, second.Result.HolonsUpdated);
            Assert.Equal(3, store.Holons.Count);
        }

        [Fact]
        public async Task RecordFailuresAreReportedWithoutStoppingTheImport()
        {
            var store = new Store { FailSaveFor = NativeRecordHolonMapper.StableHolonId(Owner, ProviderType.MongoDBOASIS, "people", "2") };

            var result = await store.Manager().ImportSourceAsync(TwoPages(), "people", Owner, pageSize: 2);

            Assert.False(result.IsError);
            Assert.True(result.IsWarning);
            Assert.Equal(2, result.Result.HolonsCreated);
            Assert.Equal(1, result.Result.RecordsFailed);
            Assert.Contains("people/2: save failed: disk full", result.Result.Failures);
        }

        [Fact]
        public async Task ReadFailureStopsWithTheProvidersError()
        {
            var provider = new FakeProvider { Failure = new OASISResult<NativeRecordPage> { IsError = true, ErrorCode = "DOWN", Message = "offline" } };

            var result = await new Store().Manager().ImportSourceAsync(provider, "people", Owner);

            Assert.True(result.IsError);
            Assert.Equal("DOWN", result.ErrorCode);
        }

        [Fact]
        public async Task RecordLimitTruncatesAndSaysSo()
        {
            var provider = TwoPages();
            var result = await new Store().Manager().ImportSourceAsync(provider, "people", Owner, pageSize: 2, maximumRecords: 2);

            Assert.Equal(2, result.Result.RecordsRead);
            Assert.True(result.Result.Truncated);
            Assert.Single(provider.Reads);
        }

        [Fact]
        public async Task ProviderReturningAnotherSourcesRecordsIsRefused()
        {
            var provider = new FakeProvider { Pages = { new NativeRecordPage(new[] { new NativeRecord("orders", "1", new Dictionary<string, object>()) }, null) } };
            var result = await new Store().Manager().ImportSourceAsync(provider, "people", Owner);
            Assert.Equal("NATIVE_IMPORT_SOURCE_MISMATCH", result.ErrorCode);
        }

        [Fact]
        public async Task EmptyPageWithContinuationTokenIsRefused()
        {
            var provider = new FakeProvider { Pages = { new NativeRecordPage(Array.Empty<NativeRecord>(), "0") } };
            var result = await new Store().Manager().ImportSourceAsync(provider, "people", Owner);
            Assert.Equal("NATIVE_IMPORT_EMPTY_PAGE", result.ErrorCode);
        }

        [Fact]
        public async Task ImportAllUsesSavedMappingsAndConventionOtherwise()
        {
            var store = new Store();
            var mapping = NativeDataMapping.ForSource("people");
            mapping.HolonType = HolonType.Avatar;

            var result = await store.Manager().ImportAllAsync(TwoPages(), Owner,
                new Dictionary<string, NativeDataMapping> { ["people"] = mapping }, pageSize: 2);

            Assert.False(result.IsError, result.Message);
            Assert.Single(result.Result);
            Assert.All(store.Holons.Values, holon => Assert.Equal(HolonType.Avatar, holon.HolonType));
        }
    }
}
