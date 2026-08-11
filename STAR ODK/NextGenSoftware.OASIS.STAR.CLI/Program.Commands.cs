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

            if ((inputArgs.Length > 1 && inputArgs[1] != "template" && inputArgs[1] != "metadata" && inputArgs[1] != "collection") || (inputArgs.Length > 2 && (inputArgs[1] == "template" || inputArgs[1] == "metadata" || inputArgs[1] == "collection")))
            {
                if (inputArgs[1] != "template" && inputArgs[1] != "metadata" && inputArgs[1] != "collection" && inputArgs.Length > 2)
                    id = inputArgs[2];

                if ((inputArgs[1] == "template" || inputArgs[1] == "metadata" || inputArgs[1] == "collection") && inputArgs.Length > 3)
                    id = inputArgs[3];

                if (inputArgs.Length > 1 && !string.IsNullOrEmpty(inputArgs[1])) subCommandParam = inputArgs[1].ToLower();
                if (inputArgs.Length > 2 && !string.IsNullOrEmpty(inputArgs[2])) subCommandParam2 = inputArgs[2].ToLower();
                if (inputArgs.Length > 3 && !string.IsNullOrEmpty(inputArgs[3])) subCommandParam3 = inputArgs[3].ToLower();
                if (inputArgs.Length > 4 && !string.IsNullOrEmpty(inputArgs[4])) subCommandParam4 = inputArgs[4].ToLower();

                if (inputArgs[1] == "template" || inputArgs[1] == "metadata" || inputArgs[1] == "collection")
                {
                    if (inputArgs.Length > 2 && !string.IsNullOrEmpty(inputArgs[2])) subCommandParam = inputArgs[2].ToLower();
                    if (inputArgs.Length > 3 && !string.IsNullOrEmpty(inputArgs[3])) subCommandParam2 = inputArgs[3].ToLower();
                    if (inputArgs.Length > 4 && !string.IsNullOrEmpty(inputArgs[4])) subCommandParam3 = inputArgs[4].ToLower();
                    if (inputArgs.Length > 5 && !string.IsNullOrEmpty(inputArgs[5])) subCommandParam4 = inputArgs[5].ToLower();
                }

                if (subCommandParam2 == "allversions" || subCommandParam3 == "allversions") showAllVersions = true;
                if (subCommandParam2 == "forallavatars" || subCommandParam3 == "forallavatars") showForAllAvatars = true;
                if (subCommandParam == "detailed" || subCommandParam2 == "detailed" || subCommandParam3 == "detailed") showDetailed = true;

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
                        CLIEngine.JsonOutput, subCommand, subCommandParam, id, inputArgs,
                        subCommandParam3, subCommandParam4, web3, web4,
                        mintPredicate != null, burnPredicate != null, clonePredicate != null, convertPredicate != null,
                        importPredicate != null, exportPredicate != null,
                        addWeb4NFTToCollectionPredicate != null, removeWeb4NFTFromCollectionPredicate != null,
                        addDependencyPredicate != null, removeDependencyPredicate != null))
                    return;

                switch (subCommandParam)
                {
                    case "light":
                    {
                        if (!(string.Equals(subCommand, "OAPP", StringComparison.OrdinalIgnoreCase) || string.Equals(subCommand, "hApp", StringComparison.OrdinalIgnoreCase)))
                        { CLIEngine.ShowErrorMessage("Command Unknown."); break; }
                        if (!showCreate) { CLIEngine.ShowErrorMessage("Command not supported."); break; }
                        if (!StarnetUiScriptedCreateCli.TryParseOappLightDirectArgv(inputArgs, out string oappLightOnlyJson, out string oappLightOnlyErr))
                        { CLIEngine.ShowErrorMessage("Command Unknown."); break; }
                        if (!string.IsNullOrEmpty(oappLightOnlyErr)) { StarCliShellOutput.WriteError(CLIEngine.JsonOutput, 2, oappLightOnlyErr, "Example: star --non-interactive --json oapp light ./LightRequest.json"); break; }
                        if (!File.Exists(oappLightOnlyJson)) { StarCliShellOutput.WriteError(CLIEngine.JsonOutput, 2, $"Light JSON file not found: {oappLightOnlyJson}", "See Docs/Devs/STAR_CLI_NonInteractive.md (Light JSON schema)."); break; }
                        var oappLightOnlyOpts = new STARNETCreateOptions<OAPP, STARNETDNA> { STARNETHolon = new OAPP(), CustomCreateParams = StarnetUiScriptedCreateCli.BuildOappLightJsonCustomCreateParams(oappLightOnlyJson) };
                        OASISResult<OAPP> lightOnlyRes = await STARCLI.OAPPs.CreateAsync(oappLightOnlyOpts, null, false, false, providerType);
                        if (CLIEngine.JsonOutput)
                            EmitNiJsonForOasisResult(lightOnlyRes, $"{subCommand} light", lightOnlyRes.Result != null ? new { id = lightOnlyRes.Result.STARNETDNA?.Id, name = lightOnlyRes.Result.STARNETDNA?.Name } : null);
                    }
                    break;

                    case "create":
                        await HandleCreateCommandAsync<T>(inputArgs, subCommand, web4, showCreate, createPredicate, createWeb4Predicate, providerType);
                        break;

                    case "mint":
                    {
                        if (mintPredicate != null)
                        {
                            if (CLIEngine.NonInteractive)
                            {
                                if (!StarCliNftStructuredArgv.TryGetMintRequestJsonPath(inputArgs, out string mintJson, out string mintErr)) { StarCliShellOutput.WriteError(CLIEngine.JsonOutput, 2, mintErr ?? "Invalid mint arguments.", "Example: nft mint /path/to/MintWeb4NFTRequest.json"); break; }
                                if (string.Equals(subCommand, "nft", StringComparison.OrdinalIgnoreCase)) { OASISResult<IWeb4NFT> r = await STARCLI.NFTs.MintNFTAsync(mintJson); EmitNiJsonForOasisResult(r, "nft mint", r.Result != null ? new { web4NftId = r.Result.Id.ToString() } : null); }
                                else if (string.Equals(subCommand, "geo-nft", StringComparison.OrdinalIgnoreCase)) { OASISResult<IWeb4GeoSpatialNFT> r = await STARCLI.GeoNFTs.MintGeoNFTAsync(mintJson); EmitNiJsonForOasisResult(r, "geo-nft mint", r.Result != null ? new { web4GeoNftId = r.Result.Id.ToString() } : null); }
                                else await mintPredicate(mintJson);
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
                        bool isNft = string.Equals(subCommand, "nft", StringComparison.OrdinalIgnoreCase);
                        bool isGeoNft = string.Equals(subCommand, "geo-nft", StringComparison.OrdinalIgnoreCase);
                        string remintTarget = null;
                        if (CLIEngine.NonInteractive)
                        {
                            if (!StarCliNftStructuredArgv.TryGetRemintTargetId(inputArgs, out remintTarget, out string remintErr)) { StarCliShellOutput.WriteError(CLIEngine.JsonOutput, 2, remintErr ?? "remint requires a target id.", "Example: nft remint <web4NftGuid>"); break; }
                        }
                        if (isNft) { OASISResult<IWeb4NFT> r = await STARCLI.NFTs.RemintNFTAsync(remintTarget); EmitNiJsonForOasisResult(r, "nft remint", r.Result != null ? new { web4NftId = r.Result.Id.ToString() } : null); }
                        else if (isGeoNft) { OASISResult<IWeb4GeoSpatialNFT> r = await STARCLI.GeoNFTs.RemintGeoNFTAsync(remintTarget); EmitNiJsonForOasisResult(r, "geo-nft remint", r.Result != null ? new { web4GeoNftId = r.Result.Id.ToString() } : null); }
                        else CLIEngine.ShowErrorMessage("Command not supported.");
                    }
                    break;

                    case "place":
                    {
                        if (string.Equals(subCommand, "geo-nft", StringComparison.OrdinalIgnoreCase))
                        {
                            if (CLIEngine.NonInteractive)
                            {
                                if (!StarCliNftStructuredArgv.TryGetPlaceGeoJsonPath(inputArgs, out string placeJson, out string placeErr)) { StarCliShellOutput.WriteError(CLIEngine.JsonOutput, 2, placeErr ?? "Invalid place arguments.", "Example: geo-nft place /path/to/PlaceWeb4GeoSpatialNFTRequest.json"); break; }
                                OASISResult<IWeb4GeoSpatialNFT> r = await STARCLI.GeoNFTs.PlaceGeoNFTFromJsonFileAsync(placeJson);
                                EmitNiJsonForOasisResult(r, "geo-nft place", r.Result != null ? new { web4GeoNftId = r.Result.Id.ToString() } : null);
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
                                if (!StarCliNftStructuredArgv.TryGetBurnRequestJsonPath(inputArgs, out string burnJson, out string burnErr)) { StarCliShellOutput.WriteError(CLIEngine.JsonOutput, 2, burnErr ?? "Invalid burn arguments.", "Example: nft burn /path/to/BurnWeb3NFTRequest.json"); break; }
                                if (string.Equals(subCommand, "nft", StringComparison.OrdinalIgnoreCase)) { OASISResult<IWeb4NFT> r = await STARCLI.NFTs.BurnNFTAsync(burnJson); EmitNiJsonForOasisResult(r, "nft burn", null); }
                                else if (string.Equals(subCommand, "geo-nft", StringComparison.OrdinalIgnoreCase)) { OASISResult<IWeb4GeoSpatialNFT> r = await STARCLI.GeoNFTs.BurnGeoNFTAsync(burnJson); EmitNiJsonForOasisResult(r, "geo-nft burn", null); }
                                else await burnPredicate(burnJson);
                            }
                            else
                                await burnPredicate(null);
                        }
                        else
                            CLIEngine.ShowErrorMessage("Command not supported or comming soon...");
                    }
                    break;

                    case "import":
                        await HandleImportCommandAsync(inputArgs, subCommand, web3, importPredicate);
                        break;

                    case "export":
                    {
                        if (exportPredicate != null)
                        {
                            bool niWeb4Export = CLIEngine.NonInteractive && (string.Equals(subCommand, "nft", StringComparison.OrdinalIgnoreCase) || string.Equals(subCommand, "geo-nft", StringComparison.OrdinalIgnoreCase));
                            if (niWeb4Export)
                            {
                                if (!StarCliNftStructuredArgv.TryGetExportDest(inputArgs, out string exId, out string exPath, out string exErr)) { StarCliShellOutput.WriteError(CLIEngine.JsonOutput, 2, exErr ?? "Invalid export arguments.", "Example: nft export <idOrGuid> /dest/path"); break; }
                                if (string.Equals(subCommand, "nft", StringComparison.OrdinalIgnoreCase)) { OASISResult<IWeb4NFT> r = await STARCLI.NFTs.ExportNFTNonInteractiveAsync(exId, exPath, providerType); EmitNiJsonForOasisResult(r, "nft export", r.Result != null ? new { web4NftId = r.Result.Id.ToString(), destinationPath = exPath } : null); }
                                else { OASISResult<IWeb4GeoSpatialNFT> r = await STARCLI.GeoNFTs.ExportGeoNFTNonInteractiveAsync(exId, exPath, providerType); EmitNiJsonForOasisResult(r, "geo-nft export", r.Result != null ? new { web4GeoNftId = r.Result.Id.ToString(), destinationPath = exPath } : null); }
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
                                if (!StarCliNftStructuredArgv.TryGetFirstTokenAfterVerb(inputArgs, "clone", out string cloneSourceId, out string cloneErr)) { StarCliShellOutput.WriteError(CLIEngine.JsonOutput, 2, cloneErr ?? "clone requires a source id or name.", $"Example: {subCommand} clone <sourceIdOrName>"); break; }
                                cloneArg = cloneSourceId;
                            }
                            OASISResult<T> cloneRes = await clonePredicate(cloneArg);
                            if (CLIEngine.JsonOutput)
                                EmitNiJsonForOasisResult(cloneRes, $"{subCommand} clone", cloneRes != null && !cloneRes.IsError && cloneRes.Result != null ? new { id = cloneRes.Result.STARNETDNA?.Id, name = cloneRes.Result.STARNETDNA?.Name } : null);
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
                                if (!StarCliNftStructuredArgv.TryGetFirstTokenAfterVerb(inputArgs, "convert", out string convertId, out string convertErr)) { StarCliShellOutput.WriteError(CLIEngine.JsonOutput, 2, convertErr ?? "convert requires a source id or name.", $"Example: {subCommand} convert <sourceIdOrName>"); break; }
                                if (string.Equals(subCommand, "nft", StringComparison.OrdinalIgnoreCase)) { OASISResult<IWeb4NFT> r = await STARCLI.NFTs.ConvertNFTAsync(convertId); EmitNiJsonForOasisResult(r, "nft convert", null); }
                                else if (string.Equals(subCommand, "geo-nft", StringComparison.OrdinalIgnoreCase)) { OASISResult<IWeb4GeoSpatialNFT> r = await STARCLI.GeoNFTs.ConvertGeoNFTAsync(convertId); EmitNiJsonForOasisResult(r, "geo-nft convert", null); }
                                else await convertPredicate(convertId);
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
                            if (!StarCliNftStructuredArgv.TryGetSendArgs(inputArgs, out string sFrom, out string sTo, out string sTok, out string sMemo, out string sendErr)) { StarCliShellOutput.WriteError(CLIEngine.JsonOutput, 2, sendErr ?? "Invalid send arguments.", "Example: nft send <fromWallet> <toWallet> <tokenAddress> <memo>"); break; }
                            if (isNftSend) { OASISResult<ISendWeb4NFTResponse> r = await STARCLI.NFTs.SendNFTAsync(sFrom, sTo, sTok, sMemo); EmitNiJsonForOasisResult(r, "nft send", r.Result != null ? new { bridgeOrderId = r.Result.BridgeOrderId, sendTransactionResult = r.Result.SendTransactionResult } : null); }
                            else if (isGeoNftSend) { OASISResult<ISendWeb4NFTResponse> r = await STARCLI.GeoNFTs.SendGeoNFTAsync(sFrom, sTo, sTok, sMemo); EmitNiJsonForOasisResult(r, "geo-nft send", r.Result != null ? new { bridgeOrderId = r.Result.BridgeOrderId, sendTransactionResult = r.Result.SendTransactionResult } : null); }
                            else CLIEngine.ShowErrorMessage("Command not supported.");
                        }
                        else
                        {
                            if (isNftSend) await STARCLI.NFTs.SendNFTAsync();
                            else if (isGeoNftSend) await STARCLI.GeoNFTs.SendGeoNFTAsync();
                            else CLIEngine.ShowErrorMessage("Command not supported.");
                        }
                    }
                    break;

                    case "update":
                    {
                        if (showUpdate)
                        {
                            if (web3)
                            {
                                id = inputArgs.Length > 3 ? inputArgs[3] : "";
                                if (updateWeb3Predicate != null) await updateWeb3Predicate(id, providerType); else CLIEngine.ShowMessage("Coming Soon...");
                            }
                            else if (web4)
                            {
                                id = inputArgs.Length > 3 ? inputArgs[3] : "";
                                if (updateWeb4Predicate != null) await updateWeb4Predicate(id, providerType); else CLIEngine.ShowMessage("Coming Soon...");
                            }
                            else
                            {
                                if (updatePredicate != null)
                                {
                                    object questEditParams = null;
                                    if (CLIEngine.NonInteractive && string.Equals(subCommand, "quest", StringComparison.OrdinalIgnoreCase) && StarnetUiScriptedCreateCli.TryParseQuestUpdateArgv(inputArgs, out QuestCliEditParams qEdit))
                                        questEditParams = qEdit;
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
                        await HandleDeleteCommandAsync(inputArgs, subCommand, id, web3, web4, showDelete, deletePredicate, deleteWeb3Predicate, providerType);
                        break;

                    case "download":
                        if (downloadAndInstallPredicate != null) await downloadAndInstallPredicate(id, InstallMode.DownloadOnly, providerType); else CLIEngine.ShowMessage("Coming Soon...");
                        break;

                    case "install":
                        if (downloadAndInstallPredicate != null) await downloadAndInstallPredicate(id, InstallMode.DownloadAndInstall, providerType); else CLIEngine.ShowMessage("Coming Soon...");
                        break;

                    case "uninstall":
                        if (uninstallPredicate != null) await uninstallPredicate(id, providerType); else CLIEngine.ShowMessage("Coming Soon...");
                        break;

                    case "publish":
                        if (publishPredicate != null)
                            await publishPredicate(id, false, subCommand.ToUpper() == "RUNTIME" ? DefaultLaunchMode.None : DefaultLaunchMode.Optional, true, providerType);
                        else CLIEngine.ShowMessage("Coming Soon...");
                        break;

                    case "unpublish":
                        if (unpublishPredicate != null) await unpublishPredicate(id, providerType); else CLIEngine.ShowMessage("Coming Soon...");
                        break;

                    case "republish":
                        if (republishPredicate != null) await republishPredicate(id, providerType); else CLIEngine.ShowMessage("Coming Soon...");
                        break;

                    case "activate":
                        if (activatePredicate != null) await activatePredicate(id, providerType); else CLIEngine.ShowMessage("Coming Soon...");
                        break;

                    case "deactivate":
                        if (deactivatePredicate != null) await deactivatePredicate(id, providerType); else CLIEngine.ShowMessage("Coming Soon...");
                        break;

                    case "show":
                    {
                        if (id == "detailed") id = inputArgs[3];
                        if (web3) { id = subCommandParam3; if (showWeb3Predicate != null) await showWeb3Predicate(id, providerType); else CLIEngine.ShowMessage("Coming Soon..."); }
                        else if (web4) { id = subCommandParam3; if (showWeb4Predicate != null) await showWeb4Predicate(id, providerType); else CLIEngine.ShowMessage("Coming Soon..."); }
                        else { if (showPredicate != null) await showPredicate(id, showDetailed, providerType); else CLIEngine.ShowMessage("Coming Soon..."); }
                    }
                    break;

                    case "adddependency":
                        if (addDependencyPredicate != null) await addDependencyPredicate(id, null, subCommandParam3, subCommandParam4, providerType); else CLIEngine.ShowMessage("Coming Soon...");
                        break;

                    case "removedependency":
                        if (removeDependencyPredicate != null) await removeDependencyPredicate(id, subCommandParam3, subCommandParam4, providerType); else CLIEngine.ShowMessage("Coming Soon...");
                        break;

                    case "add":
                        if (addWeb4NFTToCollectionPredicate != null) await addWeb4NFTToCollectionPredicate(id, subCommandParam3, providerType); else CLIEngine.ShowMessage("Coming Soon...");
                        break;

                    case "remove":
                        if (removeWeb4NFTFromCollectionPredicate != null) await removeWeb4NFTFromCollectionPredicate(id, subCommandParam3, providerType); else CLIEngine.ShowMessage("Coming Soon...");
                        break;

                    case "list":
                        await HandleListCommandAsync(subCommandParam2, web3, web4, showForAllAvatars, showAllVersions, showDetailed,
                            listForBeamedInAvatarPredicate, listAllPredicate, listInstalledPredicate, listUninstalledPredicate,
                            listUnpublishedPredicate, listDeactivatedPredicate, listAllWeb3Predicate, listAllWeb4Predicate,
                            listWeb3ForBeamedInAvatarPredicate, listWeb4ForBeamedInAvatarPredicate, providerType);
                        break;

                    case "search":
                        await HandleSearchCommandAsync(inputArgs, subCommand, subCommandParam2, subCommandParam3,
                            web3, web4, showForAllAvatars, showAllVersions,
                            searchPredicate, searchWeb3Predicate, searchWeb4Predicate, providerType);
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

                ShowSubCommandMenu(subCommand, subCommandPlural, showCreate, showUpdate, showDelete);
            }
        }

    }
}
