using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Security;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Context;
using NextGenSoftware.OASIS.Common;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests;

public partial class SQLLiteDBOASISIntegrationTests
{
    private const string DeltaKey = "COSMIC.CommittedDelta";
    private sealed class DeltaGateResolver : IHolonMetadataKeyResolver, IDisposable
    {
        private int entered;
        private readonly NativeMetadataResolver keys = new();
        internal readonly ManualResetEventSlim Entered = new(false), Release = new(false);
        public OASISResult<HolonMetadataKeyMaterial> ResolveKey(HolonMetadataKeyRequest request)
        {
            if (Interlocked.Exchange(ref entered, 1) == 0)
            { Entered.Set(); if (!Release.Wait(TimeSpan.FromSeconds(20))) return new() { IsError = true, ErrorCode = "OWNED_DELTA_GATE_TIMEOUT" }; }
            return keys.ResolveKey(request);
        }
        public void Dispose() { Release.Set(); Entered.Dispose(); Release.Dispose(); }
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task AtomicCommittedDeltaUsesAuthoritativeValuesAndReplaysOriginalSummary(bool history)
    {
        var actor = Guid.NewGuid(); var root = new Holon { Id = Guid.NewGuid(), Name = "before delta", MetaData = new() {
            ["remove"] = "sensitive previous value", ["array"] = new[] { 1, 2 }, ["amount"] = 1.000000000000000000000000001m } };
        var child = new Holon { Id = Guid.NewGuid(), Name = "unselected retained child" }; root.Children.Add(child);
        using var provider = new SQLLiteDBOASIS(TestConnectionString) { IsVersionControlEnabled = history };
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var create = provider.ExecuteConditionalHolonWrite(ConditionalRequest(root, null, actor: actor,
            conditions: new[] { new HolonWriteCondition(root.Id, null), new HolonWriteCondition(child.Id, null) }));
        create.IsError.Should().BeFalse(create.Message + create.Exception);
        create.MetaData.Should().ContainKey(DeltaKey);
        var firstRevision = Revision(create.Result.Single());
        var current = (await provider.LoadHolonAsync(root.Id, loadChildren: false)).Result;
        current.Name = "committed delta"; current.MetaData.Remove("remove"); current.MetaData["nullAdded"] = null!;
        current.MetaData["array"] = new[] { 2, 1 }; current.MetaData["amount"] = 1.000000000000000000000000002m;
        var operation = Guid.NewGuid();
        var request = new ConditionalHolonWriteRequest(new ConditionalHolonWrite(operation, actor, ProviderType.SQLLiteDBOASIS,
            new[] { new HolonWriteCondition(root.Id, firstRevision) }), ConditionalHolonOperation.Save, new[] { current }, saveChildren: false);
        var saved = provider.ExecuteConditionalHolonWrite(request); saved.IsError.Should().BeFalse(saved.Message + saved.Exception);
        var summaryText = saved.MetaData[DeltaKey]; var summary = JObject.Parse(summaryText);
        summary.Value<string>("operationId").Should().Be(operation.ToString("D")); summary.Value<string>("actor").Should().Be(actor.ToString("D"));
        summary.Value<string>("authority").Should().Be("SQLLiteDBOASIS");
        var record = ((JArray)summary["records"]!).Single();
        record.Value<string>("kind").Should().Be("Updated"); record.Value<string>("previousRevision").Should().Be(firstRevision.ToString("D"));
        record.Value<string>("committedRevision").Should().Be(Revision(saved.Result.Single()).ToString("D"));
        var changes = ((JArray)record["changes"]!).ToDictionary(change => change.Value<string>("path")!);
        changes["/MetaData/remove"].Value<string>("change").Should().Be("Removed");
        changes["/MetaData/nullAdded"].Value<string>("change").Should().Be("Added");
        // Paths address the canonical stored representation, including typed collection envelopes.
        changes["/MetaData/array/$values"].Value<string>("change").Should().Be("Modified");
        changes["/MetaData/amount"].Value<string>("change").Should().Be("Modified");
        changes.Values.Should().OnlyContain(change => change["previousJson"] == null && change["currentJson"] == null);
        summaryText.Should().NotContain("sensitive previous value").And.NotContain("committed delta");
        using (var context = new DataContext(TestConnectionString))
        {
            context.Database.OpenConnection(); using var command = context.Database.GetDbConnection().CreateCommand();
            command.CommandText = "SELECT ResultJson FROM CosmicWriteReceipt WHERE OperationId=$id";
            var id = command.CreateParameter(); id.ParameterName = "$id"; id.Value = operation.ToString("D"); command.Parameters.Add(id);
            var storedDelta = JObject.Parse((string)JObject.Parse((string)command.ExecuteScalar()!)["delta"]!);
            storedDelta.Value<bool>("valuesRetained").Should().Be(history);
            storedDelta.ToString().Contains("sensitive previous value").Should().Be(history);
        }
        current.Name = "unsaved edit"; saved.MetaData[DeltaKey].Should().Be(summaryText); current.Name = "committed delta";
        var newer = (await provider.LoadHolonAsync(root.Id, loadChildren: false)).Result; newer.Name = "later committed revision";
        provider.ExecuteConditionalHolonWrite(new ConditionalHolonWriteRequest(new ConditionalHolonWrite(Guid.NewGuid(), actor,
            ProviderType.SQLLiteDBOASIS, new[] { new HolonWriteCondition(root.Id, Revision(saved.Result.Single())) }),
            ConditionalHolonOperation.Save, new[] { newer }, saveChildren: false)).IsError.Should().BeFalse();
        using var restarted = new SQLLiteDBOASIS(TestConnectionString) { IsVersionControlEnabled = !history };
        (await restarted.ActivateProviderAsync()).IsError.Should().BeFalse();
        var replay = restarted.ExecuteConditionalHolonWrite(request); replay.IsError.Should().BeFalse(replay.Message + replay.Exception);
        replay.MetaData[DeltaKey].Should().Be(summaryText); replay.Result.Single().Name.Should().Be("committed delta");
        var loaded = (await restarted.LoadHolonAsync(root.Id)).Result;
        loaded.Name.Should().Be("later committed revision"); loaded.Children.Single().Id.Should().Be(child.Id);
        var stale = restarted.ExecuteConditionalHolonWrite(ConditionalRequest(new Holon { Id = root.Id, Name = "stale" }, firstRevision));
        stale.ErrorCode.Should().Be(CosmicConcurrencyException.Code); stale.MetaData.Should().NotContainKey(DeltaKey);
    }

    [Fact]
    public async Task AtomicCommittedDeltaRollsBackWithTheReceiptAndNeverPublishesFailedEvidence()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString) { IsVersionControlEnabled = true };
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var source = new Holon { Id = Guid.NewGuid(), Name = "before rejected delta" };
        var first = provider.ExecuteConditionalHolonWrite(ConditionalRequest(source, null)); first.IsError.Should().BeFalse(first.Message + first.Exception);
        using var context = new DataContext(TestConnectionString);
        context.Database.ExecuteSqlRaw("CREATE TRIGGER owned_delta_receipt_fault BEFORE INSERT ON CosmicWriteReceipt BEGIN SELECT RAISE(ABORT,'owned delta receipt fault'); END");
        source.Name = "must not persist";
        var rejected = provider.ExecuteConditionalHolonWrite(ConditionalRequest(source, Revision(first.Result.Single())));
        rejected.IsError.Should().BeTrue(); rejected.IsSaved.Should().BeFalse(); rejected.Result.Should().BeNull();
        rejected.MetaData.Should().NotContainKey(DeltaKey);
        context.Holons.AsNoTracking().Single().Name.Should().Be("before rejected delta");
        context.Database.OpenConnection(); using var count = context.Database.GetDbConnection().CreateCommand();
        count.CommandText = "SELECT COUNT(*) FROM CosmicWriteReceipt"; Convert.ToInt32(count.ExecuteScalar()).Should().Be(1);
    }

    [Theory]
    [InlineData(false, false)] [InlineData(false, true)] [InlineData(true, false)] [InlineData(true, true)]
    public async Task AtomicCommittedDeltaDistinguishesSoftAndHardDeletionAndReplaysExactly(bool soft, bool synchronous)
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString) { IsVersionControlEnabled = true };
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var source = new Holon { Id = Guid.NewGuid(), Name = "owned delta deletion" };
        var first = provider.ExecuteConditionalHolonWrite(ConditionalRequest(source, null)); first.IsError.Should().BeFalse(first.Message + first.Exception);
        var request = new ConditionalHolonWriteRequest(new ConditionalHolonWrite(Guid.NewGuid(), Guid.NewGuid(), ProviderType.SQLLiteDBOASIS,
            new[] { new HolonWriteCondition(source.Id, Revision(first.Result.Single())) }),
            soft ? ConditionalHolonOperation.SoftDelete : ConditionalHolonOperation.HardDelete, new[] { source }, saveChildren: false);
        var deleted = synchronous ? provider.ExecuteConditionalHolonWrite(request) : await provider.ExecuteConditionalHolonWriteAsync(request);
        deleted.IsError.Should().BeFalse(deleted.Message + deleted.Exception);
        var summary = deleted.MetaData[DeltaKey]; var record = JObject.Parse(summary)["records"]!.Single();
        record.Value<string>("kind").Should().Be(soft ? "SoftDeleted" : "HardDeleted");
        record.Value<bool>("previousExists").Should().BeTrue(); record.Value<bool>("currentExists").Should().Be(soft);
        var replay = synchronous ? provider.ExecuteConditionalHolonWrite(request) : await provider.ExecuteConditionalHolonWriteAsync(request);
        replay.IsError.Should().BeFalse(replay.Message + replay.Exception); replay.MetaData[DeltaKey].Should().Be(summary);
        using var context = new DataContext(TestConnectionString); context.Holons.AsNoTracking().Count().Should().Be(soft ? 1 : 0);
    }

    [Fact]
    public async Task ManagerPublishesRedactedEncryptedDeltaAndNoOpCreatesNoReceipt()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString) { IsVersionControlEnabled = true };
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var manager = CosmicManager(provider, true); manager.MetadataEncryptionPolicy = NativeMetadataPolicy(new NativeMetadataResolver());
        var actor = Guid.NewGuid(); var source = new Holon { Name = "encrypted committed delta", MetaData = new() { ["private-note"] = "private delta before", ["search-tag"] = "coffee" } };
        var first = await manager.SaveHolonAsync(source, actor, new CosmicWriteOptions(Guid.NewGuid()), saveChildren: false);
        first.IsError.Should().BeFalse(first.Message + first.Exception); first.MetaData.Should().ContainKey(DeltaKey);
        source.MetaData["private-note"] = "private delta after";
        var saved = await manager.SaveHolonAsync(source, actor, new CosmicWriteOptions(Guid.NewGuid()), saveChildren: false);
        saved.IsError.Should().BeFalse(saved.Message + saved.Exception); saved.MetaData[DeltaKey].Should().NotContain("private delta before").And.NotContain("private delta after");
        var noop = await manager.SaveHolonAsync(source, actor, new CosmicWriteOptions(Guid.NewGuid()), saveChildren: false);
        noop.IsError.Should().BeFalse(noop.Message + noop.Exception); noop.IsSaved.Should().BeFalse();
        (noop.MetaData?.ContainsKey(DeltaKey) ?? false).Should().BeFalse();
        using var context = new DataContext(TestConnectionString); context.Database.OpenConnection();
        using var receipts = context.Database.GetDbConnection().CreateCommand(); receipts.CommandText = "SELECT ResultJson FROM CosmicWriteReceipt";
        using var reader = receipts.ExecuteReader(); var count = 0;
        while (reader.Read()) { count++; reader.GetString(0).Should().NotContain("private delta before").And.NotContain("private delta after"); }
        count.Should().Be(2);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task CommittedDeltaCannotBeReboundAndPriorIntegrityReceiptDoesNotInventHistory(bool priorEnvelope)
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var source = new Holon { Id = Guid.NewGuid(), Name = "owned delta binding" };
        var operation = Guid.NewGuid(); var request = ConditionalRequest(source, null, operation);
        var first = provider.ExecuteConditionalHolonWrite(request); first.IsError.Should().BeFalse(first.Message + first.Exception);
        using var context = new DataContext(TestConnectionString); context.Database.OpenConnection();
        using var command = context.Database.GetDbConnection().CreateCommand(); command.CommandText = "SELECT ResultJson FROM CosmicWriteReceipt WHERE OperationId=$id";
        var id = command.CreateParameter(); id.ParameterName = "$id"; id.Value = operation.ToString("D"); command.Parameters.Add(id);
        var envelope = JObject.Parse((string)command.ExecuteScalar()!);
        if (priorEnvelope) { envelope["format"] = 2; envelope.Remove("delta"); }
        else
        {
            using var original = System.Text.Json.JsonDocument.Parse((string)envelope["delta"]!);
            var delta = JObject.Parse((string)envelope["delta"]!);
            delta["committedAtUtc"] = original.RootElement.GetProperty("committedAtUtc").GetString();
            delta["records"]!.First!["committedRevision"] = Guid.NewGuid().ToString("D"); envelope["delta"] = delta.ToString(Newtonsoft.Json.Formatting.None);
        }
        envelope.Remove("digest"); envelope["digest"] = NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.ConditionalHolonPlan.ComputeFingerprint(envelope);
        var stored = envelope.ToString(Newtonsoft.Json.Formatting.None);
        context.Database.ExecuteSqlRaw("UPDATE CosmicWriteReceipt SET ResultJson={0} WHERE OperationId={1}", stored, operation.ToString("D"));
        var replay = provider.ExecuteConditionalHolonWrite(request);
        if (priorEnvelope)
        {
            replay.IsError.Should().BeFalse(replay.Message + replay.Exception); replay.MetaData.Should().NotContainKey(DeltaKey);
            replay.MetaData["COSMIC.CommittedDeltaUnavailable"].Should().Be("ReceiptPredatesDelta");
        }
        else { replay.ErrorCode.Should().Be("COSMIC_RECEIPT_INVALID"); replay.Result.Should().BeNull(); replay.MetaData.Should().NotContainKey(DeltaKey); }
        ((string)command.ExecuteScalar()!).Should().Be(stored); Revision((await provider.LoadHolonAsync(source.Id)).Result).Should().Be(Revision(first.Result.Single()));
    }

    [Fact]
    public async Task DeltaExcludesUserEditsMadeWhileAnEncryptedSaveIsPending()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString) { IsVersionControlEnabled = true };
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        using var keys = new DeltaGateResolver(); var manager = CosmicManager(provider, true);
        manager.MetadataEncryptionPolicy = new HolonMetadataEncryptionPolicy("native-scope", "key-a", keys, new[] { "search-tag" });
        var source = new Holon { Name = "staged delta name", MetaData = new() { ["private-note"] = "staged private delta" } };
        var operation = Guid.NewGuid(); var save = Task.Run(async () => await manager.SaveHolonAsync(source, Guid.NewGuid(), new CosmicWriteOptions(operation), saveChildren: false));
        try
        {
            keys.Entered.Wait(TimeSpan.FromSeconds(10)).Should().BeTrue();
            source.Name = "pending user delta edit"; source.MetaData["private-note"] = "pending private delta edit";
            keys.Release.Set(); var saved = await save.WaitAsync(TimeSpan.FromSeconds(30));
            saved.IsError.Should().BeFalse(saved.Message + saved.Exception); saved.MetaData.Should().ContainKey(DeltaKey);
            source.Name.Should().Be("pending user delta edit"); source.HasHolonChanged().Should().BeTrue();
            using var context = new DataContext(TestConnectionString); context.Holons.AsNoTracking().Single().Name.Should().Be("staged delta name");
            context.Database.OpenConnection(); using var command = context.Database.GetDbConnection().CreateCommand();
            command.CommandText = "SELECT ResultJson FROM CosmicWriteReceipt WHERE OperationId=$id";
            var id = command.CreateParameter(); id.ParameterName = "$id"; id.Value = operation.ToString("D"); command.Parameters.Add(id);
            var receipt = (string)command.ExecuteScalar()!;
            receipt.Should().Contain("staged delta name").And.NotContain("pending user delta edit").And.NotContain("pending private delta edit");
        }
        finally { keys.Release.Set(); if (!save.IsCompleted) await save.WaitAsync(TimeSpan.FromSeconds(30)); }
    }
}
