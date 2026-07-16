using System.Text.Json;
using Microsoft.Extensions.Options;
using NextGenSoftware.OASIS.ONODE.Service.Models;

namespace NextGenSoftware.OASIS.ONODE.Service.Services;

/// <summary>
/// Executes CommandHolons received from OPORTAL via the Holon bridge.
/// Maps remote commands to local supervisor actions.
/// </summary>
public class CommandExecutor
{
    private readonly ProcessSupervisor _supervisor;
    private readonly LogAggregator _logs;
    private readonly SupervisorConfig _config;
    private readonly ILogger<CommandExecutor> _logger;

    public CommandExecutor(ProcessSupervisor supervisor, LogAggregator logs,
        IOptions<SupervisorConfig> config, ILogger<CommandExecutor> logger)
    {
        _supervisor = supervisor;
        _logs = logs;
        _config = config.Value;
        _logger = logger;
    }

    public async Task<string?> ExecuteAsync(CommandHolon cmd, CancellationToken ct)
    {
        _logger.LogInformation("Executing remote command {Command} on {Service} from avatar {Avatar}",
            cmd.Command, cmd.Service ?? "all", cmd.IssuedByAvatarId);

        // Security: only accept commands targeting this node
        if (cmd.TargetNodeId != _config.NodeId)
            throw new UnauthorizedAccessException("Command not addressed to this node");

        return cmd.Command switch
        {
            CommandType.Start   => await StartAsync(cmd.Service, ct),
            CommandType.Stop    => await StopAsync(cmd.Service, ct),
            CommandType.Restart => await RestartAsync(cmd.Service, ct),
            CommandType.GetLogs => GetLogs(cmd.Service, cmd.Payload),
            CommandType.GetConfig => GetConfig(),
            CommandType.UpdateConfig => await UpdateConfigAsync(cmd.Payload, ct),
            CommandType.GetMetrics => null,
            _ => throw new NotSupportedException($"Unknown command: {cmd.Command}")
        };
    }

    async Task<string?> StartAsync(string? service, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(service) || service == "all")
            await _supervisor.StartGroupAsync("all", ct: ct);
        else
            await _supervisor.StartAsync(service, ct: ct);
        return "Started";
    }

    async Task<string?> StopAsync(string? service, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(service) || service == "all")
            await _supervisor.StopGroupAsync("all", ct);
        else
            await _supervisor.StopAsync(service, ct);
        return "Stopped";
    }

    async Task<string?> RestartAsync(string? service, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(service) || service == "all")
            await _supervisor.RestartGroupAsync("all", ct: ct);
        else
            await _supervisor.RestartAsync(service, ct: ct);
        return "Restarted";
    }

    string? GetLogs(string? service, string? payload)
    {
        int lines = 100;
        if (!string.IsNullOrEmpty(payload))
        {
            try
            {
                var opts = JsonDocument.Parse(payload).RootElement;
                if (opts.TryGetProperty("lines", out var l)) lines = l.GetInt32();
            }
            catch { }
        }

        var entries = string.IsNullOrEmpty(service)
            ? _logs.GetAllLines(lines)
            : _logs.GetLines(service, lines);

        return JsonSerializer.Serialize(entries.Select(e => new
        {
            e.ServiceId, e.Message, e.IsError,
            Timestamp = e.Timestamp.ToString("O")
        }));
    }

    string? GetConfig()
    {
        var path = _config.ResolveOASISDNAPath();
        return File.Exists(path) ? File.ReadAllText(path) : null;
    }

    async Task<string?> UpdateConfigAsync(string? payload, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(payload))
            throw new ArgumentException("No config payload provided");

        // Validate it's valid JSON before writing
        JsonDocument.Parse(payload);

        var path = _config.ResolveOASISDNAPath();
        var backup = path + $".bak.{DateTime.UtcNow:yyyyMMddHHmmss}";
        if (File.Exists(path)) File.Copy(path, backup);

        await File.WriteAllTextAsync(path, payload, ct);
        return "Config updated";
    }
}
