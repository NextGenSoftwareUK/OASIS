using System;
using System.Linq;
using System.Drawing;
using System.Diagnostics;
using System.Threading.Tasks;
using Console = System.Console;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.ONODE.Client;
using NextGenSoftware.CLI.Engine;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Events;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.STAR.Enums;
using NextGenSoftware.OASIS.STAR.CLI.Lib;
using NextGenSoftware.OASIS.STAR.CLI.Lib.Enums;
using NextGenSoftware.OASIS.STAR.ErrorEventArgs;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.Core.Objects;
using NextGenSoftware.OASIS.API.ONODE.Core.Network;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Objects.Game;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.DNA;
using System.IO;
using System.Reflection;

namespace NextGenSoftware.OASIS.STAR.CLI
{ //test
    partial class Program
    {
        private static async Task ShowONODEMenuAsync(string[] inputArgs)
        {
            if (inputArgs.Length <= 1)
            {
                ShowONODEHelp();
                return;
            }

            // Parse --hidden / --visible / --minimised flags
            string? windowMode = null;
            if (inputArgs.Any(a => a.Equals("--hidden",    StringComparison.OrdinalIgnoreCase))) windowMode = "Hidden";
            if (inputArgs.Any(a => a.Equals("--visible",   StringComparison.OrdinalIgnoreCase))) windowMode = "Visible";
            if (inputArgs.Any(a => a.Equals("--minimised", StringComparison.OrdinalIgnoreCase))) windowMode = "Minimised";

            // Service/group target: first non-flag arg after the subcommand
            string target = inputArgs.Length > 2 ? inputArgs[2].ToLower() : "all";
            if (target.StartsWith("--")) target = "all";

            using var client = new NextGenSoftware.OASIS.ONODE.Client.SupervisorClient();

            switch (inputArgs[1].ToLower())
            {
                case "start":
                    await ONODEStartAsync(client, target, windowMode);
                    break;

                case "stop":
                    await ONODEStopAsync(client, target);
                    break;

                case "restart":
                    await ONODERestartAsync(client, target, windowMode);
                    break;

                case "status":
                    await ONODEStatusAsync(client);
                    break;

                case "logs":
                    string? logService = target == "all" ? null : target;
                    int lines = 100;
                    var linesArg = inputArgs.FirstOrDefault(a => a.StartsWith("--lines="));
                    if (linesArg != null && int.TryParse(linesArg.Split('=')[1], out var l)) lines = l;
                    bool follow = inputArgs.Any(a => a.Equals("--follow", StringComparison.OrdinalIgnoreCase));
                    await ONODELogsAsync(client, logService, lines, follow);
                    break;

                case "metrics":
                    await ONODEMetricsAsync(client);
                    break;

                case "config":
                    bool edit = inputArgs.Any(a => a.Equals("--edit", StringComparison.OrdinalIgnoreCase));
                    await ONODEConfigAsync(client, edit);
                    break;

                case "providers":
                    await ONODEProvidersAsync(client, inputArgs);
                    break;

                case "startprovider":
                    if (inputArgs.Length > 2) await StartONODEProviderAsync(inputArgs[2]);
                    else CLIEngine.ShowErrorMessage("Usage: onode startprovider {ProviderName}");
                    break;

                case "stopprovider":
                    if (inputArgs.Length > 2) await StopONODEProviderAsync(inputArgs[2]);
                    else CLIEngine.ShowErrorMessage("Usage: onode stopprovider {ProviderName}");
                    break;

                case "service":
                    await ONODEServiceCommandAsync(inputArgs);
                    break;

                default:
                    CLIEngine.ShowErrorMessage($"Unknown onode subcommand: {inputArgs[1]}");
                    ShowONODEHelp();
                    break;
            }
        }

        private static async Task ONODEStartAsync(NextGenSoftware.OASIS.ONODE.Client.SupervisorClient client, string target, string? windowMode)
        {
            if (!client.IsAvailable)
            {
                CLIEngine.ShowWarningMessage("ONODEService not running — falling back to direct process spawn.");
                await ONODEStartDirectFallbackAsync(target, windowMode);
                return;
            }

            CLIEngine.ShowWorkingMessage($"Starting {target.ToUpper()}...");
            bool isGroup = new[] { "all","core","ai","extended" }.Contains(target) || target.Contains(",");
            bool isSingle = target.StartsWith("web");

            if (target.Contains(","))
            {
                var ids = target.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
                await client.StartManyAsync(ids, windowMode);
            }
            else if (isSingle)
                await client.StartAsync(target, windowMode);
            else
                await client.StartGroupAsync(target, windowMode);

            CLIEngine.ShowSuccessMessage($"Start command sent for {target.ToUpper()}.");
        }

        private static async Task ONODEStopAsync(NextGenSoftware.OASIS.ONODE.Client.SupervisorClient client, string target)
        {
            if (!client.IsAvailable)
            {
                CLIEngine.ShowWarningMessage("ONODEService not running — cannot stop via supervisor.");
                return;
            }

            CLIEngine.ShowWorkingMessage($"Stopping {target.ToUpper()}...");
            if (target.Contains(","))
            {
                var ids = target.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
                await client.StopManyAsync(ids);
            }
            else if (target.StartsWith("web"))
                await client.StopAsync(target);
            else
                await client.StopGroupAsync(target);

            CLIEngine.ShowSuccessMessage($"Stop command sent for {target.ToUpper()}.");
        }

        private static async Task ONODERestartAsync(NextGenSoftware.OASIS.ONODE.Client.SupervisorClient client, string target, string? windowMode)
        {
            if (!client.IsAvailable) { CLIEngine.ShowWarningMessage("ONODEService not running."); return; }
            CLIEngine.ShowWorkingMessage($"Restarting {target.ToUpper()}...");
            if (target.StartsWith("web")) await client.RestartAsync(target, windowMode);
            else await client.RestartGroupAsync(target, windowMode);
            CLIEngine.ShowSuccessMessage($"Restart command sent for {target.ToUpper()}.");
        }

        private static async Task ONODEStatusAsync(NextGenSoftware.OASIS.ONODE.Client.SupervisorClient client)
        {
            if (!client.IsAvailable)
            {
                CLIEngine.ShowWarningMessage("ONODEService not running. Install it with: onode service install");
                return;
            }

            var status = await client.GetStatusAsync();
            if (status == null) { CLIEngine.ShowErrorMessage("Failed to retrieve status."); return; }

            Console.WriteLine("");
            CLIEngine.ShowMessage($"ONODE SUPERVISOR STATUS", ConsoleColor.Cyan);
            CLIEngine.ShowMessage($"  Node ID : {status.NodeId}", ConsoleColor.White, false);
            CLIEngine.ShowMessage($"  Version : {status.Version}", ConsoleColor.White, false);
            CLIEngine.ShowMessage($"  Uptime  : {FormatUptime((DateTime.UtcNow - status.StartedAt).TotalSeconds)}", ConsoleColor.White, false);
            CLIEngine.ShowMessage($"  Peers   : {status.Metrics.TotalPeers}", ConsoleColor.White, false);
            Console.WriteLine("");
            CLIEngine.ShowMessage($"  {"SERVICE",-10} {"STATUS",-12} {"PID",-8} {"PORT",-6} {"UPTIME",-12} {"RESTARTS"}", ConsoleColor.Green);
            foreach (var svc in status.Services)
            {
                var col = svc.Status == "Running" ? ConsoleColor.Green :
                          svc.Status == "Stopped" ? ConsoleColor.Gray :
                          svc.Status == "Degraded" || svc.Status == "Crashed" ? ConsoleColor.Red :
                          ConsoleColor.Yellow;
                var pid = svc.Pid.HasValue ? svc.Pid.ToString() : "-";
                CLIEngine.ShowMessage($"  {svc.Id.ToUpper(),-10} {svc.Status,-12} {pid,-8} {svc.Port,-6} {FormatUptime(svc.UptimeSeconds),-12} {svc.RestartCount}", col, false);
            }
            Console.WriteLine("");
        }

        private static async Task ONODELogsAsync(NextGenSoftware.OASIS.ONODE.Client.SupervisorClient client, string? serviceId, int lines, bool follow)
        {
            if (!client.IsAvailable) { CLIEngine.ShowWarningMessage("ONODEService not running."); return; }

            do
            {
                var entries = await client.GetLogsAsync(serviceId, lines);
                if (entries != null)
                {
                    Console.Clear();
                    foreach (var e in entries)
                    {
                        var col = e.IsError ? ConsoleColor.Red : ConsoleColor.Gray;
                        CLIEngine.ShowMessage($"[{e.Timestamp:HH:mm:ss}] [{e.ServiceId.ToUpper()}] {e.Message}", col, false);
                    }
                }
                if (follow) await Task.Delay(2000);
            } while (follow);
        }

        private static async Task ONODEMetricsAsync(NextGenSoftware.OASIS.ONODE.Client.SupervisorClient client)
        {
            if (!client.IsAvailable) { CLIEngine.ShowWarningMessage("ONODEService not running."); return; }

            var metrics = await client.GetMetricsAsync();
            if (metrics == null) { CLIEngine.ShowErrorMessage("Failed to retrieve metrics."); return; }

            Console.WriteLine("");
            CLIEngine.ShowMessage("ONODE AGGREGATE METRICS", ConsoleColor.Cyan);
            CLIEngine.ShowMessage($"  Total Peers       : {metrics.Aggregate.TotalPeers}", ConsoleColor.White, false);
            CLIEngine.ShowMessage($"  Bytes Read/s      : {FormatBytes(metrics.Aggregate.TotalBytesReadPerSec)}", ConsoleColor.White, false);
            CLIEngine.ShowMessage($"  Bytes Written/s   : {FormatBytes(metrics.Aggregate.TotalBytesWrittenPerSec)}", ConsoleColor.White, false);
            CLIEngine.ShowMessage($"  Requests/s        : {metrics.Aggregate.TotalRequestsPerSec:F1}", ConsoleColor.White, false);
            Console.WriteLine("");
            CLIEngine.ShowMessage($"  {"SERVICE",-10} {"PEERS",-8} {"READ/s",-12} {"WRITE/s",-12} {"REQ/s",-8} {"LATENCY ms"}", ConsoleColor.Green);
            foreach (var (id, m) in metrics.Services)
                CLIEngine.ShowMessage($"  {id.ToUpper(),-10} {m.PeersConnected,-8} {FormatBytes(m.BytesReadPerSec),-12} {FormatBytes(m.BytesWrittenPerSec),-12} {m.RequestsPerSec,-8:F1} {m.AvgLatencyMs:F1}", ConsoleColor.White, false);
            Console.WriteLine("");
        }

        private static async Task ONODEConfigAsync(NextGenSoftware.OASIS.ONODE.Client.SupervisorClient client, bool edit)
        {
            if (!client.IsAvailable) { CLIEngine.ShowWarningMessage("ONODEService not running."); return; }

            var config = await client.GetConfigAsync();
            if (config == null) { CLIEngine.ShowErrorMessage("Could not read OASISDNA.json."); return; }

            if (edit)
            {
                // Write to temp file and open in $EDITOR
                var tmp = Path.Combine(Path.GetTempPath(), "OASISDNA_edit.json");
                await File.WriteAllTextAsync(tmp, config);
                var editor = Environment.GetEnvironmentVariable("EDITOR") ?? (OperatingSystem.IsWindows() ? "notepad" : "nano");
                var psi = new ProcessStartInfo(editor, $"\"{tmp}\"") { UseShellExecute = true };
                var proc = Process.Start(psi);
                proc?.WaitForExit();
                var updated = await File.ReadAllTextAsync(tmp);
                await client.UpdateConfigAsync(updated);
                CLIEngine.ShowSuccessMessage("OASISDNA.json updated.");
            }
            else
            {
                Console.WriteLine(config);
            }
        }

        private static async Task ONODEServiceCommandAsync(string[] inputArgs)
        {
            if (inputArgs.Length < 3) { CLIEngine.ShowErrorMessage("Usage: onode service [install|uninstall|start|stop|restart]"); return; }
            switch (inputArgs[2].ToLower())
            {
                case "install":
                    CLIEngine.ShowMessage("To install ONODEService, run the service binary directly with --install:", ConsoleColor.Yellow, false);
                    CLIEngine.ShowMessage("  dotnet run --project <path-to-ONODEService> -- --install", ConsoleColor.White, false);
                    CLIEngine.ShowMessage("Or publish it and run: NextGenSoftware.OASIS.ONODE.Service install", ConsoleColor.White, false);
                    break;
                case "uninstall":
                    CLIEngine.ShowMessage("To uninstall ONODEService, run:", ConsoleColor.Yellow, false);
                    CLIEngine.ShowMessage("  NextGenSoftware.OASIS.ONODE.Service uninstall", ConsoleColor.White, false);
                    break;
                default:
                    CLIEngine.ShowErrorMessage($"Unknown service subcommand: {inputArgs[2]}");
                    break;
            }
            await Task.CompletedTask;
        }

        // Fallback: direct spawn when ONODEService is not running
        private static async Task ONODEStartDirectFallbackAsync(string target, string? windowMode)
        {
            var services = target switch
            {
                "all"      => new[] { "web4","web5","web6","web7","web8","web9","web10" },
                "core"     => new[] { "web4","web5" },
                "ai"       => new[] { "web6" },
                "extended" => new[] { "web7","web8","web9","web10" },
                _ when target.Contains(",") => target.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
                _ => new[] { target }
            };

            string oasisRoot = @"C:\Source\OASIS2";
            if (!OperatingSystem.IsWindows())
                oasisRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Source", "OASIS2");

            foreach (var svc in services)
            {
                var projectPath = svc switch
                {
                    "web4"  => Path.Combine(oasisRoot, "ONODE", "NextGenSoftware.OASIS.API.ONODE.WebAPI"),
                    "web5"  => Path.Combine(oasisRoot, "STAR ODK", "NextGenSoftware.OASIS.STAR.WebAPI"),
                    "web6"  => Path.Combine(oasisRoot, "WEB6",  "NextGenSoftware.OASIS.Web6.WebAPI"),
                    "web7"  => Path.Combine(oasisRoot, "WEB7",  "NextGenSoftware.OASIS.Web7.WebAPI"),
                    "web8"  => Path.Combine(oasisRoot, "WEB8",  "NextGenSoftware.OASIS.Web8.WebAPI"),
                    "web9"  => Path.Combine(oasisRoot, "WEB9",  "NextGenSoftware.OASIS.Web9.WebAPI"),
                    "web10" => Path.Combine(oasisRoot, "WEB10", "NextGenSoftware.OASIS.Web10.WebAPI"),
                    _ => ""
                };
                if (string.IsNullOrEmpty(projectPath) || !Directory.Exists(projectPath))
                {
                    CLIEngine.ShowWarningMessage($"{svc.ToUpper()} not found at {projectPath} — skipping.");
                    continue;
                }
                var hidden = windowMode?.Equals("Hidden", StringComparison.OrdinalIgnoreCase) == true;
                var psi = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = $"run --project \"{projectPath}\"",
                    WorkingDirectory = projectPath,
                    UseShellExecute = !hidden,
                    CreateNoWindow = hidden,
                    WindowStyle = hidden ? ProcessWindowStyle.Hidden : ProcessWindowStyle.Normal
                };
                var process = Process.Start(psi);
                if (process != null)
                {
                    _webApiProcesses[svc] = process;
                    CLIEngine.ShowSuccessMessage($"{svc.ToUpper()} started (pid {process.Id}).");
                }
                else CLIEngine.ShowErrorMessage($"Failed to start {svc.ToUpper()}.");
            }
            await Task.CompletedTask;
        }

        private static void ShowONODEHelp()
        {
            Console.WriteLine("");
            CLIEngine.ShowMessage("ONODE SUBCOMMANDS:", ConsoleColor.Green);
            Console.WriteLine("");
            CLIEngine.ShowMessage("  start   [target] [--hidden|--visible|--minimised]", ConsoleColor.Green, false);
            CLIEngine.ShowMessage("  stop    [target]", ConsoleColor.Green, false);
            CLIEngine.ShowMessage("  restart [target] [--hidden|--visible|--minimised]", ConsoleColor.Green, false);
            CLIEngine.ShowMessage("  status", ConsoleColor.Green, false);
            CLIEngine.ShowMessage("  logs    [target] [--lines=N] [--follow]", ConsoleColor.Green, false);
            CLIEngine.ShowMessage("  metrics", ConsoleColor.Green, false);
            CLIEngine.ShowMessage("  config  [--edit]", ConsoleColor.Green, false);
            CLIEngine.ShowMessage("  providers", ConsoleColor.Green, false);
            CLIEngine.ShowMessage("  startprovider {name}", ConsoleColor.Green, false);
            CLIEngine.ShowMessage("  stopprovider  {name}", ConsoleColor.Green, false);
            CLIEngine.ShowMessage("  service [install|uninstall]", ConsoleColor.Green, false);
            Console.WriteLine("");
            CLIEngine.ShowMessage("  [target] = web4|web5|web6|web7|web8|web9|web10|all|core|ai|extended|web4,web6,...", ConsoleColor.DarkGreen, false);
            Console.WriteLine("");
        }

        private static string FormatUptime(double seconds)
        {
            var ts = TimeSpan.FromSeconds(seconds);
            if (ts.TotalHours >= 1) return $"{(int)ts.TotalHours}h{ts.Minutes:D2}m";
            if (ts.TotalMinutes >= 1) return $"{ts.Minutes}m{ts.Seconds:D2}s";
            return $"{ts.Seconds}s";
        }

        private static string FormatBytes(long bytes)
        {
            if (bytes >= 1_000_000) return $"{bytes / 1_000_000.0:F1}MB";
            if (bytes >= 1_000) return $"{bytes / 1_000.0:F1}KB";
            return $"{bytes}B";
        }
        private static async Task ShowONODEStatusAsync()
        {
            try
            {
                await InitializeONETAsync();
                CLIEngine.ShowWorkingMessage("Getting ONODE status...");

                var statusResult = await _onetManager!.GetNetworkStatusAsync();
                if (statusResult.IsError)
                {
                    CLIEngine.ShowErrorMessage($"Failed to get ONODE status: {statusResult.Message}");
                    return;
                }

            var status = statusResult.Result;
            Console.WriteLine();
            CLIEngine.ShowMessage("=== ONODE STATUS ===", ConsoleColor.Green);
            CLIEngine.ShowMessage($"Network ID: {status.NetworkId}", ConsoleColor.White);
            CLIEngine.ShowMessage($"Is Running: {status.IsRunning}", ConsoleColor.White);
            CLIEngine.ShowMessage($"Connected Nodes: {status.ConnectedNodes}", ConsoleColor.White);
            CLIEngine.ShowMessage($"Network Health: {status.NetworkHealth:P1}", ConsoleColor.White);
            CLIEngine.ShowMessage($"Last Activity: {status.LastActivity}", ConsoleColor.White);
            }
            catch (Exception ex)
            {
                CLIEngine.ShowErrorMessage($"Error getting ONODE status: {ex.Message}");
            }
        }

        private static async Task OpenONODEConfigAsync()
        {
            try
            {
                CLIEngine.ShowWorkingMessage("Opening ONODE WEB4 OASIS DNA configuration...");
                
                var configPath = Path.Combine(Environment.CurrentDirectory, "DNA", "OASIS_DNA.json");
                if (File.Exists(configPath))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = configPath,
                        UseShellExecute = true
                    });
                    CLIEngine.ShowSuccessMessage("ONODE WEB4 OASIS DNA configuration opened in default editor");
                }
                else
                {
                    CLIEngine.ShowErrorMessage("OASISDNA.json configuration file not found");
                }
            }
            catch (Exception ex)
            {
                CLIEngine.ShowErrorMessage($"Error opening ONODE WEB4 OASIS DNA configuration: {ex.Message}");
            }
        }

        private static async Task OpenONODEWeb5ConfigAsync()
        {
            try
            {
                CLIEngine.ShowWorkingMessage("Opening ONODE WEB5 STAR DNA configuration...");

                var configPath = Path.Combine(Environment.CurrentDirectory, "DNA", "STAR_DNA.json");
                if (File.Exists(configPath))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = configPath,
                        UseShellExecute = true
                    });
                    CLIEngine.ShowSuccessMessage("ONODE WEB5 STAR DNA configuration opened in default editor");
                }
                else
                {
                    CLIEngine.ShowErrorMessage("STARDNA.json configuration file not found");
                }
            }
            catch (Exception ex)
            {
                CLIEngine.ShowErrorMessage($"Error opening ONODE WEB5 STAR DNA configuration: {ex.Message}");
            }
        }

        private static async Task ONODEProvidersAsync(NextGenSoftware.OASIS.ONODE.Client.SupervisorClient client, string[] inputArgs)
        {
            // onode providers [list|enable|disable|priority] [providerType] [--service web4] [--priority N]
            string sub = inputArgs.Length > 2 ? inputArgs[2].ToLower() : "list";

            if (!client.IsAvailable)
            {
                CLIEngine.ShowWarningMessage("ONODEService not running. Provider management requires the supervisor.");
                return;
            }

            switch (sub)
            {
                case "list":
                {
                    CLIEngine.ShowWorkingMessage("Loading providers…");
                    var providers = await client.GetProvidersAsync();
                    if (providers == null || providers.Count == 0)
                    {
                        CLIEngine.ShowMessage("No providers configured in OASISDNA.json.", ConsoleColor.Yellow);
                        return;
                    }
                    Console.WriteLine();
                    CLIEngine.ShowMessage("OASIS Storage Providers:", ConsoleColor.Cyan);
                    CLIEngine.ShowMessage(new string('─', 50), ConsoleColor.DarkGray);
                    foreach (var p in providers.OrderBy(x => x.Priority))
                    {
                        var dot     = p.IsEnabled ? "●" : "○";
                        var colour  = p.IsEnabled ? ConsoleColor.Green : ConsoleColor.Gray;
                        var label   = p.IsEnabled ? "Enabled" : "Disabled";
                        CLIEngine.ShowMessage($"  {p.Priority,2}. {p.ProviderType,-24} {dot} {label}", colour, false);
                    }
                    break;
                }

                case "enable":
                {
                    var providerType = inputArgs.Length > 3 ? inputArgs[3] : null;
                    if (string.IsNullOrEmpty(providerType))
                    { CLIEngine.ShowErrorMessage("Usage: onode providers enable <ProviderType>"); return; }
                    CLIEngine.ShowWorkingMessage($"Enabling {providerType}…");
                    var result = await client.EnableProviderAsync(providerType);
                    if (result != null)
                    {
                        CLIEngine.ShowSuccessMessage(result.Message);
                        if (result.ReloadRequired)
                            CLIEngine.ShowMessage("⚠ Restart the affected service to apply: onode restart web4", ConsoleColor.Yellow);
                    }
                    else CLIEngine.ShowErrorMessage($"Failed to enable {providerType}. Check it exists in OASISDNA.json.");
                    break;
                }

                case "disable":
                {
                    var providerType = inputArgs.Length > 3 ? inputArgs[3] : null;
                    if (string.IsNullOrEmpty(providerType))
                    { CLIEngine.ShowErrorMessage("Usage: onode providers disable <ProviderType>"); return; }
                    CLIEngine.ShowWorkingMessage($"Disabling {providerType}…");
                    var result = await client.DisableProviderAsync(providerType);
                    if (result != null)
                    {
                        CLIEngine.ShowSuccessMessage(result.Message);
                        if (result.ReloadRequired)
                            CLIEngine.ShowMessage("⚠ Restart the affected service to apply: onode restart web4", ConsoleColor.Yellow);
                    }
                    else CLIEngine.ShowErrorMessage($"Failed to disable {providerType}.");
                    break;
                }

                case "priority":
                {
                    var providerType = inputArgs.Length > 3 ? inputArgs[3] : null;
                    var priorityStr  = inputArgs.Length > 4 ? inputArgs[4] : null;
                    if (string.IsNullOrEmpty(providerType) || !int.TryParse(priorityStr, out int priority))
                    { CLIEngine.ShowErrorMessage("Usage: onode providers priority <ProviderType> <N>"); return; }
                    CLIEngine.ShowWorkingMessage($"Setting {providerType} priority to {priority}…");
                    var result = await client.SetProviderPriorityAsync(providerType, priority);
                    if (result != null) CLIEngine.ShowSuccessMessage(result.Message);
                    else CLIEngine.ShowErrorMessage($"Failed to set priority for {providerType}.");
                    break;
                }

                default:
                    CLIEngine.ShowErrorMessage($"Unknown providers subcommand '{sub}'. Use: list | enable | disable | priority");
                    break;
            }
        }

        private static async Task ShowONODEProvidersAsync()
        {
            // Legacy stub — now routed to ONODEProvidersAsync
            CLIEngine.ShowMessage("Use: onode providers list|enable|disable|priority", ConsoleColor.Yellow);
            await Task.CompletedTask;
        }

        private static async Task StartONODEProviderAsync(string providerName)
        {
            try
            {
                await InitializeONETAsync();
                CLIEngine.ShowWorkingMessage($"Starting provider: {providerName}...");

            // Provider management not implemented in ONETManager
            CLIEngine.ShowErrorMessage($"Provider management not implemented for {providerName}");
            }
            catch (Exception ex)
            {
                CLIEngine.ShowErrorMessage($"Error starting provider {providerName}: {ex.Message}");
            }
        }

        private static async Task StopONODEProviderAsync(string providerName)
        {
            try
            {
                await InitializeONETAsync();
                CLIEngine.ShowWorkingMessage($"Stopping provider: {providerName}...");

            // Provider management not implemented in ONETManager
            CLIEngine.ShowErrorMessage($"Provider management not implemented for {providerName}");
            }
            catch (Exception ex)
            {
                CLIEngine.ShowErrorMessage($"Error stopping provider {providerName}: {ex.Message}");
            }
        }
    }
}
