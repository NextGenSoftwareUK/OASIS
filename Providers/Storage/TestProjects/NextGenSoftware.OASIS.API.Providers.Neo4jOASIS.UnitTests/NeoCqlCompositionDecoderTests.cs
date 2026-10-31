using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.UnitTests;

[TestClass]
public sealed class NeoCqlCompositionDecoderTests
{
    private static CosmicProjectionQuery<Holon, string> Query(int take = 1)
        => CosmicQuery<Holon>.Create().Select(h => h.Name).Take(take).Result;
    private static Dictionary<string, object> Source() => new()
    {
        { "id", "10000000-0000-0000-0000-000000000001" }, { "creator", Guid.Empty.ToString("D") },
        { "visible", true }, { "valid", true }, { "domainValid", true }, { "cycle", false },
        { "traversalLimit", false }, { "payloadLimit", false }, { "bytes", 100L },
        { "keys", Array.Empty<object>() }, { "ordering", Array.Empty<object>() },
        { "values", new object[] { new Dictionary<string, object> { { "s", 2L }, { "v", "coffee" } } } }
    };
    private static Dictionary<string, object>[] Records() => new[]
    {
        Record(-2, -1, -1), Record(-1, 0, 0, Source()), Record(0, -1, -1), Record(0, 0, 0)
    };
    private static Dictionary<string, object> Record(long phase, long position, long source, object? proof = null) => new()
    {
        { "phase", phase }, { "position", position }, { "sourcePosition", source },
        { "proof", proof! }, { "nativeCount", 1L }, { "stageWork", 1L }
    };
    private static Neo4jOASIS.NeoCqlCompositionDecoder<Holon, string> Decoder(int[]? limits = null, Action? check = null, int take = 1)
    {
        limits ??= new[] { 10, 10000, 100, 10000, 10, 10000 };
        return new(Query(take), new CosmicProjectionCompositionExecutionLimits(
            limits[0], limits[1], limits[2], limits[3], limits[4], limits[5]), check ?? (() => { }));
    }

    [TestMethod]
    public void CompleteProofPassesTheSharedCoreConstructionBarrier()
    {
        var decoder = Decoder(); foreach (var record in Records()) decoder.Add(record);
        var result = CosmicProjectionCompositionResults.Materialize(Query(), decoder.Complete());
        Assert.IsFalse(result.IsError, result.Message); Assert.AreEqual("coffee", result.Result.Single());
    }

    [DataTestMethod]
    [DataRow(0)] [DataRow(1)] [DataRow(2)] [DataRow(3)] [DataRow(4)] [DataRow(5)]
    [DataRow(6)] [DataRow(7)] [DataRow(8)] [DataRow(9)] [DataRow(10)]
    public void MalformedOrIncompleteStreamsCannotReturnPackets(int mutation)
    {
        var records = Records(); var decoder = Decoder();
        if (mutation == 0) records[0]["nativeCount"] = 1; // Int32 is not the wire Int64 contract.
        if (mutation == 1) records[0]["stageWork"] = -1L;
        if (mutation == 2) ((Dictionary<string, object>)records[1]["proof"])["domainValid"] = false;
        if (mutation == 3) ((Dictionary<string, object>)records[1]["proof"])["values"] = new object[]
            { new Dictionary<string, object> { { "s", 2L }, { "v", 7L } } };
        if (mutation == 5) records[2]["nativeCount"] = 2L;
        if (mutation == 6) records[3]["sourcePosition"] = 9L;
        if (mutation == 7) records[3]["proof"] = Source();
        if (mutation == 8) records[3]["nativeCount"] = 0L;
        Assert.ThrowsException<InvalidDataException>(() =>
        {
            if (mutation == 4) { decoder.Add(records[0]); decoder.Add(records[2]); }
            else foreach (var record in records.Take(mutation == 10 ? 3 : 4)) decoder.Add(record);
            if (mutation == 9) decoder.Add(records[2]);
            decoder.Complete();
        });
    }

    [DataTestMethod]
    [DataRow(0)] [DataRow(1)] [DataRow(2)] [DataRow(3)] [DataRow(4)] [DataRow(5)]
    public void EveryRemainingBudgetIsEnforced(int exhausted)
    {
        var limits = new[] { 10, 10000, 100, 10000, 10, 10000 }; limits[exhausted] = 0;
        var decoder = Decoder(limits);
        Exception? failure = null;
        try
        {
            foreach (var record in Records()) decoder.Add(record);
            decoder.Complete();
        }
        catch (Exception cause) { failure = cause; }
        Assert.IsNotNull(failure);
        Assert.AreEqual(typeof(Neo4jOASIS).FullName + "+NeoCqlLimitException", failure.GetType().FullName);
    }

    [TestMethod]
    public void CoreRejectsStructurallyValidButFalseNativeStageMembership()
    {
        var decoder = Decoder(take: 0); foreach (var record in Records()) decoder.Add(record);
        var result = CosmicProjectionCompositionResults.Materialize(Query(0), decoder.Complete());
        Assert.IsTrue(result.IsError); Assert.IsNull(result.Result);
    }

    [TestMethod]
    public void CancellationBeforePublicationRejectsCompleteBufferedProof()
    {
        using var cancellation = new CancellationTokenSource();
        var decoder = Decoder(check: cancellation.Token.ThrowIfCancellationRequested);
        foreach (var record in Records()) decoder.Add(record);
        cancellation.Cancel();
        Assert.ThrowsException<OperationCanceledException>(() => decoder.Complete());
    }
}
