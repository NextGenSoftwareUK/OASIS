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

    [TestMethod]
    public void MetadataStringDomainExcludesFramedNumericPaths()
    {
        var query = CosmicQuery<Holon>.Create().Where(h => CosmicQueryMetadata.Value<string>(h, "precise") == "9007199254740993.125");
        Assert.IsFalse(query.IsError, query.Message);
        var sql = new CosmosCqlPredicateSql().Translate(query.Result.Predicates.Single());
        StringAssert.Contains(sql, "NOT EXISTS");
        StringAssert.Contains(sql, "_oasis_exact_numbers");
        StringAssert.Contains(sql, "ARRAY_LENGTH");
    }

    [TestMethod]
    public void MetadataNullIsExplicitAndDoesNotAdmitMissing()
    {
        var query = CosmicQuery<Holon>.Create().Where(h => CosmicQueryMetadata.Value<string>(h, "category") == null);
        Assert.IsFalse(query.IsError, query.Message);
        var sql = new CosmosCqlPredicateSql().Translate(query.Result.Predicates.Single());
        StringAssert.Contains(sql, "IS_DEFINED");
        StringAssert.Contains(sql, "IS_NULL");
        Assert.IsFalse(sql.Contains("NOT IS_DEFINED"));
    }

    [TestMethod]
    public void MetadataSegmentsAreEscapedAsJsonIdentifiers()
    {
        const string segment = "x\"] OR true --";
        var query = CosmicQuery<Holon>.Create().Where(h => CosmicQueryMetadata.Exists(h, segment));
        Assert.IsFalse(query.IsError, query.Message);
        var sql = new CosmosCqlPredicateSql().Translate(query.Result.Predicates.Single());
        StringAssert.Contains(sql, Newtonsoft.Json.JsonConvert.SerializeObject(segment));
    }

    [TestMethod]
    public void AuthoritativeCatalogueBindsCanonicalGuidArray()
    {
        var id = Guid.NewGuid();
        var access = CosmicQueryReadAccess.AuthoritativeRecords(Guid.NewGuid(),
            NextGenSoftware.OASIS.API.Core.Enums.ProviderType.AzureCosmosDBOASIS, new[] { id });
        Assert.IsFalse(access.IsError, access.Message);
        var query = CosmicQuery<Holon>.Create().WithReadAccess(access.Result);
        Assert.IsFalse(query.IsError, query.Message);
        var translator = new CosmosCqlPredicateSql();
        var sql = translator.Translate(query.Result.Predicates.Single());
        StringAssert.Contains(sql, "ARRAY_CONTAINS");
        var command = translator.BuildCommand("SELECT * FROM c WHERE " + sql);
        CollectionAssert.AreEqual(new object[] { id.ToString("D") },
            (object[])command.GetQueryParameters().Single().Value);
    }

    [TestMethod]
    public void CanonicalStringMembershipPreservesNullAndAbsentSemantics()
    {
        var names = new string?[] { "coffee", null };
        var query = CosmicQuery<Holon>.Create().Where(h => names.Contains(h.Description));
        Assert.IsFalse(query.IsError, query.Message);
        var sql = new CosmosCqlPredicateSql().Translate(query.Result.Predicates.Single());
        StringAssert.Contains(sql, "ARRAY_CONTAINS");
        StringAssert.Contains(sql, "NOT IS_DEFINED");
        StringAssert.Contains(sql, "IS_NULL");
    }

    [TestMethod]
    public void MetadataMembershipRemainsAnIndependentUnsupportedCapability()
    {
        var names = new[] { "coffee", "tea" };
        var query = CosmicQuery<Holon>.Create().Where(h => names.Contains(CosmicQueryMetadata.Value<string>(h, "category")));
        Assert.IsFalse(query.IsError, query.Message);
        Assert.IsTrue(AzureCosmosDBOASIS.CqlCapabilities.Admit(query.Result).IsError);
    }
}
