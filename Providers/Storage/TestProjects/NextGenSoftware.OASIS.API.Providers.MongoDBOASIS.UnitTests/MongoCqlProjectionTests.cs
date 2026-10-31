using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Repositories;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.UnitTests;

[TestClass]
public class MongoCqlProjectionTests
{
    [TestMethod]
    public void ProjectionInputWindowIsNotReducedByGraphOutputBudget()
    {
        var query = CosmicQuery<Holon>.Create().WithChildren(CosmicQueryGraphOptions.Create(
            CosmicQueryGraphConsistency.ProviderSnapshot, maximumNodes: 1).Result)
            .OrderBy(h => h.Name).Take(137).Select(h => h.Name).Result;
        var command = new MongoCqlTranslator().CompileProjection(query, 200);
        Assert.IsTrue(command.RequiresSnapshot); Assert.IsFalse(command.IsEmpty);
        Assert.AreEqual(137, command.Pipeline.Single(stage => stage.Contains("$limit"))["$limit"].AsInt32);
        var output = command.Pipeline.Last()["$project"].AsBsonDocument;
        Assert.IsTrue(output.Contains("cells")); Assert.IsFalse(output.Contains("row"));
    }

    [TestMethod]
    public void DescendantErrorBranchRemainsOutsideTheProjectionWindow()
    {
        var query = CosmicQuery<Holon>.Create().WhereAnyDescendant(h => h.Name == "absent",
            CosmicQueryTraversalOptions.Create(2, 1).Result).Skip(1).Take(1).Select(h => h.Name).Result;
        var command = new MongoCqlTranslator().CompileProjection(query, 20);
        Assert.IsTrue(command.HasTraversalEnvelope); Assert.IsTrue(command.RequiresMembership); Assert.IsTrue(command.RequiresSnapshot);
        var facet = command.Pipeline.Single(stage => stage.Contains("$facet"))["$facet"].AsBsonDocument;
        Assert.IsFalse(facet["invalid"].AsBsonArray.Any(stage => stage.AsBsonDocument.Contains("$skip")));
        Assert.IsTrue(facet["roots"].AsBsonArray.Any(stage => stage.AsBsonDocument.Contains("$limit")));
        Assert.IsTrue(facet["roots"].ToString().Contains("cells"));
    }

    [TestMethod]
    public void MetadataNamesRemainLiteralsAndArrayDictionaryDuplicatesAreNotCollapsed()
    {
        var query = CosmicQuery<Holon>.Create().Select(h => CosmicQueryMetadata.Value<string>(h, "$hostile.dot")).Result;
        var pipeline = new BsonArray(new MongoCqlTranslator().CompileProjection(query, 10).Pipeline).ToString();
        Assert.IsTrue(pipeline.Contains("$literal")); Assert.IsTrue(pipeline.Contains("$hostile.dot"));
        Assert.IsTrue(pipeline.Contains("$filter")); Assert.IsFalse(pipeline.Contains("$arrayToObject"));
    }

    [DataTestMethod]
    [DataRow(0L)] [DataRow(9007199254740993L)] [DataRow(long.MaxValue)]
    public void NativeCellsRetainExactInt64(long value)
    {
        var query = CosmicQuery<Holon>.Create().Select(h => CosmicQueryMetadata.Value<long>(h, "stock")).Result;
        var row = HolonRepository.ReadNativeCqlProjectionRow(new BsonDocument("cells", new BsonArray
        { new BsonDocument { { "s", 2 }, { "v", new BsonInt64(value) } } }), query.Columns);
        Assert.AreEqual(value, row.Values.Single());
    }

    [TestMethod]
    public void InvalidNativeStateDomainOrWidthCannotBecomeSuccessfulCells()
    {
        var query = CosmicQuery<Holon>.Create().Select(h => CosmicQueryMetadata.Value<long>(h, "stock")).Result;
        foreach (var value in new BsonValue[] { new BsonDouble(1), new BsonString("1"), BsonNull.Value })
            Assert.ThrowsException<InvalidDataException>(() => HolonRepository.ReadNativeCqlProjectionRow(
                new BsonDocument("cells", new BsonArray { new BsonDocument { { "s", 2 }, { "v", value } } }), query.Columns));
        foreach (var state in new[] { 0, 1, 3, -1 })
            Assert.ThrowsException<InvalidDataException>(() => HolonRepository.ReadNativeCqlProjectionRow(
                new BsonDocument("cells", new BsonArray { new BsonDocument { { "s", state }, { "v", BsonNull.Value } } }), query.Columns));
        Assert.ThrowsException<InvalidDataException>(() => HolonRepository.ReadNativeCqlProjectionRow(new BsonDocument("cells", new BsonArray()), query.Columns));
    }
    [TestMethod]
    public void DateProjectionAdmitsOnlyOneOwnedPrecisionCompanion()
    {
        var query = CosmicQuery<Holon>.Create().Select(h => CosmicQueryMetadata.Value<DateTime>(h, "instant")).Result;
        var pipeline = new BsonArray(new MongoCqlTranslator().CompileProjection(query, 10).Pipeline).ToString();
        Assert.IsTrue(pipeline.Contains("cqlprojectiondates"));
        Assert.IsTrue(pipeline.Contains("MetaDataExactUtcTicks"));
        Assert.IsTrue(pipeline.Contains("$lte"));
    }

}
