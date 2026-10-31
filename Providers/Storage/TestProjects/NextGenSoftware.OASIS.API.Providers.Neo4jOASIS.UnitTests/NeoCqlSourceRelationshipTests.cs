using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.UnitTests;

[TestClass]
public sealed class NeoCqlSourceRelationshipTests
{
    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void CompoundSourceFilterFollowsProtectedTraversalState(bool conjunction)
    {
        var id = Guid.NewGuid();
        var bounds = CosmicQueryTraversalOptions.Create(2, 1).Result;
        var source = CosmicQuery<Holon>.Create().Where(row => row.Id == id).Result;
        var query = conjunction
            ? source.Where(row => row.Name == "business filter" && CosmicQueryRelations.AnyDescendant(row, child => true, bounds))
            : source.Where(row => row.Name == "business filter" || CosmicQueryRelations.AnyDescendant(row, child => true, bounds));
        Assert.IsFalse(query.IsError, query.Message);

        var compile = typeof(Neo4jOASIS).GetMethod("CompileCqlSource", BindingFlags.NonPublic | BindingFlags.Static)!;
        var compiled = compile.MakeGenericMethod(typeof(Holon)).Invoke(null, new object?[] { query.Result, null })!;
        string Field(string name) => (string)compiled.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(compiled)!;
        var eligible = Field("EligibleSource");
        var filter = Field("DescendantPredicate");
        var call = eligible.IndexOf("CALL {", StringComparison.Ordinal);
        Assert.IsTrue(call > 0, "The protected scope must be evaluated before the business filter.");
        Assert.IsFalse(eligible[..call].Contains(".matched"), "Traversal state cannot be referenced before it is produced.");
        StringAssert.Contains(filter, ".matched");
        Assert.AreNotEqual("false", Field("Cycle"));
        Assert.AreNotEqual("false", Field("TraversalLimit"));
    }
}
