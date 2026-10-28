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
/// Reads documents other applications keep in MongoDB so NativeDataImportManager can holonize them. Given a
/// <see cref="NativeConnection"/> (<c>uri</c> and <c>database</c>) it reads whatever the caller points it at; with none it
/// reads <see cref="NativeImportDatabaseName"/> on this deployment, which must not be the OASIS database.
/// </summary>
public partial class MongoDBOASIS : INativeDataImportProvider
{
    private static readonly JsonWriterSettings CanonicalJson = new() { OutputMode = JsonOutputMode.CanonicalExtendedJson };
    private static readonly JsonWriterSettings ReadableJson = new() { OutputMode = JsonOutputMode.RelaxedExtendedJson };
    private static readonly string[] ConnectionSettings = { "uri", "database" };
    private static readonly string[] ReservedDatabases = { "admin", "local", "config" };
    private static readonly TimeSpan NativeOperationLimit = TimeSpan.FromSeconds(30);

    /// <summary>The database holding the application data to import, on the same deployment as <see cref="DBName"/>.</summary>
    public string NativeImportDatabaseName { get; set; }

    public ProviderType NativeDataProviderType => API.Core.Enums.ProviderType.MongoDBOASIS;

    public string DescribeNativeConnection(NativeConnection connection)
    {
        if (connection == null) return null;
        var (url, database) = ParseConnection(connection);
        return "mongodb://" + string.Join(",", url.Servers.Select(server => server.ToString().ToLowerInvariant()).OrderBy(server => server, StringComparer.Ordinal)) + "/" + database;
    }

    public async Task<OASISResult<IReadOnlyList<NativeDataSource>>> ListNativeDataSourcesAsync(NativeConnection connection = null,
        CancellationToken cancellationToken = default)
    {
        var result = new OASISResult<IReadOnlyList<NativeDataSource>>();
        IDisposable lease = null;
        try
        {
            IMongoDatabase database;
            (database, lease) = await ResolveNativeDatabaseAsync(connection, cancellationToken).ConfigureAwait(false);
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
        catch (NativeConnectionRefusedException cause)
        {
            return Refuse(result, Redact(connection, cause.Message), "NATIVE_IMPORT_CONNECTION_REFUSED");
        }
        catch (InvalidOperationException cause)
        {
            return Refuse(result, cause.Message);
        }
        catch (MongoException cause)
        {
            OASISErrorHandling.HandleError(ref result, $"Listing Mongo native collections failed: {Redact(connection, cause.Message)}");
            result.ErrorCode = "NATIVE_IMPORT_LIST_FAILED";
        }
        finally { lease?.Dispose(); }
        return result;
    }

    public async Task<OASISResult<NativeRecordPage>> ReadNativeRecordsAsync(string sourceName, string continuationToken, int pageSize,
        NativeConnection connection = null, CancellationToken cancellationToken = default)
    {
        var result = new OASISResult<NativeRecordPage>();
        if (string.IsNullOrWhiteSpace(sourceName) || sourceName.StartsWith("system.", StringComparison.Ordinal))
            return Refuse(result, $"'{sourceName}' is not an importable Mongo collection.");
        if (pageSize < 1)
            return Refuse(result, "Page size must be at least 1.");

        IDisposable lease = null;
        try
        {
            IMongoDatabase database;
            (database, lease) = await ResolveNativeDatabaseAsync(connection, cancellationToken).ConfigureAwait(false);

            // $expr compares across BSON types in sort order, unlike a plain $gt, so collections with mixed _id types page completely.
            var filter = continuationToken == null
                ? FilterDefinition<BsonDocument>.Empty
                : new BsonDocument("$expr", new BsonDocument("$gt", new BsonArray { "$_id", ParseToken(continuationToken) }));
            var documents = await database.GetCollection<BsonDocument>(sourceName)
                .Find(filter, new FindOptions { MaxTime = NativeOperationLimit })
                .Sort(Builders<BsonDocument>.Sort.Ascending("_id")).Limit(pageSize + 1)
                .ToListAsync(cancellationToken).ConfigureAwait(false);

            var more = documents.Count > pageSize;
            var page = documents.Take(pageSize).Select(document => ToNativeRecord(sourceName, document)).ToList();
            var next = more ? documents[pageSize - 1]["_id"].ToJson(CanonicalJson) : null;
            result.Result = new NativeRecordPage(page, next);
        }
        catch (NativeConnectionRefusedException cause)
        {
            return Refuse(result, Redact(connection, cause.Message), "NATIVE_IMPORT_CONNECTION_REFUSED");
        }
        catch (InvalidOperationException cause)
        {
            return Refuse(result, cause.Message);
        }
        catch (Exception cause) when (cause is MongoException or FormatException)
        {
            OASISErrorHandling.HandleError(ref result, $"Reading Mongo collection '{sourceName}' failed: {Redact(connection, cause.Message)}");
            result.ErrorCode = "NATIVE_IMPORT_READ_FAILED";
        }
        finally { lease?.Dispose(); }
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

    /// <summary>The caller's database when a connection is given (a client opened for this call, released by the lease), otherwise the configured one.</summary>
    private async Task<(IMongoDatabase Database, IDisposable Lease)> ResolveNativeDatabaseAsync(NativeConnection connection, CancellationToken cancellationToken)
    {
        if (connection == null)
        {
            if (string.IsNullOrWhiteSpace(NativeImportDatabaseName))
                throw new InvalidOperationException("Set NativeImportDatabaseName to the Mongo database holding the data to import, or send a connection.");
            if (string.Equals(NativeImportDatabaseName, DBName, StringComparison.Ordinal))
                throw new InvalidOperationException($"'{NativeImportDatabaseName}' is the OASIS database; native import reads application data from a different database.");
            if (!IsProviderActivated || Database?.MongoClient == null)
                throw new InvalidOperationException("The MongoDB provider must be activated before importing native data.");
            return (Database.MongoClient.GetDatabase(NativeImportDatabaseName), null);
        }

        var (url, databaseName) = ParseConnection(connection);
        foreach (var server in url.Servers)
            await NativeConnectionGuard.EnsurePublicHostAsync(server.Host, cancellationToken).ConfigureAwait(false);

        var settings = MongoClientSettings.FromUrl(url);
        settings.ApplicationName = "OASIS-native-import";
        settings.ServerSelectionTimeout = TimeSpan.FromSeconds(10);
        settings.ConnectTimeout = TimeSpan.FromSeconds(10);
        settings.SocketTimeout = NativeOperationLimit;
        var client = new MongoClient(settings);
        return (client.GetDatabase(databaseName), client);
    }

    private static (MongoUrl Url, string Database) ParseConnection(NativeConnection connection)
    {
        connection.RequireOnly(ConnectionSettings);
        MongoUrl url;
        try { url = new MongoUrl(connection.Require("uri")); }
        catch (Exception cause) when (cause is MongoConfigurationException or FormatException or ArgumentException)
        { throw new NativeConnectionRefusedException("The MongoDB uri is not valid (expected mongodb://... or mongodb+srv://...)."); }

        var database = connection.Optional("database") ?? url.DatabaseName
            ?? throw new NativeConnectionRefusedException("Name the database to import: send 'database' or put it in the uri.");
        if (ReservedDatabases.Contains(database, StringComparer.OrdinalIgnoreCase))
            throw new NativeConnectionRefusedException($"'{database}' is a MongoDB system database and cannot be imported.");
        return (url, database);
    }

    private static string Redact(NativeConnection connection, string message)
    {
        if (connection == null) return message;
        message = connection.Redact(message, "uri");
        try
        {
            var password = new MongoUrl(connection.Optional("uri") ?? "mongodb://x").Password;
            if (!string.IsNullOrEmpty(password) && password.Length >= 3) message = message.Replace(password, "***", StringComparison.Ordinal);
        }
        catch (Exception cause) when (cause is MongoConfigurationException or FormatException or ArgumentException) { }
        return message;
    }

    private static OASISResult<T> Refuse<T>(OASISResult<T> result, string message, string code = "NATIVE_IMPORT_UNAVAILABLE")
    {
        OASISErrorHandling.HandleError(ref result, message);
        result.ErrorCode = code;
        return result;
    }
}
