using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.UnitTests;

[TestClass]
public class CosmicQueryTranslationTests
{
    [TestMethod]
    public void MetadataKeysAndValuesRemainBoundParameters()
    {
        const string hostile = "x']) DETACH DELETE h //";
        var built = CosmicQuery<Holon>.Create().Where(h => CosmicQueryMetadata.Value<string>(h, hostile).Contains(hostile, StringComparison.Ordinal));
        Assert.IsFalse(built.IsError, built.Message);
        Assert.IsFalse(NeoCqlContract.Capabilities.Admit(built.Result).IsError);
        var translator = new NeoCqlTranslator();
        var query = translator.Translate(built.Result.Predicates.Single());
        Assert.IsFalse(query.Contains(hostile, StringComparison.Ordinal));
        Assert.IsTrue(translator.Parameters.Values.Contains(hostile));
        Assert.IsTrue(translator.Parameters.Values.Contains(NeoCqlProjection.PathKey(new[] { hostile }) + "_kind"));
    }

    [TestMethod]
    public void NullableInequalityAndNegationUseTotalPredicatesWithExplicitNull()
    {
        var built = CosmicQuery<Holon>.Create().Where(h => !(CosmicQueryMetadata.Value<int?>(h, "score") != 7));
        Assert.IsFalse(built.IsError, built.Message);
        Assert.IsFalse(NeoCqlContract.Capabilities.Admit(built.Result).IsError);
        var query = new NeoCqlTranslator().Translate(built.Result.Predicates.Single());
        StringAssert.Contains(query, "NOT coalesce");
        StringAssert.Contains(query, "='null'");
        StringAssert.Contains(query, "='integer'");
        StringAssert.Contains(query, ">=-2147483648");
    }

    [TestMethod]
    public void UnsupportedDomainsRejectDuringSharedAdmission()
    {
        var built = CosmicQuery<Holon>.Create().Where(h => CosmicQueryMetadata.Value<decimal>(h, "amount") > 1m);
        Assert.IsFalse(built.IsError, built.Message);
        Assert.IsTrue(NeoCqlContract.Capabilities.Admit(built.Result).IsError);
    }
}
