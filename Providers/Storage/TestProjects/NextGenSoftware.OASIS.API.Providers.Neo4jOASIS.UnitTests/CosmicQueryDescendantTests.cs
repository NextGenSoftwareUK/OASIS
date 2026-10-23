using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.UnitTests;

[TestClass]
public class CosmicQueryDescendantTests
{
    [TestMethod]
    public void NativeScopeUsesValidatedDepthAndBoundedDistinctStates()
    {
        var query = CosmicQuery<Holon>.Create().WhereAnyDescendant(child => child.Name == "$target",
            CosmicQueryTraversalOptions.Create(3, 25).Result);
        var translator = new NeoCqlTranslator();
        var condition = translator.Translate(query.Result.Predicates.Single());
        StringAssert.Contains(condition, ".matched");
        var native = translator.TraversalClauses.Single();
        StringAssert.Contains(native, "{1,3}(descendant:Holon)");
        StringAssert.Contains(native, "WHERE EXISTS { MATCH (hopParent");
        StringAssert.Contains(native, ") MATCH (hopChild");
        StringAssert.Contains(native, "WITH DISTINCT descendant");
        StringAssert.Contains(native, "LIMIT $");
        StringAssert.Contains(native, ".f_ParentHolonId=");
        Assert.IsTrue(translator.Parameters.Values.Contains(26));
        Assert.IsTrue(translator.Parameters.Values.Contains("$target"));
        Assert.AreEqual(1, translator.TraversalCycles.Count);
        Assert.AreEqual(1, translator.TraversalLimits.Count);
    }

    [TestMethod]
    public void DescendantCapabilityDoesNotPromiseNeoSnapshotIsolation()
    {
        var profile = Neo4jOASIS.CqlCapabilities;
        Assert.AreEqual(32, profile.Traversal.MaximumDepth);
        Assert.AreEqual(10000, profile.Traversal.MaximumNodes);
        Assert.IsFalse(profile.Features.HasFlag(CosmicQueryFeatures.GraphSnapshot));
    }
}
