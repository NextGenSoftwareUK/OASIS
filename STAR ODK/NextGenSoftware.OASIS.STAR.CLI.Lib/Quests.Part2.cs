using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Globalization;
using System.Text.Json;
using System.Threading.Tasks;
using NextGenSoftware.CLI.Engine;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.Core.Objects;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.STAR.DNA;

namespace NextGenSoftware.OASIS.STAR.CLI.Lib
{
    public partial class Quests : STARNETUIBase<Quest, DownloadedQuest, InstalledQuest, STARNETDNA>
    {

        private static OASISResult<Quest> TryApplyQuestObjectivesFromScriptedParams(ISTARNETCreateOptions<Quest, STARNETDNA> createOptions)
        {
            OASISResult<Quest> r = new OASISResult<Quest>();
            if (createOptions?.CustomCreateParams == null)
                return r;

            if (!createOptions.CustomCreateParams.TryGetValue(StarCliNonInteractiveCreateKeys.QuestObjectivesJsonPath, out object pathObj) || pathObj == null)
                return r;

            string path = pathObj.ToString()?.Trim();
            if (string.IsNullOrWhiteSpace(path))
                return r;

            if (!File.Exists(path))
            {
                OASISErrorHandling.HandleError(ref r, $"Quest objectives JSON file not found: {path}");
                return r;
            }

            try
            {
                string json = File.ReadAllText(path);
                OASISResult<QuestObjectivesJsonParseResult> parsed = ParseQuestObjectivesJsonFile(json, allowObjectivesOmitted: false);
                if (parsed.IsError)
                {
                    OASISErrorHandling.HandleError(ref r, parsed.Message);
                    return r;
                }

                createOptions.STARNETHolon.Objectives = parsed.Result.Objectives;
                if (parsed.Result.QuestLinkedGeoHotSpotId.HasValue)
                    createOptions.STARNETHolon.LinkedGeoHotSpotId = parsed.Result.QuestLinkedGeoHotSpotId;
                if (!string.IsNullOrWhiteSpace(parsed.Result.QuestExternalHandoffUri))
                    createOptions.STARNETHolon.ExternalHandoffUri = parsed.Result.QuestExternalHandoffUri.Trim();
                return r;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref r, $"Failed to read quest objectives JSON: {ex.Message}");
                return r;
            }
        }

        private sealed class QuestObjectiveCliJson
        {
            public Guid? Id { get; set; }
            public int? Order { get; set; }
            public string Title { get; set; }
            public string Description { get; set; }
            public Guid? LinkedGeoHotSpotId { get; set; }
            public string ExternalHandoffUri { get; set; }
        }

        /// <summary>Result of parsing <c>--objectives-json</c> file: objectives plus optional quest-level GeoHotSpot / handoff (wrapper object).</summary>
        private sealed class QuestObjectivesJsonParseResult
        {
            /// <summary>Null when JSON object omitted <c>objectives</c> (update-only: do not replace existing objectives).</summary>
            public List<Objective> Objectives { get; set; }
            public Guid? QuestLinkedGeoHotSpotId { get; set; }
            public string QuestExternalHandoffUri { get; set; }
        }

        private static void TryApplyQuestHandoffFromScriptedKeys(ISTARNETCreateOptions<Quest, STARNETDNA> createOptions)
        {
            if (createOptions?.CustomCreateParams == null || createOptions.STARNETHolon == null)
                return;

            Dictionary<string, object> p = createOptions.CustomCreateParams;
            if (p.TryGetValue(StarCliNonInteractiveCreateKeys.QuestLinkedGeoHotSpotId, out object lg) && lg != null)
            {
                string s = lg.ToString()?.Trim();
                if (!string.IsNullOrEmpty(s) && Guid.TryParse(s, out Guid g))
                    createOptions.STARNETHolon.LinkedGeoHotSpotId = g;
            }

            if (p.TryGetValue(StarCliNonInteractiveCreateKeys.QuestExternalHandoffUri, out object ho) && ho != null)
            {
                string s = ho.ToString()?.Trim();
                if (!string.IsNullOrEmpty(s))
                    createOptions.STARNETHolon.ExternalHandoffUri = s;
            }
        }

        private static void InteractiveAppendQuestLevelGeoAndHandoffForCreate(Quest quest) =>
            PromptQuestLevelGeoAndHandoffFields(quest, withIntroConfirmation: true);

        /// <param name="withIntroConfirmation">When true (quest create wizard), ask whether to set fields at all; when false (quest update), caller already confirmed.</param>
        private static void PromptQuestLevelGeoAndHandoffFields(Quest quest, bool withIntroConfirmation)
        {
            if (CLIEngine.NonInteractive || quest == null)
                return;

            Console.WriteLine("");
            if (withIntroConfirmation && !CLIEngine.GetConfirmation("Set optional quest-level linked GeoHotSpot or external handoff (OPortal, web, messaging)?"))
                return;

            string gh = CLIEngine.GetValidInput("Linked GeoHotSpot id (GUID), or leave blank to skip:")?.Trim();
            if (!string.IsNullOrEmpty(gh) && gh != "exit")
            {
                if (Guid.TryParse(gh, out Guid g))
                    quest.LinkedGeoHotSpotId = g;
                else
                    CLIEngine.ShowWarningMessage("Invalid GUID; skipped quest-level GeoHotSpot link.");
            }

            string uri = CLIEngine.GetValidInput("External handoff URI (optional, blank to skip):")?.Trim();
            if (!string.IsNullOrEmpty(uri) && uri != "exit")
                quest.ExternalHandoffUri = uri;
        }

        private static OASISResult<QuestObjectivesJsonParseResult> ParseQuestObjectivesJsonFile(string json, bool allowObjectivesOmitted = false)
        {
            OASISResult<QuestObjectivesJsonParseResult> r = new OASISResult<QuestObjectivesJsonParseResult>();
            try
            {
                var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                List<QuestObjectiveCliJson> dtos;
                Guid? questLinked = null;
                string questHandoff = null;
                bool objectivesSpecified = true;

                using (JsonDocument doc = JsonDocument.Parse(json))
                {
                    if (doc.RootElement.ValueKind == JsonValueKind.Array)
                    {
                        dtos = JsonSerializer.Deserialize<List<QuestObjectiveCliJson>>(json, opts);
                    }
                    else if (doc.RootElement.ValueKind == JsonValueKind.Object)
                    {
                        JsonElement root = doc.RootElement;
                        if (root.TryGetProperty("linkedGeoHotSpotId", out JsonElement lgEl) || root.TryGetProperty("LinkedGeoHotSpotId", out lgEl))
                        {
                            if (lgEl.ValueKind == JsonValueKind.String)
                            {
                                string gs = lgEl.GetString()?.Trim();
                                if (!string.IsNullOrEmpty(gs))
                                {
                                    if (Guid.TryParse(gs, out Guid g))
                                        questLinked = g;
                                    else
                                    {
                                        OASISErrorHandling.HandleError(ref r, "Invalid linkedGeoHotSpotId in JSON (expected a GUID).");
                                        return r;
                                    }
                                }
                            }
                        }

                        if (root.TryGetProperty("externalHandoffUri", out JsonElement hoEl) || root.TryGetProperty("ExternalHandoffUri", out hoEl))
                        {
                            if (hoEl.ValueKind == JsonValueKind.String)
                                questHandoff = hoEl.GetString()?.Trim();
                        }

                        if (root.TryGetProperty("objectives", out JsonElement arr))
                        {
                            if (arr.ValueKind != JsonValueKind.Array)
                            {
                                OASISErrorHandling.HandleError(ref r, "Property \"objectives\" must be a JSON array.");
                                return r;
                            }

                            dtos = JsonSerializer.Deserialize<List<QuestObjectiveCliJson>>(arr.GetRawText(), opts);
                        }
                        else
                        {
                            objectivesSpecified = false;
                            dtos = new List<QuestObjectiveCliJson>();
                        }
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref r, "Quest objectives JSON must be a JSON array or an object with optional linkedGeoHotSpotId, externalHandoffUri, and objectives array.");
                        return r;
                    }
                }

                if (!objectivesSpecified && !allowObjectivesOmitted)
                {
                    OASISErrorHandling.HandleError(ref r, "Quest objectives JSON object must include an \"objectives\" array.");
                    return r;
                }

                if (dtos == null)
                    dtos = new List<QuestObjectiveCliJson>();

                List<Objective> list = null;
                if (objectivesSpecified)
                {
                    list = new List<Objective>();
                    for (int j = 0; j < dtos.Count; j++)
                    {
                        QuestObjectiveCliJson d = dtos[j];
                        if (d == null)
                            continue;

                        if (string.IsNullOrWhiteSpace(d.Title) || string.IsNullOrWhiteSpace(d.Description))
                        {
                            OASISErrorHandling.HandleError(ref r, "Each objective requires non-empty title and description.");
                            return r;
                        }

                        list.Add(new Objective
                        {
                            Id = d.Id ?? Guid.NewGuid(),
                            Order = d.Order ?? j,
                            Title = d.Title.Trim(),
                            Description = d.Description.Trim(),
                            LinkedGeoHotSpotId = d.LinkedGeoHotSpotId,
                            ExternalHandoffUri = d.ExternalHandoffUri?.Trim() ?? string.Empty
                        });
                    }
                }

                r.Result = new QuestObjectivesJsonParseResult
                {
                    Objectives = list,
                    QuestLinkedGeoHotSpotId = questLinked,
                    QuestExternalHandoffUri = questHandoff
                };
                return r;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref r, $"Invalid quest objectives JSON: {ex.Message}");
                return r;
            }
        }

        //public async Task<OASISResult<IQuest>> AddGeoNFTToQuestAsync(string idOrNameOfQuest, string idOrNameOfGeoNFT, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<IQuest> result = new OASISResult<IQuest>();
        //    OASISResult <Quest> parentResult = await FindAsync("use", idOrNameOfQuest, true, providerType: providerType);

        //    if (parentResult != null && !parentResult.IsError && parentResult.Result != null)
        //    {
        //        OASISResult<InstalledGeoNFT> installedGeoNFT = await STARCLI.GeoNFTs.FindAndInstallIfNotInstalledAsync("use", idOrNameOfGeoNFT, providerType: providerType);

        //        if (installedGeoNFT != null && installedGeoNFT.Result != null && !installedGeoNFT.IsError)
        //        {
        //            OASISResult<IQuest> addResult = ((QuestManager)STARNETManager).AddGeoNFTToQuest(STAR.BeamedInAvatar.Id, parentResult.Result.Id, installedGeoNFT.Result.Id, providerType);

        //            if (addResult != null && addResult.Result != null && !addResult.IsError)
        //                CLIEngine.ShowSuccessMessage($"Successfully added GeoNFT {installedGeoNFT.Result.Name} to Quest {parentResult.Result.Name}.");
        //            else
        //                OASISErrorHandling.HandleError(ref result, $"Error occured adding GeoNFT {installedGeoNFT.Result.Name} to Quest {parentResult.Result.Name}. Reason: {addResult.Message}");
        //        }
        //        else
        //            OASISErrorHandling.HandleError(ref result, $"Error occured finding and installing GeoNFT {idOrNameOfGeoNFT}. Reason: {installedGeoNFT.Message}");
        //    }
        //    else
        //        OASISErrorHandling.HandleError(ref result, $"Error occured finding Quest {idOrNameOfQuest}. Reason: {parentResult.Message}");

        //    return result;
        //}

        //public async Task<OASISResult<IQuest>> AddGeoNFTToQuestAsync(string idOrNameOfQuest, string idOrNameOfGeoNFT, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<IQuest> result = new OASISResult<IQuest>();
        //    OASISResult<Quest> parentResult = await FindAsync("use", idOrNameOfQuest, true, providerType: providerType);

        //    if (parentResult != null && !parentResult.IsError && parentResult.Result != null)
        //    {
        //        OASISResult<InstalledGeoNFT> installedGeoNFT = await STARCLI.GeoNFTs.FindAndInstallIfNotInstalledAsync("use", idOrNameOfGeoNFT, providerType: providerType);

        //        if (installedGeoNFT != null && installedGeoNFT.Result != null && !installedGeoNFT.IsError)
        //        {
        //            OASISResult<IQuest> addResult = ((QuestManager)STARNETManager).AddGeoNFTToQuest(STAR.BeamedInAvatar.Id, parentResult.Result.Id, installedGeoNFT.Result.Id, providerType);

        //            if (addResult != null && addResult.Result != null && !addResult.IsError)
        //                CLIEngine.ShowSuccessMessage($"Successfully added GeoNFT {installedGeoNFT.Result.Name} to Quest {parentResult.Result.Name}.");
        //            else
        //                OASISErrorHandling.HandleError(ref result, $"Error occured adding GeoNFT {installedGeoNFT.Result.Name} to Quest {parentResult.Result.Name}. Reason: {addResult.Message}");
        //        }
        //        else
        //            OASISErrorHandling.HandleError(ref result, $"Error occured finding and installing GeoNFT {idOrNameOfGeoNFT}. Reason: {installedGeoNFT.Message}");
        //    }
        //    else
        //        OASISErrorHandling.HandleError(ref result, $"Error occured finding Quest {idOrNameOfQuest}. Reason: {parentResult.Message}");

        //    return result;
        //}
        //public async Task<OASISResult<IQuest>> RemoveGeoNFTFromQuestAsync(string idOrNameOfQuest, string idOrNameOfGeoNFT, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<IQuest> result = new OASISResult<IQuest>();
        //    OASISResult<Quest> parentResult = await FindAsync("use", idOrNameOfQuest, true, providerType: providerType);

        //    if (parentResult != null && !parentResult.IsError && parentResult.Result != null)
        //    {
        //        //OASISResult<InstalledGeoNFT> installedGeoNFT = await STARCLI.GeoNFTs.FindAndInstallIfNotInstalledAsync("use", idOrNameOfGeoNFT, providerType: providerType);

        //        //if (installedGeoNFT != null && installedGeoNFT.Result != null && !installedGeoNFT.IsError)
        //        //{
        //        //    OASISResult<IQuest> addResult = ((QuestManager)STARNETManager).AddGeoNFTToQuest(STAR.BeamedInAvatar.Id, parentResult.Result.Id, installedGeoNFT.Result.Id, providerType);

        //        //    if (addResult != null && addResult.Result != null && !addResult.IsError)
        //        //        CLIEngine.ShowSuccessMessage($"Successfully removed GeoNFT {installedGeoNFT.Result.Name} from Quest {parentResult.Result.Name}.");
        //        //    else
        //        //        OASISErrorHandling.HandleError(ref result, $"Error occured removing GeoNFT {installedGeoNFT.Result.Name} from Quest {parentResult.Result.Name}. Reason: {addResult.Message}");
        //        //}
        //        //else
        //        //    OASISErrorHandling.HandleError(ref result, $"Error occured finding and installing GeoNFT {idOrNameOfGeoNFT}. Reason: {installedGeoNFT.Message}");
        //    }
        //    else
        //        OASISErrorHandling.HandleError(ref result, $"Error occured finding Quest {idOrNameOfQuest}. Reason: {parentResult.Message}");

        //    return result;
        //}

        /// <summary>
        /// Lists **avatar quest state** (same data as ODOOM/OQuake / STAR API client): objectives, progress lines, and quest type — not STARNET published .oquest metadata.
        /// </summary>
        public override async Task ListAllCreatedByBeamedInAvatarAsync(bool showAllVersions = false, bool showDetailedInfo = false, ProviderType providerType = ProviderType.Default)
        {
            if (STAR.BeamedInAvatar == null)
            {
                if (CLIEngine.JsonOutput)
                {
                    Environment.ExitCode = 2;
                    Console.Out.WriteLine(JsonSerializer.Serialize(new { success = false, exitCode = 2, error = "No Avatar Is Beamed In. Please Beam In First!", detail = (string?)null }, QuestListJsonOptions));
                    return;
                }

                CLIEngine.ShowErrorMessage("No Avatar Is Beamed In. Please Beam In First!");
                return;
            }

            Console.WriteLine("");
            if (!CLIEngine.JsonOutput)
                CLIEngine.ShowWorkingMessage("Loading quests for your avatar (OASIS quest state)...");

            CLIEngine.SupressConsoleLogging = true;
            OASISResult<IEnumerable<IQuest>> result = await STAR.STARAPI.Quests.LoadAllQuestsForAvatarAsync(STAR.BeamedInAvatar.Id, showAllVersions, version: 0, providerType);
            CLIEngine.SupressConsoleLogging = false;

            if (result == null || result.IsError)
            {
                string msg = result?.Message ?? "Failed to load quests.";
                if (CLIEngine.JsonOutput)
                {
                    Environment.ExitCode = 1;
                    Console.Out.WriteLine(JsonSerializer.Serialize(new { success = false, exitCode = 1, error = msg, detail = (string?)null }, QuestListJsonOptions));
                    return;
                }

                CLIEngine.ShowErrorMessage($"Error loading quests: {msg}");
                return;
            }

            List<IQuest> list = result.Result?.Where(q => q != null).OrderBy(q => q.Order).ToList() ?? new List<IQuest>();

            if (CLIEngine.JsonOutput)
            {
                var payload = list.Select(q => BuildQuestListJsonObject(q, showDetailedInfo)).ToList();
                Console.Out.WriteLine(JsonSerializer.Serialize(new
                {
                    success = true,
                    message = result.Message,
                    data = new { count = payload.Count, quests = payload }
                }, QuestListJsonOptions));
                return;
            }

            if (list.Count == 0)
            {
                CLIEngine.ShowWarningMessage("No quests found for this avatar.");
                return;
            }

            CLIEngine.ShowMessage($"{list.Count} quest(s) for {STAR.BeamedInAvatar.Username}:", ConsoleColor.Green);
            Console.WriteLine("");
            foreach (IQuest iq in list)
            {
                WriteRuntimeQuestToConsole(iq, !showDetailedInfo);

                // In "list detailed", include full STARNET holon + STARNET DNA info (mirrors the base list/show output).
                if (showDetailedInfo && iq != null)
                {
                    //Console.WriteLine("");
                    await ShowAsync(iq, showHeader: false, showFooter: true, showNumbers: false, number: 0, showDetailedInfo: true);
                    Console.WriteLine("");
                }
            }

            //CLIEngine.ShowDivider();
        }
    }
}
