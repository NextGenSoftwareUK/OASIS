using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Serialization;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public sealed partial class CqlV1Web4NativeTests
{
    [DataTestMethod]
    [DataRow("MongoDBOASIS")] [DataRow("SQLLiteDBOASIS")] [DataRow("Neo4jOASIS")]
    public Task PropertySelectionStatusReadsExactDurableOutcomeWithoutMutation(string providerName)
        => RunPropertyNativeFixture(providerName, true, true);

    private static async Task<int> VerifySelectedNativeStatus(IOASISStorageProvider provider,
        ProviderType authority, Holon source, Guid actor, HashSet<Guid> operations, string work,
        Func<Guid, Task<string>> inspectReceipt, HolonManager? routedManager = null,
        PropertyOutcomeBoundary? routeProbe = null)
    {
        var storage = (IConditionalHolonPropertyStorageProvider)provider;
        var whole = (IConditionalHolonWriteRecoveryStorageProvider)provider;
        var checks = 0;
        Task<OASISResult<ConditionalHolonWriteOutcome>> ReadSelected(ConditionalHolonPropertyWriteRequest original)
            => routedManager == null ? storage.ReadConditionalHolonPropertyWriteOutcomeAsync(original)
                : routedManager.ReadConditionalHolonPropertyWriteOutcomeAsync(original, actor);
        string Capture(IHolon value) => StoredRecordJson.CaptureRuntimeToken(value).ToString(Newtonsoft.Json.Formatting.None);
        Guid Revision(IHolon value) => CosmicConcurrency.GetProviderRevisionId(value, authority)
            ?? throw new InvalidOperationException("Native fixture revision missing.");
        ConditionalHolonPropertyWriteRequest Request(Holon proposed, Guid revision, params string[] paths)
        {
            var basis = new ConditionalHolonWriteRequest(new ConditionalHolonWrite(Guid.NewGuid(), actor, authority,
                new[] { new HolonWriteCondition(source.Id, revision) }), ConditionalHolonOperation.Save,
                new[] { proposed }, false, false, 0);
            return new(basis, new Dictionary<Guid, IReadOnlyList<string>> { [source.Id] = paths });
        }
        async Task<Holon> Read(int version)
        {
            var result = await provider.LoadHolonAsync(source.Id, false, false, 0, true, false, 0);
            Assert.IsFalse(result.IsError, result.Message + result.Exception);
            Assert.AreEqual(version, result.Result.Version);
            return StoredRecordJson.Project<Holon>(result.Result);
        }
        async Task<OASISResult<ConditionalHolonWriteOutcome>> Observe(
            Func<Task<OASISResult<ConditionalHolonWriteOutcome>>> read, Guid operation)
        {
            operations.Add(operation);
            var before = await inspectReceipt(operation);
            var result = await read();
            Assert.AreEqual(before, await inspectReceipt(operation), "Outcome reads must neither create nor change native receipts.");
            return result;
        }
        async Task<ConditionalHolonWriteOutcome> Status(ConditionalHolonPropertyWriteRequest original,
            ConditionalHolonReceiptState expected)
        {
            var reads = routeProbe?.OutcomeReads ?? 0;
            var result = await Observe(() => ReadSelected(original),
                original.WriteRequest.Write.OperationId);
            if (routeProbe != null) Assert.AreEqual(reads + 1, routeProbe.OutcomeReads);
            Assert.IsFalse(result.IsError, result.Message + result.Exception); Assert.IsTrue(result.IsLoaded);
            var value = result.Result;
            Assert.AreEqual(original.WriteRequest.Write.OperationId, value.OperationId);
            Assert.AreEqual(actor, value.ActorId); Assert.AreEqual(authority, value.Authority);
            Assert.AreEqual(ConditionalHolonOperation.Save, value.Operation);
            Assert.AreEqual(expected, value.ReceiptState); Assert.IsNull(value.ReceiptEvidence);
            if (expected == ConditionalHolonReceiptState.Absent) Assert.IsNull(value.CommittedDeltaSummaryJson);
            return value;
        }
        async Task<string> Write(ConditionalHolonPropertyWriteRequest request, bool synchronous, int version)
        {
            operations.Add(request.WriteRequest.Write.OperationId);
            await File.WriteAllTextAsync(Path.Combine(work, "selected-status-fixture.json"),
                System.Text.Json.JsonSerializer.Serialize(new { id = source.Id, actor, operations, stage = "before-status-fixture-write" }));
            var result = synchronous ? storage.ExecuteConditionalHolonPropertyWrite(request)
                : await storage.ExecuteConditionalHolonPropertyWriteAsync(request);
            Assert.IsFalse(result.IsError, result.Message + result.Exception); Assert.IsTrue(result.IsSaved);
            Assert.AreEqual(version, result.Result.Single().Version);
            return result.MetaData[HolonCommittedDelta.ResultMetadataKey];
        }

        var baseline = Capture(source);
        var proposed = StoredRecordJson.Project<Holon>(source);
        proposed.Name = "unselected status caller name"; proposed.Description = "first selected value";
        var caller = Capture(proposed);
        var first = Request(proposed, Revision(source), "/Description");
        await Status(first, ConditionalHolonReceiptState.Absent);
        Assert.AreEqual(Revision(source), Revision(await Read(1))); checks++;

        var firstSummary = await Write(first, false, 2);
        var current = await Read(2); var currentRevision = Revision(current);
        Assert.AreEqual(firstSummary, (await Status(first, ConditionalHolonReceiptState.Committed)).CommittedDeltaSummaryJson);
        Assert.AreEqual(firstSummary, (await Status(first, ConditionalHolonReceiptState.Committed)).CommittedDeltaSummaryJson);
        Assert.AreEqual(currentRevision, Revision(await Read(2))); checks++;

        // A known absent receipt with now-stale conditions is still absence, not a rejection or retry grant.
        var absent = Request(proposed, Revision(source), "/Description");
        await Status(absent, ConditionalHolonReceiptState.Absent);
        Assert.AreEqual(currentRevision, Revision(await Read(2))); checks++;

        var removed = StoredRecordJson.Project<Holon>(current); removed.MetaData.Remove("nullable");
        var removal = Request(removed, currentRevision, "/MetaData/nullable");
        var removalSummary = await Write(removal, true, 3);
        current = await Read(3); Assert.IsFalse(current.MetaData.ContainsKey("nullable")); currentRevision = Revision(current);
        Assert.AreEqual(removalSummary, (await Status(removal, ConditionalHolonReceiptState.Committed)).CommittedDeltaSummaryJson);
        Assert.AreEqual(currentRevision, Revision(await Read(3))); checks++;

        // Read the old receipt after a later write: never infer its outcome from the current holon.
        Assert.AreEqual(firstSummary, (await Status(first, ConditionalHolonReceiptState.Committed)).CommittedDeltaSummaryJson);
        Assert.AreEqual(currentRevision, Revision(await Read(3))); checks++;

        var changed = new ConditionalHolonPropertyWriteRequest(first.WriteRequest,
            new Dictionary<Guid, IReadOnlyList<string>> { [source.Id] = new[] { "/Name" } });
        var wrongMask = await Observe(() => ReadSelected(changed), first.WriteRequest.Write.OperationId);
        Assert.IsTrue(wrongMask.IsError); Assert.AreEqual("COSMIC_OPERATION_STATUS_UNVERIFIED", wrongMask.ErrorCode);
        Assert.IsNull(wrongMask.Result);
        var downgraded = await Observe(() => whole.ReadConditionalHolonWriteOutcomeAsync(first.WriteRequest), first.WriteRequest.Write.OperationId);
        Assert.IsTrue(downgraded.IsError); Assert.AreEqual("COSMIC_OPERATION_STATUS_UNVERIFIED", downgraded.ErrorCode);
        Assert.IsNull(downgraded.Result); Assert.AreEqual(currentRevision, Revision(await Read(3))); checks++;

        var otherActor = new ConditionalHolonWriteRequest(new ConditionalHolonWrite(first.WriteRequest.Write.OperationId,
            Guid.NewGuid(), authority, first.WriteRequest.Write.Conditions), ConditionalHolonOperation.Save,
            first.WriteRequest.Roots, false, false, 0);
        var unauthorizedIntent = new ConditionalHolonPropertyWriteRequest(otherActor,
            new Dictionary<Guid, IReadOnlyList<string>> { [source.Id] = new[] { "/Description" } });
        var priorReads = routeProbe?.OutcomeReads ?? 0;
        var denied = await Observe(() => ReadSelected(unauthorizedIntent), first.WriteRequest.Write.OperationId);
        Assert.IsTrue(denied.IsError);
        Assert.AreEqual(routedManager == null ? "COSMIC_OPERATION_STATUS_UNVERIFIED" : "COSMIC_OPERATION_STATUS_INTENT_INVALID", denied.ErrorCode);
        Assert.IsNull(denied.Result);
        Assert.IsTrue((await ReadSelected(null!)).IsError);
        if (routeProbe != null) Assert.AreEqual(priorReads, routeProbe.OutcomeReads);
        Assert.AreEqual(currentRevision, Revision(await Read(3)));
        Assert.AreEqual(baseline, Capture(source)); Assert.AreEqual(caller, Capture(proposed)); checks++;
        if (routedManager != null)
        {
            Assert.IsNotNull(routeProbe);
            var calls = routeProbe.OutcomeReads;
            var emptyActor = await routedManager.ReadConditionalHolonPropertyWriteOutcomeAsync(first, Guid.Empty);
            Assert.IsTrue(emptyActor.IsError); Assert.AreEqual("COSMIC_OPERATION_STATUS_INTENT_INVALID", emptyActor.ErrorCode);
            Assert.AreEqual(calls, routeProbe.OutcomeReads);
            Assert.AreEqual(currentRevision, Revision(await Read(3))); checks++;
        }
        Assert.AreEqual(routedManager == null ? 7 : 8, checks);
        return checks;
    }
}
