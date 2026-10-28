using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;

namespace NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.UnitTests;

/// <summary>The Cosmos native reader's document conversion, paging query and container allowlist, without a live account.</summary>
[TestClass]
public class CosmosNativeDataImportTests
{
    private static AzureCosmosDBOASIS Provider(string? nativeDatabase, string? containers, string oasisDatabase = "oasis") =>
        new AzureCosmosDBOASIS(new Uri("https://localhost:8081"), "key", oasisDatabase,
            new List<string> { "avatarItems", "avatarDetailItems", "holonItems" })
        { NativeImportDatabaseName = nativeDatabase, NativeImportContainers = containers };

    [TestMethod]
    public void DocumentsBecomeRecordsKeyedByIdWithScalarFields()
    {
        var document = JObject.Parse(@"{
            'id': 'c-1', 'name': 'Ada', 'age': 36, 'rating': 4.5, 'vip': true, 'note': null,
            'address': { 'city': 'London' }, 'tags': ['a','b'],
            '_rid': 'x', '_self': 'y', '_etag': 'z', '_attachments': 'a', '_ts': 1 }");

        var record = AzureCosmosDBOASIS.ToNativeRecord("customers", document);

        Assert.AreEqual("customers", record.SourceName);
        Assert.AreEqual("c-1", record.NativeKey);
        Assert.AreEqual("Ada", record.Fields["name"]);
        Assert.AreEqual(36L, record.Fields["age"]);
        Assert.AreEqual(4.5, record.Fields["rating"]);
        Assert.AreEqual(true, record.Fields["vip"]);
        Assert.IsNull(record.Fields["note"]);
        Assert.AreEqual("{\"city\":\"London\"}", record.Fields["address"]);
        Assert.AreEqual("[\"a\",\"b\"]", record.Fields["tags"]);
        Assert.IsFalse(record.Fields.Keys.Any(key => key.StartsWith("_")));
    }

    [TestMethod]
    public void DocumentWithoutAStringIdHasNoStableKeyAndIsRefused()
    {
        Assert.ThrowsException<InvalidOperationException>(() => AzureCosmosDBOASIS.ToNativeRecord("customers", JObject.Parse("{'name':'x'}")));
        Assert.ThrowsException<InvalidOperationException>(() => AzureCosmosDBOASIS.ToNativeRecord("customers", JObject.Parse("{'id':7}")));
    }

    [TestMethod]
    public void PageQueryOrdersByIdAndResumesAfterTheLastKey()
    {
        var first = AzureCosmosDBOASIS.NativePageQuery(null, 10);
        StringAssert.Contains(first.QueryText, "ORDER BY c.id");
        Assert.IsFalse(first.QueryText.Contains("@after"));
        Assert.AreEqual(11, first.GetQueryParameters().Single(parameter => parameter.Name == "@take").Value);

        var next = AzureCosmosDBOASIS.NativePageQuery("c-9", 10);
        StringAssert.Contains(next.QueryText, "c.id > @after");
        Assert.AreEqual("c-9", next.GetQueryParameters().Single(parameter => parameter.Name == "@after").Value);
    }

    [TestMethod]
    public async Task UnconfiguredAndOasisOwnedSourcesAreRefusedBeforeAnyRead()
    {
        Assert.AreEqual("NATIVE_IMPORT_UNAVAILABLE", (await Provider(null, "customers").ListNativeDataSourcesAsync()).ErrorCode);
        Assert.AreEqual("NATIVE_IMPORT_UNAVAILABLE", (await Provider("shop", null).ListNativeDataSourcesAsync()).ErrorCode);

        var own = await Provider("oasis", "customers, holonItems").ListNativeDataSourcesAsync();
        Assert.AreEqual("NATIVE_IMPORT_UNAVAILABLE", own.ErrorCode);
        StringAssert.Contains(own.Message, "holonItems");

        var notListed = await Provider("shop", "customers").ReadNativeRecordsAsync("orders", null, 5);
        Assert.AreEqual("NATIVE_IMPORT_READ_FAILED", notListed.ErrorCode);
    }
}
