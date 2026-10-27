using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;

/// <summary>
/// Reads documents other applications stored on the same Mongo deployment so NativeDataImportManager can holonize them.
/// Native data is read from <see cref="NativeImportDatabaseName"/>, which must not be the OASIS database: OASIS's own
/// collections are never treated as foreign data, without maintaining a list of them here.
/// </summary>
public partial class MongoDBOASIS : INativeDataImportProvider
{
    private static readonly JsonWriterSettings CanonicalJson = new() { OutputMode = JsonOutputMode.CanonicalExtendedJson };
    private static readonly JsonWriterSettings ReadableJson = new() { OutputMode = JsonOutputMode.RelaxedExtendedJson };

    /// <summary>The database holding the application data to import, on the same deployment as <see cref="DBName"/>.</summary>
    public string NativeImportDatabaseName { get; set; }

    public ProviderType NativeDataProviderType => API.Core.Enums.ProviderType.MongoDBOASIS;

    public async Task<OASISResult<IReadOnlyList<NativeDataSource>>> ListNativeDataSourcesAsync(CancellationToken cancellationToken = default)
    {
        var result = new OASISResult<IReadOnlyList<NativeDataSource>>();
        var database = NativeImportDatabase(out var error);
        if (database == null) return Refuse(result, error);

        try
        {
            var names = await (await database.ListCollectionNamesAsync(cancellationToken: cancellationToken).ConfigureAwait(false))
                .ToListAsync(cancellationToken).ConfigureAwait(false);
            var sources = new List<NativeDataSource>();
            foreach (var name in names.Where(name => !name.StartsWith("system.", StringComparison.Ordinal)).OrderBy(name => name, StringComparer.Ordinal))
            {
                var count = await database.GetCollection<BsonDocument>(name)
                    .EstimatedDocumentCountAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
                sources.Add(new NativeDataSource(name, "collection", count));
            }
            result.Result = sources;
            result.Message = $"{sources.Count} collections in native database '{database.DatabaseNamespace.DatabaseName}'.";
        }
        catch (MongoException cause)
        {
            OASISErrorHandling.HandleError(ref result, $"Listing Mongo native collections failed: {cause.Message}", cause);
            result.ErrorCode = "NATIVE_IMPORT_LIST_FAILED";
        }
        return result;
    }

    public async Task<OASISResult<NativeRecordPage>> ReadNativeRecordsAsync(string sourceName, string continuationToken, int pageSize,
        CancellationToken cancellationToken = default)
    {
        var result = new OASISResult<NativeRecordPage>();
        var database = NativeImportDatabase(out var error);
        if (database == null) return Refuse(result, error);
        if (string.IsNullOrWhiteSpace(sourceName) || sourceName.StartsWith("system.", StringComparison.Ordinal))
            return Refuse(result, $"'{sourceName}' is not an importable Mongo collection.");
        if (pageSize < 1)
            return Refuse(result, "Page size must be at least 1.");

        try
        {
            // $expr compares across BSON types in sort order, unlike a plain $gt, so collections with mixed _id types page completely.
            var filter = continuationToken == null
                ? FilterDefinition<BsonDocument>.Empty
                : new BsonDocument("$expr", new BsonDocument("$gt", new BsonArray { "$_id", ParseToken(continuationToken) }));
            var documents = await database.GetCollection<BsonDocument>(sourceName).Find(filter)
                .Sort(Builders<BsonDocument>.Sort.Ascending("_id")).Limit(pageSize + 1)
                .ToListAsync(cancellationToken).ConfigureAwait(false);

            var more = documents.Count > pageSize;
            var page = documents.Take(pageSize).Select(document => ToNativeRecord(sourceName, document)).ToList();
            var next = more ? documents[pageSize - 1]["_id"].ToJson(CanonicalJson) : null;
            result.Result = new NativeRecordPage(page, next);
        }
        catch (Exception cause) when (cause is MongoException or FormatException)
        {
            OASISErrorHandling.HandleError(ref result, $"Reading Mongo collection '{sourceName}' failed: {cause.Message}", cause);
            result.ErrorCode = "NATIVE_IMPORT_READ_FAILED";
        }
        return result;
    }

    internal static NativeRecord ToNativeRecord(string sourceName, BsonDocument document)
    {
        if (!document.TryGetValue("_id", out var id))
            throw new FormatException($"A document in '{sourceName}' has no _id.");

        var fields = new Dictionary<string, object>(StringComparer.Ordinal);
        foreach (var element in document.Elements.Where(element => element.Name != "_id"))
            fields[element.Name] = ToScalar(element.Value);
        return new NativeRecord(sourceName, id.ToJson(CanonicalJson), fields);
    }

    internal static object ToScalar(BsonValue value) => value.BsonType switch
    {
        BsonType.Null or BsonType.Undefined => null,
        BsonType.String => value.AsString,
        BsonType.Boolean => value.AsBoolean,
        BsonType.Int32 => value.AsInt32,
        BsonType.Int64 => value.AsInt64,
        BsonType.Double => value.AsDouble,
        BsonType.Decimal128 => (decimal)value.AsDecimal128,
        BsonType.DateTime => value.ToUniversalTime(),
        BsonType.ObjectId => value.AsObjectId.ToString(),
        BsonType.Binary when value.AsBsonBinaryData.SubType == BsonBinarySubType.UuidStandard
            => value.AsBsonBinaryData.ToGuid(GuidRepresentation.Standard),
        _ => value.ToJson(ReadableJson)
    };

    private static BsonValue ParseToken(string token) => BsonDocument.Parse("{\"v\":" + token + "}")["v"];

    private IMongoDatabase NativeImportDatabase(out string error)
    {
        error = null;
        if (string.IsNullOrWhiteSpace(NativeImportDatabaseName))
            error = "Set NativeImportDatabaseName to the Mongo database holding the data to import.";
        else if (string.Equals(NativeImportDatabaseName, DBName, StringComparison.Ordinal))
            error = $"'{NativeImportDatabaseName}' is the OASIS database; native import reads application data from a different database.";
        else if (!IsProviderActivated || Database?.MongoClient == null)
            error = "The MongoDB provider must be activated before importing native data.";
        return error == null ? Database.MongoClient.GetDatabase(NativeImportDatabaseName) : null;
    }

    private static OASISResult<T> Refuse<T>(OASISResult<T> result, string message)
    {
        OASISErrorHandling.HandleError(ref result, message);
        result.ErrorCode = "NATIVE_IMPORT_UNAVAILABLE";
        return result;
    }
}
