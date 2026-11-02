using System.Text.Json;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

internal sealed record CqlChainflipOriginalFixture(Guid Actor, Guid ForeignActor, string Nonce,
    string[] Allowed, Guid[] Copies, Guid[] Operations);

internal static class CqlChainflipFixtureContinuation
{
    internal const string Database = "cosmic_cql_chainflip_http_demo";
    internal const string Owner = "cosmic-cql-chainflip-http-v1";

    internal static CqlChainflipOriginalFixture Validate(JsonElement journal,
        IReadOnlyList<JsonElement> acknowledgements, IReadOnlyList<string> allowed)
    {
        static void Require(bool valid) { if (!valid) throw new InvalidDataException("Original acknowledged Chainflip fixture differs; no writes or replacement permitted."); }
        Require(journal.ValueKind == JsonValueKind.Object && acknowledgements.Count == 2);
        Require(journal.GetProperty("Owner").GetString() == Owner && journal.GetProperty("Database").GetString() == Database);
        var actor = journal.GetProperty("actor").GetGuid();
        var foreign = journal.GetProperty("foreignActor").GetGuid();
        var nonce = journal.GetProperty("nonce").GetString()!;
        var keys = journal.GetProperty("allowed").EnumerateArray().Select(value => value.GetString()!).ToArray();
        var copies = journal.GetProperty("copies").EnumerateArray().Select(value => value.GetGuid()).ToArray();
        var operations = journal.GetProperty("operations").EnumerateArray().Select(value => value.GetGuid()).ToArray();
        Require(actor != Guid.Empty && foreign != Guid.Empty && actor != foreign && Guid.TryParse(nonce, out var parsed) && parsed != Guid.Empty);
        Require(keys.Length == 3 && keys.SequenceEqual(allowed, StringComparer.Ordinal));
        Require(copies.Length == 2 && copies.All(id => id != Guid.Empty) && copies.Distinct().Count() == 2);
        Require(operations.Length == 2 && operations.All(id => id != Guid.Empty) && operations.Distinct().Count() == 2);
        for (int index = 0; index < 2; index++)
            Require(acknowledgements[index].GetProperty("id").GetGuid() == copies[index] &&
                acknowledgements[index].GetProperty("operation").GetGuid() == operations[index] &&
                acknowledgements[index].GetProperty("version").GetInt32() == 1);
        return new(actor, foreign, nonce, keys, copies, operations);
    }
}

public class CqlChainflipFixtureContinuationTests
{
    [Theory]
    [InlineData("valid")] [InlineData("owner")] [InlineData("database")] [InlineData("actor")]
    [InlineData("keys")] [InlineData("copies")] [InlineData("operation")] [InlineData("version")] [InlineData("missing-ack")]
    public void OnlyTheOriginalTwoAcknowledgedCopiesCanResumeWithoutWrites(string fault)
    {
        var actor = Guid.NewGuid(); var copies = new[] { Guid.NewGuid(), Guid.NewGuid() };
        var operations = new[] { Guid.NewGuid(), Guid.NewGuid() }; var keys = new[] { "500000", "500001", "500002" };
        var journal = JsonSerializer.SerializeToElement(new {
            Owner = fault == "owner" ? "other" : CqlChainflipFixtureContinuation.Owner,
            Database = fault == "database" ? "other" : CqlChainflipFixtureContinuation.Database,
            actor = fault == "actor" ? Guid.Empty : actor, foreignActor = Guid.NewGuid(), nonce = Guid.NewGuid().ToString("D"),
            allowed = fault == "keys" ? keys.Reverse().ToArray() : keys,
            copies = fault == "copies" ? new[] { copies[0], copies[0] } : copies, operations });
        var acks = copies.Select((id, index) => JsonSerializer.SerializeToElement(new { id,
            operation = fault == "operation" && index == 0 ? Guid.NewGuid() : operations[index],
            version = fault == "version" && index == 0 ? 2 : 1 })).ToList();
        if (fault == "missing-ack") acks.RemoveAt(1);
        if (fault == "valid") {
            var result = CqlChainflipFixtureContinuation.Validate(journal, acks, keys);
            Assert.Equal(actor, result.Actor); Assert.Equal(copies, result.Copies); Assert.Equal(operations, result.Operations);
        } else Assert.Throws<InvalidDataException>(() => CqlChainflipFixtureContinuation.Validate(journal, acks, keys));
    }
}
