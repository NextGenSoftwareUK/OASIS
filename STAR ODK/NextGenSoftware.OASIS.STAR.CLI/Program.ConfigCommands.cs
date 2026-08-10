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
        private static async Task ShowConfigSubCommandAsync(string[] inputArgs)
        {
            Console.WriteLine("");
            if (inputArgs.Length > 1 && inputArgs[1].ToLower() == "dna")
            {
                ShowDNAPaths();
                Console.WriteLine("");
                return;
            }
            ShowDNAPaths();
            Console.WriteLine("");
            if (inputArgs.Length > 1)
            {
                switch (inputArgs[1].ToLower())
                {
                    case "dna":
                        // Handled above
                        break;

                    case "cosmicdetailedoutput":
                        { 
                            if (inputArgs.Length > 2)
                            {
                                switch (inputArgs[2].ToLower())
                                {
                                    case "enabled":
                                        {
                                            STAR.IsDetailedCOSMICOutputsEnabled = true;
                                            CLIEngine.ShowMessage("Detailed COSMIC Output Enabled.");
                                        }
                                        break;

                                    case "disabled":
                                        {
                                            STAR.IsDetailedCOSMICOutputsEnabled = false;
                                            CLIEngine.ShowMessage("Detailed COSMIC Output Disabled.");
                                        }
                                        break;

                                    case "status":
                                        {
                                            if (STAR.IsDetailedCOSMICOutputsEnabled)
                                                CLIEngine.ShowSuccessMessage("COSMIC Detailed Output Status: Enabled.");
                                            else
                                                CLIEngine.ShowSuccessMessage("COSMIC Detailed Output Status: Disabled.");
                                        }
                                        break;

                                    default:
                                        CLIEngine.ShowErrorMessage("Command Unknown.");
                                        break;
                                }
                            }
                            else
                            {
                                if (STAR.IsDetailedCOSMICOutputsEnabled)
                                    CLIEngine.ShowSuccessMessage("COSMIC Detailed Output Status: Enabled.");
                                else
                                    CLIEngine.ShowSuccessMessage("COSMIC Detailed Output Status: Disabled.");
                            }
                        }
                        break;

                    case "starstatusdetailedoutput":
                        {
                            if (inputArgs.Length > 2)
                            {
                                switch (inputArgs[2].ToLower())
                                {
                                    case "enabled":
                                        {
                                            STAR.IsDetailedCOSMICOutputsEnabled = true;
                                            CLIEngine.ShowSuccessMessage("STAR Detailed Status Enabled.");
                                        }
                                        break;

                                    case "disabled":
                                        {
                                            STAR.IsDetailedCOSMICOutputsEnabled = false;
                                            CLIEngine.ShowSuccessMessage("STAR Detailed Status Disabled.");
                                        }
                                        break;

                                    case "status":
                                        {
                                            if (STAR.IsDetailedCOSMICOutputsEnabled)
                                                CLIEngine.ShowMessage("STAR Detailed Status: Enabled.");
                                            else
                                                CLIEngine.ShowMessage("STAR Detailed Status: Disabled.");
                                        }
                                        break;

                                    default:
                                        CLIEngine.ShowErrorMessage("Command Unknown.");
                                        break;
                                }
                            }
                            else
                            {
                                if (STAR.IsDetailedCOSMICOutputsEnabled)
                                    CLIEngine.ShowMessage("STAR Detailed Status: Enabled.");
                                else
                                    CLIEngine.ShowMessage("STAR Detailed Status: Disabled.");
                            }
                        }
                        break;

                    case "logproviderswitching":
                        {
                            if (inputArgs.Length > 2)
                            {
                                switch (inputArgs[2].ToLower())
                                {
                                    case "enabled":
                                        {
                                            ProviderManager.Instance.OASISDNA.OASIS.StorageProviders.LogSwitchingProviders = true;
                                            CLIEngine.ShowSuccessMessage("OASIS Hyperdrive Provider Switching Logging: Enabled.");
                                        }
                                        break;

                                    case "disabled":
                                        {
                                            ProviderManager.Instance.OASISDNA.OASIS.StorageProviders.LogSwitchingProviders = false;
                                            CLIEngine.ShowSuccessMessage("OASIS Hyperdrive Provider Switching Logging: Disabled.");
                                        }
                                        break;

                                    case "status":
                                        {
                                            if (ProviderManager.Instance.OASISDNA.OASIS.StorageProviders.LogSwitchingProviders)
                                                CLIEngine.ShowMessage("OASIS Hyperdrive Provider Switching Logging: Enabled.");
                                            else
                                                CLIEngine.ShowMessage("OASIS Hyperdrive Provider Switching Logging: Disabled.");
                                        }
                                        break;

                                    default:
                                        CLIEngine.ShowErrorMessage("Command Unknown.");
                                        break;
                                }
                            }
                            else
                            {
                                if (ProviderManager.Instance.OASISDNA.OASIS.StorageProviders.LogSwitchingProviders)
                                    CLIEngine.ShowMessage("OASIS Hyperdrive Provider Switching Logging: Enabled.");
                                else
                                    CLIEngine.ShowMessage("OASIS Hyperdrive Provider Switching Logging: Disabled.");
                            }
                        }
                        break;

                    default:
                        CLIEngine.ShowErrorMessage("Command Unknown.");
                        break;
                }
            }
            else
            {
                Console.WriteLine("");
                CLIEngine.ShowMessage($"CONFIG SUBCOMMANDS:", ConsoleColor.Green);
                Console.WriteLine("");
                CLIEngine.ShowMessage("    dna                       Shows paths to DNATemplates, OASIS DNA and STAR DNA.", ConsoleColor.Green, false);
                CLIEngine.ShowMessage("    cosmicdetailedoutput     [enable/disable/status] Enables/disables COSMIC Detailed Output.", ConsoleColor.Green, false);
                CLIEngine.ShowMessage("    starstatusdetailedoutput [enable/disable/status] Enables/disables STAR ODK Detailed Output.", ConsoleColor.Green, false);
                CLIEngine.ShowMessage("    logproviderswitching     [enable/disable/status] Enables/disables OASIS Hyperdrive Provider Switching Logging.", ConsoleColor.Green, false);
                CLIEngine.ShowMessage("More Coming Soon...", ConsoleColor.Green);
            }
        }

        // ─── ONODE Commands ────────────────────────────────────────────────────────
        // All onode commands route through ONODEService supervisor API (127.0.0.1:8765).
        // Falls back to direct process spawn if supervisor is not installed/running.

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



        private static async Task ShowONETStatusAsync()
        {
            try
            {
                await InitializeONETAsync();
                CLIEngine.ShowWorkingMessage("Getting ONET network status...");

                var statusResult = await _onetManager!.GetNetworkStatusAsync();
                if (statusResult.IsError)
                {
                    CLIEngine.ShowErrorMessage($"Failed to get ONET status: {statusResult.Message}");
                    return;
                }

                var status = statusResult.Result;
                Console.WriteLine();
                CLIEngine.ShowMessage("=== ONET NETWORK STATUS ===", ConsoleColor.Green);
                CLIEngine.ShowMessage($"Is Running: {status.IsRunning}", ConsoleColor.White);
                CLIEngine.ShowMessage($"Connected Nodes: {status.ConnectedNodes}", ConsoleColor.White);
                CLIEngine.ShowMessage($"Network Health: {status.NetworkHealth:P1}", ConsoleColor.White);
                CLIEngine.ShowMessage($"Network ID: {status.NetworkId}", ConsoleColor.White);
                CLIEngine.ShowMessage($"Last Activity: {status.LastActivity}", ConsoleColor.White);
            }
            catch (Exception ex)
            {
                CLIEngine.ShowErrorMessage($"Error getting ONET status: {ex.Message}");
            }
        }

        private static async Task ShowONETProvidersAsync()
        {
            try
            {
                await InitializeONETAsync();
                CLIEngine.ShowWorkingMessage("Getting ONET network providers...");

                // Get network stats instead of providers (providers method doesn't exist)
                var statsResult = await _onetManager!.GetNetworkStatsAsync();
                if (statsResult.IsError)
                {
                    CLIEngine.ShowErrorMessage($"Failed to get ONET stats: {statsResult.Message}");
                    return;
                }

                var stats = statsResult.Result;
                Console.WriteLine();
                CLIEngine.ShowMessage("=== ONET NETWORK STATS ===", ConsoleColor.Green);
                
                foreach (var stat in stats)
                {
                    CLIEngine.ShowMessage($"â€¢ {stat.Key}: {stat.Value}", ConsoleColor.White);
                }
            }
            catch (Exception ex)
            {
                CLIEngine.ShowErrorMessage($"Error getting ONET providers: {ex.Message}");
            }
        }

        private static async Task DiscoverONETNodesAsync()
        {
            try
            {
                await InitializeONETAsync();
                CLIEngine.ShowWorkingMessage("Discovering ONET nodes...");

                var discoveryResult = await _onetDiscovery!.DiscoverAvailableNodesAsync();
                if (discoveryResult.IsError)
                {
                    CLIEngine.ShowErrorMessage($"Failed to discover nodes: {discoveryResult.Message}");
                    return;
                }

                var nodes = discoveryResult.Result;
                Console.WriteLine();
                CLIEngine.ShowMessage("=== DISCOVERED ONET NODES ===", ConsoleColor.Green);
                
                if (nodes.Any())
                {
                    foreach (var node in nodes)
                    {
                        CLIEngine.ShowMessage($"â€¢ {node.Id} - {node.Address}", ConsoleColor.White);
                        CLIEngine.ShowMessage($"  Status: {node.Status} | Latency: {node.Latency}ms | Reliability: {node.Reliability}%", ConsoleColor.Gray);
                        CLIEngine.ShowMessage($"  Capabilities: {string.Join(", ", node.Capabilities)}", ConsoleColor.Gray);
                    }
                }
                else
                {
                    CLIEngine.ShowMessage("No ONET nodes discovered", ConsoleColor.Yellow);
                }
            }
            catch (Exception ex)
            {
                CLIEngine.ShowErrorMessage($"Error discovering ONET nodes: {ex.Message}");
            }
        }

        private static async Task ConnectToONETNodeAsync(string nodeAddress)
        {
            try
            {
                await InitializeONETAsync();
                CLIEngine.ShowWorkingMessage($"Connecting to ONET node: {nodeAddress}...");

                var result = await _onetManager!.ConnectToNodeAsync(nodeAddress, nodeAddress);
                if (result.IsError)
                {
                    CLIEngine.ShowErrorMessage($"Failed to connect to node {nodeAddress}: {result.Message}");
                }
                else
                {
                    CLIEngine.ShowSuccessMessage($"Successfully connected to ONET node: {nodeAddress}");
                }
            }
            catch (Exception ex)
            {
                CLIEngine.ShowErrorMessage($"Error connecting to ONET node {nodeAddress}: {ex.Message}");
            }
        }

        private static async Task DisconnectFromONETNodeAsync(string nodeAddress)
        {
            try
            {
                await InitializeONETAsync();
                CLIEngine.ShowWorkingMessage($"Disconnecting from ONET node: {nodeAddress}...");

                var result = await _onetManager!.DisconnectFromNodeAsync(nodeAddress);
                if (result.IsError)
                {
                    CLIEngine.ShowErrorMessage($"Failed to disconnect from node {nodeAddress}: {result.Message}");
                }
                else
                {
                    CLIEngine.ShowSuccessMessage($"Successfully disconnected from ONET node: {nodeAddress}");
                }
            }
            catch (Exception ex)
            {
                CLIEngine.ShowErrorMessage($"Error disconnecting from ONET node {nodeAddress}: {ex.Message}");
            }
        }

        private static async Task ShowONETTopologyAsync()
        {
            try
            {
                await InitializeONETAsync();
                CLIEngine.ShowWorkingMessage("Getting ONET network topology...");

                var topologyResult = await _onetManager!.GetNetworkTopologyAsync();
                if (topologyResult.IsError)
                {
                    CLIEngine.ShowErrorMessage($"Failed to get network topology: {topologyResult.Message}");
                    return;
                }

                var topology = topologyResult.Result;
                Console.WriteLine();
                CLIEngine.ShowMessage("=== ONET NETWORK TOPOLOGY ===", ConsoleColor.Green);
                CLIEngine.ShowMessage($"Total Nodes: {topology.Nodes.Count}", ConsoleColor.White);
                CLIEngine.ShowMessage($"Connections: {topology.Connections.Count}", ConsoleColor.White);
                CLIEngine.ShowMessage($"Last Updated: {topology.LastUpdated}", ConsoleColor.White);
                
                if (topology.Nodes.Any())
                {
                    CLIEngine.ShowMessage("\nNodes:", ConsoleColor.Yellow);
                    foreach (var node in topology.Nodes)
                    {
                        CLIEngine.ShowMessage($"â€¢ {node.Id} - {node.Address} (Status: {node.Status})", ConsoleColor.Gray);
                    }
                }
                
                if (topology.Connections.Any())
                {
                    CLIEngine.ShowMessage("\nConnections:", ConsoleColor.Yellow);
                    foreach (var connection in topology.Connections)
                    {
                        CLIEngine.ShowMessage($"â€¢ {connection.FromNodeId} â†” {connection.ToNodeId} (Latency: {connection.Latency}ms)", ConsoleColor.Gray);
                    }
                }
            }
            catch (Exception ex)
            {
                CLIEngine.ShowErrorMessage($"Error getting ONET topology: {ex.Message}");
            }
        }



        private static async Task ShowGameSessionCommandAsync(string[] inputArgs, string command)
        {
            try
            {
                if (inputArgs.Length < 3)
                {
                    CLIEngine.ShowErrorMessage($"Usage: game {command} <gameId>");
                    return;
                }

                if (!Guid.TryParse(inputArgs[2], out Guid gameId))
                {
                    CLIEngine.ShowErrorMessage("Invalid game ID. Please provide a valid GUID.");
                    return;
                }

                var gameManager = new NextGenSoftware.OASIS.API.ONODE.Core.Managers.GameManager(STAR.BeamedInAvatar?.Id ?? Guid.Empty, STAR.STARDNA);
                OASISResult<GameSession> result;

                switch (command.ToLower())
                {
                    case "start":
                        CLIEngine.ShowWorkingMessage($"Starting game session for game {gameId}...");
                        result = await gameManager.StartGameAsync(gameId, STAR.BeamedInAvatar?.Id ?? Guid.Empty);
                        if (!result.IsError && result.Result != null)
                        {
                            CLIEngine.ShowSuccessMessage($"Game session started successfully. Session ID: {result.Result.Id}");
                        }
                        else
                        {
                            CLIEngine.ShowErrorMessage($"Failed to start game session: {result.Message}");
                        }
                        break;

                    case "end":
                        CLIEngine.ShowWorkingMessage($"Ending game session for game {gameId}...");
                        var endResult = await gameManager.EndGameAsync(gameId, STAR.BeamedInAvatar?.Id ?? Guid.Empty);
                        if (!endResult.IsError)
                        {
                            CLIEngine.ShowSuccessMessage("Game session ended successfully.");
                        }
                        else
                        {
                            CLIEngine.ShowErrorMessage($"Failed to end game session: {endResult.Message}");
                        }
                        break;

                    case "load":
                        CLIEngine.ShowWorkingMessage($"Loading game {gameId}...");
                        var loadResult = await gameManager.LoadGameAsync(gameId, STAR.BeamedInAvatar?.Id ?? Guid.Empty);
                        if (!loadResult.IsError)
                        {
                            CLIEngine.ShowSuccessMessage("Game loaded successfully.");
                        }
                        else
                        {
                            CLIEngine.ShowErrorMessage($"Failed to load game: {loadResult.Message}");
                        }
                        break;

                    case "unload":
                        CLIEngine.ShowWorkingMessage($"Unloading game {gameId}...");
                        var unloadResult = await gameManager.UnloadGameAsync(gameId, STAR.BeamedInAvatar?.Id ?? Guid.Empty);
                        if (!unloadResult.IsError)
                        {
                            CLIEngine.ShowSuccessMessage("Game unloaded successfully.");
                        }
                        else
                        {
                            CLIEngine.ShowErrorMessage($"Failed to unload game: {unloadResult.Message}");
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                CLIEngine.ShowErrorMessage($"Error executing game session command: {ex.Message}");
            }
        }

        private static async Task ShowGameLevelCommandAsync(string[] inputArgs, string command)
        {
            try
            {
                if (inputArgs.Length < 4)
                {
                    CLIEngine.ShowErrorMessage($"Usage: game {command} <gameId> <level> [x] [y] [z]");
                    return;
                }

                if (!Guid.TryParse(inputArgs[2], out Guid gameId))
                {
                    CLIEngine.ShowErrorMessage("Invalid game ID. Please provide a valid GUID.");
                    return;
                }

                string level = inputArgs[3];
                var gameManager = new NextGenSoftware.OASIS.API.ONODE.Core.Managers.GameManager(STAR.BeamedInAvatar?.Id ?? Guid.Empty, STAR.STARDNA);
                OASISResult<bool> result;

                switch (command.ToLower())
                {
                    case "loadlevel":
                        CLIEngine.ShowWorkingMessage($"Loading level '{level}' for game {gameId}...");
                        result = await gameManager.LoadLevelAsync(gameId, level, STAR.BeamedInAvatar?.Id ?? Guid.Empty);
                        if (!result.IsError && result.Result)
                        {
                            CLIEngine.ShowSuccessMessage($"Level '{level}' loaded successfully.");
                        }
                        else
                        {
                            CLIEngine.ShowErrorMessage($"Failed to load level: {result.Message}");
                        }
                        break;

                    case "unloadlevel":
                        CLIEngine.ShowWorkingMessage($"Unloading level '{level}' for game {gameId}...");
                        var unloadLevelResult = await gameManager.UnloadLevelAsync(gameId, level);
                        if (!unloadLevelResult.IsError && unloadLevelResult.Result)
                        {
                            CLIEngine.ShowSuccessMessage($"Level '{level}' unloaded successfully.");
                        }
                        else
                        {
                            CLIEngine.ShowErrorMessage($"Failed to unload level: {unloadLevelResult.Message}");
                        }
                        break;

                    case "jumptolevel":
                        CLIEngine.ShowWorkingMessage($"Jumping to level '{level}' for game {gameId}...");
                        result = await gameManager.JumpToLevelAsync(gameId, level, STAR.BeamedInAvatar?.Id ?? Guid.Empty);
                        if (!result.IsError && result.Result)
                        {
                            CLIEngine.ShowSuccessMessage($"Jumped to level '{level}' successfully.");
                        }
                        else
                        {
                            CLIEngine.ShowErrorMessage($"Failed to jump to level: {result.Message}");
                        }
                        break;

                    case "jumptopoint":
                        if (inputArgs.Length < 7)
                        {
                            CLIEngine.ShowErrorMessage("Usage: game jumptopoint <gameId> <level> <x> <y> <z>");
                            return;
                        }

                        if (!float.TryParse(inputArgs[4], out float x) || !float.TryParse(inputArgs[5], out float y) || !float.TryParse(inputArgs[6], out float z))
                        {
                            CLIEngine.ShowErrorMessage("Invalid coordinates. Please provide valid float values for x, y, and z.");
                            return;
                        }

                        CLIEngine.ShowWorkingMessage($"Jumping to point ({x}, {y}, {z}) in level '{level}' for game {gameId}...");
                        result = await gameManager.JumpToPointInLevelAsync(gameId, level, x, y, z, STAR.BeamedInAvatar?.Id ?? Guid.Empty);
                        if (!result.IsError && result.Result)
                        {
                            CLIEngine.ShowSuccessMessage($"Jumped to point ({x}, {y}, {z}) successfully.");
                        }
                        else
                        {
                            CLIEngine.ShowErrorMessage($"Failed to jump to point: {result.Message}");
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                CLIEngine.ShowErrorMessage($"Error executing game level command: {ex.Message}");
            }
        }

        private static async Task ShowGameAreaCommandAsync(string[] inputArgs, string command)
        {
            try
            {
                var gameManager = new NextGenSoftware.OASIS.API.ONODE.Core.Managers.GameManager(STAR.BeamedInAvatar?.Id ?? Guid.Empty, STAR.STARDNA);
                OASISResult<Guid> result;

                switch (command.ToLower())
                {
                    case "loadarea":
                        if (inputArgs.Length < 7)
                        {
                            CLIEngine.ShowErrorMessage("Usage: game loadarea <gameId> <x> <y> <z> <radius>");
                            return;
                        }

                        if (!Guid.TryParse(inputArgs[2], out Guid gameId) || 
                            !float.TryParse(inputArgs[3], out float x) || 
                            !float.TryParse(inputArgs[4], out float y) || 
                            !float.TryParse(inputArgs[5], out float z) || 
                            !float.TryParse(inputArgs[6], out float radius))
                        {
                            CLIEngine.ShowErrorMessage("Invalid parameters. Please provide valid GUID and float values.");
                            return;
                        }

                        CLIEngine.ShowWorkingMessage($"Loading area at ({x}, {y}, {z}) with radius {radius} for game {gameId}...");
                        result = await gameManager.LoadAreaAsync(gameId, x, y, z, radius, STAR.BeamedInAvatar?.Id ?? Guid.Empty);
                        if (!result.IsError && result.Result != Guid.Empty)
                        {
                            CLIEngine.ShowSuccessMessage($"Area loaded successfully. Area ID: {result.Result}");
                        }
                        else
                        {
                            CLIEngine.ShowErrorMessage($"Failed to load area: {result.Message}");
                        }
                        break;

                    case "unloadarea":
                        if (inputArgs.Length < 4)
                        {
                            CLIEngine.ShowErrorMessage("Usage: game unloadarea <gameId> <areaId>");
                            return;
                        }

                        if (!Guid.TryParse(inputArgs[2], out gameId) || !Guid.TryParse(inputArgs[3], out Guid areaId))
                        {
                            CLIEngine.ShowErrorMessage("Invalid game ID or area ID. Please provide valid GUIDs.");
                            return;
                        }

                        CLIEngine.ShowWorkingMessage($"Unloading area {areaId} for game {gameId}...");
                        var unloadResult = await gameManager.UnloadAreaAsync(gameId, areaId);
                        if (!unloadResult.IsError && unloadResult.Result)
                        {
                            CLIEngine.ShowSuccessMessage("Area unloaded successfully.");
                        }
                        else
                        {
                            CLIEngine.ShowErrorMessage($"Failed to unload area: {unloadResult.Message}");
                        }
                        break;

                    case "jumptoarea":
                        if (inputArgs.Length < 6)
                        {
                            CLIEngine.ShowErrorMessage("Usage: game jumptoarea <gameId> <x> <y> <z>");
                            return;
                        }

                        if (!Guid.TryParse(inputArgs[2], out gameId) || 
                            !float.TryParse(inputArgs[3], out x) || 
                            !float.TryParse(inputArgs[4], out y) || 
                            !float.TryParse(inputArgs[5], out z))
                        {
                            CLIEngine.ShowErrorMessage("Invalid parameters. Please provide valid GUID and float values.");
                            return;
                        }

                        CLIEngine.ShowWorkingMessage($"Jumping to area at ({x}, {y}, {z}) for game {gameId}...");
                        var jumpResult = await gameManager.JumpToAreaAsync(gameId, x, y, z, STAR.BeamedInAvatar?.Id ?? Guid.Empty);
                        if (!jumpResult.IsError && jumpResult.Result != Guid.Empty)
                        {
                            CLIEngine.ShowSuccessMessage($"Jumped to area successfully.");
                        }
                        else
                        {
                            CLIEngine.ShowErrorMessage($"Failed to jump to area: {jumpResult.Message}");
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                CLIEngine.ShowErrorMessage($"Error executing game area command: {ex.Message}");
            }
        }

        private static async Task ShowGameUICommandAsync(string[] inputArgs, string command)
        {
            try
            {
                if (inputArgs.Length < 3)
                {
                    CLIEngine.ShowErrorMessage($"Usage: game {command} <gameId>");
                    return;
                }

                if (!Guid.TryParse(inputArgs[2], out Guid gameId))
                {
                    CLIEngine.ShowErrorMessage("Invalid game ID. Please provide a valid GUID.");
                    return;
                }

                var gameManager = new NextGenSoftware.OASIS.API.ONODE.Core.Managers.GameManager(STAR.BeamedInAvatar?.Id ?? Guid.Empty, STAR.STARDNA);
                OASISResult<bool> result = default;

                switch (command.ToLower())
                {
                    case "showtitlescreen":
                        CLIEngine.ShowWorkingMessage($"Showing title screen for game {gameId}...");
                        result = await gameManager.ShowTitleScreenAsync(gameId, STAR.BeamedInAvatar?.Id ?? Guid.Empty);
                        break;

                    case "showmainmenu":
                        CLIEngine.ShowWorkingMessage($"Showing main menu for game {gameId}...");
                        result = await gameManager.ShowMainMenuAsync(gameId, STAR.BeamedInAvatar?.Id ?? Guid.Empty);
                        break;

                    case "showoptions":
                        CLIEngine.ShowWorkingMessage($"Showing options menu for game {gameId}...");
                        result = await gameManager.ShowOptionsAsync(gameId, STAR.BeamedInAvatar?.Id ?? Guid.Empty);
                        break;

                    case "showcredits":
                        CLIEngine.ShowWorkingMessage($"Showing credits for game {gameId}...");
                        result = await gameManager.ShowCreditsAsync(gameId, STAR.BeamedInAvatar?.Id ?? Guid.Empty);
                        break;
                }

                if (result != null)
                {
                    if (!result.IsError && result.Result)
                    {
                        CLIEngine.ShowSuccessMessage($"UI command executed successfully.");
                    }
                    else
                    {
                        CLIEngine.ShowErrorMessage($"Failed to execute UI command: {result.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                CLIEngine.ShowErrorMessage($"Error executing game UI command: {ex.Message}");
            }
        }

        private static async Task ShowGameAudioCommandAsync(string[] inputArgs, string command)
        {
            try
            {
                var gameManager = new NextGenSoftware.OASIS.API.ONODE.Core.Managers.GameManager(STAR.BeamedInAvatar?.Id ?? Guid.Empty, STAR.STARDNA);

                switch (command.ToLower())
                {
                    case "setmastervolume":
                    case "setvoicevolume":
                    case "setsoundvolume":
                        if (inputArgs.Length < 4)
                        {
                            CLIEngine.ShowErrorMessage($"Usage: game {command} <gameId> <volume> (0.0 - 1.0)");
                            return;
                        }

                        if (!Guid.TryParse(inputArgs[2], out Guid gameId) || !float.TryParse(inputArgs[3], out float volume))
                        {
                            CLIEngine.ShowErrorMessage("Invalid game ID or volume. Please provide a valid GUID and volume (0.0 - 1.0).");
                            return;
                        }

                        if (volume < 0.0f || volume > 1.0f)
                        {
                            CLIEngine.ShowErrorMessage("Volume must be between 0.0 and 1.0.");
                            return;
                        }

                        OASISResult<bool> result;
                        if (command.ToLower() == "setmastervolume")
                        {
                            CLIEngine.ShowWorkingMessage($"Setting master volume to {volume} for game {gameId}...");
                            result = await gameManager.SetMasterVolumeAsync(gameId, STAR.BeamedInAvatar?.Id ?? Guid.Empty, volume);
                        }
                        else if (command.ToLower() == "setvoicevolume")
                        {
                            CLIEngine.ShowWorkingMessage($"Setting voice volume to {volume} for game {gameId}...");
                            result = await gameManager.SetVoiceVolumeAsync(gameId, STAR.BeamedInAvatar?.Id ?? Guid.Empty, volume);
                        }
                        else
                        {
                            CLIEngine.ShowWorkingMessage($"Setting sound volume to {volume} for game {gameId}...");
                            result = await gameManager.SetSoundVolumeAsync(gameId, STAR.BeamedInAvatar?.Id ?? Guid.Empty, volume);
                        }

                        if (!result.IsError && result.Result)
                        {
                            CLIEngine.ShowSuccessMessage("Volume set successfully.");
                        }
                        else
                        {
                            CLIEngine.ShowErrorMessage($"Failed to set volume: {result.Message}");
                        }
                        break;

                    case "getmastervolume":
                    case "getvoicevolume":
                    case "getsoundvolume":
                        if (inputArgs.Length < 3)
                        {
                            CLIEngine.ShowErrorMessage($"Usage: game {command} <gameId>");
                            return;
                        }

                        if (!Guid.TryParse(inputArgs[2], out gameId))
                        {
                            CLIEngine.ShowErrorMessage("Invalid game ID. Please provide a valid GUID.");
                            return;
                        }

                        OASISResult<double> volumeResult;
                        if (command.ToLower() == "getmastervolume")
                        {
                            volumeResult = await gameManager.GetMasterVolumeAsync(gameId, STAR.BeamedInAvatar?.Id ?? Guid.Empty);
                        }
                        else if (command.ToLower() == "getvoicevolume")
                        {
                            volumeResult = await gameManager.GetVoiceVolumeAsync(gameId, STAR.BeamedInAvatar?.Id ?? Guid.Empty);
                        }
                        else
                        {
                            volumeResult = await gameManager.GetSoundVolumeAsync(gameId, STAR.BeamedInAvatar?.Id ?? Guid.Empty);
                        }

                        if (!volumeResult.IsError)
                        {
                            CLIEngine.ShowSuccessMessage($"Current volume: {volumeResult.Result}");
                        }
                        else
                        {
                            CLIEngine.ShowErrorMessage($"Failed to get volume: {volumeResult.Message}");
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                CLIEngine.ShowErrorMessage($"Error executing game audio command: {ex.Message}");
            }
        }

        private static async Task ShowGameVideoCommandAsync(string[] inputArgs, string command)
        {
            try
            {
                var gameManager = new NextGenSoftware.OASIS.API.ONODE.Core.Managers.GameManager(STAR.BeamedInAvatar?.Id ?? Guid.Empty, STAR.STARDNA);

                switch (command.ToLower())
                {
                    case "setvideosetting":
                        if (inputArgs.Length < 4)
                        {
                            CLIEngine.ShowErrorMessage("Usage: game setvideosetting <gameId> <Low|Medium|High|Custom>");
                            return;
                        }

                        if (!Guid.TryParse(inputArgs[2], out Guid gameId))
                        {
                            CLIEngine.ShowErrorMessage("Invalid game ID. Please provide a valid GUID.");
                            return;
                        }

                        if (!Enum.TryParse<VideoSetting>(inputArgs[3], true, out VideoSetting videoSetting))
                        {
                            CLIEngine.ShowErrorMessage("Invalid video setting. Please use: Low, Medium, High, or Custom");
                            return;
                        }

                        CLIEngine.ShowWorkingMessage($"Setting video setting to {videoSetting} for game {gameId}...");
                        var result = await gameManager.SetVideoSettingAsync(gameId, STAR.BeamedInAvatar?.Id ?? Guid.Empty, videoSetting);
                        if (!result.IsError && result.Result)
                        {
                            CLIEngine.ShowSuccessMessage($"Video setting set to {videoSetting} successfully.");
                        }
                        else
                        {
                            CLIEngine.ShowErrorMessage($"Failed to set video setting: {result.Message}");
                        }
                        break;

                    case "getvideosetting":
                        if (inputArgs.Length < 3)
                        {
                            CLIEngine.ShowErrorMessage("Usage: game getvideosetting <gameId>");
                            return;
                        }

                        if (!Guid.TryParse(inputArgs[2], out gameId))
                        {
                            CLIEngine.ShowErrorMessage("Invalid game ID. Please provide a valid GUID.");
                            return;
                        }

                        var getResult = await gameManager.GetVideoSettingAsync(gameId, STAR.BeamedInAvatar?.Id ?? Guid.Empty);
                        if (!getResult.IsError)
                        {
                            CLIEngine.ShowSuccessMessage($"Current video setting: {getResult.Result}");
                        }
                        else
                        {
                            CLIEngine.ShowErrorMessage($"Failed to get video setting: {getResult.Message}");
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                CLIEngine.ShowErrorMessage($"Error executing game video command: {ex.Message}");
            }
        }

        private static async Task ShowGameInputCommandAsync(string[] inputArgs, string command)
        {
            try
            {
                if (command.ToLower() == "bindkeys")
                {
                    CLIEngine.ShowMessage("Key binding functionality coming soon...");
                    CLIEngine.ShowMessage("This will allow you to configure key bindings for games.");
                }
            }
            catch (Exception ex)
            {
                CLIEngine.ShowErrorMessage($"Error executing game input command: {ex.Message}");
            }
        }

        private static async Task ShowGameInventoryCommandAsync(string[] inputArgs)
        {
            try
            {
                if (inputArgs.Length < 3)
                {
                    CLIEngine.ShowMessage("GAME INVENTORY SUBCOMMANDS:", ConsoleColor.Green);
                    CLIEngine.ShowMessage("    inventory list              List all items in shared inventory", ConsoleColor.Green, false);
                    CLIEngine.ShowMessage("    inventory add <itemName>    Add item to shared inventory", ConsoleColor.Green, false);
                    CLIEngine.ShowMessage("    inventory remove <itemId>   Remove item from shared inventory", ConsoleColor.Green, false);
                    CLIEngine.ShowMessage("    inventory has <itemId>      Check if avatar has item by ID", ConsoleColor.Green, false);
                    CLIEngine.ShowMessage("    inventory hasbyname <name>  Check if avatar has item by name", ConsoleColor.Green, false);
                    return;
                }

                var gameManager = new NextGenSoftware.OASIS.API.ONODE.Core.Managers.GameManager(STAR.BeamedInAvatar?.Id ?? Guid.Empty, STAR.STARDNA);
                var avatarId = STAR.BeamedInAvatar?.Id ?? Guid.Empty;

                switch (inputArgs[2].ToLower())
                {
                    case "list":
                        CLIEngine.ShowWorkingMessage("Loading shared inventory...");
                        var listResult = await gameManager.GetSharedAssetsAsync(avatarId);
                        if (!listResult.IsError && listResult.Result != null)
                        {
                            CLIEngine.ShowSuccessMessage($"Found {listResult.Result.Count} item(s) in shared inventory:");
                            foreach (var item in listResult.Result)
                            {
                                CLIEngine.ShowMessage($"  â€¢ {item.Name} (ID: {item.Id})", ConsoleColor.White, false);
                            }
                        }
                        else
                        {
                            CLIEngine.ShowErrorMessage($"Failed to load inventory: {listResult.Message}");
                        }
                        break;

                    case "add":
                        if (inputArgs.Length < 4)
                        {
                            CLIEngine.ShowErrorMessage("Usage: game inventory add <itemName>");
                            return;
                        }
                        CLIEngine.ShowMessage("Adding items to inventory via CLI coming soon. Use the API directly for now.");
                        break;

                    case "remove":
                        if (inputArgs.Length < 4)
                        {
                            CLIEngine.ShowErrorMessage("Usage: game inventory remove <itemId>");
                            return;
                        }
                        if (!Guid.TryParse(inputArgs[3], out Guid itemId))
                        {
                            CLIEngine.ShowErrorMessage("Invalid item ID. Please provide a valid GUID.");
                            return;
                        }
                        CLIEngine.ShowWorkingMessage($"Removing item {itemId} from inventory...");
                        var removeResult = await gameManager.RemoveItemFromInventoryAsync(avatarId, itemId);
                        if (!removeResult.IsError && removeResult.Result)
                        {
                            CLIEngine.ShowSuccessMessage("Item removed from inventory successfully.");
                        }
                        else
                        {
                            CLIEngine.ShowErrorMessage($"Failed to remove item: {removeResult.Message}");
                        }
                        break;

                    case "has":
                        if (inputArgs.Length < 4)
                        {
                            CLIEngine.ShowErrorMessage("Usage: game inventory has <itemId>");
                            return;
                        }
                        if (!Guid.TryParse(inputArgs[3], out itemId))
                        {
                            CLIEngine.ShowErrorMessage("Invalid item ID. Please provide a valid GUID.");
                            return;
                        }
                        var hasResult = await gameManager.HasItemAsync(avatarId, itemId);
                        if (!hasResult.IsError)
                        {
                            CLIEngine.ShowSuccessMessage(hasResult.Result ? "Avatar has this item." : "Avatar does not have this item.");
                        }
                        else
                        {
                            CLIEngine.ShowErrorMessage($"Failed to check item: {hasResult.Message}");
                        }
                        break;

                    case "hasbyname":
                        if (inputArgs.Length < 4)
                        {
                            CLIEngine.ShowErrorMessage("Usage: game inventory hasbyname <itemName>");
                            return;
                        }
                        var hasByNameResult = await gameManager.HasItemByNameAsync(avatarId, inputArgs[3]);
                        if (!hasByNameResult.IsError)
                        {
                            CLIEngine.ShowSuccessMessage(hasByNameResult.Result ? $"Avatar has item '{inputArgs[3]}'." : $"Avatar does not have item '{inputArgs[3]}'.");
                        }
                        else
                        {
                            CLIEngine.ShowErrorMessage($"Failed to check item: {hasByNameResult.Message}");
                        }
                        break;

                    default:
                        CLIEngine.ShowErrorMessage("Unknown inventory command.");
                        break;
                }
            }
            catch (Exception ex)
            {
                CLIEngine.ShowErrorMessage($"Error executing game inventory command: {ex.Message}");
            }
        }


    }
}
