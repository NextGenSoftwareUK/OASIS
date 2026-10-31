using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using Xunit;
using Provider = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS;

namespace NextGenSoftware.OASIS.API.Providers.MongoOASIS.IntegrationTests;

public sealed partial class CosmicConditionalWriteTests
{
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task NativeCommittedDeltaCapturesActualBsonAndReplaysAfterDeletion(bool history)
    {
        // Capability admission precedes all native fixture creation. The original
        // producer red gate can fail here without leaving synthetic mutations.
        Assert.True(typeof(IConditionalHolonDeltaStorageProvider).IsAssignableFrom(typeof(Provider)), "MongoDB must advertise atomic conditional delta support.");
        const string uri = "mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        Assert.Equal(uri, Environment.GetEnvironmentVariable("OASIS_MONGO_REPLICA_SET_CONNECTION"));
        var workspace = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../.."));
        using var lease = File.Open(Path.Combine(workspace, "TestResults/SubscriptionUsageCurrent/cosmic-delta-environment.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        var name = history ? "cosmic_delta_values_fixture" : "cosmic_delta_summary_fixture";
        var client = new MongoClient(uri); using var names = await client.ListDatabaseNamesAsync();
        Assert.DoesNotContain(name, await names.ToListAsync());
        var database = client.GetDatabase(name); var marker = database.GetCollection<BsonDocument>("CosmicOwnedFixture");
        var root = new Holon { Id = Guid.NewGuid(), Name = "owned delta root", MetaData = new() {
            ["amount"] = 1.000000000000000000000000001m, ["remove"] = "owned prior private value", ["array"] = new[] { 1, 2 } } };
        var child = new Holon { Id = Guid.NewGuid(), Name = "owned delta retained child", MetaData = new() { ["owner"] = "retained" } }; root.Children.Add(child);
        var actor = Guid.NewGuid(); var creation = Guid.NewGuid();
        await marker.InsertOneAsync(new BsonDocument { { "_id", "owned committed delta v1" }, { "root", root.Id.ToString("D") },
            { "child", child.Id.ToString("D") }, { "actor", actor.ToString("D") }, { "creation", creation.ToString("D") }, { "history", history } });
        var provider = new Provider(uri, name) { IsVersionControlEnabled = history }; var complete = false;
        var rows = database.GetCollection<BsonDocument>("Holon"); var receipts = database.GetCollection<BsonDocument>("CosmicWriteReceipt");
        Guid Revision(IHolon row) => CosmicConcurrency.GetProviderRevisionId(row, ProviderType.MongoDBOASIS)!.Value;
        ConditionalHolonWriteRequest Request(IHolon value, Guid expected, ConditionalHolonOperation operation = ConditionalHolonOperation.Save)
            => new(new ConditionalHolonWrite(Guid.NewGuid(), actor, ProviderType.MongoDBOASIS, new[] { new HolonWriteCondition(value.Id, expected) }), operation, new[] { value }, saveChildren: false);
        string Summary(NextGenSoftware.OASIS.Common.OASISResult<IEnumerable<IHolon>> saved)
        { Assert.False(saved.IsError, saved.Message + saved.Exception); Assert.True(saved.MetaData.ContainsKey(HolonCommittedDelta.ResultMetadataKey)); return saved.MetaData[HolonCommittedDelta.ResultMetadataKey]; }
        async Task<byte[]> Native(Guid id) => (await rows.Find(new BsonDocument("HolonId", id.ToString("D"))).SingleAsync()).ToBson();
        try
        {
            Assert.False((await provider.ActivateProviderAsync()).IsError);
            var create = await provider.ExecuteConditionalHolonWriteAsync(new ConditionalHolonWriteRequest(new ConditionalHolonWrite(creation, actor, ProviderType.MongoDBOASIS,
                new[] { new HolonWriteCondition(root.Id, null), new HolonWriteCondition(child.Id, null) }), ConditionalHolonOperation.Save, new[] { root }));
            Assert.Equal(2, ((JArray)JObject.Parse(Summary(create))["records"]!).Count);
            var input = create.Result.Single(); var firstRevision = Revision(input); var childBytes = await Native(child.Id);
            input.Name = "owned committed delta edit"; input.MetaData.Remove("remove"); input.MetaData["addedNull"] = null;
            input.MetaData["amount"] = 1.000000000000000000000000002m; input.MetaData["array"] = new[] { 2, 1 };
            var edit = Request(input, firstRevision); var saved = await provider.ExecuteConditionalHolonWriteAsync(edit); var originalSummary = Summary(saved);
            var record = JObject.Parse(originalSummary)["records"]!.Single();
            Assert.Equal("Updated", record.Value<string>("kind")); Assert.Equal(firstRevision.ToString("D"), record.Value<string>("previousRevision"));
            var changes = ((JArray)record["changes"]!).ToDictionary(change => change.Value<string>("path")!);
            Assert.Equal("Modified", changes["/MetaData/amount"].Value<string>("change"));
            Assert.Equal("Modified", changes["/MetaData/array/$values"].Value<string>("change"));
            Assert.Equal("Removed", changes["/MetaData/remove"].Value<string>("change")); Assert.Equal("Added", changes["/MetaData/addedNull"].Value<string>("change"));
            Assert.All(changes.Values, change => { Assert.Null(change["previousJson"]); Assert.Null(change["currentJson"]); });
            Assert.DoesNotContain("owned prior private value", originalSummary); Assert.Equal(childBytes, await Native(child.Id));
            var receipt = await receipts.Find(new BsonDocument("_id", edit.Write.OperationId.ToString("D"))).SingleAsync();
            var packet = HolonCommittedDelta.ReadStored((string)JObject.Parse(receipt["ResultJson"].AsString)["delta"]!);
            Assert.Equal(history, packet.ValuesRetained); Assert.Equal(history, packet.StoredJson.Contains("owned prior private value")); Assert.Equal(originalSummary, packet.SummaryJson);
            // Native rejection after the root write aborts the graph and its already-captured delta.
            await database.RunCommandAsync<BsonDocument>(new BsonDocument { { "collMod", "Holon" }, { "validator", new BsonDocument("Name", new BsonDocument("$ne", "reject owned delta child")) } });
            var bad = (await provider.LoadHolonAsync(root.Id)).Result; var rootBytes = await Native(root.Id);
            bad.Name = "must roll back delta"; bad.Children.Single().Name = "reject owned delta child";
            var rejected = await provider.ExecuteConditionalHolonWriteAsync(new ConditionalHolonWriteRequest(new ConditionalHolonWrite(Guid.NewGuid(), actor, ProviderType.MongoDBOASIS,
                new[] { new HolonWriteCondition(root.Id, Revision(saved.Result.Single())), new HolonWriteCondition(child.Id, Revision(create.Result.Single().Children.Single())) }), ConditionalHolonOperation.Save, new[] { bad }));
            Assert.True(rejected.IsError); Assert.False(rejected.MetaData.ContainsKey(HolonCommittedDelta.ResultMetadataKey)); Assert.Equal(rootBytes, await Native(root.Id)); Assert.Equal(childBytes, await Native(child.Id));
            var soft = await provider.ExecuteConditionalHolonWriteAsync(Request(saved.Result.Single(), Revision(saved.Result.Single()), ConditionalHolonOperation.SoftDelete));
            Assert.Equal("SoftDeleted", JObject.Parse(Summary(soft))["records"]!.Single().Value<string>("kind"));
            var hard = await provider.ExecuteConditionalHolonWriteAsync(Request(soft.Result.Single(), Revision(soft.Result.Single()), ConditionalHolonOperation.HardDelete));
            var deletion = JObject.Parse(Summary(hard))["records"]!.Single(); Assert.Equal("HardDeleted", deletion.Value<string>("kind")); Assert.False(deletion.Value<bool>("currentExists"));
            provider.IsVersionControlEnabled = !history;
            var replay = await provider.ExecuteConditionalHolonWriteAsync(edit); Assert.Equal(originalSummary, Summary(replay)); Assert.Equal("owned committed delta edit", replay.Result.Single().Name);
            Assert.Equal(1, await rows.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty)); Assert.Equal(4, await receipts.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
            Assert.Equal(childBytes, await Native(child.Id)); complete = true;
        }
        finally
        {
            await provider.DeActivateProviderAsync();
            if (complete) await client.DropDatabaseAsync(name);
            else output.WriteLine("Preserved owned committed-delta fixture for independent reconciliation: " + name);
        }
    }
}
