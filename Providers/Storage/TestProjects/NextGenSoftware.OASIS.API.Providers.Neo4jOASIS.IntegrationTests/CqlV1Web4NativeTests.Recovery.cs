using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public sealed partial class CqlV1Web4NativeTests
{
    private static async Task<int> VerifyConditionalRecoveryAsync(
        Dictionary<ProviderType, IOASISStorageProvider> providers, Guid actor, Guid[] ids, string tag,
        Func<string, object?, Task> journal, List<string> conditionalOperations)
    {
        var verified = 0;
        foreach (var pair in providers)
        {
            var recovery = pair.Value as IConditionalHolonWriteRecoveryStorageProvider;
            Assert.IsNotNull(recovery, "The selected authority must declare read-only recovery.");
            var writer = (IConditionalHolonStorageProvider)pair.Value;
            var manager = new Sources(providers).CreateManager(pair.Value);
            var originals = new List<ConditionalHolonWriteRequest>();
            var acknowledged = new List<IHolon>();
            async Task<IHolon> Execute(ConditionalHolonWriteRequest request)
            {
                conditionalOperations.Add(request.Write.OperationId.ToString("D"));
                await journal("before-recovery-original", new { provider = pair.Key.ToString(), request });
                var result = await writer.ExecuteConditionalHolonWriteAsync(request);
                Assert.IsFalse(result.IsError, result.Message + result.Exception);
                originals.Add(request);
                // No second call to execution is made to recover this response.
                // The following lookup must work even after current state changes.
                return result.Result.Single();
            }
            for (var i = 0; i < ids.Length; i++)
            {
                var source = new Holon { Id = ids[i], Name = "private recovery fixture " + i,
                    CreatedByAvatarId = actor, IsActive = true };
                source.MetaData["fixture"] = tag;
                var original = new ConditionalHolonWriteRequest(new ConditionalHolonWrite(Guid.NewGuid(), actor, pair.Key,
                    new[] { new HolonWriteCondition(source.Id, null) }), ConditionalHolonOperation.Save,
                    new[] { source }, saveChildren: false);
                var absent = await recovery.ReadConditionalHolonWriteOutcomeAsync(original);
                Assert.IsFalse(absent.IsError, absent.Message + absent.Exception);
                Assert.AreEqual(ConditionalHolonReceiptState.Absent, absent.Result.ReceiptState);
                var routedAbsent = await manager.ReadConditionalHolonWriteOutcomeAsync(original, actor);
                Assert.IsFalse(routedAbsent.IsError, routedAbsent.Message + routedAbsent.Exception);
                Assert.AreEqual(ConditionalHolonReceiptState.Absent, routedAbsent.Result.ReceiptState);
                acknowledged.Add(await Execute(original));
            }
            for (var i = 1; i < ids.Length; i++)
            {
                var record = acknowledged[i];
                var original = new ConditionalHolonWriteRequest(new ConditionalHolonWrite(Guid.NewGuid(), actor, pair.Key,
                    new[] { new HolonWriteCondition(record.Id, CosmicConcurrency.GetProviderRevisionId(record, pair.Key)) }),
                    i == 1 ? ConditionalHolonOperation.SoftDelete : ConditionalHolonOperation.HardDelete,
                    new[] { record }, saveChildren: false, recursive: false);
                _ = await Execute(original);
            }
            await journal("before-recovery-provider-restart", new { provider = pair.Key.ToString(), actor, ids,
                operations = originals.Select(value => value.Write.OperationId).ToArray() });
            Assert.IsFalse((await pair.Value.DeActivateProviderAsync()).IsError);
            Assert.IsFalse((await pair.Value.ActivateProviderAsync()).IsError);
            foreach (var original in originals)
            {
                var first = await recovery.ReadConditionalHolonWriteOutcomeAsync(original);
                Assert.IsFalse(first.IsError, first.Message + first.Exception);
                Assert.AreEqual(ConditionalHolonReceiptState.Committed, first.Result.ReceiptState);
                Assert.AreEqual(original.Write.OperationId, first.Result.OperationId);
                Assert.AreEqual(actor, first.Result.ActorId);
                Assert.AreEqual(pair.Key, first.Result.Authority);
                Assert.AreEqual(original.Operation, first.Result.Operation);
                Assert.IsFalse(string.IsNullOrWhiteSpace(first.Result.CommittedDeltaSummaryJson));
                Assert.IsFalse(JsonSerializer.Serialize(first.Result).Contains("private recovery fixture", StringComparison.Ordinal));
                var second = await manager.ReadConditionalHolonWriteOutcomeAsync(original, actor);
                Assert.IsFalse(second.IsError, second.Message + second.Exception);
                Assert.AreEqual(JsonSerializer.Serialize(first.Result), JsonSerializer.Serialize(second.Result));
                var foreign = new ConditionalHolonWriteRequest(new ConditionalHolonWrite(original.Write.OperationId,
                    Guid.NewGuid(), pair.Key, original.Write.Conditions), original.Operation, original.Roots,
                    original.SaveChildren, original.Recursive, original.MaxChildDepth);
                var rejected = await recovery.ReadConditionalHolonWriteOutcomeAsync(foreign);
                Assert.IsTrue(rejected.IsError);
                Assert.IsNull(rejected.Result);
            }
            var retained = await pair.Value.LoadHolonAsync(ids[0], false, true, 0, true, false, 0);
            Assert.IsFalse(retained.IsError, retained.Message);
            Assert.AreEqual(CosmicConcurrency.GetProviderRevisionId(acknowledged[0], pair.Key),
                CosmicConcurrency.GetProviderRevisionId(retained.Result, pair.Key));
            var soft = await pair.Value.LoadHolonAsync(ids[1], false, true, 0, true, false, 0);
            Assert.IsTrue(soft.Result.IsDeleted);
            var hard = await pair.Value.LoadHolonAsync(ids[2], false, true, 0, true, false, 0);
            Assert.IsNull(hard.Result);
            await journal("recovery-provider-complete", new { provider = pair.Key.ToString(), originals = originals.Count });
            verified++;
        }
        return verified;
    }
}
