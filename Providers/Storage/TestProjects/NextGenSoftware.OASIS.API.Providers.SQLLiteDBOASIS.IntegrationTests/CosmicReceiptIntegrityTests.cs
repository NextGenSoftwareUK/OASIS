using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Serialization;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Context;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests;

public partial class SQLLiteDBOASISIntegrationTests
{
    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)]
    [InlineData(4)] [InlineData(5)] [InlineData(6)] [InlineData(7)]
    public async Task ConditionalReceiptRejectsCorruptOrTransplantedAcknowledgementWithoutMutation(int corruption)
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var child = new Holon { Id = Guid.NewGuid(), Name = "owned receipt child" };
        var root = new Holon { Id = Guid.NewGuid(), Name = "owned original receipt" }; root.Children.Add(child);
        var operation = Guid.NewGuid();
        var request = ConditionalRequest(root, null, operation, conditions: new[] { new HolonWriteCondition(root.Id, null), new HolonWriteCondition(child.Id, null) });
        var first = provider.ExecuteConditionalHolonWrite(request);
        first.IsError.Should().BeFalse(first.Message + first.Exception);
        var other = new Holon { Id = Guid.NewGuid(), Name = "owned unrelated receipt" };
        var otherOperation = Guid.NewGuid();
        provider.ExecuteConditionalHolonWrite(ConditionalRequest(other, null, otherOperation)).IsError.Should().BeFalse();
        using var context = new DataContext(TestConnectionString);
        context.Database.OpenConnection();
        string Read(Guid id)
        {
            using var command = context.Database.GetDbConnection().CreateCommand();
            command.CommandText = "SELECT ResultJson FROM CosmicWriteReceipt WHERE OperationId = $id";
            var parameter = command.CreateParameter(); parameter.ParameterName = "$id"; parameter.Value = id.ToString("D"); command.Parameters.Add(parameter);
            return (string)command.ExecuteScalar()!;
        }
        var envelope = JObject.Parse(Read(operation));
        var graph = envelope["graph"]?.Type == JTokenType.String ? JObject.Parse((string)envelope["graph"]!) : envelope;
        var nodes = (JArray)graph["nodes"]!;
        var rootNode = (JObject)nodes.Single(node => node["id"]!.ToObject<Guid>() == root.Id);
        var childNode = (JObject)nodes.Single(node => node["id"]!.ToObject<Guid>() == child.Id);
        switch (corruption)
        {
            case 0: case 1:
                var record = JObject.Parse((string)rootNode["record"]!);
                record[corruption == 0 ? "Name" : "Version"] = corruption == 0 ? new JValue("forged acknowledgement") : new JValue(0);
                rootNode["record"] = record.ToString(Formatting.None); break;
            case 2: graph["roots"] = new JArray(child.Id); break;
            case 3: rootNode["children"] = new JArray(); nodes.Remove(childNode); break;
            case 4:
                var extra = (JObject)JObject.Parse(Read(otherOperation))["nodes"]?.First!;
                if (extra == null) extra = (JObject)JObject.Parse((string)JObject.Parse(Read(otherOperation))["graph"]!)["nodes"]!.First!;
                nodes.Add(extra.DeepClone()); break;
            case 5: envelope["actor"] = Guid.NewGuid(); break;
            case 7:
                var childRecord = JObject.Parse((string)childNode["record"]!); childRecord["ParentHolonId"] = other.Id;
                childNode["record"] = childRecord.ToString(Formatting.None); break;
        }
        if (!ReferenceEquals(graph, envelope)) envelope["graph"] = graph.ToString(Formatting.None);
        var tampered = corruption == 6 ? Read(otherOperation) : envelope.ToString(Formatting.None);
        context.Database.ExecuteSqlRaw("UPDATE CosmicWriteReceipt SET ResultJson={0} WHERE OperationId={1}", tampered, operation.ToString("D"));
        var before = StoredRecordJson.Serialize(context.Holons.AsNoTracking().OrderBy(row => row.Id).ToList());
        var replay = provider.ExecuteConditionalHolonWrite(request);
        replay.IsError.Should().BeTrue("stored acknowledgements must be bound, complete and intact");
        replay.ErrorCode.Should().Be("COSMIC_RECEIPT_INVALID");
        replay.Result.Should().BeNull(); replay.IsSaved.Should().BeFalse();
        StoredRecordJson.Serialize(context.Holons.AsNoTracking().OrderBy(row => row.Id).ToList()).Should().Be(before);
        Read(operation).Should().Be(tampered);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task ConditionalReceiptRequiresOriginalIntentEvenWithValidChecksums(bool historical)
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var root = new Holon { Id = Guid.NewGuid(), Name = "owned original receipt intent" };
        var operation = Guid.NewGuid(); var request = ConditionalRequest(root, null, operation);
        var first = provider.ExecuteConditionalHolonWrite(request); first.IsError.Should().BeFalse(first.Message + first.Exception);
        using var context = new DataContext(TestConnectionString); context.Database.OpenConnection();
        using var command = context.Database.GetDbConnection().CreateCommand();
        command.CommandText = "SELECT ResultJson FROM CosmicWriteReceipt WHERE OperationId=$id";
        var parameter = command.CreateParameter(); parameter.ParameterName = "$id"; parameter.Value = operation.ToString("D"); command.Parameters.Add(parameter);
        var envelope = JObject.Parse((string)command.ExecuteScalar()!);
        var graph = JObject.Parse((string)envelope["graph"]!);
        if (historical)
            envelope = new JObject { ["format"] = 1, ["fingerprintFormat"] = 3, ["roots"] = graph["roots"]!.DeepClone(), ["nodes"] = graph["nodes"]!.DeepClone() };
        else
        {
            var records = NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.ConditionalHolonReceipt.Read((string)envelope["graph"]!);
            records.Single().Name = "checksum-valid wrong acknowledged intent";
            envelope["graph"] = NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.ConditionalHolonReceipt.Write(records);
            envelope.Remove("digest");
            envelope["digest"] = NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.ConditionalHolonPlan.ComputeFingerprint(envelope);
        }
        var tampered = envelope.ToString(Formatting.None);
        context.Database.ExecuteSqlRaw("UPDATE CosmicWriteReceipt SET ResultJson={0} WHERE OperationId={1}", tampered, operation.ToString("D"));
        var before = StoredRecordJson.Serialize(context.Holons.AsNoTracking().Single());
        var replay = provider.ExecuteConditionalHolonWrite(request);
        replay.IsError.Should().BeTrue(); replay.Result.Should().BeNull(); replay.IsSaved.Should().BeFalse();
        replay.ErrorCode.Should().Be(historical ? "COSMIC_RECEIPT_ENVELOPE_RECONCILIATION_REQUIRED" : "COSMIC_RECEIPT_INVALID");
        StoredRecordJson.Serialize(context.Holons.AsNoTracking().Single()).Should().Be(before);
        ((string)command.ExecuteScalar()!).Should().Be(tampered);
    }
}
