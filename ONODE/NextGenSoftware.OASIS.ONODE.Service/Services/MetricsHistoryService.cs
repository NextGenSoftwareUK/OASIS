using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;
using NextGenSoftware.OASIS.ONODE.Service.Models;

namespace NextGenSoftware.OASIS.ONODE.Service.Services;

/// <summary>
/// Records aggregate + per-service metrics to a local SQLite database every 30 seconds.
/// Keeps 24 hours of history; older rows are pruned on startup and every 6 hours.
/// Database: ~/.oasis/onode-metrics.db
/// </summary>
public class MetricsHistoryService : BackgroundService
{
    private readonly MetricsCollector _metrics;
    private readonly ProcessSupervisor _supervisor;
    private readonly SupervisorConfig _config;
    private readonly ILogger<MetricsHistoryService> _logger;
    private string _dbPath = "";

    public MetricsHistoryService(
        MetricsCollector metrics,
        ProcessSupervisor supervisor,
        IOptions<SupervisorConfig> config,
        ILogger<MetricsHistoryService> logger)
    {
        _metrics    = metrics;
        _supervisor = supervisor;
        _config     = config.Value;
        _logger     = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        _dbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".oasis", "onode-metrics.db");
        Directory.CreateDirectory(Path.GetDirectoryName(_dbPath)!);
        InitDb();
        PruneOld();

        var pruneTimer = new PeriodicTimer(TimeSpan.FromHours(6));
        var recordTimer = new PeriodicTimer(TimeSpan.FromSeconds(30));

        _ = Task.Run(async () => {
            while (await pruneTimer.WaitForNextTickAsync(ct))
                PruneOld();
        }, ct);

        while (await recordTimer.WaitForNextTickAsync(ct))
        {
            try { Record(); }
            catch (Exception ex) { _logger.LogDebug(ex, "MetricsHistory record error"); }
        }
    }

    void InitDb()
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS metrics (
                id          INTEGER PRIMARY KEY AUTOINCREMENT,
                ts          TEXT    NOT NULL,
                node_id     TEXT    NOT NULL,
                service_id  TEXT    NOT NULL,
                peers       INTEGER NOT NULL DEFAULT 0,
                bytes_in    INTEGER NOT NULL DEFAULT 0,
                bytes_out   INTEGER NOT NULL DEFAULT 0,
                req_per_sec REAL    NOT NULL DEFAULT 0,
                latency_ms  REAL    NOT NULL DEFAULT 0
            );
            CREATE INDEX IF NOT EXISTS idx_metrics_ts ON metrics(ts);
            CREATE INDEX IF NOT EXISTS idx_metrics_node ON metrics(node_id, ts);
            """;
        cmd.ExecuteNonQuery();
    }

    void Record()
    {
        var ts      = DateTime.UtcNow.ToString("o");
        var nodeId  = _config.NodeId;
        var perSvc  = _metrics.PerService;

        using var conn = Open();
        using var tx   = conn.BeginTransaction();
        using var cmd  = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO metrics (ts, node_id, service_id, peers, bytes_in, bytes_out, req_per_sec, latency_ms)
            VALUES ($ts, $node, $svc, $peers, $bi, $bo, $rps, $lat)
            """;

        var pTs  = cmd.Parameters.Add("$ts",    SqliteType.Text);
        var pNode = cmd.Parameters.Add("$node", SqliteType.Text);
        var pSvc = cmd.Parameters.Add("$svc",   SqliteType.Text);
        var pP   = cmd.Parameters.Add("$peers", SqliteType.Integer);
        var pBi  = cmd.Parameters.Add("$bi",    SqliteType.Integer);
        var pBo  = cmd.Parameters.Add("$bo",    SqliteType.Integer);
        var pRps = cmd.Parameters.Add("$rps",   SqliteType.Real);
        var pLat = cmd.Parameters.Add("$lat",   SqliteType.Real);

        foreach (var kv in perSvc)
        {
            pTs.Value   = ts;
            pNode.Value = nodeId;
            pSvc.Value  = kv.Key;
            pP.Value    = kv.Value.PeersConnected;
            pBi.Value   = kv.Value.BytesReadPerSec;
            pBo.Value   = kv.Value.BytesWrittenPerSec;
            pRps.Value  = kv.Value.RequestsPerSec;
            pLat.Value  = kv.Value.AvgLatencyMs;
            cmd.ExecuteNonQuery();
        }

        // Also record aggregate row with service_id = "_aggregate"
        var agg = _metrics.Aggregate;
        pTs.Value   = ts;
        pNode.Value = nodeId;
        pSvc.Value  = "_aggregate";
        pP.Value    = agg.TotalPeers;
        pBi.Value   = agg.TotalBytesReadPerSec;
        pBo.Value   = agg.TotalBytesWrittenPerSec;
        pRps.Value  = agg.TotalRequestsPerSec;
        pLat.Value  = 0;
        cmd.ExecuteNonQuery();

        tx.Commit();
    }

    void PruneOld()
    {
        try
        {
            var cutoff = DateTime.UtcNow.AddHours(-24).ToString("o");
            using var conn = Open();
            using var cmd  = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM metrics WHERE ts < $cutoff";
            cmd.Parameters.AddWithValue("$cutoff", cutoff);
            var deleted = cmd.ExecuteNonQuery();
            if (deleted > 0)
                _logger.LogDebug("MetricsHistory pruned {Count} old rows", deleted);
        }
        catch (Exception ex) { _logger.LogDebug(ex, "MetricsHistory prune error"); }
    }

    public List<MetricsHistoryPoint> GetHistory(string? serviceId, int hours)
    {
        var cutoff = DateTime.UtcNow.AddHours(-Math.Min(hours, 24)).ToString("o");
        var svcFilter = serviceId ?? "_aggregate";

        using var conn = Open();
        using var cmd  = conn.CreateCommand();
        cmd.CommandText = """
            SELECT ts, peers, bytes_in, bytes_out, req_per_sec, latency_ms
            FROM metrics WHERE node_id = $node AND service_id = $svc AND ts >= $cutoff
            ORDER BY ts ASC
            """;
        cmd.Parameters.AddWithValue("$node",   _config.NodeId);
        cmd.Parameters.AddWithValue("$svc",    svcFilter);
        cmd.Parameters.AddWithValue("$cutoff", cutoff);

        var results = new List<MetricsHistoryPoint>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            results.Add(new MetricsHistoryPoint
            {
                Timestamp      = reader.GetString(0),
                Peers          = reader.GetInt32(1),
                BytesIn        = reader.GetInt64(2),
                BytesOut       = reader.GetInt64(3),
                RequestsPerSec = reader.GetDouble(4),
                LatencyMs      = reader.GetDouble(5),
            });
        }
        return results;
    }

    SqliteConnection Open()
    {
        var conn = new SqliteConnection($"Data Source={_dbPath}");
        conn.Open();
        return conn;
    }
}

public class MetricsHistoryPoint
{
    public string Timestamp      { get; set; } = "";
    public int    Peers          { get; set; }
    public long   BytesIn        { get; set; }
    public long   BytesOut       { get; set; }
    public double RequestsPerSec { get; set; }
    public double LatencyMs      { get; set; }
}
