using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Repositories;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.UnitTests;

[TestClass]
public class MongoCqlScalarTests
{
    [TestMethod]
    public void NativeCountHasNoRowBufferSortOrPublicationLimitAndRetainsSnapshotIntent()
    {
        var graph = CosmicQueryGraphOptions.Create(CosmicQueryGraphConsistency.ProviderSnapshot, maximumNodes: 1).Result;
        var source = CosmicQuery<Holon>.Create().WithChildren(graph).OrderBy(row => row.Name).Skip(2).Take(4).Result;
        var command = new MongoCqlTranslator().CompileCount(source);
        Assert.IsTrue(command.RequiresSnapshot); Assert.IsFalse(command.RequiresMembership); Assert.IsFalse(command.IsEmpty);
        Assert.IsFalse(command.Pipeline.Any(stage => stage.Contains("$sort") || stage.Contains("$skip")
            || stage.Contains("$limit") || stage.Contains("$facet")));
        var group = command.Pipeline.Single(stage => stage.Contains("$group"))["$group"];
        Assert.AreEqual(1L, group["count"]["$sum"]["$literal"].AsInt64);
        var window = command.Pipeline.Last()["$project"]["count"]["$min"].AsBsonArray;
        Assert.AreEqual(4L, window[1]["$literal"].AsInt64);
        Assert.AreEqual(2L, window[0]["$max"][1]["$subtract"][1]["$literal"].AsInt64);
    }

    [TestMethod]
    public void DescendantErrorsAreReducedBeforeTheScalarWindowEvenForExistence()
    {
        var source = CosmicQuery<Holon>.Create()
            .WhereAnyDescendant(child => child.Name == "absent", CosmicQueryTraversalOptions.Create(2, 1).Result)
            .WhereAnyDescendant(child => child.Name == "present", CosmicQueryTraversalOptions.Create(1, 2).Result).Take(1).Result;
        var command = new MongoCqlTranslator().CompileCount(source);
        Assert.IsTrue(command.RequiresMembership); Assert.IsTrue(command.RequiresSnapshot);
        var groupIndex = Array.FindIndex(command.Pipeline, stage => stage.Contains("$group"));
        Assert.IsTrue(groupIndex > Array.FindLastIndex(command.Pipeline, stage => stage.Contains("$lookup")));
        var group = command.Pipeline[groupIndex]["$group"];
        Assert.IsTrue(group.AsBsonDocument.Contains("invalid")); Assert.IsTrue(group.AsBsonDocument.Contains("limited"));
        Assert.IsFalse(command.Pipeline.Any(stage => stage.Contains("$skip") || stage.Contains("$limit")));
        Assert.IsTrue(command.Pipeline.Last()["$project"].AsBsonDocument.Contains("error"));
    }

    [TestMethod]
    public void EmptyWindowIsExplicitAndDeepStageOverflowStillRejectsAtCompilation()
    {
        Assert.IsTrue(new MongoCqlTranslator().CompileCount(CosmicQuery<Holon>.Create().Take(0).Result).IsEmpty);
        var bounds = CosmicQueryTraversalOptions.Create(32, 10000).Result;
        var source = CosmicQuery<Holon>.Create().WhereAnyDescendant(child => true, bounds);
        for (var index = 1; index < 11; index++) source = source.WhereAnyDescendant(child => true, bounds);
        Assert.ThrowsException<MongoCqlLimitException>(() => new MongoCqlTranslator().CompileCount(source.Result));
    }

    [DataTestMethod]
    [DataRow(0L)] [DataRow(2147483648L)] [DataRow(9007199254740993L)] [DataRow(long.MaxValue)]
    public void NativeCountDecodingPreservesExactInt64Values(long value)
    {
        var document = new BsonDocument { { "error", 0 }, { "count", new BsonInt64(value) } };
        Assert.AreEqual(value, HolonRepository.ReadNativeCqlCount(document));
    }

    [TestMethod]
    public void NonIntegerNegativeOrMalformedCountEnvelopesCannotBecomeSuccessfulScalars()
    {
        foreach (var value in new BsonValue[] { new BsonDouble(9007199254740992d), new BsonDecimal128(1),
            new BsonString("1"), BsonNull.Value, new BsonInt64(-1) })
            Assert.ThrowsException<InvalidDataException>(() => HolonRepository.ReadNativeCqlCount(
                new BsonDocument { { "error", 0 }, { "count", value } }));
        Assert.ThrowsException<InvalidDataException>(() => HolonRepository.ReadNativeCqlCount(new BsonDocument("count", 1)));
        Assert.ThrowsException<InvalidDataException>(() => HolonRepository.ReadNativeCqlCount(
            new BsonDocument { { "error", 3 }, { "count", 1 } }));
        Assert.ThrowsException<InvalidDataException>(() => HolonRepository.ReadNativeCqlCount(
            new BsonDocument { { "error", 0 }, { "count", 1 }, { "row", new BsonDocument() } }));
    }

    [TestMethod]
    public void NativeTraversalErrorsCannotBePublishedAsAnEmptyCount()
    {
        Assert.ThrowsException<InvalidDataException>(() => HolonRepository.ReadNativeCqlCount(new BsonDocument { { "error", 1 }, { "count", 0L } }));
        Assert.ThrowsException<MongoCqlLimitException>(() => HolonRepository.ReadNativeCqlCount(new BsonDocument { { "error", 2 }, { "count", 0L } }));
    }
}
