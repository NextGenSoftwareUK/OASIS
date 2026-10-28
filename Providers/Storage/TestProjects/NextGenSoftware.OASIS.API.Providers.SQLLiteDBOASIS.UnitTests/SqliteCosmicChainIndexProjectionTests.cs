using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.DNA;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.UnitTests;

public partial class SqliteCosmicChainIndexRowsTests
{
    public sealed class IndexedDto
    {
        public static int Constructed;
        public IndexedDto() { Constructed++; }
        public Guid Id { get; set; }
        public string Name { get; set; } = "";
        public int Version { get; set; }
        public bool Public { get; set; }
    }

    [DataTestMethod]
    [DataRow("rows")] [DataRow("page")] [DataRow("empty")] [DataRow("source-missing")]
    [DataRow("source-changed")] [DataRow("stale")] [DataRow("tamper")] [DataRow("cancelled")] [DataRow("payload")]
    public async Task NativeIndexedSelectedCellsKeepVisibilityWindowAndEvidenceBeforeDtoConstruction(string mode)
    {
        using var store = new SqliteCosmicChainIndexStore(database, source, key, canonicalRowProjection: true);
        var a = Entry("A", actor, false);
        var b = Entry("B", Guid.NewGuid(), true);
        var hidden = Entry("0-hidden", Guid.NewGuid(), false);
        var deleted = Entry("0-deleted", actor, true, true);
        var entries = new[] { a, b, hidden, deleted };
        Publish(store, entries);
        if(mode == "tamper") Sql("UPDATE CosmicChainIndexRows SET Name='changed' WHERE Name='A'");
        var observer = new Observer(source, position, entries, mode);
        var query = Query().OrderBy(row => row.Name).Result;
        if(mode == "page") query = query.Skip(1).Result;
        if(mode == "empty") query = query.Where(row => row.Name == "missing").Result;
        query = query.Take(1).Result;
        var selected = query.Select(row => new IndexedDto { Id = row.Id, Name = row.Name, Version = row.Version, Public = row.IsPublic });
        Assert.IsFalse(selected.IsError, selected.Message);
        var plan = CosmicProjectionPlanner.Plan(selected.Result, source.SourceProvider, new[] {
            new CosmicProjectionTarget(source.SourceProvider, store.IndexedRowCapabilities, store.IndexedProjectionCapabilities, source) });
        Assert.IsFalse(plan.IsError, plan.Message);
        IndexedDto.Constructed = 0;
        var attempts = new SqlAttemptOwner();
        using var cancellation = new CancellationTokenSource();
        if(mode == "cancelled") cancellation.Cancel();
        var result = await CosmicIndexedReadAttempts.RunAsync(source, attempts, () => CosmicIndexedProjectionExecutor.ExecuteAsync(
            plan.Result, store, observer, maximumPayloadBytes: mode == "payload" ? 1 : 10000, cancellationToken: cancellation.Token));
        Assert.AreEqual(mode == "cancelled" ? 0 : 1, attempts.Count);
        if(mode is "rows" or "page" or "empty")
        {
            Assert.IsFalse(result.IsError, result.Message + result.Exception);
            Assert.AreEqual(mode == "empty" ? 0 : 1, result.Result.Rows.Count);
            Assert.AreEqual(result.Result.Rows.Count, IndexedDto.Constructed);
            Assert.AreEqual(result.Result.Rows.Count, result.Result.Receipts.Count);
            Assert.AreEqual(position.Slot, result.Result.Checkpoint.Position.Slot);
            Assert.AreEqual(1, observer.HeadCalls);
            Assert.AreEqual(result.Result.Rows.Count, observer.RecordCalls);
            if(mode != "empty")
            {
                var expected = mode == "page" ? b : a;
                Assert.AreEqual(expected.Receipt.Record.Id, result.Result.Rows.Single().Id);
                Assert.AreEqual(mode == "page" ? "B" : "A", result.Result.Rows.Single().Name);
                Assert.AreEqual(mode == "page", result.Result.Rows.Single().Public);
                Assert.AreEqual(expected.Receipt.CommitReference, result.Result.Receipts.Single().CommitReference);
            }
        }
        else
        {
            Assert.IsTrue(result.IsError, mode);
            Assert.IsNull(result.Result);
            Assert.AreEqual(0, IndexedDto.Constructed);
        }
    }

    [TestMethod]
    public void IndexedMetadataProjectionRejectsBeforeSqlOrSourceDispatch()
    {
        using var store = new SqliteCosmicChainIndexStore(database, source, key, canonicalRowProjection: true);
        var selected = Query().Select(row => CosmicQueryMetadata.Value<string>(row, "private"));
        Assert.IsFalse(selected.IsError, selected.Message);
        var plan = CosmicProjectionPlanner.Plan(selected.Result, source.SourceProvider, new[] {
            new CosmicProjectionTarget(source.SourceProvider, store.IndexedRowCapabilities, store.IndexedProjectionCapabilities, source) });
        Assert.IsTrue(plan.IsError);
        Assert.AreEqual("CQL_CAPABILITY_UNSUPPORTED", plan.ErrorCode);
        Assert.IsFalse(File.Exists(database));
    }

    [TestMethod]
    public async Task NativeSelectedScalarAndEveryCanonicalColumnRetainExactClrDomains()
    {
        using var store = new SqliteCosmicChainIndexStore(database, source, key, canonicalRowProjection: true);
        var entry = Entry("A", actor, false);
        Publish(store, entry);
        var observer = new Observer(source, position, new[] { entry }, "rows");
        var target = new CosmicProjectionTarget(source.SourceProvider, store.IndexedRowCapabilities, store.IndexedProjectionCapabilities, source);
        var scalar = CosmicProjectionPlanner.Plan(Query().Select(row => row.Name).Result, source.SourceProvider, new[] { target });
        Assert.IsFalse(scalar.IsError, scalar.Message);
        var value = await CosmicIndexedProjectionExecutor.ExecuteAsync(scalar.Result, store, observer);
        Assert.IsFalse(value.IsError, value.Message + value.Exception);
        Assert.AreEqual("A", value.Result.Rows.Single());

        var selected = Query().Select(row => new { row.Id, row.Name, row.Version, row.VersionId, row.CreatedByAvatarId, row.IsPublic, row.IsDeleted });
        var plan = CosmicProjectionPlanner.Plan(selected.Result, source.SourceProvider, new[] { target });
        Assert.IsFalse(plan.IsError, plan.Message);
        var result = await CosmicIndexedProjectionExecutor.ExecuteAsync(plan.Result, store, observer);
        Assert.IsFalse(result.IsError, result.Message + result.Exception);
        var row = result.Result.Rows.Single();
        Assert.AreEqual(entry.Receipt.Record.Id, row.Id);
        Assert.AreEqual(entry.Receipt.Record.VersionId, row.VersionId);
        Assert.AreEqual(actor, row.CreatedByAvatarId);
        Assert.AreEqual(1, row.Version);
        Assert.AreEqual("A", row.Name);
        Assert.IsFalse(row.IsPublic);
        Assert.IsFalse(row.IsDeleted);
    }

    [DataTestMethod]
    [DataRow("rows")] [DataRow("deferred")] [DataRow("empty")] [DataRow("source-changed")]
    [DataRow("stages")] [DataRow("cancelled")]
    public async Task StandardIndexedSelectDefersSourceBindingAndRetainsEvidence(string mode)
    {
        using var store = new SqliteCosmicChainIndexStore(database, source, key, canonicalRowProjection: true);
        var entry = Entry("A", actor, false);
        Publish(store, entry);
        var observer = new Observer(source, position, new[] { entry }, mode);
        var dna = new OASISDNA { OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS { HyperDriveMode = HyperDriveModes.V2 } };
        var manager = new HolonManager(null, dna, new ProviderManager(null, dna));
        var created = CosmicLinq.CreateIndexed(manager, store, observer, Query());
        Assert.IsFalse(created.IsError, created.Message);
        var name = mode is "deferred" or "empty" ? "missing" : "A";
        IndexedDto.Constructed = 0;
        var selected = created.Result.Where(row => row.Name == name).OrderBy(row => row.Name).Take(1)
            .Select(row => new IndexedDto { Id = row.Id, Name = row.Name });
        if(mode == "stages") selected = selected.Where(row => row.Name == "A");
        Assert.AreEqual(0, IndexedDto.Constructed);
        Assert.AreEqual(0, observer.RecordCalls);
        if(mode == "deferred") name = "A";
        using var cancellation = new CancellationTokenSource();
        if(mode == "cancelled") cancellation.Cancel();
        var attempts = new SqlAttemptOwner();
        var result = await CosmicIndexedReadAttempts.RunAsync(source, attempts,
            () => selected.ToCosmicIndexedProjectionPageAsync(cancellation.Token));
        Assert.AreEqual(mode is "stages" or "cancelled" ? 0 : 1, attempts.Count);
        if(mode is "rows" or "deferred" or "empty")
        {
            Assert.IsFalse(result.IsError, result.Message + result.Exception);
            Assert.AreEqual(mode == "empty" ? 0 : 1, result.Result.Rows.Count);
            Assert.AreEqual(result.Result.Rows.Count, IndexedDto.Constructed);
            Assert.AreEqual(result.Result.Rows.Count, result.Result.Receipts.Count);
            if(mode != "empty") Assert.AreEqual(entry.Receipt.Record.Id, result.Result.Rows.Single().Id);
        }
        else { Assert.IsTrue(result.IsError); Assert.IsNull(result.Result); Assert.AreEqual(0, IndexedDto.Constructed); }
    }

    [TestMethod]
    public async Task IndexedSelectedListsReselectAndUnsupportedTerminalsUseSharedBinder()
    {
        using var store = new SqliteCosmicChainIndexStore(database, source, key, canonicalRowProjection: true);
        var entry = Entry("A", actor, false);
        Publish(store, entry);
        var observer = new Observer(source, position, new[] { entry }, "rows");
        var dna = new OASISDNA { OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS { HyperDriveMode = HyperDriveModes.V2 } };
        var manager = new HolonManager(null, dna, new ProviderManager(null, dna));
        var created = CosmicLinq.CreateIndexed(manager, store, observer, Query());
        Assert.IsFalse(created.IsError, created.Message);
        IndexedDto.Constructed = 0;
        var selected = created.Result.Select(row => new IndexedDto { Name = row.Name });
        var flat = selected.Select(dto => dto.Name);
        var page = await flat.ToCosmicIndexedProjectionPageAsync();
        Assert.IsFalse(page.IsError, page.Message + page.Exception);
        Assert.AreEqual("A", page.Result.Rows.Single());
        Assert.AreEqual(0, IndexedDto.Constructed); // No intermediate DTO is executed.
        var list = await flat.ToCosmicListAsync();
        Assert.IsFalse(list.IsError, list.Message);
        Assert.AreEqual("A", list.Result.Single());
        Assert.AreEqual("A", flat.AsEnumerable().Single());
        var calls = observer.RecordCalls;
        var failure = Assert.ThrowsException<CosmicLinqException>(() => flat.Count());
        Assert.AreEqual("CQL_EXPRESSION_UNSUPPORTED", failure.Code);
        Assert.AreEqual(calls, observer.RecordCalls);
    }
}
