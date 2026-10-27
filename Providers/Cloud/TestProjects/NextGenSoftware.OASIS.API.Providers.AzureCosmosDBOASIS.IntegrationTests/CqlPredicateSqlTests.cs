using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.Infrastructure;

namespace NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.IntegrationTests;

[TestClass]
public class CqlPredicateSqlTests
{
    [TestMethod]
    public void CallerTextIsBoundAsParameter()
    {
        const string text = "coffee\" OR true --";
        var query = CosmicQuery<Holon>.Create().Where(h => h.Name == text);
        Assert.IsFalse(query.IsError, query.Message);
        var translator = new CosmosCqlPredicateSql();
        var sql = translator.Translate(query.Result.Predicates.Single());
        Assert.IsFalse(sql.Contains(text));
        var command = translator.BuildCommand("SELECT * FROM c WHERE " + sql);
        Assert.AreEqual(text, command.GetQueryParameters().Single().Value);
    }

    [TestMethod]
    public void StringInequalityNormalizesMissingBeforeNegation()
    {
        var query = CosmicQuery<Holon>.Create().Where(h => h.Name != "coffee");
        Assert.IsFalse(query.IsError, query.Message);
        var sql = new CosmosCqlPredicateSql().Translate(query.Result.Predicates.Single());
        StringAssert.StartsWith(sql, "(NOT IIF(IS_STRING(");
        StringAssert.EndsWith(sql, ", false))");
    }

    [TestMethod]
    public void NullStringIncludesUndefinedAndExplicitNull()
    {
        var query = CosmicQuery<Holon>.Create().Where(h => h.Name == null);
        Assert.IsFalse(query.IsError, query.Message);
        var sql = new CosmosCqlPredicateSql().Translate(query.Result.Predicates.Single());
        StringAssert.Contains(sql, "NOT IS_DEFINED");
        StringAssert.Contains(sql, "IS_NULL");
    }

    [TestMethod]
    public void NumericMetadataCannotBeComparedAsWireString()
    {
        var query = CosmicQuery<Holon>.Create().Where(h => CosmicQueryMetadata.Value<decimal>(h, "price") > 1m);
        Assert.IsFalse(query.IsError, query.Message);
        Assert.ThrowsException<NotSupportedException>(() => new CosmosCqlPredicateSql().Translate(query.Result.Predicates.Single()));
    }
}
