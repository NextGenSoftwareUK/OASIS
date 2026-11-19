using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.CustomAttrbiutes;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Security;
using NextGenSoftware.OASIS.API.Core.Serialization;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public sealed partial class CqlV1Web4NativeTests
{
    public sealed class SelectedCustomOwner : Holon
    {
        [CustomOASISProperty] public long Score { get; set; }
        [CustomOASISProperty] public string? Note { get; set; }
        [CustomOASISProperty] public string? Other { get; set; }
        [CustomOASISProperty(StoreAsJsonString = true)] public int JsonRating { get; set; }
        [CustomOASISProperty(StoreAsJsonString = true)] public List<string> Tags { get; set; } = new();
    }

    private static async Task<int> VerifySelectedCustomNative(IOASISStorageProvider provider, HolonManager manager,
        OwnedPropertyBoundary probe, ProviderType authority, Guid actor, Guid id, bool protectedMetadata)
    {
        var checks = 0;
        var policy = protectedMetadata ? new HolonMetadataEncryptionPolicy("selected-custom", "fixture-key-v1",
            new RecoveryPreparationKeys()) : null;
        if (policy != null) { manager.MetadataEncryptionPolicy = policy; manager.PreparationEncryptionPolicy = policy; }
        var root = new SelectedCustomOwner { Id = id, Name = "custom native", Score = 9007199254740993L,
            Note = "source note", Other = "source sibling", JsonRating = 7, Tags = new() { "source" }, IsActive = true,
            MetadataEncryptionOverride = policy };
        Guid Revision(IHolon value) => CosmicConcurrency.GetProviderRevisionId(value, authority)!.Value;
        async Task<(Holon Raw, SelectedCustomOwner Typed)> Read(int version)
        {
            var loaded = await provider.LoadHolonAsync(id, false, false, 0, true, false, 0);
            Assert.IsFalse(loaded.IsError, loaded.Message + loaded.Exception); Assert.IsNotNull(loaded.Result);
            var raw = StoredRecordJson.Project<Holon>(loaded.Result); Assert.AreEqual(version, raw.Version);
            if (protectedMetadata)
            {
                Assert.IsTrue(raw.MetaData.ContainsKey("__oasis_enc__"));
                Assert.IsFalse(raw.MetaData.ContainsKey(nameof(SelectedCustomOwner.Score)));
                Assert.IsFalse(raw.MetaData.ContainsKey(nameof(SelectedCustomOwner.Other)));
                raw.MetadataEncryptionOverride = policy;
                var decrypted = manager.DecryptHolonMetaData(raw);
                Assert.IsFalse(decrypted.IsError, decrypted.Message + decrypted.Exception);
            }
            else Assert.IsFalse(raw.MetaData.ContainsKey("__oasis_enc__"));
            var typed = StoredRecordJson.Project<SelectedCustomOwner>(raw);
            typed.MetadataEncryptionOverride = policy;
            manager.MapMetaData<SelectedCustomOwner>(typed);
            return (raw, typed);
        }
        void Preserved(SelectedCustomOwner native)
        {
            Assert.AreEqual("custom native", native.Name); Assert.AreEqual("source sibling", native.Other);
            Assert.AreEqual("excluded local sibling", root.Other);
            Assert.AreEqual("\"source sibling\"", HolonChangeTracking.GetPropertyValues(root, "/Other").Result.OriginalJson);
            Assert.IsTrue(HolonChangeTracking.GetPropertyValues(root, "/Other").Result.IsModified);
        }
        async Task Save(string path, CosmicWriteOptions options)
        {
            probe.Source = root;
            var saved = await manager.SaveOwnedHolonAsync(root, actor, options, new[] { path },
                saveChildren: false, providerType: authority);
            Assert.IsFalse(saved.IsError, saved.Message + saved.Exception); Assert.IsTrue(saved.IsSaved);
            var inspected = HolonChangeTracking.GetPropertyValues(root, path);
            Assert.IsFalse(inspected.IsError, inspected.ErrorCode + ": " + inspected.Message + inspected.Exception);
            Assert.IsNotNull(inspected.Result);
            Assert.IsFalse(inspected.Result.IsModified);
        }

        probe.Source = root;
        var created = await manager.SaveOwnedHolonAsync(root, actor, new CosmicWriteOptions(Guid.NewGuid()),
            saveChildren: false, providerType: authority);
        Assert.IsFalse(created.IsError, created.Message + created.Exception); Assert.IsTrue(created.IsSaved);
        var initial = await Read(1);
        Assert.AreEqual(9007199254740993L, initial.Typed.Score); Assert.AreEqual("source note", initial.Typed.Note);
        Assert.AreEqual(7, initial.Typed.JsonRating); CollectionAssert.AreEqual(new[] { "source" }, initial.Typed.Tags);
        var initialRevision = Revision(root); var replay = initial.Typed;
        HolonChangeTracking.AcceptChanges(replay, false); checks++;

        root.Other = "excluded local sibling";
        var set = HolonChangeTracking.SetCurrentPropertyValue(root, "/Score", 9007199254740995L);
        Assert.IsFalse(set.IsError, set.Message + set.Exception);
        Assert.IsFalse(HolonChangeTracking.SetPropertyModified(root, "/Score").IsError);
        var scoreOptions = new CosmicWriteOptions(Guid.NewGuid(), new Dictionary<Guid, Guid?> { [id] = initialRevision });
        await Save("/Score", scoreOptions);
        var second = await Read(2); Assert.AreEqual(9007199254740995L, second.Typed.Score);
        Assert.AreEqual("9007199254740995", StoredRecordJson.CaptureRuntimeToken(second.Raw.MetaData[nameof(SelectedCustomOwner.Score)]).ToString(Formatting.None));
        Preserved(second.Typed); checks++;

        root.JsonRating = 37;
        await Save("/JsonRating", new CosmicWriteOptions(Guid.NewGuid()));
        var third = await Read(3); Assert.AreEqual(37, third.Typed.JsonRating); Assert.AreEqual(9007199254740995L, third.Typed.Score);
        var encodedRating = StoredRecordJson.CaptureRuntimeToken(third.Raw.MetaData[nameof(SelectedCustomOwner.JsonRating)]);
        Assert.AreEqual(JTokenType.String, encodedRating.Type); Assert.AreEqual("37", encodedRating.Value<string>());
        Preserved(third.Typed); checks++;

        root.Note = null;
        await Save("/Note", new CosmicWriteOptions(Guid.NewGuid()));
        var fourth = await Read(4); Assert.IsNull(fourth.Typed.Note); Assert.AreEqual(37, fourth.Typed.JsonRating);
        Preserved(fourth.Typed); checks++;

        var tags = new[] { "unicode \u03bb", "embedded\0nul" };
        set = HolonChangeTracking.SetCurrentPropertyValue(root, "/Tags", tags.ToList());
        Assert.IsFalse(set.IsError, set.Message + set.Exception);
        await Save("/Tags", new CosmicWriteOptions(Guid.NewGuid()));
        var fifth = await Read(5); CollectionAssert.AreEqual(tags, fifth.Typed.Tags);
        var encodedTags = StoredRecordJson.CaptureRuntimeToken(fifth.Raw.MetaData[nameof(SelectedCustomOwner.Tags)]);
        Assert.AreEqual(JTokenType.String, encodedTags.Type);
        CollectionAssert.AreEqual(tags, StoredRecordJson.Deserialize<List<string>>(encodedTags.Value<string>()));
        Preserved(fifth.Typed); checks++;

        replay.Score = 9007199254740995L; replay.Other = "excluded replay sibling"; probe.Source = replay;
        var replayed = await manager.SaveOwnedHolonAsync(replay, actor, scoreOptions, new[] { "/Score" },
            saveChildren: false, providerType: authority);
        Assert.IsFalse(replayed.IsError, replayed.Message + replayed.Exception); Assert.AreEqual(2, replay.Version);
        Assert.AreEqual("excluded replay sibling", replay.Other);
        var afterReplay = await Read(5); CollectionAssert.AreEqual(tags, afterReplay.Typed.Tags);
        Assert.IsNull(afterReplay.Typed.Note); Assert.AreEqual(37, afterReplay.Typed.JsonRating);
        Assert.AreEqual(9007199254740995L, afterReplay.Typed.Score); checks++;

        var stale = afterReplay.Typed; stale.Score = 9007199254740997L; probe.Source = stale;
        var beforeStale = StoredRecordJson.SerializeRuntime(stale);
        var rejected = await manager.SaveOwnedHolonAsync(stale, actor,
            new CosmicWriteOptions(Guid.NewGuid(), new Dictionary<Guid, Guid?> { [id] = initialRevision }),
            new[] { "/Score" }, saveChildren: false, providerType: authority);
        Assert.IsTrue(rejected.IsError); Assert.AreEqual(CosmicConcurrencyException.Code, rejected.ErrorCode);
        Assert.AreEqual(beforeStale, StoredRecordJson.SerializeRuntime(stale));
        var unchanged = await Read(5); Assert.AreEqual(9007199254740995L, unchanged.Typed.Score);
        CollectionAssert.AreEqual(tags, unchanged.Typed.Tags);
        Assert.AreEqual(1, probe.MutationIntents.Count, "Only initial creation may use the whole-record path.");
        Assert.AreEqual(6, probe.PropertyAsyncWrites); Assert.AreEqual(4, probe.SelectedCommittedOperations.Count);
        checks++; return checks;
    }
}
