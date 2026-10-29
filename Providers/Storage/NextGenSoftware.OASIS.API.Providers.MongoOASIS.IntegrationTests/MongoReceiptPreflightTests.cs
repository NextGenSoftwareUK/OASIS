using System;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Serialization;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using Xunit;
using Provider = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS;

namespace NextGenSoftware.OASIS.API.Providers.MongoOASIS.IntegrationTests;

public partial class MongoStorageIdentityTests
{
    [Fact]
    public async Task NativeReceiptPreflightClassifiesMissingCurrentLaterAndHardDeletedReplicaWithoutMutation()
    {
        await RunOwned(async (client, source, _) =>
        {
            var target = new Provider(Uri, Second);
            try
            {
                Assert.False((await target.ActivateProviderAsync()).IsError);
                var sourceBinding = await source.CaptureStorageIdentityAsync(); Assert.False(sourceBinding.IsError);
                var targetBinding = await target.CaptureStorageIdentityAsync(); Assert.False(targetBinding.IsError);
                var actor = Guid.NewGuid();
                var enrollment = new HolonReplicationEnrollment("owned-preflight-lineage", new[] { "target", "target-b", "\uE000", "\U0001F600" });
                var input = new Holon { Id = Guid.NewGuid(), Name = "original-preflight-value" };
                ConditionalHolonWriteRequest Original(IHolon row, Guid? expected, ConditionalHolonOperation operation = ConditionalHolonOperation.Save)
                    => new(new ConditionalHolonWrite(Guid.NewGuid(), actor, ProviderType.MongoDBOASIS,
                        new[] { new HolonWriteCondition(row.Id, expected) }, enrollment), operation, new[] { row }, saveChildren: false);
                ConditionalHolonReceiptEvidenceRequest Evidence(ConditionalHolonWriteRequest original)
                    => new(original, sourceBinding.Result, targetBinding.Result, new[] { input.Id });
                async Task<HolonReplicationReceiptPreflightReport> Observe(ConditionalHolonWriteRequest original)
                {
                    var observed = await HolonReplicationReceiptPreflight.ReadAsync(source, target, Evidence(original));
                    Assert.False(observed.IsError, observed.Message + observed.Exception);
                    Assert.False(observed.IsSaved); Assert.False(observed.IsDeleted);
                    return observed.Result;
                }
                async Task Apply(ConditionalHolonWriteRequest original, IHolon acknowledged)
                {
                    var result = await target.ApplyBoundCommittedHolonReplicaAsync(new CommittedHolonReplicaRequest(
                        original.Write.OperationId, enrollment.SourceId, ProviderType.MongoDBOASIS, actor, original.Operation,
                        new[] { acknowledged }, saveChildren: false), targetBinding.Result);
                    Assert.False(result.IsError, result.Message + result.Exception);
                }
                async Task<string> Snapshot()
                {
                    var parts = new System.Collections.Generic.List<string>();
                    foreach (var name in new[] { First, Second })
                        foreach (var collection in new[] { "Holon", "CosmicReplicaHead", "CosmicWriteReceipt", "CosmicHolonOutbox" })
                            parts.Add(string.Join("|", (await client.GetDatabase(name).GetCollection<BsonDocument>(collection)
                                .Find(FilterDefinition<BsonDocument>.Empty).Sort(new BsonDocument("_id", 1)).ToListAsync()).Select(r => r.ToJson())));
                    return string.Join("\n", parts);
                }
                var create = Original(input, null);
                var saved = await source.ExecuteReplicatingHolonWriteAsync(create, enrollment);
                Assert.False(saved.IsError, saved.Message + saved.Exception);
                var first = saved.Result.Single();
                var missing = await Observe(create);
                Assert.Equal(HolonReplicationReceiptComparisonKind.MissingReplicaHead, missing.Records[0].Kind);
                await Apply(create, first);
                var boundSource = source.GetBoundHolonReplicationOutbox(sourceBinding.Result);
                Assert.False(boundSource.IsError, boundSource.Message + boundSource.Exception);
                await CosmicNativeAcceptance.CosmicOperationInventoryProbe.CompleteAlreadyAppliedTargetAsync(
                    boundSource.Result, sourceBinding.Result, create.Write.OperationId, "target");
                await CosmicNativeAcceptance.CosmicOperationInventoryProbe.VerifyAsync(boundSource.Result, sourceBinding.Result,
                    create.Write.OperationId, enrollment.SourceId, actor, enrollment.TargetIds, requireCompleted: true);

                var before = await Snapshot();
                var matched = await Observe(create);
                Assert.Equal(HolonReplicationReceiptComparisonKind.MatchesOriginalReceipt, matched.Records[0].Kind);
                Assert.True(matched.IsCompleteOriginalGraph);
                Assert.Equal(matched.ObservationDigest, (await Observe(create)).ObservationDigest);
                Assert.Equal(before, await Snapshot());
                var edit = StoredRecordJson.Project<Holon>(first); edit.Name = "later-preflight-value";
                var update = Original(edit, CosmicConcurrency.GetProviderRevisionId(first, ProviderType.MongoDBOASIS));
                var updated = await source.ExecuteReplicatingHolonWriteAsync(update, enrollment);
                Assert.False(updated.IsError, updated.Message + updated.Exception);
                var second = updated.Result.Single(); await Apply(update, second);
                await CosmicNativeAcceptance.CosmicOperationInventoryProbe.VerifyAsync(boundSource.Result, sourceBinding.Result,
                    create.Write.OperationId, enrollment.SourceId, actor, enrollment.TargetIds, requireCompleted: true);

                before = await Snapshot();
                var stale = await Observe(create);
                Assert.Equal(HolonReplicationReceiptComparisonKind.LogicalVersionConflict, stale.Records[0].Kind);
                Assert.NotEqual(matched.ObservationDigest, stale.ObservationDigest);
                Assert.Equal(HolonReplicationReceiptComparisonKind.MatchesOriginalReceipt, (await Observe(update)).Records[0].Kind);
                Assert.Equal(before, await Snapshot());
                var deletion = Original(StoredRecordJson.Project<Holon>(second),
                    CosmicConcurrency.GetProviderRevisionId(second, ProviderType.MongoDBOASIS), ConditionalHolonOperation.HardDelete);
                var deleted = await source.ExecuteReplicatingHolonWriteAsync(deletion, enrollment);
                Assert.False(deleted.IsError, deleted.Message + deleted.Exception);
                await Apply(deletion, deleted.Result.Single());
                before = await Snapshot();
                var tombstone = await Observe(deletion);
                Assert.Equal(HolonReplicationReceiptComparisonKind.MatchesOriginalReceipt, tombstone.Records[0].Kind);
                Assert.True(tombstone.Records[0].Original.HardDeleted);
                Assert.False(tombstone.Records[0].Target.HasCurrentRecord);
                Assert.Equal(before, await Snapshot());
            }
            finally { Assert.False((await target.DeActivateProviderAsync()).IsError); }
        });
    }
}
