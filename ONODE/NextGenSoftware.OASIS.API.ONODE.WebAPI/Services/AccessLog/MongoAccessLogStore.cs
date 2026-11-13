using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.DNA;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.AccessLog
{
    /// <summary>
    /// Append-only access events in the MongoDBOASIS database (collection AccessLogEvents), using the connection already in
    /// OASIS_DNA.json (StorageProviders.MongoDBOASIS). Indexed by holon id (multikey), avatar id and time; retention is a
    /// Mongo TTL index driven by AccessLog.RetentionDays. Insert-only: no update or delete is ever issued.
    /// </summary>
    public class MongoAccessLogStore : IAccessLogStore
    {
        public const string CollectionName = "AccessLogEvents";
        private readonly Func<OASISDNA> _dna;
        private IMongoCollection<Doc> _collection;
        private readonly object _gate = new object();

        public MongoAccessLogStore(Func<OASISDNA> dna) { _dna = dna; }

        [BsonIgnoreExtraElements]
        private class Doc
        {
            [BsonId, BsonGuidRepresentation(GuidRepresentation.Standard)] public Guid Id { get; set; }
            [BsonElement("t")] public DateTime Timestamp { get; set; }
            [BsonElement("a"), BsonGuidRepresentation(GuidRepresentation.Standard)] public Guid AvatarId { get; set; }
            [BsonElement("app")] public string AppId { get; set; }
            [BsonElement("op")] public string Operation { get; set; }
            [BsonElement("type")] public string HolonType { get; set; }
            [BsonElement("h"), BsonGuidRepresentation(GuidRepresentation.Standard)] public List<Guid> HolonIds { get; set; }
            [BsonElement("n")] public int TotalCount { get; set; }
            [BsonElement("ip")] public string Ip { get; set; }
        }

        private IMongoCollection<Doc> Collection()
        {
            lock (_gate)
            {
                if (_collection != null) return _collection;
                var dna = _dna();
                var mongo = dna?.OASIS?.StorageProviders?.MongoDBOASIS;
                if (mongo == null || string.IsNullOrWhiteSpace(mongo.ConnectionString) || string.IsNullOrWhiteSpace(mongo.DBName))
                    throw new InvalidOperationException("AccessLog needs StorageProviders.MongoDBOASIS (ConnectionString and DBName) in OASIS_DNA.json.");
                var collection = new MongoClient(mongo.ConnectionString).GetDatabase(mongo.DBName).GetCollection<Doc>(CollectionName);
                var retention = dna.OASIS.AccessLog?.RetentionDays ?? 365;
                collection.Indexes.CreateMany(new[]
                {
                    new CreateIndexModel<Doc>(Builders<Doc>.IndexKeys.Ascending(d => d.HolonIds).Descending(d => d.Timestamp), new CreateIndexOptions { Name = "ix_holon_time" }),
                    new CreateIndexModel<Doc>(Builders<Doc>.IndexKeys.Ascending(d => d.AvatarId).Descending(d => d.Timestamp), new CreateIndexOptions { Name = "ix_avatar_time" }),
                    new CreateIndexModel<Doc>(Builders<Doc>.IndexKeys.Ascending(d => d.Timestamp),
                        new CreateIndexOptions { Name = "ix_ttl", ExpireAfter = retention > 0 ? TimeSpan.FromDays(retention) : (TimeSpan?)null })
                });
                return _collection = collection;
            }
        }

        public Task InsertAsync(IReadOnlyList<AccessLogEvent> batch, CancellationToken c)
            => Collection().InsertManyAsync(batch.Select(e => new Doc
            {
                Id = e.Id, Timestamp = e.Timestamp, AvatarId = e.AvatarId, AppId = e.AppId, Operation = e.Operation,
                HolonType = e.HolonType, HolonIds = e.HolonIds, TotalCount = e.TotalCount, Ip = e.IpAddress
            }), new InsertManyOptions { IsOrdered = false }, c);

        public async Task<IReadOnlyList<AccessLogEvent>> QueryByHolonAsync(Guid holonId, int limit, int offset, CancellationToken c)
            => Map(await Collection().Find(d => d.HolonIds.Contains(holonId)).SortByDescending(d => d.Timestamp).Skip(offset).Limit(limit).ToListAsync(c));

        public async Task<IReadOnlyList<AccessLogEvent>> QueryByAvatarAsync(Guid avatarId, int limit, int offset, CancellationToken c)
            => Map(await Collection().Find(d => d.AvatarId == avatarId).SortByDescending(d => d.Timestamp).Skip(offset).Limit(limit).ToListAsync(c));

        private static IReadOnlyList<AccessLogEvent> Map(List<Doc> docs) => docs.Select(d => new AccessLogEvent
        {
            Id = d.Id, Timestamp = d.Timestamp, AvatarId = d.AvatarId, AppId = d.AppId, Operation = d.Operation,
            HolonType = d.HolonType, HolonIds = d.HolonIds ?? new List<Guid>(), TotalCount = d.TotalCount, IpAddress = d.Ip
        }).ToList();
    }

    /// <summary>Drains the access-log queue on a timer. Does nothing at all while AccessLog.Enabled is false.</summary>
    public class AccessLogFlushHostedService : Microsoft.Extensions.Hosting.BackgroundService
    {
        private readonly AccessLogService _service;
        private readonly Func<AccessLogSettings> _settings;
        public AccessLogFlushHostedService(AccessLogService service, Func<AccessLogSettings> settings) { _service = service; _settings = settings; }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var seconds = Math.Max(1, _settings()?.FlushIntervalSeconds ?? 5);
                try { await Task.Delay(TimeSpan.FromSeconds(seconds), stoppingToken).ConfigureAwait(false); } catch (OperationCanceledException) { break; }
                if (_service.IsEnabled) await _service.FlushAsync(stoppingToken).ConfigureAwait(false);
            }
            if (_service.IsEnabled) await _service.FlushAsync().ConfigureAwait(false);
        }
    }
}
