using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NextGenSoftware.OASIS.ONODE.Service.Models;
using NextGenSoftware.OASIS.ONODE.Service.Services;

namespace NextGenSoftware.OASIS.ONODE.Service.Api;

[ApiController]
[Route("supervisor")]
public class SupervisorController : ControllerBase
{
    private readonly ProcessSupervisor _supervisor;
    private readonly MetricsCollector _metrics;
    private readonly LogAggregator _logs;
    private readonly TokenService _token;
    private readonly SupervisorConfig _config;

    public SupervisorController(ProcessSupervisor supervisor, MetricsCollector metrics,
        LogAggregator logs, TokenService token, IOptions<SupervisorConfig> config)
    {
        _supervisor = supervisor;
        _metrics = metrics;
        _logs = logs;
        _token = token;
        _config = config.Value;
    }

    bool Authorised() => _token.Validate(Request.Headers.Authorization);

    // GET /supervisor/status
    [HttpGet("status")]
    public IActionResult Status()
    {
        if (!Authorised()) return Unauthorized();
        return Ok(new SupervisorStatus
        {
            NodeId = _config.NodeId,
            StartedAt = Program.StartedAt,
            Services = _supervisor.States.Values.ToList(),
            Metrics = _metrics.Aggregate
        });
    }

    // GET /supervisor/services
    [HttpGet("services")]
    public IActionResult Services()
    {
        if (!Authorised()) return Unauthorized();
        return Ok(_supervisor.States.Values);
    }

    // GET /supervisor/services/{id}
    [HttpGet("services/{id}")]
    public IActionResult GetService(string id)
    {
        if (!Authorised()) return Unauthorized();
        var state = _supervisor.GetState(id.ToLower());
        return state == null ? NotFound() : Ok(state);
    }

    // POST /supervisor/start/{id}
    [HttpPost("start/{id}")]
    public async Task<IActionResult> StartOne(string id, [FromQuery] string? windowMode, CancellationToken ct)
    {
        if (!Authorised()) return Unauthorized();
        WindowMode? mode = Enum.TryParse<WindowMode>(windowMode, true, out var m) ? m : null;
        await _supervisor.StartAsync(id.ToLower(), mode, ct);
        return Ok(new { message = $"{id} start requested" });
    }

    // POST /supervisor/stop/{id}
    [HttpPost("stop/{id}")]
    public async Task<IActionResult> StopOne(string id, CancellationToken ct)
    {
        if (!Authorised()) return Unauthorized();
        await _supervisor.StopAsync(id.ToLower(), ct);
        return Ok(new { message = $"{id} stop requested" });
    }

    // POST /supervisor/restart/{id}
    [HttpPost("restart/{id}")]
    public async Task<IActionResult> RestartOne(string id, [FromQuery] string? windowMode, CancellationToken ct)
    {
        if (!Authorised()) return Unauthorized();
        WindowMode? mode = Enum.TryParse<WindowMode>(windowMode, true, out var m) ? m : null;
        await _supervisor.RestartAsync(id.ToLower(), mode, ct);
        return Ok(new { message = $"{id} restart requested" });
    }

    // POST /supervisor/start  body: { "ids": ["web4","web6"] }
    [HttpPost("start")]
    public async Task<IActionResult> StartMany([FromBody] ServiceSelectionRequest req, CancellationToken ct)
    {
        if (!Authorised()) return Unauthorized();
        WindowMode? mode = Enum.TryParse<WindowMode>(req.WindowMode, true, out var m) ? m : null;
        foreach (var id in req.Ids ?? [])
            await _supervisor.StartAsync(id.ToLower(), mode, ct);
        return Ok(new { message = "Start requested", ids = req.Ids });
    }

    // POST /supervisor/stop  body: { "ids": ["web4","web6"] }
    [HttpPost("stop")]
    public async Task<IActionResult> StopMany([FromBody] ServiceSelectionRequest req, CancellationToken ct)
    {
        if (!Authorised()) return Unauthorized();
        foreach (var id in req.Ids ?? [])
            await _supervisor.StopAsync(id.ToLower(), ct);
        return Ok(new { message = "Stop requested", ids = req.Ids });
    }

    // POST /supervisor/restart  body: { "ids": ["web4","web6"] }
    [HttpPost("restart")]
    public async Task<IActionResult> RestartMany([FromBody] ServiceSelectionRequest req, CancellationToken ct)
    {
        if (!Authorised()) return Unauthorized();
        WindowMode? mode = Enum.TryParse<WindowMode>(req.WindowMode, true, out var m) ? m : null;
        foreach (var id in req.Ids ?? [])
            await _supervisor.RestartAsync(id.ToLower(), mode, ct);
        return Ok(new { message = "Restart requested", ids = req.Ids });
    }

    // POST /supervisor/start-group/{group}
    [HttpPost("start-group/{group}")]
    public async Task<IActionResult> StartGroup(string group, [FromQuery] string? windowMode, CancellationToken ct)
    {
        if (!Authorised()) return Unauthorized();
        WindowMode? mode = Enum.TryParse<WindowMode>(windowMode, true, out var m) ? m : null;
        await _supervisor.StartGroupAsync(group, mode, ct);
        return Ok(new { message = $"Group '{group}' start requested" });
    }

    // POST /supervisor/stop-group/{group}
    [HttpPost("stop-group/{group}")]
    public async Task<IActionResult> StopGroup(string group, CancellationToken ct)
    {
        if (!Authorised()) return Unauthorized();
        await _supervisor.StopGroupAsync(group, ct);
        return Ok(new { message = $"Group '{group}' stop requested" });
    }

    // POST /supervisor/restart-group/{group}
    [HttpPost("restart-group/{group}")]
    public async Task<IActionResult> RestartGroup(string group, [FromQuery] string? windowMode, CancellationToken ct)
    {
        if (!Authorised()) return Unauthorized();
        WindowMode? mode = Enum.TryParse<WindowMode>(windowMode, true, out var m) ? m : null;
        await _supervisor.RestartGroupAsync(group, mode, ct);
        return Ok(new { message = $"Group '{group}' restart requested" });
    }

    // GET /supervisor/logs/{id}?lines=200
    [HttpGet("logs/{id}")]
    public IActionResult GetServiceLogs(string id, [FromQuery] int lines = 200)
    {
        if (!Authorised()) return Unauthorized();
        return Ok(_logs.GetLines(id.ToLower(), lines));
    }

    // GET /supervisor/logs?lines=500
    [HttpGet("logs")]
    public IActionResult GetAllLogs([FromQuery] int lines = 500)
    {
        if (!Authorised()) return Unauthorized();
        return Ok(_logs.GetAllLines(lines));
    }

    // GET /supervisor/metrics
    [HttpGet("metrics")]
    public IActionResult GetMetrics()
    {
        if (!Authorised()) return Unauthorized();
        return Ok(new { aggregate = _metrics.Aggregate, services = _metrics.Current });
    }

    // GET /supervisor/metrics/{id}
    [HttpGet("metrics/{id}")]
    public IActionResult GetServiceMetrics(string id)
    {
        if (!Authorised()) return Unauthorized();
        _metrics.Current.TryGetValue(id.ToLower(), out var m);
        return m == null ? NotFound() : Ok(m);
    }

    // GET /supervisor/config
    [HttpGet("config")]
    public IActionResult GetConfig()
    {
        if (!Authorised()) return Unauthorized();
        var path = _config.ResolveOASISDNAPath();
        if (!File.Exists(path)) return NotFound(new { error = $"OASISDNA.json not found at {path}" });
        return Content(File.ReadAllText(path), "application/json");
    }

    // PUT /supervisor/config
    [HttpPut("config")]
    public async Task<IActionResult> UpdateConfig(CancellationToken ct)
    {
        if (!Authorised()) return Unauthorized();
        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync(ct);
        try { JsonDocument.Parse(body); } // validate JSON
        catch { return BadRequest(new { error = "Invalid JSON" }); }

        var path = _config.ResolveOASISDNAPath();
        var dir = Path.GetDirectoryName(path)!;
        Directory.CreateDirectory(dir);

        if (File.Exists(path))
            File.Copy(path, path + $".bak.{DateTime.UtcNow:yyyyMMddHHmmss}", overwrite: true);

        await File.WriteAllTextAsync(path, body, ct);
        return Ok(new { message = "OASISDNA.json updated" });
    }

    // GET /supervisor/services/{id}/config
    [HttpGet("services/{id}/config")]
    public IActionResult GetServiceConfig(string id)
    {
        if (!Authorised()) return Unauthorized();
        if (!_config.Services.TryGetValue(id.ToLower(), out var cfg)) return NotFound();
        return Ok(cfg);
    }

    // PUT /supervisor/services/{id}/config
    [HttpPut("services/{id}/config")]
    public IActionResult UpdateServiceConfig(string id, [FromBody] ServiceConfig updated)
    {
        if (!Authorised()) return Unauthorized();
        if (!_config.Services.ContainsKey(id.ToLower())) return NotFound();
        _config.Services[id.ToLower()] = updated;
        return Ok(new { message = $"{id} config updated" });
    }

    // GET /supervisor/node-info
    [HttpGet("node-info")]
    public IActionResult NodeInfo()
    {
        if (!Authorised()) return Unauthorized();
        return Ok(new
        {
            NodeId = _config.NodeId,
            AvatarId = _config.AvatarId,
            Version = "1.0.0",
            StartedAt = Program.StartedAt,
            OASISRootPath = _config.OASISRootPath,
            OASISDNAPath = _config.ResolveOASISDNAPath()
        });
    }
}

public class ServiceSelectionRequest
{
    public List<string>? Ids { get; set; }
    public string? WindowMode { get; set; }
}
