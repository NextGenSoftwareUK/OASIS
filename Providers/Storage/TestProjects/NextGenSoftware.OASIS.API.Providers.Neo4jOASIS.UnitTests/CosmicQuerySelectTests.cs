using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.UnitTests;

[TestClass]
public class CosmicQuerySelectTests
{
    [TestMethod]
    public void ProjectionColumnsBindPathsAndKeepMissingNullAndInvalidDistinct()
    {
        var query = CosmicQuery<Holon>.Create().Select(h => new { h.Name,
            Value = CosmicQueryMetadata.Value<long?>(h, "hostile.key") }).Result;
        var translator = new NeoCqlTranslator();
        var columns = translator.TranslateProjection(query.Columns);
        Assert.AreEqual(2, columns.Count);
        Assert.IsTrue(columns[1].State.Contains("THEN 0")); Assert.IsTrue(columns[1].State.Contains("THEN 1"));
        Assert.IsTrue(columns[1].State.Contains("ELSE 3"));
        Assert.IsTrue(translator.Parameters.Values.Any(value => value is string text && text.EndsWith("_kind")));
        Assert.IsFalse(columns[1].Value.Contains("hostile.key"));
    }

    [DataTestMethod]
    [DataRow(0L)] [DataRow(9007199254740993L)] [DataRow(long.MaxValue)]
    public void NativeProjectionCellsRetainExactInt64(long value)
    {
        var query = CosmicQuery<Holon>.Create().Select(h => CosmicQueryMetadata.Value<long>(h, "stock")).Result;
        var row = Neo4jOASIS.ReadNativeProjectionRow(new object[] { new Dictionary<string, object>
        { ["s"] = 2L, ["v"] = value } }, query.Columns);
        Assert.AreEqual(value, row.Values.Single());
    }

    [TestMethod]
    public void InvalidNativeCellDomainsDoNotBecomeSuccessfulValues()
    {
        var query = CosmicQuery<Holon>.Create().Select(h => CosmicQueryMetadata.Value<long>(h, "stock")).Result;
        foreach (var value in new object[] { "7", 7d, true })
            Assert.ThrowsException<InvalidDataException>(() => Neo4jOASIS.ReadNativeProjectionRow(new object[]
            { new Dictionary<string, object> { ["s"] = 2L, ["v"] = value } }, query.Columns));
        foreach (var state in new long[] { 0, 1, 3, -1 })
            Assert.ThrowsException<InvalidDataException>(() => Neo4jOASIS.ReadNativeProjectionRow(new object[]
            { new Dictionary<string, object> { ["s"] = state, ["v"] = null! } }, query.Columns));
    }

    [TestMethod]
    public void NullableCellsMapMissingAndNullWithoutInventingValues()
    {
        var query = CosmicQuery<Holon>.Create().Select(h => CosmicQueryMetadata.Value<long?>(h, "stock")).Result;
        foreach (var state in new long[] { 0, 1 })
        {
            var row = Neo4jOASIS.ReadNativeProjectionRow(new object[]
            { new Dictionary<string, object> { ["s"] = state, ["v"] = null! } }, query.Columns);
            Assert.IsNull(row.Values.Single());
        }
    }
}
