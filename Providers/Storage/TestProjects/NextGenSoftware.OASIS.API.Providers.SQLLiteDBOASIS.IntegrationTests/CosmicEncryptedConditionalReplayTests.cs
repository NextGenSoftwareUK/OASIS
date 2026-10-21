using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Security;
using NextGenSoftware.OASIS.API.Core.Serialization;
using NextGenSoftware.OASIS.Common;
using System.Text.Json.Nodes;
using Microsoft.Data.Sqlite;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests;

public partial class SQLLiteDBOASISIntegrationTests
{
    private sealed class PreparationResolver : IHolonMetadataKeyResolver
    {
        internal bool DenyOldPreparation;
        internal List<HolonMetadataKeyRequest> Requests = new();
        public OASISResult<HolonMetadataKeyMaterial> ResolveKey(HolonMetadataKeyRequest request)
        {
            Requests.Add(request);
            if (DenyOldPreparation && request.Purpose == HolonMetadataKeyPurpose.DecryptPreparation && request.KeyId == "key-a")
                return new() { IsError = true, ErrorCode = "OLD_PREPARATION_DENIED" };
            return new(new HolonMetadataKeyMaterial(Enumerable.Repeat(request.KeyId == "key-a" ? (byte)17 : (byte)34, 32).ToArray()));
        }
    }
    private string ReadNativePreparation(Guid operation)
    {
        using var connection = new SqliteConnection(TestConnectionString); connection.Open();
        using var command = connection.CreateCommand(); command.CommandText = "SELECT PayloadJson FROM CosmicWritePreparation WHERE OperationId=$op";
        command.Parameters.AddWithValue("$op", operation.ToString("D")); return (string)command.ExecuteScalar()!;
    }

    [Theory]
    [InlineData(false, false)] [InlineData(true, false)] [InlineData(false, true)] [InlineData(true, true)]
    public async Task EncryptedPreparedGraphsAndBatchesReplayAcrossRestartAndWriteKeyRotation(bool synchronous, bool batch)
    {
        const decimal exact = 1.000000000000000000000000001m;
        var op = Guid.NewGuid(); var actor = Guid.NewGuid();
        NativeDecimalRow[] Inputs() => Enumerable.Range(0, batch ? 2 : 1).Select(index => new NativeDecimalRow {
            Name = "prepared graph " + index, Price = exact,
            MetaData = new() { ["private-note"] = "root secret", ["search-tag"] = "coffee" },
            Children = new List<IHolon> { new Holon { MetadataEncryptionOverride = NativeMetadataPolicy(new NativeMetadataResolver(), "key-b", "child-native-scope"),
                MetaData = new() { ["private-note"] = "child secret", ["exact"] = exact } } }
        }).ToArray();
        async Task<OASISResult<IEnumerable<IHolon>>> Save(Core.Managers.HolonManager manager, NativeDecimalRow[] roots)
        {
            if (batch) return synchronous ? manager.SaveHolons(roots, actor, new CosmicWriteOptions(op), providerType: ProviderType.SQLLiteDBOASIS)
                : await manager.SaveHolonsAsync(roots, actor, new CosmicWriteOptions(op), providerType: ProviderType.SQLLiteDBOASIS);
            var single = synchronous ? manager.SaveHolon(roots[0], actor, new CosmicWriteOptions(op), providerType: ProviderType.SQLLiteDBOASIS)
                : await manager.SaveHolonAsync(roots[0], actor, new CosmicWriteOptions(op), providerType: ProviderType.SQLLiteDBOASIS);
            return new(single.Result == null ? null : new[] { single.Result }) { IsError = single.IsError, Message = single.Message, Exception = single.Exception };
        }
        var first = Inputs(); Guid[] revisions; Guid[] versions;
        using (var provider = new SQLLiteDBOASIS(TestConnectionString))
        {
            (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
            var manager = CosmicManager(provider, true); manager.MetadataEncryptionPolicy = NativeMetadataPolicy(new NativeMetadataResolver());
            var saved = await Save(manager, first); saved.IsError.Should().BeFalse(saved.Message + saved.Exception);
            revisions = first.SelectMany(root => new[] { (IHolon)root, root.Children.Single() }).Select(Revision).ToArray();
            versions = first.SelectMany(root => new[] { (IHolon)root, root.Children.Single() }).Select(row => row.VersionId).ToArray();
        }
        var journal = ReadNativePreparation(op); journal.Should().NotContain("root secret").And.NotContain("child secret").And.NotContain(exact.ToString());
        var contexts = JsonNode.Parse(journal)!.AsArray().Select(record => JsonNode.Parse(record!["envelope"]!.GetValue<string>().Substring("oasis-preparation1:".Length))!["contextId"]!.GetValue<string>());
        contexts.Should().Contain("native-scope").And.Contain("child-native-scope");
        using var restarted = new SQLLiteDBOASIS(TestConnectionString); (await restarted.ActivateProviderAsync()).IsError.Should().BeFalse();
        var retry = CosmicManager(restarted, true); var keys = new PreparationResolver();
        retry.MetadataEncryptionPolicy = new HolonMetadataEncryptionPolicy("native-scope", "key-b", keys, new[] { "search-tag" });
        var fresh = Inputs(); var replayed = await Save(retry, fresh); replayed.IsError.Should().BeFalse(replayed.Message + replayed.Exception);
        fresh.SelectMany(root => new[] { (IHolon)root, root.Children.Single() }).Select(Revision).Should().Equal(revisions);
        fresh.SelectMany(root => new[] { (IHolon)root, root.Children.Single() }).Select(row => row.VersionId).Should().Equal(versions);
        foreach (var row in fresh) { row.Price.Should().Be(exact); row.MetaData["private-note"].ToString().Should().Be("root secret");
            StoredRecordJson.Serialize(row.Children.Single().MetaData).Should().Contain(exact.ToString()); HolonChangeTracking.HasChanges(row).Should().BeFalse(); }
        keys.Requests.Should().Contain(request => request.KeyId == "key-a" && request.Purpose == HolonMetadataKeyPurpose.DecryptPreparation);
        ReadNativePreparation(op).Should().Be(journal);
    }

    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)] [InlineData(4)] [InlineData(5)] [InlineData(6)]
    public async Task EncryptedPreparationRejectsChangedLogicalIntentWithoutAnotherMutation(int fault)
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString); (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var manager = CosmicManager(provider, true); manager.MetadataEncryptionPolicy = NativeMetadataPolicy(new NativeMetadataResolver());
        var op = Guid.NewGuid(); var actor = Guid.NewGuid();
        Holon Input() => new() { Name = "same name", MetaData = new() { ["private-note"] = 1.000000000000000000000000001m, ["search-tag"] = "coffee" } };
        var original = Input(); var saved = await manager.SaveHolonAsync(original, actor, new CosmicWriteOptions(op), providerType: ProviderType.SQLLiteDBOASIS);
        saved.IsError.Should().BeFalse(saved.Message + saved.Exception);
        var before = StoredRecordJson.Serialize((await provider.LoadHolonAsync(original.Id, loadChildren: false)).Result); var journal = ReadNativePreparation(op);
        var changed = Input();
        if (fault == 0) changed.MetaData["private-note"] = 1.000000000000000000000000002m;
        if (fault == 1) changed.MetaData["search-tag"] = "tea";
        if (fault == 2) changed.Name = "different name";
        if (fault == 3) changed.Children.Add(new Holon());
        if (fault == 6) changed.ProviderUniqueStorageKey[ProviderType.MongoDBOASIS] = "different foreign key";
        var conditions = fault == 4 ? new Dictionary<Guid, Guid?> { [original.Id] = Guid.Empty } : null;
        var rejected = await manager.SaveHolonAsync(changed, fault == 5 ? Guid.NewGuid() : actor, new CosmicWriteOptions(op, conditions), providerType: ProviderType.SQLLiteDBOASIS);
        rejected.IsError.Should().BeTrue(); rejected.ErrorCode.Should().Be(fault == 5 ? "COSMIC_PREPARATION_ACTOR_MISMATCH" : "COSMIC_OPERATION_ID_REUSED");
        changed.Id.Should().BeEmpty(); changed.VersionId.Should().BeEmpty();
        ReadNativePreparation(op).Should().Be(journal);
        StoredRecordJson.Serialize((await provider.LoadHolonAsync(original.Id, loadChildren: false)).Result).Should().Be(before);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task EncryptedPreparationRefusesDeniedOldKeysOrTamperedWinnerBeforeReceiptReplay(bool tamper)
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString); (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var writer = CosmicManager(provider, true); writer.MetadataEncryptionPolicy = NativeMetadataPolicy(new NativeMetadataResolver());
        var op = Guid.NewGuid(); var actor = Guid.NewGuid();
        Holon Input() => new() { MetaData = new() { ["private-note"] = "secret" } };
        var original = Input(); (await writer.SaveHolonAsync(original, actor, new CosmicWriteOptions(op), saveChildren: false, providerType: ProviderType.SQLLiteDBOASIS)).IsError.Should().BeFalse();
        var before = StoredRecordJson.Serialize((await provider.LoadHolonAsync(original.Id, loadChildren: false)).Result);
        if (tamper)
        {
            var records = JsonNode.Parse(ReadNativePreparation(op))!.AsArray(); var envelope = records[0]!["envelope"]!.GetValue<string>();
            const string prefix = "oasis-preparation1:"; var body = JsonNode.Parse(envelope.Substring(prefix.Length))!;
            var bytes = Convert.FromBase64String(body["tag"]!.GetValue<string>()); bytes[0] ^= 1; body["tag"] = Convert.ToBase64String(bytes);
            records[0]!["envelope"] = prefix + body.ToJsonString();
            using var connection = new SqliteConnection(TestConnectionString); connection.Open(); using var command = connection.CreateCommand();
            command.CommandText = "UPDATE CosmicWritePreparation SET PayloadJson=$payload WHERE OperationId=$op";
            command.Parameters.AddWithValue("$payload", records.ToJsonString()); command.Parameters.AddWithValue("$op", op.ToString("D")); command.ExecuteNonQuery();
        }
        var reader = CosmicManager(provider, true); var keys = new PreparationResolver { DenyOldPreparation = !tamper };
        reader.MetadataEncryptionPolicy = new HolonMetadataEncryptionPolicy("native-scope", "key-b", keys);
        var retry = Input(); var rejected = await reader.SaveHolonAsync(retry, actor, new CosmicWriteOptions(op), saveChildren: false, providerType: ProviderType.SQLLiteDBOASIS);
        rejected.IsError.Should().BeTrue(); rejected.ErrorCode.Should().Be(tamper ? "COSMIC_PREPARATION_INVALID" : "OLD_PREPARATION_DENIED");
        retry.Id.Should().BeEmpty();
        StoredRecordJson.Serialize((await provider.LoadHolonAsync(original.Id, loadChildren: false)).Result).Should().Be(before);
    }
    [Theory]
    [InlineData(false, false)] [InlineData(true, false)] [InlineData(false, true)] [InlineData(true, true)]
    public async Task EncryptedCrossClientRacesConvergeOrRejectChangedIntent(bool synchronous, bool different)
    {
        using var a = new SQLLiteDBOASIS(TestConnectionString); using var b = new SQLLiteDBOASIS(TestConnectionString);
        (await a.ActivateProviderAsync()).IsError.Should().BeFalse(); (await b.ActivateProviderAsync()).IsError.Should().BeFalse();
        var ma = CosmicManager(a, true); var mb = CosmicManager(b, true);
        ma.MetadataEncryptionPolicy = NativeMetadataPolicy(new NativeMetadataResolver()); mb.MetadataEncryptionPolicy = NativeMetadataPolicy(new NativeMetadataResolver());
        var left = new Holon { MetaData = new() { ["private-note"] = "same" } };
        var right = new Holon { MetaData = new() { ["private-note"] = different ? "different" : "same" } };
        var op = Guid.NewGuid(); var actor = Guid.NewGuid(); using var start = new ManualResetEventSlim();
        async Task<OASISResult<IHolon>> Save(Core.Managers.HolonManager manager, Holon row)
        { start.Wait(); return synchronous ? manager.SaveHolon(row, actor, new CosmicWriteOptions(op), saveChildren: false, providerType: ProviderType.SQLLiteDBOASIS)
            : await manager.SaveHolonAsync(row, actor, new CosmicWriteOptions(op), saveChildren: false, providerType: ProviderType.SQLLiteDBOASIS); }
        var one = Task.Run(() => Save(ma, left)); var two = Task.Run(() => Save(mb, right)); start.Set();
        var results = await Task.WhenAll(one, two);
        results.Count(result => result.IsError).Should().Be(different ? 1 : 0);
        if (different)
        {
            results.Single(result => result.IsError).ErrorCode.Should().Be("COSMIC_OPERATION_ID_REUSED");
            var rejected = results[0].IsError ? left : right; rejected.Id.Should().BeEmpty();
        }
        else { left.Id.Should().Be(right.Id); left.VersionId.Should().Be(right.VersionId); Revision(left).Should().Be(Revision(right)); }
        var winner = results.First(result => !result.IsError).Result;
        var stored = (await a.LoadHolonAsync(winner.Id, loadChildren: false)).Result; stored.Version.Should().Be(1);
        stored.VersionId.Should().Be(winner.VersionId); Revision(stored).Should().Be(Revision(winner));
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task EncryptedOriginalReceiptReplayAfterLaterWriteDoesNotOverwriteTheLaterRevision(bool synchronous)
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString); (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var manager = CosmicManager(provider, true); manager.MetadataEncryptionPolicy = NativeMetadataPolicy(new NativeMetadataResolver());
        Holon Input() => new() { Name = "original", MetaData = new() { ["private-note"] = "original secret" } };
        var source = Input(); var op = Guid.NewGuid(); var actor = Guid.NewGuid();
        var created = await manager.SaveHolonAsync(source, actor, new CosmicWriteOptions(op), saveChildren: false, providerType: ProviderType.SQLLiteDBOASIS);
        created.IsError.Should().BeFalse(created.Message + created.Exception); var originalVersion = source.VersionId; var originalRevision = Revision(source);
        var laterManager = CosmicManager(provider, true); laterManager.MetadataEncryptionPolicy = NativeMetadataPolicy(new NativeMetadataResolver(), "key-b");
        var loaded = await laterManager.LoadHolonAsync(source.Id, loadChildren: false, providerType: ProviderType.SQLLiteDBOASIS);
        loaded.IsError.Should().BeFalse(loaded.Message); loaded.Result.Name = "later"; loaded.Result.MetaData["private-note"] = "later secret";
        var later = await laterManager.SaveHolonAsync(loaded.Result, actor, new CosmicWriteOptions(Guid.NewGuid()), saveChildren: false, providerType: ProviderType.SQLLiteDBOASIS);
        later.IsError.Should().BeFalse(later.Message + later.Exception);
        var before = StoredRecordJson.Serialize((await provider.LoadHolonAsync(source.Id, loadChildren: false)).Result);
        using var restarted = new SQLLiteDBOASIS(TestConnectionString); (await restarted.ActivateProviderAsync()).IsError.Should().BeFalse();
        var retry = CosmicManager(restarted, true); retry.MetadataEncryptionPolicy = NativeMetadataPolicy(new NativeMetadataResolver(), "key-b");
        var replayedSource = Input(); var replay = synchronous
            ? retry.SaveHolon(replayedSource, actor, new CosmicWriteOptions(op), saveChildren: false, providerType: ProviderType.SQLLiteDBOASIS)
            : await retry.SaveHolonAsync(replayedSource, actor, new CosmicWriteOptions(op), saveChildren: false, providerType: ProviderType.SQLLiteDBOASIS);
        replay.IsError.Should().BeFalse(replay.Message + replay.Exception); replayedSource.Name.Should().Be("original");
        replayedSource.VersionId.Should().Be(originalVersion); Revision(replayedSource).Should().Be(originalRevision);
        replayedSource.MetaData["private-note"].ToString().Should().Be("original secret");
        StoredRecordJson.Serialize((await restarted.LoadHolonAsync(source.Id, loadChildren: false)).Result).Should().Be(before);
    }

    [Theory]
    [InlineData(false, false)] [InlineData(true, false)]
    [InlineData(false, true)] [InlineData(true, true)]
    public async Task EncryptedConditionalCreateReplaysOriginalReceiptAcrossFreshManagers(bool synchronous, bool encrypted)
    {
        var operation = Guid.NewGuid(); var actor = Guid.NewGuid();
        Holon Input() => new() { Name = "encrypted retry", MetaData = new() { ["private-note"] = "same logical intent" } };
        Guid id; Guid version; Guid revision;
        using (var provider = new SQLLiteDBOASIS(TestConnectionString))
        {
            (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
            var manager = CosmicManager(provider, true);
            if (encrypted) manager.MetadataEncryptionPolicy = NativeMetadataPolicy(new NativeMetadataResolver());
            var source = Input();
            var first = synchronous
                ? manager.SaveHolon(source, actor, new CosmicWriteOptions(operation), saveChildren: false, providerType: ProviderType.SQLLiteDBOASIS)
                : await manager.SaveHolonAsync(source, actor, new CosmicWriteOptions(operation), saveChildren: false, providerType: ProviderType.SQLLiteDBOASIS);
            first.IsError.Should().BeFalse(first.Message + first.Exception);
            id = source.Id; version = source.VersionId; revision = Revision(source);
        }
        using var restarted = new SQLLiteDBOASIS(TestConnectionString);
        (await restarted.ActivateProviderAsync()).IsError.Should().BeFalse();
        var retryManager = CosmicManager(restarted, true);
        if (encrypted) retryManager.MetadataEncryptionPolicy = NativeMetadataPolicy(new NativeMetadataResolver());
        var retrySource = Input();
        var replay = synchronous
            ? retryManager.SaveHolon(retrySource, actor, new CosmicWriteOptions(operation), saveChildren: false, providerType: ProviderType.SQLLiteDBOASIS)
            : await retryManager.SaveHolonAsync(retrySource, actor, new CosmicWriteOptions(operation), saveChildren: false, providerType: ProviderType.SQLLiteDBOASIS);
        replay.IsError.Should().BeFalse(replay.ErrorCode + ": " + replay.Message + replay.Exception);
        retrySource.Id.Should().Be(id); retrySource.VersionId.Should().Be(version); Revision(retrySource).Should().Be(revision);
        retrySource.MetaData["private-note"].ToString().Should().Be("same logical intent");
        var current = (await restarted.LoadHolonAsync(id, loadChildren: false)).Result;
        current.VersionId.Should().Be(version); Revision(current).Should().Be(revision);
    }
}
