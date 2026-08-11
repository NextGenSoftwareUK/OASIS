using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.CLI.Engine;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.STAR.CLI.Lib;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.Core.Objects;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;

namespace NextGenSoftware.OASIS.STAR.CLI
{
    partial class Program
    {
        private static async Task HandleCreateCommandAsync<T>(
            string[] inputArgs, string subCommand, bool web4, bool showCreate,
            Func<ISTARNETCreateOptions<T, STARNETDNA>, object, bool, bool, ProviderType, Task> createPredicate,
            Func<object, ProviderType, Task> createWeb4Predicate,
            ProviderType providerType) where T : ISTARNETHolon, new()
        {
            if (!showCreate) { CLIEngine.ShowErrorMessage("Command not supported."); return; }

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
                            StarCliShellOutput.WriteError(CLIEngine.JsonOutput, 2, w4Err ?? "Invalid create arguments.", "web4 flag with no web4-specific create: using same argv as web5 scripted create.");
                            return;
                        }
                        var w4Opts = new STARNETCreateOptions<T, STARNETDNA> { STARNETHolon = new T(), CustomCreateParams = StarnetUiScriptedCreateCli.BuildScriptedCustomCreateParams(w4Name, w4Desc, w4Cat, w4Parent, w4LibLang) };
                        await createPredicate(w4Opts, null, false, false, providerType);
                    }
                    else
                        StarCliShellOutput.WriteError(CLIEngine.JsonOutput, 2, "Non-interactive web4 create is not available for this entity.", "Omit web4 keyword or use a holon with scripted create. See Docs/Devs/STAR_CLI_NonInteractive.md.");
                }
                else if (createWeb4Predicate != null)
                    await createWeb4Predicate(null, providerType);
                else
                    CLIEngine.ShowMessage("Coming Soon...");
                return;
            }

            if (CLIEngine.NonInteractive)
            {
                await HandleNonInteractiveCreateAsync<T>(inputArgs, subCommand, createPredicate, providerType);
                return;
            }

            if (createPredicate != null)
                await createPredicate(null, null, true, true, providerType);
            else
                CLIEngine.ShowMessage("Coming Soon...");
        }

        private static async Task HandleNonInteractiveCreateAsync<T>(
            string[] inputArgs, string subCommand,
            Func<ISTARNETCreateOptions<T, STARNETDNA>, object, bool, bool, ProviderType, Task> createPredicate,
            ProviderType providerType) where T : ISTARNETHolon, new()
        {
            if (StarnetUiScriptedCreateCli.HolonLabelBypassesBaseScriptedCreate(subCommand))
            {
                StarCliShellOutput.WriteError(CLIEngine.JsonOutput, 2,
                    $"Non-interactive scripted create is not available for '{subCommand}' (this holon type does not delegate to STARNETUIBase scripted create).",
                    "See StarnetUiScriptedCreateCli.HolonLabelBypassesBaseScriptedCreate in STAR.CLI.Lib and Docs/Devs/STAR_CLI_NonInteractive.md (Generic design).");
                return;
            }

            STARNETCreateOptions<T, STARNETDNA> scriptedOpts = null;

            if (string.Equals(subCommand, "geo-hotspot", StringComparison.OrdinalIgnoreCase))
            {
                if (!StarnetUiScriptedCreateCli.TryParseGeoHotSpotCreateArgv(inputArgs, out string ghName, out string ghDesc, out string ghType, out double ghLat, out double ghLon, out int ghRad, out string ghTrig, out int? ghTime, out string ghParent, out string ghErr))
                {
                    StarCliShellOutput.WriteError(CLIEngine.JsonOutput, 2, ghErr ?? "Invalid geo-hotspot create arguments.", "Example: geo-hotspot create MyHS \"Desc\" Audio 51.5 -0.1 25 WhenArrivedAtGeoLocation [parentFolder] --audio-url https://example.com/a.mp3");
                    return;
                }
                var ghParams = StarnetUiScriptedCreateCli.BuildGeoHotSpotScriptedCustomCreateParams(ghName, ghDesc, ghType, ghLat, ghLon, ghRad, ghTrig, ghTime, ghParent);
                StarnetUiScriptedCreateCli.ApplyGeoHotSpotMediaOptionalArgs(inputArgs, ghParams);
                scriptedOpts = new STARNETCreateOptions<T, STARNETDNA> { STARNETHolon = new T(), CustomCreateParams = ghParams };
            }
            else if (string.Equals(subCommand, "nft collection", StringComparison.OrdinalIgnoreCase))
            {
                if (StarnetUiScriptedCreateCli.TryParseNewWeb4NftCollectionCreateArgv(inputArgs, out string newCollName, out string newCollDesc, out string newCollErr))
                {
                    if (!string.IsNullOrEmpty(newCollErr)) { StarCliShellOutput.WriteError(CLIEngine.JsonOutput, 2, newCollErr, "Example: nft collection create \"MyColl\" \"Description\""); return; }
                    scriptedOpts = new STARNETCreateOptions<T, STARNETDNA> { STARNETHolon = new T(), CustomCreateParams = StarnetUiScriptedCreateCli.BuildMinimalWeb4NFTCollectionScriptedParams(newCollName, newCollDesc) };
                }
                else if (!StarnetUiScriptedCreateCli.TryParseWrapOnlyWeb4CollectionCreateArgv(inputArgs, out string wrapCollId, out string collErr))
                {
                    StarCliShellOutput.WriteError(CLIEngine.JsonOutput, 2, collErr ?? "Invalid nft collection create arguments.", "Wrap: nft collection create <web4CollectionGuidOrName>  |  New: nft collection create <name> <description>");
                    return;
                }
                else
                    scriptedOpts = new STARNETCreateOptions<T, STARNETDNA> { STARNETHolon = new T(), CustomCreateParams = StarnetUiScriptedCreateCli.BuildWrapWeb4NFTCollectionScriptedParams(wrapCollId) };
            }
            else if (string.Equals(subCommand, "geo-nft collection", StringComparison.OrdinalIgnoreCase))
            {
                if (StarnetUiScriptedCreateCli.TryParseNewWeb4GeoNftCollectionCreateArgv(inputArgs, out string newGeoCollName, out string newGeoCollDesc, out string newGeoCollErr))
                {
                    if (!string.IsNullOrEmpty(newGeoCollErr)) { StarCliShellOutput.WriteError(CLIEngine.JsonOutput, 2, newGeoCollErr, "Example: geo-nft collection create \"MyColl\" \"Description\""); return; }
                    scriptedOpts = new STARNETCreateOptions<T, STARNETDNA> { STARNETHolon = new T(), CustomCreateParams = StarnetUiScriptedCreateCli.BuildMinimalWeb4GeoNFTCollectionScriptedParams(newGeoCollName, newGeoCollDesc) };
                }
                else if (!StarnetUiScriptedCreateCli.TryParseWrapOnlyWeb4CollectionCreateArgv(inputArgs, out string wrapGeoCollId, out string gcollErr))
                {
                    StarCliShellOutput.WriteError(CLIEngine.JsonOutput, 2, gcollErr ?? "Invalid geo-nft collection create arguments.", "Wrap: geo-nft collection create <web4CollectionGuidOrName>  |  New: geo-nft collection create <name> <description>");
                    return;
                }
                else
                    scriptedOpts = new STARNETCreateOptions<T, STARNETDNA> { STARNETHolon = new T(), CustomCreateParams = StarnetUiScriptedCreateCli.BuildWrapWeb4GeoNFTCollectionScriptedParams(wrapGeoCollId) };
            }
            else if (string.Equals(subCommand, "nft", StringComparison.OrdinalIgnoreCase))
            {
                if (!StarnetUiScriptedCreateCli.TryParseWrapOnlyWeb4CreateArgv(inputArgs, out string wrapNftId, out string wErr))
                { StarCliShellOutput.WriteError(CLIEngine.JsonOutput, 2, wErr ?? "Invalid nft create arguments.", "Example: nft create <web4NftGuid>"); return; }
                scriptedOpts = new STARNETCreateOptions<T, STARNETDNA> { STARNETHolon = new T(), CustomCreateParams = StarnetUiScriptedCreateCli.BuildWrapWeb4NftScriptedParams(wrapNftId) };
            }
            else if (string.Equals(subCommand, "geo-nft", StringComparison.OrdinalIgnoreCase))
            {
                if (!StarnetUiScriptedCreateCli.TryParseWrapOnlyWeb4CreateArgv(inputArgs, out string wrapGeoId, out string wgErr))
                { StarCliShellOutput.WriteError(CLIEngine.JsonOutput, 2, wgErr ?? "Invalid geo-nft create arguments.", "Example: geo-nft create <web4GeoNftGuid>"); return; }
                scriptedOpts = new STARNETCreateOptions<T, STARNETDNA> { STARNETHolon = new T(), CustomCreateParams = StarnetUiScriptedCreateCli.BuildWrapWeb4GeoSpatialNftScriptedParams(wrapGeoId) };
            }
            else if (string.Equals(subCommand, "plugin", StringComparison.OrdinalIgnoreCase))
            {
                if (!StarnetUiScriptedCreateCli.TryParsePluginCreateArgv(inputArgs, out string plugName, out string plugDesc, out string plugParent, out string plugErr))
                { StarCliShellOutput.WriteError(CLIEngine.JsonOutput, 2, plugErr ?? "Invalid plugin create arguments.", "Example: plugin create \"MyPlugin\" \"Description\" [/optional/parent/dir]"); return; }
                scriptedOpts = new STARNETCreateOptions<T, STARNETDNA> { STARNETHolon = new T(), CustomCreateParams = StarnetUiScriptedCreateCli.BuildPluginScriptedCustomCreateParams(plugName, plugDesc, plugParent) };
            }
            else if ((string.Equals(subCommand, "OAPP", StringComparison.OrdinalIgnoreCase) || string.Equals(subCommand, "hApp", StringComparison.OrdinalIgnoreCase))
                     && StarnetUiScriptedCreateCli.TryParseOappLightJsonCreateArgv(inputArgs, out string oappLightJson, out string oappLightErr))
            {
                if (!string.IsNullOrEmpty(oappLightErr)) { StarCliShellOutput.WriteError(CLIEngine.JsonOutput, 2, oappLightErr, "Example: star --non-interactive --json oapp light ./LightRequest.json"); return; }
                if (!File.Exists(oappLightJson)) { StarCliShellOutput.WriteError(CLIEngine.JsonOutput, 2, $"Light JSON file not found: {oappLightJson}", "See Docs/Devs/STAR_CLI_NonInteractive.md (Light JSON schema)."); return; }
                scriptedOpts = new STARNETCreateOptions<T, STARNETDNA> { STARNETHolon = new T(), CustomCreateParams = StarnetUiScriptedCreateCli.BuildOappLightJsonCustomCreateParams(oappLightJson) };
            }
            else if (!StarnetUiScriptedCreateCli.TryParseCreateArgv(inputArgs, subCommand, out string cName, out string cDesc, out string cCat, out string cLibLang, out string cParent, out string cErr))
            {
                StarCliShellOutput.WriteError(CLIEngine.JsonOutput, 2, cErr ?? "Invalid create arguments.", "Example: star --non-interactive oapp template create \"MyTpl\" \"Desc\" Console /optional/parent/dir");
                return;
            }
            else
                scriptedOpts = new STARNETCreateOptions<T, STARNETDNA> { STARNETHolon = new T(), CustomCreateParams = StarnetUiScriptedCreateCli.BuildScriptedCustomCreateParams(cName, cDesc, cCat, cParent, cLibLang) };

            if (scriptedOpts != null && string.Equals(subCommand, "quest", StringComparison.OrdinalIgnoreCase))
            {
                scriptedOpts.CustomCreateParams ??= new Dictionary<string, object>();
                if (StarnetUiScriptedCreateCli.TryParseOptionalQuestObjectivesJsonPath(inputArgs, out string questObjJsonPath))
                    scriptedOpts.CustomCreateParams[StarCliNonInteractiveCreateKeys.QuestObjectivesJsonPath] = questObjJsonPath;
                if (StarnetUiScriptedCreateCli.TryParseOptionalQuestLinkedHandoffArgv(inputArgs, out string qLinked, out string qHandoff))
                {
                    if (!string.IsNullOrWhiteSpace(qLinked)) scriptedOpts.CustomCreateParams[StarCliNonInteractiveCreateKeys.QuestLinkedGeoHotSpotId] = qLinked.Trim();
                    if (!string.IsNullOrWhiteSpace(qHandoff)) scriptedOpts.CustomCreateParams[StarCliNonInteractiveCreateKeys.QuestExternalHandoffUri] = qHandoff.Trim();
                }
            }

            if (createPredicate == null) { CLIEngine.ShowMessage("Coming Soon..."); return; }

            bool lightFromJson = scriptedOpts?.CustomCreateParams != null && scriptedOpts.CustomCreateParams.ContainsKey(StarCliNonInteractiveCreateKeys.LightRequestJsonPath);
            if (lightFromJson && typeof(T) == typeof(OAPP))
            {
                var oappLightOpts = new STARNETCreateOptions<OAPP, STARNETDNA> { STARNETHolon = new OAPP(), CustomCreateParams = scriptedOpts.CustomCreateParams };
                if (scriptedOpts.STARNETDNA != null) oappLightOpts.STARNETDNA = scriptedOpts.STARNETDNA;
                OASISResult<OAPP> lightCreateRes = await STARCLI.OAPPs.CreateAsync(oappLightOpts, null, false, false, providerType);
                if (CLIEngine.JsonOutput)
                    EmitNiJsonForOasisResult(lightCreateRes, $"{subCommand} light", lightCreateRes.Result != null ? new { id = lightCreateRes.Result.STARNETDNA?.Id, name = lightCreateRes.Result.STARNETDNA?.Name } : null);
            }
            else
                await createPredicate(scriptedOpts, null, false, false, providerType);
        }

        private static async Task HandleImportCommandAsync(
            string[] inputArgs, string subCommand, bool web3,
            Func<object, Task> importPredicate)
        {
            if (importPredicate == null) { CLIEngine.ShowErrorMessage("Command not supported or comming soon..."); return; }

            bool niWeb4NftImport = CLIEngine.NonInteractive
                && (string.Equals(subCommand, "nft", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(subCommand, "geo-nft", StringComparison.OrdinalIgnoreCase));

            if (!niWeb4NftImport) { await importPredicate(web3); return; }

            if (string.Equals(subCommand, "nft", StringComparison.OrdinalIgnoreCase))
            {
                if (!StarCliNftStructuredArgv.TryResolveNftNonInteractiveImport(inputArgs, out string nftImpPath, out StarCliNftStructuredArgv.NftNonInteractiveImportKind nftImpKind, out string nftImpErr))
                {
                    StarCliShellOutput.WriteError(CLIEngine.JsonOutput, 2, nftImpErr ?? "Invalid import arguments.", "Example: nft import /path/to/file.json");
                    return;
                }
                switch (nftImpKind)
                {
                    case StarCliNftStructuredArgv.NftNonInteractiveImportKind.Web3MintFromJson:
                        { OASISResult<IWeb4NFT> r = await STARCLI.NFTs.ImportNFTWeb3MintFromJsonFileAsync(nftImpPath); EmitNiJsonForOasisResult(r, "nft import", r.Result != null ? new { web4NftId = r.Result.Id.ToString() } : null); } break;
                    case StarCliNftStructuredArgv.NftNonInteractiveImportKind.Web3TokenFromJson:
                        { OASISResult<IWeb4NFT> r = await STARCLI.NFTs.ImportNFTWeb3TokenFromJsonFileAsync(nftImpPath); EmitNiJsonForOasisResult(r, "nft import", r.Result != null ? new { web4NftId = r.Result.Id.ToString() } : null); } break;
                    default:
                        { OASISResult<IWeb4NFT> r = await STARCLI.NFTs.ImportNFTAsync(nftImpPath); EmitNiJsonForOasisResult(r, "nft import", r.Result != null ? new { web4NftId = r.Result.Id.ToString() } : null); } break;
                }
            }
            else
            {
                if (!StarCliNftStructuredArgv.TryGetImportPath(inputArgs, out string importPath, out string importErr))
                {
                    StarCliShellOutput.WriteError(CLIEngine.JsonOutput, 2, importErr ?? "Invalid import arguments.", "Example: geo-nft import /path/to/file");
                    return;
                }
                OASISResult<IWeb4GeoSpatialNFT> impGeo = await STARCLI.GeoNFTs.ImportGeoNFTAsync(importPath);
                EmitNiJsonForOasisResult(impGeo, "geo-nft import", impGeo.Result != null ? new { web4GeoNftId = impGeo.Result.Id.ToString() } : null);
            }
        }

        private static async Task HandleDeleteCommandAsync(
            string[] inputArgs, string subCommand, string id,
            bool web3, bool web4, bool showDelete,
            Func<string, bool, ProviderType, Task> deletePredicate,
            Func<string, bool?, bool?, ProviderType, Task<OASISResult<bool>>> deleteWeb3Predicate,
            ProviderType providerType)
        {
            if (!showDelete) { CLIEngine.ShowErrorMessage("Command not supported."); return; }

            bool temp = false;
            bool? softDelete = null;
            if (inputArgs.Length > 3 && bool.TryParse(inputArgs[3], out temp)) softDelete = temp;

            if (web3)
            {
                string web3Id = inputArgs.Length > 3 ? inputArgs[3] : "";
                bool burnWeb3NFT = true;
                if (inputArgs.Length > 4 && bool.TryParse(inputArgs[4], out temp)) softDelete = temp;
                if (inputArgs.Length > 5) bool.TryParse(inputArgs[5], out burnWeb3NFT);
                if (deleteWeb3Predicate != null)
                {
                    var deleteResult = await deleteWeb3Predicate(web3Id, softDelete, burnWeb3NFT, providerType);
                    if (deleteResult != null && deleteResult.IsError) CLIEngine.ShowErrorMessage(deleteResult.Message);
                }
                else
                    CLIEngine.ShowMessage("Coming Soon...");
                return;
            }

            if (web4)
            {
                string web4Id = inputArgs.Length > 3 ? inputArgs[3] : "";
                bool? deleteChildWeb4NFTs = null, deleteChildWeb3NFTs = null, burnChildWeb3NFTs = null;
                if (inputArgs.Length > 4 && bool.TryParse(inputArgs[4], out temp)) softDelete = temp;
                if (inputArgs.Length > 5 && bool.TryParse(inputArgs[5], out temp)) deleteChildWeb4NFTs = temp;
                if (inputArgs.Length > 6 && bool.TryParse(inputArgs[6], out temp)) deleteChildWeb3NFTs = temp;
                if (inputArgs.Length > 7 && bool.TryParse(inputArgs[7], out temp)) burnChildWeb3NFTs = temp;
                switch (subCommand.ToUpper())
                {
                    case "NFT":             await STARCLI.NFTs.DeleteWeb4NFTAsync(web4Id, softDelete, deleteChildWeb3NFTs, burnChildWeb3NFTs); break;
                    case "GEONFT":          await STARCLI.GeoNFTs.DeleteWeb4GeoNFTAsync(web4Id, softDelete, deleteChildWeb3NFTs, burnChildWeb3NFTs); break;
                    case "NFTCOLLECTION":   await STARCLI.NFTCollections.DeleteWeb4NFTCollectionAsync(web4Id, softDelete, deleteChildWeb4NFTs, deleteChildWeb3NFTs, burnChildWeb3NFTs); break;
                    case "GEONFTCOLLECTION": await STARCLI.GeoNFTCollections.DeleteWeb4GeoNFTCollectionAsync(web4Id, softDelete, deleteChildWeb4NFTs, deleteChildWeb3NFTs, burnChildWeb3NFTs); break;
                    default: CLIEngine.ShowMessage("Coming Soon..."); break;
                }
                return;
            }

            if (softDelete == null) softDelete = true;
            if (deletePredicate != null)
                await deletePredicate(id, softDelete.Value, providerType);
            else
                CLIEngine.ShowMessage("Coming Soon...");
        }

        private static async Task HandleListCommandAsync(
            string subCommandParam2, bool web3, bool web4,
            bool showForAllAvatars, bool showAllVersions, bool showDetailed,
            Func<bool, bool, ProviderType, Task> listForBeamedInAvatarPredicate,
            Func<bool, bool, int, ProviderType, Task> listAllPredicate,
            Func<ProviderType, Task> listInstalledPredicate,
            Func<ProviderType, Task> listUninstalledPredicate,
            Func<ProviderType, Task> listUnpublishedPredicate,
            Func<ProviderType, Task> listDeactivatedPredicate,
            Func<ProviderType, Task> listAllWeb3Predicate,
            Func<ProviderType, Task> listAllWeb4Predicate,
            Func<ProviderType, Task> listWeb3ForBeamedInAvatarPredicate,
            Func<ProviderType, Task> listWeb4ForBeamedInAvatarPredicate,
            ProviderType providerType)
        {
            switch (subCommandParam2.ToLower())
            {
                case "installed":
                    if (listInstalledPredicate != null) await listInstalledPredicate(providerType); else CLIEngine.ShowMessage("Coming Soon...");
                    break;
                case "uninstalled":
                    if (listUninstalledPredicate != null) await listUninstalledPredicate(providerType); else CLIEngine.ShowMessage("Coming Soon...");
                    break;
                case "unpublished":
                    if (listUnpublishedPredicate != null) await listUnpublishedPredicate(providerType); else CLIEngine.ShowMessage("Coming Soon...");
                    break;
                case "deactivated":
                    if (listDeactivatedPredicate != null) await listDeactivatedPredicate(providerType); else CLIEngine.ShowMessage("Coming Soon...");
                    break;
                default:
                    if (showForAllAvatars)
                    {
                        if (web3) { if (listAllWeb3Predicate != null) await listAllWeb3Predicate(providerType); else CLIEngine.ShowMessage("Coming Soon..."); }
                        else if (web4) { if (listAllWeb4Predicate != null) await listAllWeb4Predicate(providerType); else CLIEngine.ShowMessage("Coming Soon..."); }
                        else { if (listAllPredicate != null) await listAllPredicate(showAllVersions, showDetailed, 0, providerType); else CLIEngine.ShowMessage("Coming Soon..."); }
                    }
                    else
                    {
                        if (web3) { if (listWeb3ForBeamedInAvatarPredicate != null) await listWeb3ForBeamedInAvatarPredicate(providerType); else CLIEngine.ShowMessage("Coming Soon..."); }
                        else if (web4) { if (listWeb4ForBeamedInAvatarPredicate != null) await listWeb4ForBeamedInAvatarPredicate(providerType); else CLIEngine.ShowMessage("Coming Soon..."); }
                        else { if (listForBeamedInAvatarPredicate != null) await listForBeamedInAvatarPredicate(showAllVersions, showDetailed, providerType); else CLIEngine.ShowMessage("Coming Soon..."); }
                    }
                    break;
            }
        }

        private static async Task HandleSearchCommandAsync(
            string[] inputArgs, string subCommand, string subCommandParam2, string subCommandParam3,
            bool web3, bool web4, bool showForAllAvatars, bool showAllVersions,
            Func<string, Guid, bool, bool, ProviderType, int, Task> searchPredicate,
            Func<string, bool, ProviderType, Task> searchWeb3Predicate,
            Func<string, bool, ProviderType, Task> searchWeb4Predicate,
            ProviderType providerType)
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
                    StarCliShellOutput.WriteError(CLIEngine.JsonOutput, 2, "search requires explicit criteria in non-interactive mode.", $"Example: {subCommand} search <criteria> [<maxResults>]  |  Global: --search-limit N");
                    return;
                }
            }

            if (CLIEngine.NonInteractive && string.IsNullOrWhiteSpace(searchCriteria))
            {
                StarCliShellOutput.WriteError(CLIEngine.JsonOutput, 2, "search requires a criteria token (name fragment or id). Example: oapp search MyOAPP 25", $"Entity: {subCommand}. Optional trailing integer limits rows (or use --search-limit N).");
                return;
            }

            int effectiveSearchMax = searchMax > 0 ? searchMax : CLIEngine.MaxHolonSearchResults;
            if (web3) { if (searchWeb3Predicate != null) await searchWeb3Predicate(searchCriteria, showForAllAvatars, providerType); else CLIEngine.ShowMessage("Coming Soon..."); }
            else if (web4) { if (searchWeb4Predicate != null) await searchWeb4Predicate(searchCriteria, showForAllAvatars, providerType); else CLIEngine.ShowMessage("Coming Soon..."); }
            else { if (searchPredicate != null) await searchPredicate(searchCriteria, default, showAllVersions, showForAllAvatars, providerType, effectiveSearchMax); else CLIEngine.ShowMessage("Coming Soon..."); }
        }

        private static void ShowSubCommandMenu(
            string subCommand, string subCommandPlural,
            bool showCreate, bool showUpdate, bool showDelete)
        {
            Console.WriteLine("");
            CLIEngine.ShowMessage($"{subCommand.ToUpper()} SUBCOMMANDS:", ConsoleColor.Green);
            Console.WriteLine("");

            int commandSpace = 22;
            int paramSpace = 23;
            string paramDivider = "  ";
            string web4Param = (subCommand.ToUpper() == "NFT" || subCommand.ToUpper() == "GEO-NFT") ? "[web3] [web4]" : "";

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
                CLIEngine.ShowMessage(string.Concat("    export".PadRight(commandSpace), "{id/name}".PadRight(paramSpace), paramDivider, "Exports a OASIS ", subCommand, " for the given {id} or {name} as a JSON file as well as a WEB3 JSON MetaData file."), ConsoleColor.Green, false);
                CLIEngine.ShowMessage(string.Concat("    burn".PadRight(commandSpace), "{id/name}".PadRight(paramSpace), paramDivider, "Burn's a OASIS ", subCommand, " for the given {id} or {name}"), ConsoleColor.Green, false);
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

            if (subCommand.ToUpper() == "OAPP" || subCommand.ToUpper() == "OAPPTEMPLATE" || subCommand.ToUpper() == "HAPP")
            {
                string publishParam = subCommand.ToUpper() == "HAPP" ? "{hAppPath} [publishDotNet]" : "{oappPath} [publishDotNet]";
                CLIEngine.ShowMessage(string.Concat("    publish".PadRight(commandSpace), publishParam.PadRight(paramSpace), paramDivider, "Publish a ", subCommand, " for the given path."), ConsoleColor.Green, false);
            }
            else
                CLIEngine.ShowMessage(string.Concat("    publish".PadRight(commandSpace), "{id/name}".PadRight(paramSpace), paramDivider, "Publish a ", subCommand, " to STARNET for the given {id} or {name}."), ConsoleColor.Green, false);

            CLIEngine.ShowMessage(string.Concat("    unpublish".PadRight(commandSpace), "{id/name}".PadRight(paramSpace), paramDivider, "Unpublish a ", subCommand, " from STARNET for the given {id} or {name}."), ConsoleColor.Green, false);
            CLIEngine.ShowMessage(string.Concat("    republish".PadRight(commandSpace), "{id/name}".PadRight(paramSpace), paramDivider, "Republish a ", subCommand, " to STARNET for the given {id} or {name}."), ConsoleColor.Green, false);
            CLIEngine.ShowMessage(string.Concat("    activate".PadRight(commandSpace), "{id/name}".PadRight(paramSpace), paramDivider, "Activate (show) a ", subCommand, " on the STARNET for the given {id} or {name}."), ConsoleColor.Green, false);
            CLIEngine.ShowMessage(string.Concat("    deactivate".PadRight(commandSpace), "{id/name}".PadRight(paramSpace), paramDivider, "Deactivate (hide) a ", subCommand, " on the STARNET for the given {id} or {name}."), ConsoleColor.Green, false);
            CLIEngine.ShowMessage(string.Concat("    list".PadRight(commandSpace), string.Concat("", web4Param).PadRight(paramSpace), paramDivider, "List all ", subCommandPlural, " that have been created."), ConsoleColor.Green, false);
            CLIEngine.ShowMessage(string.Concat("    list installed".PadRight(commandSpace), "".PadRight(paramSpace), paramDivider, "List all ", subCommandPlural, " installed for the currently beamed in avatar."), ConsoleColor.Green, false);
            CLIEngine.ShowMessage(string.Concat("    list uninstalled".PadRight(commandSpace), "".PadRight(paramSpace), paramDivider, "List all ", subCommandPlural, " uninstalled for the currently beamed in avatar (allows reinstalling)."), ConsoleColor.Green, false);
            CLIEngine.ShowMessage(string.Concat("    list unpublished".PadRight(commandSpace), "".PadRight(paramSpace), paramDivider, "List all ", subCommandPlural, " unpublished for the currently beamed in avatar (allows republishing)."), ConsoleColor.Green, false);
            CLIEngine.ShowMessage(string.Concat("    list deactivated".PadRight(commandSpace), "".PadRight(paramSpace), paramDivider, "List all ", subCommandPlural, " deactivated for the currently beamed in avatar (allows reactivating)."), ConsoleColor.Green, false);
            CLIEngine.ShowMessage(string.Concat("    show".PadRight(commandSpace), string.Concat("{id/name} ", web4Param).PadRight(paramSpace), paramDivider, "Shows the ", subCommandPlural, " for the given {id} or {name}."), ConsoleColor.Green, false);
            CLIEngine.ShowMessage(string.Concat("    search".PadRight(commandSpace), string.Concat("{id/name} ", web4Param).PadRight(paramSpace), paramDivider, "Searches the ", subCommandPlural, " for the given search critera."), ConsoleColor.Green, false);

            if (subCommand.ToUpper() == "OAPP") CLIEngine.ShowMessage(string.Concat("    template".PadRight(commandSpace), "".PadRight(paramSpace), paramDivider, "Shows the OAPP Template Subcommand menu."), ConsoleColor.Green, false);
            if (subCommand.ToUpper() == "CELESTIAL BODY") CLIEngine.ShowMessage(string.Concat("    metadata".PadRight(commandSpace), "".PadRight(paramSpace), paramDivider, "Shows the CelestialBody MetaData DNA Subcommand menu."), ConsoleColor.Green, false);
            if (subCommand.ToUpper() == "ZOME") CLIEngine.ShowMessage(string.Concat("    metadata".PadRight(commandSpace), "".PadRight(paramSpace), paramDivider, "Shows the Zome MetaData DNA Subcommand menu."), ConsoleColor.Green, false);
            if (subCommand.ToUpper() == "HOLON") CLIEngine.ShowMessage(string.Concat("    metadata".PadRight(commandSpace), "".PadRight(paramSpace), paramDivider, "Shows the Holon MetaData DNA Subcommand menu."), ConsoleColor.Green, false);

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
            if (subCommand.ToUpper() == "OAPP") CLIEngine.ShowMessage($"For the publish command, if the flag [publishDotNet] is specified it will first do a dotnet publish before publishing to STARNET.", ConsoleColor.Green);
            CLIEngine.ShowMessage($"For the list & search commands, if [allVersions] is omitted it will list the current version, otherwise it will list all versions. If [forAllAvatars] is omitted it will list only your {subCommandPlural}'s otherwise it will list all published {subCommandPlural}'s as well as yours.", ConsoleColor.Green);
            CLIEngine.ShowMessage($"For the list & show commands, if [detailed] is included it will list detailed stats also such as all dependenices installed.", ConsoleColor.Green);
            if (subCommand.ToUpper() == "GEO-NFT") CLIEngine.ShowMessage($"For the update, delete, list, show or search command, if [web4] is included it will update/delete/list/show/search WEB4 OASIS Geo-NFT's, otherwise it will update/delete/list/show/search WEB5 STAR Geo-NFT's.", ConsoleColor.Green);
            if (subCommand.ToUpper() == "NFT")
            {
                CLIEngine.ShowMessage($"For the import command if [web3] is included it will import an existing WEB3 NFT(JSON MetaData or NFT Token Address) and wrap it in a new WEB4 OASIS NFT.", ConsoleColor.Green);
                CLIEngine.ShowMessage($"For the update, delete, list, show or search command, if [web3] is included it will update/delete/list/show/search WEB3 NFT's, if [web4] is included it will update/delete/list/show/search WEB4 OASIS NFT's, otherwise it will update/delete/list/show/search WEB5 STAR NFT's.", ConsoleColor.Green);
            }
            if (subCommand.ToUpper() == "GEO-NFT COLLECTION") CLIEngine.ShowMessage($"For the create, update, delete, list, show or search command, if [web4] is included it will create/update/delete/list/show/search WEB4 OASIS Geo-NFT Collection's, otherwise it will create/update/delete/list/show/search WEB5 STAR Geo-NFT Collection's.", ConsoleColor.Green);
            if (subCommand.ToUpper() == "NFT COLLECTION") CLIEngine.ShowMessage($"For the create, update, delete, list, show or search command, if [web4] is included it will create/update/delete/list/show/search WEB4 OASIS NFT Collection's, otherwise it will create/update/delete/list/show/search WEB5 STAR NFT Collection's.", ConsoleColor.Green);
            CLIEngine.ShowMessage("More Coming Soon...", ConsoleColor.Green);
        }
    }
}
