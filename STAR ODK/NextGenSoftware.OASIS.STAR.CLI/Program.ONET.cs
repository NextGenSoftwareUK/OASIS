using System;
using System.IO;
using System.Diagnostics;
using System.Threading.Tasks;
using NextGenSoftware.CLI.Engine;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.ONODE.Core.Network;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers;

namespace NextGenSoftware.OASIS.STAR.CLI
{
    partial class Program
    {
        private static ONETManager? _onetManager;
        private static ONETDiscovery? _onetDiscovery;

        private static async Task InitializeONETAsync()
        {
            try
            {
                if (_onetManager == null)
                {
                    CLIEngine.ShowWorkingMessage("Initializing ONET (OASIS Network)...");
                    _onetManager = new ONETManager(ProviderManager.Instance.CurrentStorageProvider, OASISBootLoader.OASISBootLoader.OASISDNA);
                    _onetDiscovery = new ONETDiscovery(ProviderManager.Instance.CurrentStorageProvider, OASISBootLoader.OASISBootLoader.OASISDNA);
                    await _onetDiscovery.InitializeAsync();
                    CLIEngine.ShowSuccessMessage("ONET initialized successfully");
                }
            }
            catch (Exception ex)
            {
                CLIEngine.ShowErrorMessage($"Error initializing ONET: {ex.Message}");
            }
        }

        private static async Task StartONODEAsync()
        {
            try
            {
                await InitializeONETAsync();
                CLIEngine.ShowWorkingMessage("Starting ONODE...");

                var result = await _onetManager!.StartNetworkAsync();
                if (result.IsError)
                    CLIEngine.ShowErrorMessage($"Failed to start ONODE: {result.Message}");
                else
                    CLIEngine.ShowSuccessMessage($"ONODE started successfully. Node ID: {result.Result}");
            }
            catch (Exception ex)
            {
                CLIEngine.ShowErrorMessage($"Error starting ONODE: {ex.Message}");
            }
        }

        private static async Task StopONODEAsync()
        {
            try
            {
                if (_onetManager == null)
                {
                    CLIEngine.ShowErrorMessage("ONODE is not running");
                    return;
                }

                CLIEngine.ShowWorkingMessage("Stopping ONODE...");
                var result = await _onetManager.StopNetworkAsync();
                if (result.IsError)
                    CLIEngine.ShowErrorMessage($"Failed to stop ONODE: {result.Message}");
                else
                    CLIEngine.ShowSuccessMessage("ONODE stopped successfully");
            }
            catch (Exception ex)
            {
                CLIEngine.ShowErrorMessage($"Error stopping ONODE: {ex.Message}");
            }
        }

        private static async Task StartWeb4APIAsync()
        {
            try
            {
                if (_webApiProcesses.ContainsKey("web4") && _webApiProcesses["web4"] != null && !_webApiProcesses["web4"].HasExited)
                {
                    CLIEngine.ShowWarningMessage("WEB4 OASIS API is already running.");
                    return;
                }

                CLIEngine.ShowWorkingMessage("Starting WEB4 OASIS API REST WebAPI...");

                string web4ApiPath = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "..", "..", "..", "..", "..", "ONODE", "NextGenSoftware.OASIS.API.ONODE.WebAPI"));
                string csprojPath = Path.Combine(web4ApiPath, "NextGenSoftware.OASIS.API.ONODE.WebAPI.csproj");

                if (!File.Exists(csprojPath))
                {
                    CLIEngine.ShowErrorMessage($"WEB4 OASIS API project not found at: {csprojPath}");
                    return;
                }

                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = $"run --project \"{csprojPath}\" --urls \"http://localhost:5000\"",
                    WorkingDirectory = web4ApiPath,
                    UseShellExecute = true,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Normal
                };

                Process process = Process.Start(startInfo);
                if (process != null)
                {
                    _webApiProcesses["web4"] = process;
                    await Task.Delay(2000);
                    CLIEngine.ShowSuccessMessage("WEB4 OASIS API started successfully in a new window. URL: http://localhost:5000");
                }
                else
                    CLIEngine.ShowErrorMessage("Failed to start WEB4 OASIS API.");
            }
            catch (Exception ex)
            {
                CLIEngine.ShowErrorMessage($"Error starting WEB4 OASIS API: {ex.Message}");
            }
        }

        private static async Task StartWeb5APIAsync()
        {
            try
            {
                if (_webApiProcesses.ContainsKey("web5") && _webApiProcesses["web5"] != null && !_webApiProcesses["web5"].HasExited)
                {
                    CLIEngine.ShowWarningMessage("WEB5 STAR API is already running.");
                    return;
                }

                CLIEngine.ShowWorkingMessage("Starting WEB5 STAR API REST WebAPI...");

                string web5ApiPath = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "..", "..", "..", "..", "NextGenSoftware.OASIS.STAR.WebAPI"));
                string csprojPath = Path.Combine(web5ApiPath, "NextGenSoftware.OASIS.STAR.WebAPI.csproj");

                if (!File.Exists(csprojPath))
                {
                    CLIEngine.ShowErrorMessage($"WEB5 STAR API project not found at: {csprojPath}");
                    return;
                }

                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = $"run --project \"{csprojPath}\" --urls \"http://localhost:5001\"",
                    WorkingDirectory = web5ApiPath,
                    UseShellExecute = false,
                    CreateNoWindow = false,
                    WindowStyle = ProcessWindowStyle.Normal
                };

                Process process = Process.Start(startInfo);
                if (process != null)
                {
                    _webApiProcesses["web5"] = process;
                    await Task.Delay(2000);
                    CLIEngine.ShowSuccessMessage("WEB5 STAR API started successfully in a new window. URL: http://localhost:5001");
                }
                else
                    CLIEngine.ShowErrorMessage("Failed to start WEB5 STAR API.");
            }
            catch (Exception ex)
            {
                CLIEngine.ShowErrorMessage($"Error starting WEB5 STAR API: {ex.Message}");
            }
        }

        private static async Task StopWeb4APIAsync()
        {
            try
            {
                if (!_webApiProcesses.ContainsKey("web4") || _webApiProcesses["web4"] == null || _webApiProcesses["web4"].HasExited)
                {
                    CLIEngine.ShowWarningMessage("WEB4 OASIS API is not running.");
                    return;
                }

                CLIEngine.ShowWorkingMessage("Stopping WEB4 OASIS API...");
                Process process = _webApiProcesses["web4"];
                try
                {
                    if (!process.HasExited)
                    {
                        process.Kill();
                        process.WaitForExit(5000);
                    }
                }
                catch (Exception ex)
                {
                    CLIEngine.ShowWarningMessage($"Error stopping process: {ex.Message}.");
                }
                finally
                {
                    process?.Dispose();
                    _webApiProcesses.Remove("web4");
                }
                CLIEngine.ShowSuccessMessage("WEB4 OASIS API stopped successfully.");
            }
            catch (Exception ex)
            {
                CLIEngine.ShowErrorMessage($"Error stopping WEB4 OASIS API: {ex.Message}");
            }
        }

        private static async Task StopWeb5APIAsync()
        {
            try
            {
                if (!_webApiProcesses.ContainsKey("web5") || _webApiProcesses["web5"] == null || _webApiProcesses["web5"].HasExited)
                {
                    CLIEngine.ShowWarningMessage("WEB5 STAR API is not running.");
                    return;
                }

                CLIEngine.ShowWorkingMessage("Stopping WEB5 STAR API...");
                Process process = _webApiProcesses["web5"];
                try
                {
                    if (!process.HasExited)
                    {
                        process.Kill();
                        process.WaitForExit(5000);
                    }
                }
                catch (Exception ex)
                {
                    CLIEngine.ShowWarningMessage($"Error stopping process: {ex.Message}.");
                }
                finally
                {
                    process?.Dispose();
                    _webApiProcesses.Remove("web5");
                }
                CLIEngine.ShowSuccessMessage("WEB5 STAR API stopped successfully.");
            }
            catch (Exception ex)
            {
                CLIEngine.ShowErrorMessage($"Error stopping WEB5 STAR API: {ex.Message}");
            }
        }

        private static async Task ShowHyperNETSubCommandAsync(string[] inputArgs)
        {
            if (inputArgs.Length > 1)
            {
                switch (inputArgs[1].ToLower())
                {
                    case "start":
                        CLIEngine.ShowMessage("Coming Soon...");
                        break;

                    case "stop":
                        CLIEngine.ShowMessage("Coming Soon...");
                        break;

                    case "status":
                        CLIEngine.ShowMessage("Coming Soon...");
                        break;

                    case "config":
                        CLIEngine.ShowMessage("Coming Soon...");
                        break;

                    default:
                        CLIEngine.ShowErrorMessage("Command Unknown.");
                        break;
                }
            }
            else
            {
                Console.WriteLine("");
                CLIEngine.ShowMessage($"HOLONET P2P HYPERNET SUBCOMMANDS:", ConsoleColor.Green);
                Console.WriteLine("");
                CLIEngine.ShowMessage("    start   Starts the HoloNET P2P HyperNET Service.", ConsoleColor.Green, false);
                CLIEngine.ShowMessage("    stop    Stops the HoloNET P2P HyperNET Service.", ConsoleColor.Green, false);
                CLIEngine.ShowMessage("    status  Shows stats for the HoloNET P2P HyperNET Service.", ConsoleColor.Green, false);
                CLIEngine.ShowMessage("    config  Opens the HyperNET's DNA to allow changes to be made.", ConsoleColor.Green, false);
                CLIEngine.ShowMessage("More Coming Soon...", ConsoleColor.Green);
            }
        }

        private static async Task ShowONETSubCommandAsync(string[] inputArgs)
        {
            if (inputArgs.Length > 1)
            {
                switch (inputArgs[1].ToLower())
                {
                    case "start":
                        if (inputArgs.Length > 2)
                        {
                            switch (inputArgs[2].ToLower())
                            {
                                case "web4": await StartWeb4APIAsync(); break;
                                case "web5": await StartWeb5APIAsync(); break;
                                default: await StartONODEAsync(); break;
                            }
                        }
                        else
                            await StartONODEAsync();
                        break;

                    case "stop":
                        if (inputArgs.Length > 2)
                        {
                            switch (inputArgs[2].ToLower())
                            {
                                case "web4": await StopWeb4APIAsync(); break;
                                case "web5": await StopWeb5APIAsync(); break;
                                default: await StopONODEAsync(); break;
                            }
                        }
                        else
                            await StopONODEAsync();
                        break;

                    case "status":
                        await ShowONETStatusAsync();
                        break;

                    case "providers":
                        await ShowONETProvidersAsync();
                        break;

                    case "discover":
                        await DiscoverONETNodesAsync();
                        break;

                    case "connect":
                        if (inputArgs.Length > 2)
                            await ConnectToONETNodeAsync(inputArgs[2]);
                        else
                            CLIEngine.ShowErrorMessage("Please specify node address: connect {NodeAddress}");
                        break;

                    case "disconnect":
                        if (inputArgs.Length > 2)
                            await DisconnectFromONETNodeAsync(inputArgs[2]);
                        else
                            CLIEngine.ShowErrorMessage("Please specify node address: disconnect {NodeAddress}");
                        break;

                    case "topology":
                        await ShowONETTopologyAsync();
                        break;

                    default:
                        CLIEngine.ShowErrorMessage("Command Unknown.");
                        break;
                }
            }
            else
            {
                Console.WriteLine("");
                CLIEngine.ShowMessage($"ONET SUBCOMMANDS:", ConsoleColor.Green);
                Console.WriteLine("");
                CLIEngine.ShowMessage("    status      Shows stats for the OASIS Network (ONET).", ConsoleColor.Green, false);
                CLIEngine.ShowMessage("    providers   Shows what OASIS Providers are running across the ONET.", ConsoleColor.Green, false);
                CLIEngine.ShowMessage("    discover    Discovers available ONET nodes in the network.", ConsoleColor.Green, false);
                CLIEngine.ShowMessage("    connect     Connects to a specific ONET node.", ConsoleColor.Green, false);
                CLIEngine.ShowMessage("    disconnect  Disconnects from a specific ONET node.", ConsoleColor.Green, false);
                CLIEngine.ShowMessage("    topology    Shows the ONET network topology and connections.", ConsoleColor.Green, false);
                CLIEngine.ShowMessage("More Coming Soon...", ConsoleColor.Green);
            }
        }
    }
}
