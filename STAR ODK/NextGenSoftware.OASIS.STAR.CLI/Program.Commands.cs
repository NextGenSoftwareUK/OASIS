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
        private static string ReadLineWithCommandHistory(List<string> commandHistory, ref int historyIndex, int startLeft, int startTop)
        {
            // Basic line reader with Up/Down arrow history. No left/right editing; typing/backspace always operate at the end.
            var buffer = new StringBuilder();
            int prevRenderLen = 0;
            int maxLen = Math.Max(0, Console.BufferWidth - startLeft);

            void Render()
            {
                Console.SetCursorPosition(startLeft, startTop);
                string text = buffer.ToString();
                Console.Write(text);
                if (prevRenderLen > text.Length)
                    Console.Write(new string(' ', prevRenderLen - text.Length));
                prevRenderLen = text.Length;
            }

            historyIndex = commandHistory.Count;
            Render();

            while (true)
            {
                ConsoleKeyInfo key = Console.ReadKey(intercept: true);

                if (key.Key == ConsoleKey.Enter)
                {
                    Console.WriteLine();
                    return buffer.ToString();
                }

                if (key.Key == ConsoleKey.UpArrow)
                {
                    if (commandHistory.Count > 0 && historyIndex > 0)
                    {
                        historyIndex--;
                        buffer.Clear();
                        buffer.Append(commandHistory[historyIndex]);
                        Render();
                    }
                    continue;
                }

                if (key.Key == ConsoleKey.DownArrow)
                {
                    if (commandHistory.Count > 0)
                    {
                        if (historyIndex < commandHistory.Count - 1)
                        {
                            historyIndex++;
                            buffer.Clear();
                            buffer.Append(commandHistory[historyIndex]);
                        }
                        else
                        {
                            historyIndex = commandHistory.Count;
                            buffer.Clear();
                        }
                        Render();
                    }
                    continue;
                }

                if (key.Key == ConsoleKey.Backspace)
                {
                    if (buffer.Length > 0)
                    {
                        buffer.Length--;
                        Render();
                    }
                    continue;
                }

                char c = key.KeyChar;
                if (!char.IsControl(c) && buffer.Length < maxLen)
                {
                    buffer.Append(c);
                    Render();
                }
            }
        }

        /// <summary>When <see cref="CLIEngine.JsonOutput"/> is true, emit one JSON line for a holon operation result (non-interactive NFT/GeoNFT paths: mint, burn, import, export, remint, convert, place, send; STARNET holon <c>clone</c>; OAPP <c>light</c> JSON create).</summary>
        private static void EmitNiJsonForOasisResult<T>(OASISResult<T> r, string operationLabel, object successData = null)
        {
            if (!CLIEngine.JsonOutput)
                return;
            if (r == null)
            {
                StarCliShellOutput.WriteError(true, 1, $"{operationLabel}: null result", null);
                return;
            }

            if (r.IsError)
                StarCliShellOutput.WriteError(true, 1, r.Message ?? $"{operationLabel} failed", null);
            else
                StarCliShellOutput.WriteSuccess(true, string.IsNullOrEmpty(r.Message) ? $"{operationLabel} completed." : r.Message, successData);
        }

        private static async Task ShowSubCommandAsync<T>(string[] inputArgs, 
            string subCommand = "",
            string subCommandPlural = "",
            Func<ISTARNETCreateOptions<T, STARNETDNA>, object, bool, bool, ProviderType, Task> createPredicate = null,  //WEB5 Commands
            Func<string, object, bool, ProviderType, Task> updatePredicate = null, 
            Func<string, bool, ProviderType, Task> deletePredicate = null,
            Func<string, InstallMode, ProviderType, Task> downloadAndInstallPredicate = null,
            Func<string, ProviderType, Task> uninstallPredicate = null,
            //Func<string, ProviderType, Task> reinstallPredicate = null,
            Func<string, bool, DefaultLaunchMode, bool, ProviderType, Task> publishPredicate = null,
            Func<string, ProviderType, Task> unpublishPredicate = null,
            Func<string, ProviderType, Task> republishPredicate = null,
            Func<string, ProviderType, Task> activatePredicate = null,
            Func<string, ProviderType, Task> deactivatePredicate = null,
            Func<string, bool, ProviderType, Task> showPredicate = null,
            Func<bool, bool, ProviderType, Task> listForBeamedInAvatarPredicate = null,
            Func<bool, bool, int, ProviderType, Task> listAllPredicate = null,
            Func<ProviderType, Task> listInstalledPredicate = null,
            Func<ProviderType, Task> listUninstalledPredicate = null,
            Func<ProviderType, Task> listUnpublishedPredicate = null,
            Func<ProviderType, Task> listDeactivatedPredicate = null,
            Func<string, Guid, bool, bool, ProviderType, int, Task> searchPredicate = null,
            Func<string, ISTARNETDNA, string, string, ProviderType, Task> addDependencyPredicate = null,
            Func<string, string, string, ProviderType, Task> removeDependencyPredicate = null,
            Func<object, Task<OASISResult<T>>> clonePredicate = null,
            Func<object, Task> mintPredicate = null, //WEB4 Commands
            Func<object, Task> burnPredicate = null,
            Func<object, Task> importPredicate = null,
            Func<object, Task> exportPredicate = null,
            //Func<object, Task> clonePredicate = null,
            Func<object, Task> convertPredicate = null,
            Func<object, ProviderType, Task> createWeb4Predicate = null,
            Func<string, ProviderType, Task> updateWeb4Predicate = null,
            //Func<string, bool, ProviderType, Task> deleteWeb4Predicate = null,
            Func<string, ProviderType, Task> showWeb4Predicate = null,
            Func<string, bool, ProviderType, Task> searchWeb4Predicate = null,
            Func<ProviderType, Task> listAllWeb4Predicate = null,
            Func<ProviderType, Task> listWeb4ForBeamedInAvatarPredicate = null,
            Func<string, string, ProviderType, Task> addWeb4NFTToCollectionPredicate = null,
            Func<string, string, ProviderType, Task> removeWeb4NFTFromCollectionPredicate = null,
            Func<string, ProviderType, Task> updateWeb3Predicate = null, //WEB3 Commands
            Func<string, bool?, bool?, ProviderType, Task<OASISResult<bool>>> deleteWeb3Predicate = null,
            Func<ProviderType, Task> listAllWeb3Predicate = null,
            Func<ProviderType, Task> listWeb3ForBeamedInAvatarPredicate = null,
            Func<string, ProviderType, Task> showWeb3Predicate = null,
            Func<string, bool, ProviderType, Task> searchWeb3Predicate = null,
            bool showCreate = true,
            bool showUpdate = true,
            bool showDelete = true,
            ProviderType providerType = ProviderType.Default) where T : ISTARNETHolon, new()
        {
            string subCommandParam = "";
            string subCommandParam2 = "";
            string subCommandParam3 = "";
            string subCommandParam4 = "";
            bool showAllVersions = false;
            bool showForAllAvatars = false;
            bool showDetailed = false;
            bool web3 = false;
            bool web4 = false;
            string id = "";

            if (string.IsNullOrEmpty(subCommand))
                subCommand = inputArgs[0];

            //if ((inputArgs.Length > 1 && inputArgs[1] != "template" && inputArgs[1] != "metadata") || (inputArgs.Length > 2 && (inputArgs[1] == "template" || inputArgs[1] == "metadata")))
            if ((inputArgs.Length > 1 && inputArgs[1] != "template" && inputArgs[1] != "metadata" && inputArgs[1] != "collection") || (inputArgs.Length > 2 && (inputArgs[1] == "template" || inputArgs[1] == "metadata" || inputArgs[1] == "collection")))
            { 
                if (inputArgs[1] != "template" && inputArgs[1] != "metadata" && inputArgs[1] != "collection" && inputArgs.Length > 2)
                    id = inputArgs[2];

                if ((inputArgs[1] == "template" || inputArgs[1] == "metadata" || inputArgs[1] == "collection") && inputArgs.Length > 3)
                    id = inputArgs[3];


                if (inputArgs.Length > 1 && !string.IsNullOrEmpty(inputArgs[1]))
                    subCommandParam = inputArgs[1].ToLower();

                if (inputArgs.Length > 2 && !string.IsNullOrEmpty(inputArgs[2]))
                    subCommandParam2 = inputArgs[2].ToLower();

                if (inputArgs.Length > 3 && !string.IsNullOrEmpty(inputArgs[3]))
                    subCommandParam3 = inputArgs[3].ToLower();

                if (inputArgs.Length > 4 && !string.IsNullOrEmpty(inputArgs[4]))
                    subCommandParam4 = inputArgs[4].ToLower();

                if (inputArgs[1] == "template" || inputArgs[1] == "metadata" || inputArgs[1] == "collection")
                {
                    if (inputArgs.Length > 2 && !string.IsNullOrEmpty(inputArgs[2]))
                        subCommandParam = inputArgs[2].ToLower();

                    if (inputArgs.Length > 3 && !string.IsNullOrEmpty(inputArgs[3]))
                        subCommandParam2 = inputArgs[3].ToLower();

                    if (inputArgs.Length > 4 && !string.IsNullOrEmpty(inputArgs[4]))
                        subCommandParam3 = inputArgs[4].ToLower();

                    if (inputArgs.Length > 5 && !string.IsNullOrEmpty(inputArgs[5]))
                        subCommandParam4 = inputArgs[5].ToLower();
                }

                if (subCommandParam2.ToLower() == "allversions" || subCommandParam3.ToLower() == "allversions")
                    showAllVersions = true;

                if (subCommandParam2.ToLower() == "forallavatars" || subCommandParam3.ToLower() == "forallavatars")
                    showForAllAvatars = true;

                if (subCommandParam == "detailed" || subCommandParam2 == "detailed" || subCommandParam3 == "detailed")
                    showDetailed = true;

                web3 = subCommandParam == "web3" || subCommandParam2 == "web3" || subCommandParam3 == "web3" || subCommandParam4 == "web3" ? true : false;
                web4 = subCommandParam == "web4" || subCommandParam2 == "web4" || subCommandParam3 == "web4" || subCommandParam4 == "web4" ? true : false;

                if (CLIEngine.NonInteractive && StarCliStarnetNonInteractiveGuard.IsWizardOnlySubcommand(subCommandParam))
                {
                    StarCliShellOutput.WriteError(CLIEngine.JsonOutput, 2,
                        $"Subcommand '{subCommandParam}' is interactive-only (wizard). Omit --non-interactive for wizards.",
                        $"Entity: {subCommand}. Scriptable flows: list, show/update/delete/install/... with explicit id or GUID; oapp publish <path>; search <term>. See Docs/Devs/STAR_CLI_NonInteractive.md.");
                    return;
                }

                if (CLIEngine.NonInteractive &&
                    StarCliStarnetNonInteractiveGuard.WriteHolonSubCommandViolationIfNeeded(
                        CLIEngine.JsonOutput,
                        subCommand,
                        subCommandParam,
                        id,
                        inputArgs,
                        subCommandParam3,
                        subCommandParam4,
                        web3,
                        web4,
                        mintPredicate != null,
                        burnPredicate != null,
                        clonePredicate != null,
                        convertPredicate != null,
                        importPredicate != null,
                        exportPredicate != null,
                        addWeb4NFTToCollectionPredicate != null,
                        removeWeb4NFTFromCollectionPredicate != null,
                        addDependencyPredicate != null,
                        removeDependencyPredicate != null))
                    return;

                switch (subCommandParam)
                {
                    case "light":
                        {
                            if (!(string.Equals(subCommand, "OAPP", StringComparison.OrdinalIgnoreCase)
                                  || string.Equals(subCommand, "hApp", StringComparison.OrdinalIgnoreCase)))
                            {
                                CLIEngine.ShowErrorMessage("Command Unknown.");
                                break;
                            }

                            if (!showCreate)
                            {
                                CLIEngine.ShowErrorMessage("Command not supported.");
                                break;
                            }

                            if (!StarnetUiScriptedCreateCli.TryParseOappLightDirectArgv(inputArgs, out string oappLightOnlyJson, out string oappLightOnlyErr))
                            {
                                CLIEngine.ShowErrorMessage("Command Unknown.");
                                break;
                            }

                            if (!string.IsNullOrEmpty(oappLightOnlyErr))
                            {
                                StarCliShellOutput.WriteError(CLIEngine.JsonOutput, 2,
                                    oappLightOnlyErr,
                                    "Example: star --non-interactive --json oapp light ./LightRequest.json");
                                break;
                            }

                            if (!File.Exists(oappLightOnlyJson))
                            {
                                StarCliShellOutput.WriteError(CLIEngine.JsonOutput, 2,
                                    $"Light JSON file not found: {oappLightOnlyJson}",
                                    "See Docs/Devs/STAR_CLI_NonInteractive.md (Light JSON schema).");
                                break;
                            }

                            var oappLightOnlyOpts = new STARNETCreateOptions<OAPP, STARNETDNA>
                            {
                                STARNETHolon = new OAPP(),
                                CustomCreateParams = StarnetUiScriptedCreateCli.BuildOappLightJsonCustomCreateParams(oappLightOnlyJson)
                            };
                            OASISResult<OAPP> lightOnlyRes = await STARCLI.OAPPs.CreateAsync(oappLightOnlyOpts, null, false, false, providerType);
                            if (CLIEngine.JsonOutput)
                                EmitNiJsonForOasisResult(lightOnlyRes, $"{subCommand} light",
                                    lightOnlyRes.Result != null ? new { id = lightOnlyRes.Result.STARNETDNA?.Id, name = lightOnlyRes.Result.STARNETDNA?.Name } : null);
                        }
                        break;

                    case "create":
                        {
                            if (showCreate)
                            {
                                if (web4)
                                {
                                    if (CLIEngine.NonInteractive)
                                    {
                                        if (createWeb4Predicate != null)
                                            await createWeb4Predicate(null, providerType);
                                        else if (createPredicate != null && !StarnetUiScriptedCreateCli.HolonLabelBypassesBaseScriptedCreate(subCommand))
                                        {
                                            if (!StarnetUiScriptedCreateCli.TryParseCreateArgv(inputArgs, subCommand, out string w4Name, out string w4Desc, out string w4Cat, out string w4LibLang, out string w4Parent, out string w4Err))
                                            {
                                                StarCliShellOutput.WriteError(CLIEngine.JsonOutput, 2,
                                                    w4Err ?? "Invalid create arguments.",
                                                    "web4 flag with no web4-specific create: using same argv as web5 scripted create.");
                                                break;
                                            }

                                            var w4Opts = new STARNETCreateOptions<T, STARNETDNA>
                                            {
                                                STARNETHolon = new T(),
                                                CustomCreateParams = StarnetUiScriptedCreateCli.BuildScriptedCustomCreateParams(w4Name, w4Desc, w4Cat, w4Parent, w4LibLang)
                                            };
                                            await createPredicate(w4Opts, null, false, false, providerType);
                                        }
                                        else
                                        {
                                            StarCliShellOutput.WriteError(CLIEngine.JsonOutput, 2,
                                                "Non-interactive web4 create is not available for this entity.",
                                                "Omit web4 keyword or use a holon with scripted create. See Docs/Devs/STAR_CLI_NonInteractive.md.");
                                        }
                                    }
                                    else if (createWeb4Predicate != null)
                                        await createWeb4Predicate(null, providerType);
                                    else
                                        CLIEngine.ShowMessage("Coming Soon...");
                                }
                                else
                                {
                                    if (CLIEngine.NonInteractive)
                                    {
                                        if (StarnetUiScriptedCreateCli.HolonLabelBypassesBaseScriptedCreate(subCommand))
                                        {
                                            StarCliShellOutput.WriteError(CLIEngine.JsonOutput, 2,
                                                $"Non-interactive scripted create is not available for '{subCommand}' (this holon type does not delegate to STARNETUIBase scripted create).",
                                                "See StarnetUiScriptedCreateCli.HolonLabelBypassesBaseScriptedCreate in STAR.CLI.Lib and Docs/Devs/STAR_CLI_NonInteractive.md (Generic design).");
                                            break;
                                        }

                                        STARNETCreateOptions<T, STARNETDNA> scriptedOpts;
                                        if (string.Equals(subCommand, "geo-hotspot", StringComparison.OrdinalIgnoreCase))
                                        {
                                            if (!StarnetUiScriptedCreateCli.TryParseGeoHotSpotCreateArgv(inputArgs, out string ghName, out string ghDesc, out string ghType, out double ghLat, out double ghLon, out int ghRad, out string ghTrig, out int? ghTime, out string ghParent, out string ghErr))
                                            {
                                                StarCliShellOutput.WriteError(CLIEngine.JsonOutput, 2,
                                                    ghErr ?? "Invalid geo-hotspot create arguments.",
                                                    "Example: geo-hotspot create MyHS \"Desc\" Audio 51.5 -0.1 25 WhenArrivedAtGeoLocation [parentFolder] --audio-url https://example.com/a.mp3  |  --audio-file /path/to/local.mp3");
                                                break;
                                            }

                                            var ghParams = StarnetUiScriptedCreateCli.BuildGeoHotSpotScriptedCustomCreateParams(ghName, ghDesc, ghType, ghLat, ghLon, ghRad, ghTrig, ghTime, ghParent);
                                            StarnetUiScriptedCreateCli.ApplyGeoHotSpotMediaOptionalArgs(inputArgs, ghParams);
                                            scriptedOpts = new STARNETCreateOptions<T, STARNETDNA>
                                            {
                                                STARNETHolon = new T(),
                                                CustomCreateParams = ghParams
                                            };
                                        }
                                        else if (string.Equals(subCommand, "nft collection", StringComparison.OrdinalIgnoreCase))
                                        {
                                            if (StarnetUiScriptedCreateCli.TryParseNewWeb4NftCollectionCreateArgv(inputArgs, out string newCollName, out string newCollDesc, out string newCollErr))
                                            {
                                                if (!string.IsNullOrEmpty(newCollErr))
                                                {
                                                    StarCliShellOutput.WriteError(CLIEngine.JsonOutput, 2,
                                                        newCollErr,
                                                        "Example: nft collection create \"MyColl\" \"Description\"");
                                                    break;
                                                }

                                                scriptedOpts = new STARNETCreateOptions<T, STARNETDNA>
                                                {
                                                    STARNETHolon = new T(),
                                                    CustomCreateParams = StarnetUiScriptedCreateCli.BuildMinimalWeb4NFTCollectionScriptedParams(newCollName, newCollDesc)
                                                };
                                            }
                                            else if (!StarnetUiScriptedCreateCli.TryParseWrapOnlyWeb4CollectionCreateArgv(inputArgs, out string wrapCollId, out string collErr))
                                            {
                                                StarCliShellOutput.WriteError(CLIEngine.JsonOutput, 2,
                                                    collErr ?? "Invalid nft collection create arguments.",
                                                    "Wrap: nft collection create <web4CollectionGuidOrName>  |  New: nft collection create <name> <description>");
                                                break;
                                            }
                                            else
                                            {
                                                scriptedOpts = new STARNETCreateOptions<T, STARNETDNA>
                                                {
                                                    STARNETHolon = new T(),
                                                    CustomCreateParams = StarnetUiScriptedCreateCli.BuildWrapWeb4NFTCollectionScriptedParams(wrapCollId)
                                                };
                                            }
                                        }
                                        else if (string.Equals(subCommand, "geo-nft collection", StringComparison.OrdinalIgnoreCase))
                                        {
                                            if (StarnetUiScriptedCreateCli.TryParseNewWeb4GeoNftCollectionCreateArgv(inputArgs, out string newGeoCollName, out string newGeoCollDesc, out string newGeoCollErr))
                                            {
                                                if (!string.IsNullOrEmpty(newGeoCollErr))
                                                {
                                                    StarCliShellOutput.WriteError(CLIEngine.JsonOutput, 2,
                                                        newGeoCollErr,
                                                        "Example: geo-nft collection create \"MyColl\" \"Description\"");
                                                    break;
                                                }

                                                scriptedOpts = new STARNETCreateOptions<T, STARNETDNA>
                                                {
                                                    STARNETHolon = new T(),
                                                    CustomCreateParams = StarnetUiScriptedCreateCli.BuildMinimalWeb4GeoNFTCollectionScriptedParams(newGeoCollName, newGeoCollDesc)
                                                };
                                            }
                                            else if (!StarnetUiScriptedCreateCli.TryParseWrapOnlyWeb4CollectionCreateArgv(inputArgs, out string wrapGeoCollId, out string gcollErr))
                                            {
                                                StarCliShellOutput.WriteError(CLIEngine.JsonOutput, 2,
                                                    gcollErr ?? "Invalid geo-nft collection create arguments.",
                                                    "Wrap: geo-nft collection create <web4CollectionGuidOrName>  |  New: geo-nft collection create <name> <description>");
                                                break;
                                            }
                                            else
                                            {
                                                scriptedOpts = new STARNETCreateOptions<T, STARNETDNA>
                                                {
                                                    STARNETHolon = new T(),
                                                    CustomCreateParams = StarnetUiScriptedCreateCli.BuildWrapWeb4GeoNFTCollectionScriptedParams(wrapGeoCollId)
                                                };
                                            }
                                        }
                                        else if (string.Equals(subCommand, "nft", StringComparison.OrdinalIgnoreCase))
                                        {
                                            if (!StarnetUiScriptedCreateCli.TryParseWrapOnlyWeb4CreateArgv(inputArgs, out string wrapNftId, out string wErr))
                                            {
                                                StarCliShellOutput.WriteError(CLIEngine.JsonOutput, 2,
                                                    wErr ?? "Invalid nft create arguments.",
                                                    "Example: nft create <web4NftGuid>");
                                                break;
                                            }

                                            scriptedOpts = new STARNETCreateOptions<T, STARNETDNA>
                                            {
                                                STARNETHolon = new T(),
                                                CustomCreateParams = StarnetUiScriptedCreateCli.BuildWrapWeb4NftScriptedParams(wrapNftId)
                                            };
                                        }
                                        else if (string.Equals(subCommand, "geo-nft", StringComparison.OrdinalIgnoreCase))
                                        {
                                            if (!StarnetUiScriptedCreateCli.TryParseWrapOnlyWeb4CreateArgv(inputArgs, out string wrapGeoId, out string wgErr))
                                            {
                                                StarCliShellOutput.WriteError(CLIEngine.JsonOutput, 2,
                                                    wgErr ?? "Invalid geo-nft create arguments.",
                                                    "Example: geo-nft create <web4GeoNftGuid>");
                                                break;
                                            }

                                            scriptedOpts = new STARNETCreateOptions<T, STARNETDNA>
                                            {
                                                STARNETHolon = new T(),
                                                CustomCreateParams = StarnetUiScriptedCreateCli.BuildWrapWeb4GeoSpatialNftScriptedParams(wrapGeoId)
                                            };
                                        }
                                        else if (string.Equals(subCommand, "plugin", StringComparison.OrdinalIgnoreCase))
                                        {
                                            if (!StarnetUiScriptedCreateCli.TryParsePluginCreateArgv(inputArgs, out string plugName, out string plugDesc, out string plugParent, out string plugErr))
                                            {
                                                StarCliShellOutput.WriteError(CLIEngine.JsonOutput, 2,
                                                    plugErr ?? "Invalid plugin create arguments.",
                                                    "Example: plugin create \"MyPlugin\" \"Description\" [/optional/parent/dir]");
                                                break;
                                            }

                                            scriptedOpts = new STARNETCreateOptions<T, STARNETDNA>
                                            {
                                                STARNETHolon = new T(),
                                                CustomCreateParams = StarnetUiScriptedCreateCli.BuildPluginScriptedCustomCreateParams(plugName, plugDesc, plugParent)
                                            };
                                        }
                                        else if ((string.Equals(subCommand, "OAPP", StringComparison.OrdinalIgnoreCase)
                                                 || string.Equals(subCommand, "hApp", StringComparison.OrdinalIgnoreCase))
                                                 && StarnetUiScriptedCreateCli.TryParseOappLightJsonCreateArgv(inputArgs, out string oappLightJson, out string oappLightErr))
                                        {
                                            if (!string.IsNullOrEmpty(oappLightErr))
                                            {
                                                StarCliShellOutput.WriteError(CLIEngine.JsonOutput, 2,
                                                    oappLightErr,
                                                    "Example: star --non-interactive --json oapp light ./LightRequest.json");
                                                break;
                                            }

                                            if (!System.IO.File.Exists(oappLightJson))
                                            {
                                                StarCliShellOutput.WriteError(CLIEngine.JsonOutput, 2,
                                                    $"Light JSON file not found: {oappLightJson}",
                                                    "See Docs/Devs/STAR_CLI_NonInteractive.md (Light JSON schema).");
                                                break;
                                            }

                                            scriptedOpts = new STARNETCreateOptions<T, STARNETDNA>
                                            {
                                                STARNETHolon = new T(),
                                                CustomCreateParams = StarnetUiScriptedCreateCli.BuildOappLightJsonCustomCreateParams(oappLightJson)
                                            };
                                        }
                                        else if (!StarnetUiScriptedCreateCli.TryParseCreateArgv(inputArgs, subCommand, out string cName, out string cDesc, out string cCat, out string cLibLang, out string cParent, out string cErr))
                                        {
                                            StarCliShellOutput.WriteError(CLIEngine.JsonOutput, 2,
                                                cErr ?? "Invalid create arguments.",
                                                "Example: star --non-interactive oapp template create \"MyTpl\" \"Desc\" Console /optional/parent/dir");
                                            break;
                                        }
                                        else
                                        {
                                            scriptedOpts = new STARNETCreateOptions<T, STARNETDNA>
                                            {
                                                STARNETHolon = new T(),
                                                CustomCreateParams = StarnetUiScriptedCreateCli.BuildScriptedCustomCreateParams(cName, cDesc, cCat, cParent, cLibLang)
                                            };
                                        }

                                        if (string.Equals(subCommand, "quest", StringComparison.OrdinalIgnoreCase))
                                        {
                                            scriptedOpts.CustomCreateParams ??= new Dictionary<string, object>();
                                            if (StarnetUiScriptedCreateCli.TryParseOptionalQuestObjectivesJsonPath(inputArgs, out string questObjJsonPath))
                                                scriptedOpts.CustomCreateParams[StarCliNonInteractiveCreateKeys.QuestObjectivesJsonPath] = questObjJsonPath;
                                            if (StarnetUiScriptedCreateCli.TryParseOptionalQuestLinkedHandoffArgv(inputArgs, out string qLinked, out string qHandoff))
                                            {
                                                if (!string.IsNullOrWhiteSpace(qLinked))
                                                    scriptedOpts.CustomCreateParams[StarCliNonInteractiveCreateKeys.QuestLinkedGeoHotSpotId] = qLinked.Trim();
                                                if (!string.IsNullOrWhiteSpace(qHandoff))
                                                    scriptedOpts.CustomCreateParams[StarCliNonInteractiveCreateKeys.QuestExternalHandoffUri] = qHandoff.Trim();
                                            }
                                        }

                                        if (createPredicate != null)
                                        {
                                            bool lightFromJson = scriptedOpts.CustomCreateParams != null
                                                && scriptedOpts.CustomCreateParams.ContainsKey(StarCliNonInteractiveCreateKeys.LightRequestJsonPath);
                                            if (lightFromJson && typeof(T) == typeof(OAPP))
                                            {
                                                var oappLightOpts = new STARNETCreateOptions<OAPP, STARNETDNA>
                                                {
                                                    STARNETHolon = new OAPP(),
                                                    CustomCreateParams = scriptedOpts.CustomCreateParams
                                                };
                                                if (scriptedOpts.STARNETDNA != null)
                                                    oappLightOpts.STARNETDNA = scriptedOpts.STARNETDNA;
                                                OASISResult<OAPP> lightCreateRes = await STARCLI.OAPPs.CreateAsync(oappLightOpts, null, false, false, providerType);
                                                if (CLIEngine.JsonOutput)
                                                    EmitNiJsonForOasisResult(lightCreateRes, $"{subCommand} light",
                                                        lightCreateRes.Result != null ? new { id = lightCreateRes.Result.STARNETDNA?.Id, name = lightCreateRes.Result.STARNETDNA?.Name } : null);
                                            }
                                            else
                                                await createPredicate(scriptedOpts, null, false, false, providerType);
                                        }
                                        else
                                            CLIEngine.ShowMessage("Coming Soon...");
                                    }
                                    else if (createPredicate != null)
                                        await createPredicate(null, null, true, true, providerType);
                                    else
                                        CLIEngine.ShowMessage("Coming Soon...");
                                }
                            }
                            else
                                CLIEngine.ShowErrorMessage("Command not supported.");
                        }
                        break;

                    case "mint":
                        {
                            if (mintPredicate != null)
                            {
                                if (CLIEngine.NonInteractive)
                                {
                                    if (!StarCliNftStructuredArgv.TryGetMintRequestJsonPath(inputArgs, out string mintJson, out string mintErr))
                                    {
                                        StarCliShellOutput.WriteError(CLIEngine.JsonOutput, 2,
                                            mintErr ?? "Invalid mint arguments.",
                                            "Example: nft mint /path/to/MintWeb4NFTRequest.json");
                                        break;
                                    }

                                    if (string.Equals(subCommand, "nft", StringComparison.OrdinalIgnoreCase))
                                    {
                                        OASISResult<IWeb4NFT> mintRes = await STARCLI.NFTs.MintNFTAsync(mintJson);
                                        EmitNiJsonForOasisResult(mintRes, "nft mint",
                                            mintRes.Result != null ? new { web4NftId = mintRes.Result.Id.ToString() } : null);
                                    }
                                    else if (string.Equals(subCommand, "geo-nft", StringComparison.OrdinalIgnoreCase))
                                    {
                                        OASISResult<IWeb4GeoSpatialNFT> mintGeo = await STARCLI.GeoNFTs.MintGeoNFTAsync(mintJson);
                                        EmitNiJsonForOasisResult(mintGeo, "geo-nft mint",
                                            mintGeo.Result != null ? new { web4GeoNftId = mintGeo.Result.Id.ToString() } : null);
                                    }
                                    else
                                        await mintPredicate(mintJson);
                                }
                                else
                                    await mintPredicate(null);
                            }
                            else
                                CLIEngine.ShowErrorMessage("Command not supported.");
                        }
                        break;

                    case "remint":
                        {
                            bool isNftEntity = string.Equals(subCommand, "nft", StringComparison.OrdinalIgnoreCase);
                            bool isGeoNftEntity = string.Equals(subCommand, "geo-nft", StringComparison.OrdinalIgnoreCase);
                            string remintTarget = null;

                            if (CLIEngine.NonInteractive)
                            {
                                if (!StarCliNftStructuredArgv.TryGetRemintTargetId(inputArgs, out remintTarget, out string remintErr))
                                {
                                    StarCliShellOutput.WriteError(CLIEngine.JsonOutput, 2,
                                        remintErr ?? "remint requires a target id.",
                                        "Example: nft remint <web4NftGuid>");
                                    break;
                                }
                            }

                            if (isNftEntity)
                            {
                                OASISResult<IWeb4NFT> remintRes = await STARCLI.NFTs.RemintNFTAsync(remintTarget);
                                EmitNiJsonForOasisResult(remintRes, "nft remint",
                                    remintRes.Result != null ? new { web4NftId = remintRes.Result.Id.ToString() } : null);
                            }
                            else if (isGeoNftEntity)
                            {
                                OASISResult<IWeb4GeoSpatialNFT> remintGeo = await STARCLI.GeoNFTs.RemintGeoNFTAsync(remintTarget);
                                EmitNiJsonForOasisResult(remintGeo, "geo-nft remint",
                                    remintGeo.Result != null ? new { web4GeoNftId = remintGeo.Result.Id.ToString() } : null);
                            }
                            else
                                CLIEngine.ShowErrorMessage("Command not supported.");
                        }
                        break;

                    case "place":
                        {
                            if (string.Equals(subCommand, "geo-nft", StringComparison.OrdinalIgnoreCase))
                            {
                                if (CLIEngine.NonInteractive)
                                {
                                    if (!StarCliNftStructuredArgv.TryGetPlaceGeoJsonPath(inputArgs, out string placeJson, out string placeErr))
                                    {
                                        StarCliShellOutput.WriteError(CLIEngine.JsonOutput, 2,
                                            placeErr ?? "Invalid place arguments.",
                                            "Example: geo-nft place /path/to/PlaceWeb4GeoSpatialNFTRequest.json");
                                        break;
                                    }

                                    OASISResult<IWeb4GeoSpatialNFT> placeRes = await STARCLI.GeoNFTs.PlaceGeoNFTFromJsonFileAsync(placeJson);
                                    EmitNiJsonForOasisResult(placeRes, "geo-nft place",
                                        placeRes.Result != null ? new { web4GeoNftId = placeRes.Result.Id.ToString() } : null);
                                }
                                else
                                    await STARCLI.GeoNFTs.PlaceGeoNFTAsync();
                            }
                            else
                                CLIEngine.ShowWarningMessage("place with JSON is supported for 'geo-nft' (WEB4).");
                        }
                        break;

                    case "burn":
                        {
                            if (burnPredicate != null)
                            {
                                if (CLIEngine.NonInteractive)
                                {
                                    if (!StarCliNftStructuredArgv.TryGetBurnRequestJsonPath(inputArgs, out string burnJson, out string burnErr))
                                    {
                                        StarCliShellOutput.WriteError(CLIEngine.JsonOutput, 2,
                                            burnErr ?? "Invalid burn arguments.",
                                            "Example: nft burn /path/to/BurnWeb3NFTRequest.json");
                                        break;
                                    }

                                    if (string.Equals(subCommand, "nft", StringComparison.OrdinalIgnoreCase))
                                    {
                                        OASISResult<IWeb4NFT> burnRes = await STARCLI.NFTs.BurnNFTAsync(burnJson);
                                        EmitNiJsonForOasisResult(burnRes, "nft burn", null);
                                    }
                                    else if (string.Equals(subCommand, "geo-nft", StringComparison.OrdinalIgnoreCase))
                                    {
                                        OASISResult<IWeb4GeoSpatialNFT> burnGeo = await STARCLI.GeoNFTs.BurnGeoNFTAsync(burnJson);
                                        EmitNiJsonForOasisResult(burnGeo, "geo-nft burn", null);
                                    }
                                    else
                                        await burnPredicate(burnJson);
                                }
                                else
                                    await burnPredicate(null);
                            }
                            else
                                CLIEngine.ShowErrorMessage("Command not supported or comming soon...");
                        }
                        break;

                    case "import":
                        {
                            if (importPredicate != null)
                            {
                                bool niWeb4NftImport = CLIEngine.NonInteractive
                                    && (string.Equals(subCommand, "nft", StringComparison.OrdinalIgnoreCase)
                                        || string.Equals(subCommand, "geo-nft", StringComparison.OrdinalIgnoreCase));

                                if (niWeb4NftImport)
                                {
                                    if (string.Equals(subCommand, "nft", StringComparison.OrdinalIgnoreCase))
                                    {
                                        if (!StarCliNftStructuredArgv.TryResolveNftNonInteractiveImport(inputArgs, out string nftImpPath, out StarCliNftStructuredArgv.NftNonInteractiveImportKind nftImpKind, out string nftImpErr))
                                        {
                                            StarCliShellOutput.WriteError(CLIEngine.JsonOutput, 2,
                                                nftImpErr ?? "Invalid import arguments.",
                                                "Example: nft import /path/to/file.json  (JSON shape selects WEB3 mint vs token vs WEB4 file). Legacy: nft import web3-mint <file> | nft import web3-token <file>.");
                                            break;
                                        }

                                        switch (nftImpKind)
                                        {
                                            case StarCliNftStructuredArgv.NftNonInteractiveImportKind.Web3MintFromJson:
                                                {
                                                    OASISResult<IWeb4NFT> w3m = await STARCLI.NFTs.ImportNFTWeb3MintFromJsonFileAsync(nftImpPath);
                                                    EmitNiJsonForOasisResult(w3m, "nft import",
                                                        w3m.Result != null ? new { web4NftId = w3m.Result.Id.ToString() } : null);
                                                }
                                                break;
                                            case StarCliNftStructuredArgv.NftNonInteractiveImportKind.Web3TokenFromJson:
                                                {
                                                    OASISResult<IWeb4NFT> w3t = await STARCLI.NFTs.ImportNFTWeb3TokenFromJsonFileAsync(nftImpPath);
                                                    EmitNiJsonForOasisResult(w3t, "nft import",
                                                        w3t.Result != null ? new { web4NftId = w3t.Result.Id.ToString() } : null);
                                                }
                                                break;
                                            default:
                                                {
                                                    OASISResult<IWeb4NFT> impNft = await STARCLI.NFTs.ImportNFTAsync(nftImpPath);
                                                    EmitNiJsonForOasisResult(impNft, "nft import",
                                                        impNft.Result != null ? new { web4NftId = impNft.Result.Id.ToString() } : null);
                                                }
                                                break;
                                        }
                                    }
                                    else if (string.Equals(subCommand, "geo-nft", StringComparison.OrdinalIgnoreCase))
                                    {
                                        if (!StarCliNftStructuredArgv.TryGetImportPath(inputArgs, out string importPath, out string importErr))
                                        {
                                            StarCliShellOutput.WriteError(CLIEngine.JsonOutput, 2,
                                                importErr ?? "Invalid import arguments.",
                                                "Example: geo-nft import /path/to/file");
                                            break;
                                        }

                                        OASISResult<IWeb4GeoSpatialNFT> impGeo = await STARCLI.GeoNFTs.ImportGeoNFTAsync(importPath);
                                        EmitNiJsonForOasisResult(impGeo, "geo-nft import",
                                            impGeo.Result != null ? new { web4GeoNftId = impGeo.Result.Id.ToString() } : null);
                                    }
                                }
                                else
                                    await importPredicate(web3);
                            }
                            else
                                CLIEngine.ShowErrorMessage("Command not supported or comming soon...");
                        }
                        break;

                    case "export":
                        {
                            if (exportPredicate != null)
                            {
                                bool niWeb4NftExport = CLIEngine.NonInteractive
                                    && (string.Equals(subCommand, "nft", StringComparison.OrdinalIgnoreCase)
                                        || string.Equals(subCommand, "geo-nft", StringComparison.OrdinalIgnoreCase));

                                if (niWeb4NftExport)
                                {
                                    if (!StarCliNftStructuredArgv.TryGetExportDest(inputArgs, out string exId, out string exPath, out string exErr))
                                    {
                                        StarCliShellOutput.WriteError(CLIEngine.JsonOutput, 2,
                                            exErr ?? "Invalid export arguments.",
                                            "Example: nft export <idOrGuid> /dest/path  |  geo-nft export <idOrGuid> /dest/path");
                                        break;
                                    }

                                    if (string.Equals(subCommand, "nft", StringComparison.OrdinalIgnoreCase))
                                    {
                                        OASISResult<IWeb4NFT> exNft = await STARCLI.NFTs.ExportNFTNonInteractiveAsync(exId, exPath, providerType);
                                        EmitNiJsonForOasisResult(exNft, "nft export",
                                            exNft.Result != null ? new { web4NftId = exNft.Result.Id.ToString(), destinationPath = exPath } : null);
                                    }
                                    else
                                    {
                                        OASISResult<IWeb4GeoSpatialNFT> exGeo = await STARCLI.GeoNFTs.ExportGeoNFTNonInteractiveAsync(exId, exPath, providerType);
                                        EmitNiJsonForOasisResult(exGeo, "geo-nft export",
                                            exGeo.Result != null ? new { web4GeoNftId = exGeo.Result.Id.ToString(), destinationPath = exPath } : null);
                                    }
                                }
                                else
                                    await exportPredicate(null);
                            }
                            else
                                CLIEngine.ShowErrorMessage("Command not supported or comming soon...");
                        }
                        break;

                    case "clone":
                        {
                            if (clonePredicate != null)
                            {
                                object cloneArg = null;
                                if (CLIEngine.NonInteractive)
                                {
                                    if (!StarCliNftStructuredArgv.TryGetFirstTokenAfterVerb(inputArgs, "clone", out string cloneSourceId, out string cloneErr))
                                    {
                                        StarCliShellOutput.WriteError(CLIEngine.JsonOutput, 2,
                                            cloneErr ?? "clone requires a source id or name.",
                                            $"Example: {subCommand} clone <sourceIdOrName>");
                                        break;
                                    }

                                    cloneArg = cloneSourceId;
                                }

                                OASISResult<T> cloneRes = await clonePredicate(cloneArg);
                                if (CLIEngine.JsonOutput)
                                {
                                    object cloneSuccessData = null;
                                    if (cloneRes != null && !cloneRes.IsError && cloneRes.Result != null)
                                    {
                                        cloneSuccessData = new
                                        {
                                            id = cloneRes.Result.STARNETDNA?.Id,
                                            name = cloneRes.Result.STARNETDNA?.Name
                                        };
                                    }

                                    EmitNiJsonForOasisResult(cloneRes, $"{subCommand} clone", cloneSuccessData);
                                }
                            }
                            else
                                CLIEngine.ShowErrorMessage("Command not supported or comming soon...");
                        }
                        break;

                    case "convert":
                        {
                            if (convertPredicate != null)
                            {
                                if (CLIEngine.NonInteractive)
                                {
                                    if (!StarCliNftStructuredArgv.TryGetFirstTokenAfterVerb(inputArgs, "convert", out string convertSourceId, out string convertErr))
                                    {
                                        StarCliShellOutput.WriteError(CLIEngine.JsonOutput, 2,
                                            convertErr ?? "convert requires a source id or name.",
                                            $"Example: {subCommand} convert <sourceIdOrName>");
                                        break;
                                    }

                                    bool isNftConvert = string.Equals(subCommand, "nft", StringComparison.OrdinalIgnoreCase);
                                    bool isGeoConvert = string.Equals(subCommand, "geo-nft", StringComparison.OrdinalIgnoreCase);
                                    if (isNftConvert)
                                    {
                                        OASISResult<IWeb4NFT> cv = await STARCLI.NFTs.ConvertNFTAsync(convertSourceId);
                                        EmitNiJsonForOasisResult(cv, "nft convert", null);
                                    }
                                    else if (isGeoConvert)
                                    {
                                        OASISResult<IWeb4GeoSpatialNFT> cvGeo = await STARCLI.GeoNFTs.ConvertGeoNFTAsync(convertSourceId);
                                        EmitNiJsonForOasisResult(cvGeo, "geo-nft convert", null);
                                    }
                                    else
                                        await convertPredicate(convertSourceId);
                                }
                                else
                                    await convertPredicate(null);
                            }
                            else
                                CLIEngine.ShowErrorMessage("Command not supported or comming soon...");
                        }
                        break;

                    case "send":
                        {
                            bool isNftSend = string.Equals(subCommand, "nft", StringComparison.OrdinalIgnoreCase);
                            bool isGeoNftSend = string.Equals(subCommand, "geo-nft", StringComparison.OrdinalIgnoreCase);

                            if (CLIEngine.NonInteractive)
                            {
                                if (!StarCliNftStructuredArgv.TryGetSendArgs(inputArgs, out string sFrom, out string sTo, out string sTok, out string sMemo, out string sendErr))
                                {
                                    StarCliShellOutput.WriteError(CLIEngine.JsonOutput, 2,
                                        sendErr ?? "Invalid send arguments.",
                                        "Example: nft send <fromWallet> <toWallet> <tokenAddress> <memo>");
                                    break;
                                }

                                if (isNftSend)
                                {
                                    OASISResult<ISendWeb4NFTResponse> sendNft = await STARCLI.NFTs.SendNFTAsync(sFrom, sTo, sTok, sMemo);
                                    EmitNiJsonForOasisResult(sendNft, "nft send",
                                        sendNft.Result != null
                                            ? new
                                            {
                                                bridgeOrderId = sendNft.Result.BridgeOrderId,
                                                sendTransactionResult = sendNft.Result.SendTransactionResult
                                            }
                                            : null);
                                }
                                else if (isGeoNftSend)
                                {
                                    OASISResult<ISendWeb4NFTResponse> sendGeo = await STARCLI.GeoNFTs.SendGeoNFTAsync(sFrom, sTo, sTok, sMemo);
                                    EmitNiJsonForOasisResult(sendGeo, "geo-nft send",
                                        sendGeo.Result != null
                                            ? new
                                            {
                                                bridgeOrderId = sendGeo.Result.BridgeOrderId,
                                                sendTransactionResult = sendGeo.Result.SendTransactionResult
                                            }
                                            : null);
                                }
                                else
                                    CLIEngine.ShowErrorMessage("Command not supported.");
                            }
                            else
                            {
                                if (isNftSend)
                                    await STARCLI.NFTs.SendNFTAsync();
                                else if (isGeoNftSend)
                                    await STARCLI.GeoNFTs.SendGeoNFTAsync();
                                else
                                    CLIEngine.ShowErrorMessage("Command not supported.");
                            }
                        }
                        break;

                    case "update":
                        {
                            if (showUpdate)
                            {
                                if (web3)
                                {
                                    id = "";

                                    if (inputArgs.Length > 3)
                                        id = inputArgs[3];

                                    if (updateWeb3Predicate != null)
                                        await updateWeb3Predicate(id, providerType);
                                    else
                                        CLIEngine.ShowMessage("Coming Soon...");
                                }
                                else if (web4)
                                {
                                    id = "";

                                    if (inputArgs.Length > 3)
                                        id = inputArgs[3];

                                    if (updateWeb4Predicate != null)
                                        await updateWeb4Predicate(id, providerType); //TODO: Pass in params in a object or dynamic obj.
                                    else
                                        CLIEngine.ShowMessage("Coming Soon...");
                                }
                                else
                                {
                                    if (updatePredicate != null)
                                    {
                                        object questEditParams = null;
                                        if (CLIEngine.NonInteractive && string.Equals(subCommand, "quest", StringComparison.OrdinalIgnoreCase)
                                            && StarnetUiScriptedCreateCli.TryParseQuestUpdateArgv(inputArgs, out QuestCliEditParams qEdit))
                                        {
                                            questEditParams = qEdit;
                                        }

                                        await updatePredicate(id, questEditParams, true, providerType);
                                    }
                                    else
                                        CLIEngine.ShowMessage("Coming Soon...");
                                }
                            }
                            else
                                CLIEngine.ShowErrorMessage("Command not supported.");
                        }
                        break;

                    case "delete":
                        {
                            if (showDelete)
                            {
                                bool temp = false;
                                bool? softDelete = null;

                                if (inputArgs.Length > 3 && bool.TryParse(inputArgs[3], out temp))
                                    softDelete = temp;

                                if (web3)
                                {
                                    id = "";
                                    bool burnWeb3NFT = true;

                                    if (inputArgs.Length > 3)
                                        id = inputArgs[3];

                                    if (inputArgs.Length > 4 && bool.TryParse(inputArgs[4], out temp))
                                        softDelete = temp;

                                    if (inputArgs.Length > 5)
                                        bool.TryParse(inputArgs[5], out burnWeb3NFT);

                                    if (deleteWeb3Predicate != null)
                                    {
                                        var deleteResult = await deleteWeb3Predicate(id, softDelete, burnWeb3NFT, providerType);
                                        if (deleteResult != null && deleteResult.IsError)
                                            CLIEngine.ShowErrorMessage(deleteResult.Message);
                                    }
                                    else
                                        CLIEngine.ShowMessage("Coming Soon...");
                                }
                                else if (web4)
                                {
                                    id = "";
                                    bool? deleteChildWeb4NFTs = null;
                                    bool? deleteChildWeb3NFTs = null;
                                    bool? burnChildWeb3NFTs = null;

                                    if (inputArgs.Length > 3)
                                        id = inputArgs[3];

                                    if (inputArgs.Length > 4 && bool.TryParse(inputArgs[4], out temp))
                                        softDelete = temp;

                                    if (inputArgs.Length > 5 && bool.TryParse(inputArgs[5], out temp))
                                        deleteChildWeb4NFTs = temp;

                                    if (inputArgs.Length > 6 && bool.TryParse(inputArgs[6], out temp))
                                        deleteChildWeb3NFTs = temp;

                                    if (inputArgs.Length > 7 && bool.TryParse(inputArgs[7], out temp))
                                        burnChildWeb3NFTs = temp;

                                    switch (subCommand.ToUpper())
                                    {
                                        case "NFT":
                                            await STARCLI.NFTs.DeleteWeb4NFTAsync(id, softDelete, deleteChildWeb3NFTs, burnChildWeb3NFTs);
                                            break;

                                        case "GEONFT":
                                            await STARCLI.GeoNFTs.DeleteWeb4GeoNFTAsync(id, softDelete, deleteChildWeb3NFTs, burnChildWeb3NFTs);
                                            break;

                                        case "NFTCOLLECTION":
                                            await STARCLI.NFTCollections.DeleteWeb4NFTCollectionAsync(id, softDelete, deleteChildWeb4NFTs, deleteChildWeb3NFTs, burnChildWeb3NFTs);
                                            break;

                                        case "GEONFTCOLLECTION":
                                            await STARCLI.GeoNFTCollections.DeleteWeb4GeoNFTCollectionAsync(id, softDelete, deleteChildWeb4NFTs, deleteChildWeb3NFTs, burnChildWeb3NFTs);
                                            break;

                                        default:
                                            CLIEngine.ShowMessage("Coming Soon...");
                                            break;
                                    }

                                    //if (deleteWeb4Predicate != null)
                                    //    await deleteWeb4Predicate(id, softDelete, providerType);
                                    //else
                                    //    CLIEngine.ShowMessage("Coming Soon...");
                                }
                                else
                                {
                                    //TODO: Temp, need to make so can pass nullable softDelete to Web5 delete functions.
                                    if (softDelete == null)
                                        softDelete = true;

                                    if (deletePredicate != null)
                                        await deletePredicate(id, softDelete.Value, providerType); //TODO: Fix later so we pass the ?bool softDelete value in like above for web4 and web3!
                                    else
                                        CLIEngine.ShowMessage("Coming Soon...");
                                }
                            }
                            else
                                CLIEngine.ShowErrorMessage("Command not supported.");
                        }
                        break;

                    case "download":
                        {
                            if (downloadAndInstallPredicate != null)
                                await downloadAndInstallPredicate(id, InstallMode.DownloadOnly, providerType);
                            else
                                CLIEngine.ShowMessage("Coming Soon...");
                        }
                        break;

                    case "install":
                        {
                            if (downloadAndInstallPredicate != null)
                                await downloadAndInstallPredicate(id, InstallMode.DownloadAndInstall, providerType);
                            else
                                CLIEngine.ShowMessage("Coming Soon...");
                        }
                        break;

                    case "uninstall":
                        {
                            if (uninstallPredicate != null)
                                await uninstallPredicate(id, providerType);
                            else
                                CLIEngine.ShowMessage("Coming Soon...");
                        }
                        break;

                    //case "reinstall":
                    //    {
                    //        if (isOAPPOrHappOrRuntime)
                    //        {
                    //            if (reinstallPredicate != null)
                    //                await reinstallPredicate(id, providerType);
                    //            else
                    //                CLIEngine.ShowMessage("Coming Soon...");
                    //        }
                    //        else
                    //            CLIEngine.ShowMessage("Command not supported.");
                    //    }
                    //    break;

                    case "publish":
                        {
                            if (publishPredicate != null)
                            {
                                if (subCommand.ToUpper() == "RUNTIME")
                                    await publishPredicate(id, false, DefaultLaunchMode.None, true, providerType);
                                else
                                    await publishPredicate(id, false, DefaultLaunchMode.Optional, true, providerType);
                            }
                            else
                                CLIEngine.ShowMessage("Coming Soon...");
                        }
                        break;

                    case "unpublish":
                        {
                            if (unpublishPredicate != null)
                                await unpublishPredicate(id, providerType);
                            else
                                CLIEngine.ShowMessage("Coming Soon...");
                        }
                        break;

                    case "republish":
                        {
                            if (republishPredicate != null)
                                await republishPredicate(id, providerType);
                            else
                                CLIEngine.ShowMessage("Coming Soon...");
                        }
                        break;

                    case "activate":
                        {
                            if (activatePredicate != null)
                                await activatePredicate(id, providerType);
                            else
                                CLIEngine.ShowMessage("Coming Soon...");
                        }
                        break;

                    case "deactivate":
                        {
                            if (deactivatePredicate != null)
                                await deactivatePredicate(id, providerType);
                            else
                                CLIEngine.ShowMessage("Coming Soon...");
                        }
                        break;

                    case "show":
                        {
                            if (id == "detailed")
                                id = inputArgs[3];

                            if (web3)
                            {
                                id = subCommandParam3;

                                if (showWeb3Predicate != null)
                                    await showWeb3Predicate(id, providerType);
                                else
                                    CLIEngine.ShowMessage("Coming Soon...");
                            }
                            else if (web4)
                            {
                                id = subCommandParam3;

                                if (showWeb4Predicate != null)
                                    await showWeb4Predicate(id, providerType);
                                else
                                    CLIEngine.ShowMessage("Coming Soon...");
                            }
                            else
                            {
                                if (showPredicate != null)
                                    await showPredicate(id, showDetailed, providerType);
                                else
                                    CLIEngine.ShowMessage("Coming Soon...");
                            }
                        }
                        break;

                    case "adddependency":
                        {
                            if (addDependencyPredicate != null)
                                await addDependencyPredicate(id, null, subCommandParam3, subCommandParam4, providerType);
                            else
                                CLIEngine.ShowMessage("Coming Soon...");
                        }
                        break;

                    case "removedependency":
                        {
                            if (removeDependencyPredicate != null)
                                await removeDependencyPredicate(id, subCommandParam3, subCommandParam4, providerType);
                            else
                                CLIEngine.ShowMessage("Coming Soon...");
                        }
                        break;

                    case "add":
                        {
                            if (addWeb4NFTToCollectionPredicate != null)
                                await addWeb4NFTToCollectionPredicate(id, subCommandParam3, providerType);
                            else
                                CLIEngine.ShowMessage("Coming Soon...");
                        }
                        break;

                    case "remove":
                        {
                            if (removeWeb4NFTFromCollectionPredicate != null)
                                await removeWeb4NFTFromCollectionPredicate(id, subCommandParam3, providerType);
                            else
                                CLIEngine.ShowMessage("Coming Soon...");
                        }
                        break;

                    case "list":
                        {
                            switch (subCommandParam2.ToLower())
                            {
                                case "installed":
                                    {
                                        if (listInstalledPredicate != null)
                                            await listInstalledPredicate(providerType);
                                        else
                                            CLIEngine.ShowMessage("Coming Soon...");
                                    }
                                    break;

                                case "uninstalled":
                                    {
                                        if (listInstalledPredicate != null)
                                            await listUninstalledPredicate(providerType);
                                        else
                                            CLIEngine.ShowMessage("Coming Soon...");
                                    }
                                    break;

                                case "unpublished":
                                    {
                                        if (listUnpublishedPredicate != null)
                                            await listUnpublishedPredicate(providerType);
                                        else
                                            CLIEngine.ShowMessage("Coming Soon...");
                                    }
                                    break;

                                case "deactivated":
                                    {
                                        if (listDeactivatedPredicate != null)
                                            await listDeactivatedPredicate(providerType);
                                        else
                                            CLIEngine.ShowMessage("Coming Soon...");
                                    }
                                    break;

                                default:
                                    {
                                        if (showForAllAvatars)
                                        {
                                            if (web3)
                                            {
                                                if (listAllWeb3Predicate != null)
                                                    await listAllWeb3Predicate(providerType);
                                                else
                                                    CLIEngine.ShowMessage("Coming Soon...");
                                            }
                                            else if (web4)
                                            {
                                                if (listAllWeb4Predicate != null)
                                                    await listAllWeb4Predicate(providerType);
                                                else
                                                    CLIEngine.ShowMessage("Coming Soon...");
                                            }
                                            else
                                            {
                                                if (listAllPredicate != null)
                                                    await listAllPredicate(showAllVersions, showDetailed, 0, providerType);
                                                else
                                                    CLIEngine.ShowMessage("Coming Soon...");
                                            }
                                        }
                                        else
                                        {
                                            if (web3)
                                            {
                                                if (listWeb3ForBeamedInAvatarPredicate != null)
                                                    await listWeb3ForBeamedInAvatarPredicate(providerType);
                                                else
                                                    CLIEngine.ShowMessage("Coming Soon...");
                                            }
                                            else if (web4)
                                            {
                                                if (listWeb4ForBeamedInAvatarPredicate != null)
                                                    await listWeb4ForBeamedInAvatarPredicate(providerType);
                                                else
                                                    CLIEngine.ShowMessage("Coming Soon...");
                                            }
                                            else
                                            {
                                                if (listForBeamedInAvatarPredicate != null)
                                                    await listForBeamedInAvatarPredicate(showAllVersions, showDetailed, providerType);
                                                else
                                                    CLIEngine.ShowMessage("Coming Soon...");
                                            }
                                        }
                                    }
                                    break;
                            }
                        }
                        break;

                    case "search":
                        {
                            string searchCriteria;
                            int searchMax = 0;
                            if (StarCliStarnetSearchArgv.TryParse(inputArgs, out string parsedCriteria, out int parsedMax, out _))
                            {
                                searchCriteria = parsedCriteria;
                                searchMax = parsedMax;
                            }
                            else
                            {
                                searchCriteria = !string.IsNullOrWhiteSpace(subCommandParam3) ? subCommandParam3 : subCommandParam2;
                                if (CLIEngine.NonInteractive)
                                {
                                    StarCliShellOutput.WriteError(CLIEngine.JsonOutput, 2,
                                        "search requires explicit criteria in non-interactive mode.",
                                        $"Example: {subCommand} search <criteria> [<maxResults>]  |  Global: --search-limit N");
                                    break;
                                }
                            }

                            if (CLIEngine.NonInteractive && string.IsNullOrWhiteSpace(searchCriteria))
                            {
                                StarCliShellOutput.WriteError(CLIEngine.JsonOutput, 2,
                                    "search requires a criteria token (name fragment or id). Example: oapp search MyOAPP 25",
                                    $"Entity: {subCommand}. Optional trailing integer limits rows (or use --search-limit N).");
                                break;
                            }

                            int effectiveSearchMax = searchMax > 0 ? searchMax : CLIEngine.MaxHolonSearchResults;

                            if (web3)
                            {
                                if (searchWeb3Predicate != null)
                                    await searchWeb3Predicate(searchCriteria, showForAllAvatars, providerType);
                                else
                                    CLIEngine.ShowMessage("Coming Soon...");
                            }
                            else if (web4)
                            {
                                if (searchWeb4Predicate != null)
                                    await searchWeb4Predicate(searchCriteria, showForAllAvatars, providerType);
                                else
                                    CLIEngine.ShowMessage("Coming Soon...");
                            }
                            else
                            {
                                if (searchPredicate != null)
                                    await searchPredicate(searchCriteria, default, showAllVersions, showForAllAvatars, providerType, effectiveSearchMax);
                                else
                                    CLIEngine.ShowMessage("Coming Soon...");
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
                if (CLIEngine.NonInteractive)
                {
                    StarCliShellOutput.WriteError(CLIEngine.JsonOutput, 2,
                        $"Non-interactive mode requires an explicit subcommand and arguments for '{subCommand}'.",
                        "Examples: oapp list | runtime show <idOrName> | holon list. See Docs/Devs/STAR_CLI_NonInteractive.md.");
                    return;
                }

                if (string.IsNullOrEmpty(subCommandPlural))
                    subCommandPlural = $"{subCommand}'s";

                Console.WriteLine("");
                Console.ForegroundColor = ConsoleColor.Green;
                CLIEngine.ShowMessage($"{subCommand.ToUpper()} SUBCOMMANDS:", ConsoleColor.Green);
                Console.WriteLine("");

                int commandSpace = 22;
                int paramSpace = 23;
                string paramDivider = "  ";
                string web4Param = "";

                if (subCommand.ToUpper() == "NFT" || subCommand.ToUpper() == "GEO-NFT")
                    web4Param = "[web3] [web4]";

                if (showCreate)
                {
                    if (subCommand.ToUpper() == "GEONFT")
                    {
                        CLIEngine.ShowMessage(string.Concat("    mint".PadRight(commandSpace), "".PadRight(paramSpace), paramDivider, "Mints a WEB4 OASIS Geo-NFT and places in Our World for the currently beamed in avatar."), ConsoleColor.Green, false);
                        CLIEngine.ShowMessage(string.Concat("    create".PadRight(commandSpace), "".PadRight(paramSpace), paramDivider, "Creates a WEB5 STAR Geo-NFT by wrapping around a WEB4 OASIS Geo-NFT."), ConsoleColor.Green, false);
                    }

                    else if (subCommand.ToUpper() == "NFT")
                    {
                        CLIEngine.ShowMessage(string.Concat("    mint".PadRight(commandSpace), "".PadRight(paramSpace), paramDivider, "Mints a WEB4 OASIS NFT for the currently beamed in avatar."), ConsoleColor.Green, false);
                        CLIEngine.ShowMessage(string.Concat("    create".PadRight(commandSpace), "".PadRight(paramSpace), paramDivider, "Creates a WEB5 STAR NFT by wrapping around a WEB4 OASIS NFT."), ConsoleColor.Green, false);
                    }

                    else if (subCommand.ToUpper() == "NFT COLLECTION")
                        CLIEngine.ShowMessage(string.Concat("    create".PadRight(commandSpace), "{id/name} [web4]".PadRight(paramSpace), paramDivider, "Creates a WEB5 STAR NFT by wrapping around a WEB4 OASIS NFT (see notes)."), ConsoleColor.Green, false);

                    else if (subCommand.ToUpper() == "GEO-NFT COLLECTION")
                        CLIEngine.ShowMessage(string.Concat("    create".PadRight(commandSpace), "{id/name} [web4]".PadRight(paramSpace), paramDivider, "Creates a WEB5 STAR GEO-NFT by wrapping around a WEB4 OASIS GEO-NFT (see notes)."), ConsoleColor.Green, false);

                    else
                        CLIEngine.ShowMessage(string.Concat("    create".PadRight(commandSpace), "{id/name}".PadRight(paramSpace), paramDivider, "Create a ", subCommand, "."), ConsoleColor.Green, false);
                }

                if (showUpdate)
                    CLIEngine.ShowMessage(string.Concat("    update".PadRight(commandSpace), string.Concat("{id/name} ", web4Param).PadRight(paramSpace), paramDivider, "Update an existing ", subCommand, " for the given {id} or {name}."), ConsoleColor.Green, false);

                if (showDelete)
                    CLIEngine.ShowMessage(string.Concat("    delete".PadRight(commandSpace), string.Concat("{id/name} ", web4Param).PadRight(paramSpace), paramDivider, "Delete an existing ", subCommand, " for the given {id} or {name}."), ConsoleColor.Green, false);

                if (subCommand.ToUpper() == "NFT" || subCommand.ToUpper() == "GEO-NFT")
                {
                    CLIEngine.ShowMessage(string.Concat("    remint".PadRight(commandSpace), "{id/name}".PadRight(paramSpace), paramDivider, "Remint an existing Web4 OASIS ", subCommand, " for the given {id} or {name} to create new Web3 Varients."), ConsoleColor.Green, false);
                    CLIEngine.ShowMessage(string.Concat("    burn".PadRight(commandSpace), "{id/name}".PadRight(paramSpace), paramDivider, "Burn's a OASIS ", subCommand, " for the given {id} or {name}"), ConsoleColor.Green, false);
                    CLIEngine.ShowMessage(string.Concat("    send".PadRight(commandSpace), "{id/name}".PadRight(paramSpace), paramDivider, "Send a OASIS ", subCommand, " for the given {id} or {name} to another wallet cross-chain."), ConsoleColor.Green, false);

                    if (subCommand.ToUpper() == "NFT")
                        CLIEngine.ShowMessage(string.Concat("    import".PadRight(commandSpace), "{id/name} [web3]".PadRight(paramSpace), paramDivider, "Imports a OASIS ", subCommand, " JSON file for the given {id} or {name}."), ConsoleColor.Green, false);
                    else
                        CLIEngine.ShowMessage(string.Concat("    import".PadRight(commandSpace), "{id/name}".PadRight(paramSpace), paramDivider, "Imports a OASIS ", subCommand, " JSON file for the given {id} or {name}."), ConsoleColor.Green, false);

                    CLIEngine.ShowMessage(string.Concat("    export".PadRight(commandSpace), "{id/name}".PadRight(paramSpace), paramDivider, "Exports a OASIS ", subCommand, " for the given {id} or {name} as a JSON file as well as a WEB3 JSON MetaData file."), ConsoleColor.Green, false);CLIEngine.ShowMessage(string.Concat("    burn".PadRight(commandSpace), "{id/name}".PadRight(paramSpace), paramDivider, "Burn's a OASIS ", subCommand, " for the given {id} or {name}"), ConsoleColor.Green, false);
                    CLIEngine.ShowMessage(string.Concat("    convert".PadRight(commandSpace), "{id/name}".PadRight(paramSpace), paramDivider, "Allows the minting of different WEB3 NFT Standards for different chains from the same OASIS WEB4 Metadata."), ConsoleColor.Green, false);
                }

                if (subCommand.ToUpper() == "GEO-NFT")
                    CLIEngine.ShowMessage(string.Concat("    place".PadRight(commandSpace), "{id/name}".PadRight(paramSpace), paramDivider, "Create a OASIS Geo-NFT from an existing OASIS NFT for the given {id} or {name} and place within Our World."), ConsoleColor.Green, false);

                CLIEngine.ShowMessage(string.Concat("    clone".PadRight(commandSpace), "{id/name}".PadRight(paramSpace), paramDivider, "Clones a OASIS ", subCommand, " for the given {id} or {name}."), ConsoleColor.Green, false);
                CLIEngine.ShowMessage(string.Concat("    adddependency".PadRight(commandSpace), "{id/name}".PadRight(paramSpace), paramDivider, "Adds a dependency to the ", subCommand, " for the given {id} or {name}."), ConsoleColor.Green, false);
                CLIEngine.ShowMessage(string.Concat("    removedependency".PadRight(commandSpace), "{id/name}".PadRight(paramSpace), paramDivider, "Removes a dependency from the ", subCommand, " for the given {id} or {name}."), ConsoleColor.Green, false);
                CLIEngine.ShowMessage(string.Concat("    download".PadRight(commandSpace), "{id/name}".PadRight(paramSpace), paramDivider, "Download a ", subCommand, " for the given {id} or {name}."), ConsoleColor.Green, false);
                CLIEngine.ShowMessage(string.Concat("    install".PadRight(commandSpace), "{id/name}".PadRight(paramSpace), paramDivider, "Install/download a ", subCommand, " for the given {id} or {name}."), ConsoleColor.Green, false);
                CLIEngine.ShowMessage(string.Concat("    uninstall".PadRight(commandSpace), "{id/name}".PadRight(paramSpace), paramDivider, "Uninstall a ", subCommand, " for the given {id} or {name}."), ConsoleColor.Green, false);
                //CLIEngine.ShowMessage(string.Concat("    reinstall".PadRight(commandSpace), "{id/name}".PadRight(paramSpace), paramDivider, "Reinstall a ", subCommand, " for the given {id} or {name}."), ConsoleColor.Green, false);

                if (subCommand.ToUpper() == "OAPP" || subCommand.ToUpper() == "OAPPTEMPLATE" || subCommand.ToUpper() == "HAPP")
                {
                    if (subCommand.ToUpper() == "HAPP")
                        CLIEngine.ShowMessage(string.Concat("    publish".PadRight(commandSpace), ("{hAppPath} [publishDotNet]".PadRight(paramSpace)), paramDivider, "Publish a ", subCommand, " for the given {hAppPath}."), ConsoleColor.Green, false);
                    else
                        CLIEngine.ShowMessage(string.Concat("    publish".PadRight(commandSpace), "{oappPath} [publishDotNet]".PadRight(paramSpace), paramDivider, "Publish a ", subCommand, " for the given {oappPath}."), ConsoleColor.Green, false);
                }
                else
                    CLIEngine.ShowMessage(string.Concat("    publish".PadRight(commandSpace), "{id/name}".PadRight(paramSpace), paramDivider, "Publish a ", subCommand, " to STARNET for the given {id} or {name}."), ConsoleColor.Green, false);

                CLIEngine.ShowMessage(string.Concat("    unpublish".PadRight(commandSpace), "{id/name}".PadRight(paramSpace), paramDivider, "Unpublish a ", subCommand, " from STARNET for the given {id} or {name}."), ConsoleColor.Green, false);
                CLIEngine.ShowMessage(string.Concat("    republish".PadRight(commandSpace), "{id/name}".PadRight(paramSpace), paramDivider, "Republish a ", subCommand, " to STARNET for the given {id} or {name}."), ConsoleColor.Green, false);
                CLIEngine.ShowMessage(string.Concat("    activate".PadRight(commandSpace), "{id/name}".PadRight(paramSpace), paramDivider, "Activate (show) a ", subCommand, " on the STARNET for the given {id} or {name}."), ConsoleColor.Green, false);
                CLIEngine.ShowMessage(string.Concat("    deactivate".PadRight(commandSpace), "{id/name}".PadRight(paramSpace), paramDivider, "Deactivate (hide) a ", subCommand, " on the STARNET for the given {id} or {name}."), ConsoleColor.Green, false);
                //CLIEngine.ShowMessage(string.Concat("    list".PadRight(commandSpace), string.Concat("[allVersions] [forAllAvatars] [detailed]", web4Param).PadRight(paramSpace), paramDivider, "List all ", subCommandPlural, " that have been created."), ConsoleColor.Green, false);
                CLIEngine.ShowMessage(string.Concat("    list".PadRight(commandSpace), string.Concat("", web4Param).PadRight(paramSpace), paramDivider, "List all ", subCommandPlural, " that have been created."), ConsoleColor.Green, false);
                CLIEngine.ShowMessage(string.Concat("    list installed".PadRight(commandSpace), "".PadRight(paramSpace), paramDivider, "List all ", subCommandPlural, " installed for the currently beamed in avatar."), ConsoleColor.Green, false);
                CLIEngine.ShowMessage(string.Concat("    list uninstalled".PadRight(commandSpace), "".PadRight(paramSpace), paramDivider, "List all ", subCommandPlural, " uninstalled for the currently beamed in avatar (allows reinstalling)."), ConsoleColor.Green, false);
                CLIEngine.ShowMessage(string.Concat("    list unpublished".PadRight(commandSpace), "".PadRight(paramSpace), paramDivider, "List all ", subCommandPlural, " unpublished for the currently beamed in avatar (allows republishing)."), ConsoleColor.Green, false);
                CLIEngine.ShowMessage(string.Concat("    list deactivated".PadRight(commandSpace), "".PadRight(paramSpace), paramDivider, "List all ", subCommandPlural, " deactivated for the currently beamed in avatar (allows reactivating)."), ConsoleColor.Green, false);
                CLIEngine.ShowMessage(string.Concat("    show".PadRight(commandSpace), string.Concat("{id/name} ", web4Param).PadRight(paramSpace), paramDivider, "Shows the ", subCommandPlural, " for the given {id} or {name}."), ConsoleColor.Green, false);
                CLIEngine.ShowMessage(string.Concat("    search".PadRight(commandSpace), string.Concat("{id/name} ", web4Param).PadRight(paramSpace), paramDivider, "Searches the ", subCommandPlural, " for the given search critera."), ConsoleColor.Green, false);
                
                if (subCommand.ToUpper() == "OAPP")
                    CLIEngine.ShowMessage(string.Concat("    template".PadRight(commandSpace), "".PadRight(paramSpace), paramDivider, "Shows the OAPP Template Subcommand menu."), ConsoleColor.Green, false);

                if (subCommand.ToUpper() == "CELESTIAL BODY")
                    CLIEngine.ShowMessage(string.Concat("    metadata".PadRight(commandSpace), "".PadRight(paramSpace), paramDivider, "Shows the CelestialBody MetaData DNA Subcommand menu."), ConsoleColor.Green, false);

                if (subCommand.ToUpper() == "ZOME")
                    CLIEngine.ShowMessage(string.Concat("    metadata".PadRight(commandSpace), "".PadRight(paramSpace), paramDivider, "Shows the Zome MetaData DNA Subcommand menu."), ConsoleColor.Green, false);

                if (subCommand.ToUpper() == "HOLON")
                    CLIEngine.ShowMessage(string.Concat("    metadata".PadRight(commandSpace), "".PadRight(paramSpace), paramDivider, "Shows the Holon MetaData DNA Subcommand menu."), ConsoleColor.Green, false);

                if (subCommand.ToUpper() == "NFT COLLECTION")
                {
                    CLIEngine.ShowMessage(string.Concat("    add".PadRight(commandSpace), "{id/name} {id/name}".PadRight(paramSpace), paramDivider, "Add's a WEB4 OASIS NFT to the collection."), ConsoleColor.Green, false);
                    CLIEngine.ShowMessage(string.Concat("    remove".PadRight(commandSpace), "{id/name} {id/name}".PadRight(paramSpace), paramDivider, "Remove's a WEB4 OASIS NFT from the collection."), ConsoleColor.Green, false);
                }

                if (subCommand.ToUpper() == "GEONFT COLLECTION")
                {
                    CLIEngine.ShowMessage(string.Concat("    add".PadRight(commandSpace), "{id/name} {id/name}".PadRight(paramSpace), paramDivider, "Add's a WEB4 OASIS GEO-NFT to the collection."), ConsoleColor.Green, false);
                    CLIEngine.ShowMessage(string.Concat("    remove".PadRight(commandSpace), "{id/name} {id/name}".PadRight(paramSpace), paramDivider, "Remove's a WEB4 OASIS GEO-NFT from the collection."), ConsoleColor.Green, false);
                }

                CLIEngine.ShowMessage($"NOTES:", ConsoleColor.Green);

                if (subCommand.ToUpper() == "OAPP")
                    CLIEngine.ShowMessage($"For the publish command, if the flag [publishDotNet] is specified it will first do a dotnet publish before publishing to STARNET.", ConsoleColor.Green);
                
                CLIEngine.ShowMessage($"For the list & search commands, if [allVersions] is omitted it will list the current version, otherwise it will list all versions. If [forAllAvatars] is omitted it will list only your {subCommandPlural}'s otherwise it will list all published {subCommandPlural}'s as well as yours.", ConsoleColor.Green);
                CLIEngine.ShowMessage($"For the list & show commands, if [detailed] is included it will list detailed stats also such as all dependenices installed.", ConsoleColor.Green);

                if (subCommand.ToUpper() == "GEO-NFT")
                    CLIEngine.ShowMessage($"For the update, delete, list, show or search command, if [web4] is included it will update/delete/list/show/search WEB4 OASIS Geo-NFT's, otherwise it will update/delete/list/show/search WEB5 STAR Geo-NFT's.", ConsoleColor.Green);

                if (subCommand.ToUpper() == "NFT")
                {
                    //If the[web3] parameter is included 
                    CLIEngine.ShowMessage($"For the import command if [web3] is included it will import an existing WEB3 NFT(JSON MetaData or NFT Token Address) and wrap it in a new WEB4 OASIS NFT.", ConsoleColor.Green);
                    CLIEngine.ShowMessage($"For the update, delete, list, show or search command, if [web3] is included it will update/delete/list/show/search WEB3 NFT's, if [web4] is included it will update/delete/list/show/search WEB4 OASIS NFT's, otherwise it will update/delete/list/show/search WEB5 STAR NFT's.", ConsoleColor.Green);                    
                }

                if (subCommand.ToUpper() == "GEO-NFT COLLECTION")
                    CLIEngine.ShowMessage($"For the create, update, delete, list, show or search command, if [web4] is included it will create/update/delete/list/show/search WEB4 OASIS Geo-NFT Collection's, otherwise it will create/update/delete/list/show/search WEB5 STAR Geo-NFT Collection's.", ConsoleColor.Green);

                if (subCommand.ToUpper() == "NFT COLLECTION")
                    CLIEngine.ShowMessage($"For the create, update, delete, list, show or search command, if [web4] is included it will create/update/delete/list/show/search WEB4 OASIS NFT Collection's, otherwise it will create/update/delete/list/show/search WEB5 STAR NFT Collection's.", ConsoleColor.Green);


                CLIEngine.ShowMessage("More Coming Soon...", ConsoleColor.Green);
            }
        }

    }
}
