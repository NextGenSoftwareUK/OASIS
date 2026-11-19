using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
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
    public Task PropertySelectionUsesNativeBeforeImagesAndIntegrityBoundReceipts(string providerName)
        => RunPropertyNativeFixture(providerName, true);

    private static async Task<int> VerifySelectedNativeWrites(IOASISStorageProvider provider, HolonManager manager,
        ProviderType authority, Holon source, Guid actor, HashSet<Guid> operations, string work)
    {
        Assert.IsTrue(provider is IConditionalHolonPropertyStorageProvider);
        var storage = (IConditionalHolonPropertyStorageProvider)provider;
        var checks = 0;
        string Capture(IHolon value) => StoredRecordJson.CaptureRuntimeToken(value).ToString(Newtonsoft.Json.Formatting.None);
        Guid Revision(IHolon value)
        {
            var token = CosmicConcurrency.GetProviderRevisionId(value, authority);
            Assert.IsTrue(token.HasValue); return token.Value;
        }
        ConditionalHolonPropertyWriteRequest Request(Holon proposed, Guid revision, params string[] paths)
        {
            var write = new ConditionalHolonWrite(Guid.NewGuid(), actor, authority,
                new[] { new HolonWriteCondition(source.Id, revision) });
            var basis = new ConditionalHolonWriteRequest(write, ConditionalHolonOperation.Save,
                new[] { proposed }, false, false, 0);
            return new(basis, new Dictionary<Guid, IReadOnlyList<string>> { [source.Id] = paths });
        }
        async Task<OASISResult<IEnumerable<IHolon>>> Call(ConditionalHolonPropertyWriteRequest request, bool synchronous)
        {
            operations.Add(request.WriteRequest.Write.OperationId);
            await File.WriteAllTextAsync(Path.Combine(work, "selected-fixture.json"),
                System.Text.Json.JsonSerializer.Serialize(new { id = source.Id, actor, operations, stage = "before-selected-native-call" }));
            return synchronous ? storage.ExecuteConditionalHolonPropertyWrite(request)
                : await storage.ExecuteConditionalHolonPropertyWriteAsync(request);
        }
        async Task<Holon> Read(int version)
        {
            var loaded = await provider.LoadHolonAsync(source.Id, false, false, 0, true, false, 0);
            Assert.IsFalse(loaded.IsError, loaded.Message + loaded.Exception);
            Assert.AreEqual(version, loaded.Result.Version);
            Assert.AreEqual(actor, loaded.Result.CreatedByAvatarId);
            return StoredRecordJson.Project<Holon>(loaded.Result);
        }
        IHolon Acknowledgement(OASISResult<IEnumerable<IHolon>> result, int version)
        {
            Assert.IsFalse(result.IsError, result.Message + result.Exception);
            Assert.IsTrue(result.IsSaved);
            var value = result.Result.Single(); Assert.AreEqual(version, value.Version);
            Assert.IsTrue(result.MetaData?.ContainsKey(HolonCommittedDelta.ResultMetadataKey) == true);
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.MetaData[HolonCommittedDelta.ResultMetadataKey]));
            return value;
        }

        var initial = Capture(source);
        var proposed = StoredRecordJson.Project<Holon>(source);
        proposed.Name = "unselected local name"; proposed.Description = "selected description";
        proposed.MetaData["original"] = 5L; proposed.MetaData["ghost"] = "must not persist";
        proposed.Version = 9000; proposed.CreatedByAvatarId = Guid.NewGuid();
        var caller = Capture(proposed);
        var firstRequest = Request(proposed, Revision(source), "/Description");
        var first = await Call(firstRequest, false);
        var firstAck = Acknowledgement(first, 2);
        Assert.AreEqual("original", firstAck.Name); Assert.AreEqual("selected description", firstAck.Description);
        var stored = await Read(2);
        Assert.AreEqual("original", stored.Name); Assert.AreEqual("selected description", stored.Description);
        Assert.AreEqual("9007199254740993", StoredRecordJson.CaptureRuntimeToken(stored.MetaData["original"]).ToString(Newtonsoft.Json.Formatting.None));
        Assert.IsFalse(stored.MetaData.ContainsKey("ghost"));
        Assert.AreNotEqual(Revision(source), Revision(stored));
        Assert.AreEqual(caller, Capture(proposed)); checks++;

        var replay = await Call(firstRequest, true);
        Assert.AreEqual(Revision(firstAck), Revision(Acknowledgement(replay, 2)));
        Assert.AreEqual(first.MetaData[HolonCommittedDelta.ResultMetadataKey], replay.MetaData[HolonCommittedDelta.ResultMetadataKey]);
        Assert.AreEqual(Revision(stored), Revision(await Read(2))); checks++;

        var changedMask = new ConditionalHolonPropertyWriteRequest(firstRequest.WriteRequest,
            new Dictionary<Guid, IReadOnlyList<string>> { [source.Id] = new[] { "/Name" } });
        var reused = await Call(changedMask, false);
        Assert.IsTrue(reused.IsError); Assert.AreEqual("COSMIC_OPERATION_ID_REUSED", reused.ErrorCode);
        Assert.AreEqual(Revision(stored), Revision(await Read(2))); checks++;

        var stale = await Call(Request(proposed, Revision(source), "/Description"), true);
        Assert.IsTrue(stale.IsError); Assert.AreEqual(CosmicConcurrencyException.Code, stale.ErrorCode);
        Assert.AreEqual(Revision(stored), Revision(await Read(2))); checks++;

        var metadata = StoredRecordJson.Project<Holon>(stored);
        metadata.Name = "another unselected name";
        metadata.MetaData.Remove("nullable"); metadata.MetaData["added-null"] = null!;
        metadata.MetaData["branch"] = new Dictionary<string, object> { ["selected"] = 9L, ["sibling"] = 99L };
        var metadataBefore = Capture(metadata);
        var metadataRequest = Request(metadata, Revision(stored), "/MetaData/nullable", "/MetaData/added-null", "/MetaData/branch/selected");
        Acknowledgement(await Call(metadataRequest, true), 3);
        stored = await Read(3);
        Assert.AreEqual("original", stored.Name); Assert.AreEqual("selected description", stored.Description);
        Assert.IsFalse(stored.MetaData.ContainsKey("nullable")); Assert.IsTrue(stored.MetaData.ContainsKey("added-null"));
        Assert.IsNull(stored.MetaData["added-null"]);
        var branch = (JObject)StoredRecordJson.CaptureRuntimeToken(stored.MetaData["branch"]);
        Assert.AreEqual(9L, branch["selected"].Value<long>()); Assert.AreEqual(2L, branch["sibling"].Value<long>());
        Assert.AreEqual("9007199254740993", StoredRecordJson.CaptureRuntimeToken(stored.MetaData["original"]).ToString(Newtonsoft.Json.Formatting.None));
        Assert.AreEqual(Revision(stored), Revision(Acknowledgement(await Call(metadataRequest, false), 3)));
        Assert.AreEqual(Revision(stored), Revision(await Read(3)));
        Assert.AreEqual(metadataBefore, Capture(metadata)); checks++;

        var equal = Request(StoredRecordJson.Project<Holon>(stored), Revision(stored), "/Name");
        var beforeEqual = Revision(stored);
        Acknowledgement(await Call(equal, false), 4); stored = await Read(4);
        Assert.AreNotEqual(beforeEqual, Revision(stored)); Assert.AreEqual("original", stored.Name); checks++;

        var invalidField = await Call(Request(StoredRecordJson.Project<Holon>(stored), Revision(stored), "/Version"), true);
        Assert.IsTrue(invalidField.IsError); Assert.AreEqual(Revision(stored), Revision(await Read(4)));
        Assert.AreEqual(initial, Capture(source)); Assert.AreEqual(caller, Capture(proposed));
        Assert.AreEqual("\"original\"", HolonChangeTracking.GetPropertyValues(source, "/Name").Result.OriginalJson); checks++;

        var foreign = StoredRecordJson.Project<Holon>(stored); foreign.Description = "forbidden host write";
        var denied = await manager.SaveOwnedHolonAsync(foreign, Guid.NewGuid(), new CosmicWriteOptions(Guid.NewGuid()),
            saveChildren: false, providerType: authority);
        Assert.IsTrue(denied.IsError); Assert.AreEqual("COSMIC_WRITE_FORBIDDEN", denied.ErrorCode);
        Assert.AreEqual(Revision(stored), Revision(await Read(4))); checks++;
        Assert.AreEqual(8, checks);
        return checks;
    }
}
