using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Security;
using NextGenSoftware.OASIS.API.Core.Serialization;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public sealed partial class CqlV1Web4NativeTests
{
    // Owned fixture key material only; never used by a deployed host.
    private sealed class RecoveryPreparationKeys : IHolonMetadataKeyResolver
    {
        public OASISResult<HolonMetadataKeyMaterial> ResolveKey(HolonMetadataKeyRequest request)
            => new(new HolonMetadataKeyMaterial(Enumerable.Repeat((byte)83, 32).ToArray()));
    }
    private static async Task<int> VerifyConditionalRecoveryAsync(
        Dictionary<ProviderType, IOASISStorageProvider> providers, Guid actor, Guid[] ids, string tag, string sqlitePath,
        Func<string, object?, Task> journal, List<string> conditionalOperations)
    {
        var verified = 0;
        foreach (var pair in providers)
        {
            var recovery = pair.Value as IConditionalHolonWriteRecoveryStorageProvider;
            Assert.IsNotNull(recovery, "The selected authority must declare read-only recovery.");
            var writer = (IConditionalHolonStorageProvider)pair.Value;
            var preparations = pair.Value as IHolonWritePreparationRecoveryStorageProvider;
            Assert.IsNotNull(preparations, "The selected authority must expose read-only preparation retrieval.");
            var storedPreparations = new Dictionary<Guid, HolonWritePreparation>();
            var manager = new Sources(providers).CreateManager(pair.Value);
            var protection = new HolonMetadataEncryptionPolicy("owned-recovery-fixture", "fixture-key-v1", new RecoveryPreparationKeys());
            var originals = new List<ConditionalHolonWriteRequest>();
            var acknowledged = new List<IHolon>();
            async Task<IHolon?> Execute(ConditionalHolonWriteRequest request)
            {
                conditionalOperations.Add(request.Write.OperationId.ToString("D"));
                await journal("before-recovery-original", new { provider = pair.Key.ToString(), request });
                // Storage conformance uses opaque test envelopes. Cryptographic
                // authentication and stored-only reconstruction are Core tests.
                var absentPreparation = await preparations.ReadHolonWritePreparationAsync(request.Write.OperationId, actor);
                Assert.IsFalse(absentPreparation.IsError, absentPreparation.Message + absentPreparation.Exception);
                Assert.IsNull(absentPreparation.Result);
                var routedAbsentPreparation = await manager.ReadHolonWritePreparationAsync(request.Write.OperationId, actor, pair.Key);
                Assert.IsFalse(routedAbsentPreparation.IsError, routedAbsentPreparation.Message + routedAbsentPreparation.Exception);
                Assert.IsNull(routedAbsentPreparation.Result);
                if (request.Operation != ConditionalHolonOperation.SoftDelete)
                {
                var preparation = new HolonWritePreparation(request.Write.OperationId, actor, pair.Key,
                    request.Roots.Select(root => new ProtectedHolonPreparation(root.Id,
                        "oasis-preparation1:owned-status-" + request.Write.OperationId.ToString("D"))).ToArray());
                var claimed = await preparations.ClaimHolonWritePreparationAsync(preparation);
                Assert.IsFalse(claimed.IsError, claimed.Message + claimed.Exception);
                storedPreparations.Add(request.Write.OperationId, preparation);
                }
                if (request.Operation == ConditionalHolonOperation.SoftDelete)
                {
                    // Exercise the public manager's real protected enrollment,
                    // preserving the untouched original request for comparison.
                    manager.PreparationEncryptionPolicy = protection;
                    var detached = StoredRecordJson.Deserialize<IHolon>(StoredRecordJson.Serialize(request.Roots.Single()));
                    var options = new CosmicWriteOptions(request.Write.OperationId,
                        request.Write.Conditions.ToDictionary(value => value.HolonId, value => value.ExpectedRevisionId));
                    var deleted = await manager.DeleteHolonAsync(detached, actor, options, true, pair.Key);
                    Assert.IsFalse(deleted.IsError, deleted.Message + deleted.Exception);
                    Assert.IsTrue(deleted.IsDeleted);
                    var stored = await preparations.ReadHolonWritePreparationAsync(request.Write.OperationId, actor);
                    Assert.IsFalse(stored.IsError, stored.Message + stored.Exception);
                    Assert.IsNotNull(stored.Result);
                    Assert.IsFalse(stored.Result.Records.Single().Envelope.Contains("private recovery fixture", StringComparison.Ordinal));
                    storedPreparations.Add(request.Write.OperationId, stored.Result);
                    originals.Add(request);
                    return deleted.Result;
                }
                if (request.Operation == ConditionalHolonOperation.HardDelete)
                {
                    var uncertain = await ExecuteWithLostCommitAcknowledgement(pair.Key, pair.Value, request, sqlitePath);
                    Assert.AreEqual(1, uncertain.Commits, "Exactly one real commit preceded acknowledgement loss.");
                    Assert.IsTrue(uncertain.Result.IsError);
                    Assert.IsFalse(uncertain.Result.IsDeleted);
                    Assert.AreEqual(request.Write.OperationId.ToString("D"), uncertain.Result.MetaData["COSMIC.OperationId"]);
                    Assert.AreEqual("COSMIC_WRITE_RECONCILIATION_REQUIRED", uncertain.Result.ErrorCode);
                    Assert.IsTrue(NextGenSoftware.OASIS.API.Core.Helpers.NativeMutationReconciliation.IsRequired(uncertain.Result.Exception));
                    Assert.IsFalse(request.Roots.Single().IsDeleted, "An uncertain acknowledgement must not publish deletion into the caller.");
                    originals.Add(request);
                    await journal("recovery-acknowledgement-lost", new { provider = pair.Key.ToString(),
                        operation = request.Write.OperationId, commits = uncertain.Commits, code = uncertain.Result.ErrorCode });
                    return null;
                }
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
                var created = await Execute(original);
                Assert.IsNotNull(created);
                acknowledged.Add(created);
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
                var stored = await preparations.ReadHolonWritePreparationAsync(original.Write.OperationId, actor);
                Assert.IsFalse(stored.IsError, stored.Message + stored.Exception);
                Assert.IsNotNull(stored.Result);
                Assert.AreEqual(JsonSerializer.Serialize(storedPreparations[original.Write.OperationId]), JsonSerializer.Serialize(stored.Result));
                var routedPreparation = await manager.ReadHolonWritePreparationAsync(original.Write.OperationId, actor, pair.Key);
                Assert.IsFalse(routedPreparation.IsError, routedPreparation.Message + routedPreparation.Exception);
                Assert.AreEqual(JsonSerializer.Serialize(stored.Result), JsonSerializer.Serialize(routedPreparation.Result));
                if (original.Operation == ConditionalHolonOperation.SoftDelete)
                {
                    var storedStatus = await manager.RecoverConditionalHolonWriteOutcomeAsync(original.Write.OperationId, actor, pair.Key,
                        _ => protection, (name, assembly) => name == typeof(Holon).FullName
                            && assembly == typeof(Holon).Assembly.GetName().Name ? typeof(Holon) : null!);
                    Assert.IsFalse(storedStatus.IsError, storedStatus.Message + storedStatus.Exception);
                    Assert.AreEqual(ConditionalHolonReceiptState.Committed, storedStatus.Result.ReceiptState);
                    Assert.AreEqual(ConditionalHolonOperation.SoftDelete, storedStatus.Result.Operation);
                }
                var foreignPreparation = await preparations.ReadHolonWritePreparationAsync(original.Write.OperationId, Guid.NewGuid());
                Assert.IsTrue(foreignPreparation.IsError);
                Assert.AreEqual("COSMIC_PREPARATION_ACTOR_MISMATCH", foreignPreparation.ErrorCode);
                Assert.IsNull(foreignPreparation.Result);
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
