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
