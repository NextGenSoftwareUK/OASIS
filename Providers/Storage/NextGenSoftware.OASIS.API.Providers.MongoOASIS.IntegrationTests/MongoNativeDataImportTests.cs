using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using MongoProvider = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.MongoOASIS.IntegrationTests;

public sealed class MongoNativeDataImportTests
{
    [Fact]
    public void BsonValuesBecomeNativeRecordScalars()
    {
        var id = ObjectId.GenerateNewId();
        var uuid = Guid.NewGuid();
        var when = new DateTime(2026, 10, 27, 12, 0, 0, DateTimeKind.Utc);
        var record = MongoProvider.ToNativeRecord("people", new BsonDocument
        {
            { "_id", id }, { "name", "Ada" }, { "age", 36 }, { "big", 5L }, { "ratio", 0.5 }, { "price", new BsonDecimal128(1.25m) },
            { "active", true }, { "joined", when }, { "owner", ObjectId.Parse("64b7f0c2a1b2c3d4e5f60718") },
            { "uuid", new BsonBinaryData(uuid, GuidRepresentation.Standard) }, { "missing", BsonNull.Value },
            { "tags", new BsonArray { "a", "b" } }, { "address", new BsonDocument("city", "London") }
        });

        Assert.Equal(id.ToJson(new MongoDB.Bson.IO.JsonWriterSettings { OutputMode = MongoDB.Bson.IO.JsonOutputMode.CanonicalExtendedJson }), record.NativeKey);
        Assert.False(record.Fields.ContainsKey("_id"));
        Assert.Equal("Ada", record.Fields["name"]);
        Assert.Equal(36, record.Fields["age"]);
        Assert.Equal(5L, record.Fields["big"]);
        Assert.Equal(0.5, record.Fields["ratio"]);
        Assert.Equal(1.25m, record.Fields["price"]);
        Assert.Equal(true, record.Fields["active"]);
        Assert.Equal(when, record.Fields["joined"]);
        Assert.Equal("64b7f0c2a1b2c3d4e5f60718", record.Fields["owner"]);
        Assert.Equal(uuid, record.Fields["uuid"]);
        Assert.Null(record.Fields["missing"]);
        Assert.Equal("[\"a\", \"b\"]", record.Fields["tags"]);
        Assert.Equal("{ \"city\" : \"London\" }", record.Fields["address"]);
        Assert.All(record.Fields.Values, value => Assert.True(NativeRecord.IsSupportedValue(value)));
    }

    [Fact]
    public async Task ListsAndPagesEveryDocumentIncludingMixedIdTypes()
    {
        var connection = Environment.GetEnvironmentVariable("OASIS_MONGO_NATIVE_IMPORT_CONNECTION");
        Assert.False(string.IsNullOrWhiteSpace(connection), "Set OASIS_MONGO_NATIVE_IMPORT_CONNECTION to a disposable Mongo deployment.");

        var nativeDatabase = "oasis_native_import_test_" + Guid.NewGuid().ToString("N");
        var provider = new MongoProvider(connection, "oasis_native_import_owner_" + Guid.NewGuid().ToString("N"));
        var client = new MongoClient(connection);
        try
        {
            var people = client.GetDatabase(nativeDatabase).GetCollection<BsonDocument>("people");
            await people.InsertManyAsync(new[]
            {
                new BsonDocument { { "_id", 3 }, { "name", "Three" } },
                new BsonDocument { { "_id", "b" }, { "name", "Bee" } },
                new BsonDocument { { "_id", ObjectId.GenerateNewId() }, { "name", "Oid" } },
                new BsonDocument { { "_id", 1 }, { "name", "One" } },
                new BsonDocument { { "_id", "a" }, { "name", "Ay" } }
            });
            await client.GetDatabase(nativeDatabase).GetCollection<BsonDocument>("orders").InsertOneAsync(new BsonDocument("total", 9));
            Assert.False((await provider.ActivateProviderAsync()).IsError);

            provider.NativeImportDatabaseName = provider.DBName;
            Assert.Equal("NATIVE_IMPORT_UNAVAILABLE", (await provider.ListNativeDataSourcesAsync()).ErrorCode);

            provider.NativeImportDatabaseName = nativeDatabase;
            var sources = await provider.ListNativeDataSourcesAsync();
            Assert.False(sources.IsError, sources.Message);
            Assert.Equal(new[] { "orders", "people" }, sources.Result.Select(source => source.Name));

            var names = new List<string>();
            string token = null;
            var pages = 0;
            do
            {
                var page = await provider.ReadNativeRecordsAsync("people", token, 2);
                Assert.False(page.IsError, page.Message);
                names.AddRange(page.Result.Records.Select(record => (string)record.Fields["name"]));
                token = page.Result.ContinuationToken;
                pages++;
            }
            while (token != null);

            Assert.Equal(3, pages);
            Assert.Equal(new[] { "One", "Three", "Ay", "Bee", "Oid" }, names);
        }
        finally
        {
            await client.DropDatabaseAsync(nativeDatabase);
            await client.DropDatabaseAsync(provider.DBName);
        }
    }
}
