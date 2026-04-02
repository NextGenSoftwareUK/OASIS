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
    //public class Quests : STARNETUIBase<Quest, DownloadedQuest, InstalledQuest, QuestDNA>
    public class Quests : STARNETUIBase<Quest, DownloadedQuest, InstalledQuest, STARNETDNA>
    {
        private static readonly JsonSerializerOptions QuestListJsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        public Quests(Guid avatarId, STARDNA STARDNA) : base(new API.ONODE.Core.Managers.QuestManager(avatarId, STARDNA),
            "Welcome to the Quest Wizard", new List<string> 
            {
                "This wizard will allow you create a Quest which contains Sub-Quest's. Larger Quest's can be broken into Chapter's.",
                "Quest's can contain both Quest's and Chapter's. Quest's can also have sub-quests.",
                "Quest's contain GeoNFT's & GeoHotSpot's which can reward you various InventoryItem's for the avatar who completes the quest, triggers the GeoHotSpot or collects the GeoNFT.",
                "Quest's can optionally be linked to OAPP's.",
                "The wizard will create an empty folder with a QuestDNA.json file in it. You then simply place any files/folders you need for the assets (optional) for the quest into this folder.",
                "Finally you run the sub-command 'quest publish' to convert the folder containing the quest (can contain any number of files and sub-folders) into a OASIS Quest file (.oquest) as well as optionally upload to STARNET.",
                "You can then share the .oquest file with others across any platform or OS, who can then install the Quest from the file using the sub-command 'quest install'.",
                "You can also optionally choose to upload the .oquest file to the STARNET store so others can search, download and install the quest."
            },
            STAR.STARDNA.DefaultQuestsSourcePath, "DefaultQuestsSourcePath",
            STAR.STARDNA.DefaultQuestsPublishedPath, "DefaultQuestsPublishedPath",
            STAR.STARDNA.DefaultQuestsDownloadedPath, "DefaultQuestsDownloadedPath",
            STAR.STARDNA.DefaultQuestsInstalledPath, "DefaultQuestsInstalledPath")
        { }

        public override async Task UpdateAsync(string idOrName = "", object editParams = null, bool editLaunchTarget = true, ProviderType providerType = ProviderType.Default)
        {
            if (CLIEngine.NonInteractive && editParams is QuestCliEditParams qep && !string.IsNullOrWhiteSpace(qep.ObjectivesJsonPath))
            {
                await UpdateQuestObjectivesFromJsonFileAsync(idOrName, qep.ObjectivesJsonPath, providerType);
                return;
            }

            await base.UpdateAsync(idOrName, editParams, editLaunchTarget, providerType);
        }

        public override async Task<OASISResult<Quest>> CreateAsync(ISTARNETCreateOptions<Quest, STARNETDNA> createOptions = null, object holonSubType = null, bool showHeaderAndInro = true, bool addDependencies = true, ProviderType providerType = ProviderType.Default)
        {
            if (TryReadScriptedNonInteractiveCreate(createOptions, out _, out _, out _, out _))
            {
                if (createOptions == null)
                    createOptions = new STARNETCreateOptions<Quest, STARNETDNA>() { STARNETHolon = new Quest() };
                else if (createOptions.STARNETHolon == null)
                    createOptions.STARNETHolon = new Quest();

                OASISResult<Quest> objErr = TryApplyQuestObjectivesFromScriptedParams(createOptions);
                if (objErr != null && objErr.IsError)
                    return objErr;

                return await base.CreateAsync(createOptions, holonSubType, showHeaderAndInro, addDependencies, providerType);
            }

            OASISResult<Quest> result = new OASISResult<Quest>();
            Mission parentMission = null;
            Quest parentQuest = null;
            //InstalledQuest parentQuest = null;
            int order = 0;

            ShowHeader();

            if (CLIEngine.GetConfirmation("Does this quest belong to a Mission?"))
            {
                Console.WriteLine("");
                OASISResult<InstalledMission> missionResult = await STARCLI.Missions.FindAndInstallIfNotInstalledAsync("use for the parent");

                if (missionResult != null && missionResult.Result != null && !missionResult.IsError)
                {
                    OASISResult<Mission> loadResult = await STAR.STARAPI.Missions.LoadAsync(STAR.BeamedInAvatar.Id, missionResult.Result.STARNETDNA.Id, missionResult.Result.STARNETDNA.VersionSequence, providerType: providerType);

                    if (loadResult != null && loadResult.Result != null && !loadResult.IsError)
                        parentMission = loadResult.Result;
                }
            }
            else if (CLIEngine.GetConfirmation("\n Does this quest belong to another quest?"))
            {
                Console.WriteLine("");
                OASISResult<InstalledQuest> questResult = await STARCLI.Quests.FindAndInstallIfNotInstalledAsync("use for the parent");

                if (questResult != null && questResult.Result != null && !questResult.IsError)
                {
                    //parentQuest = questResult.Result;
                    OASISResult<Quest> loadResult = await STAR.STARAPI.Quests.LoadAsync(STAR.BeamedInAvatar.Id, questResult.Result.STARNETDNA.Id, questResult.Result.STARNETDNA.VersionSequence, providerType: providerType);

                    if (loadResult != null && loadResult.Result != null && !loadResult.IsError)
                        parentQuest = loadResult.Result;
                }
            }

            if (parentMission != null)
                order = parentMission.Quests.Count() + 1;

            if (parentQuest != null)
                order = parentQuest.Quests.Count() + 1;

            if (createOptions == null)
                createOptions = new STARNETCreateOptions<Quest, STARNETDNA>() { STARNETHolon = new Quest() };

            if (parentMission != null)
                createOptions.STARNETHolon.ParentMissionId = parentMission.Id;
            
            if (parentQuest != null)
                createOptions.STARNETHolon.ParentQuestId = parentQuest.Id;

            createOptions.STARNETHolon.Order = order;

            InteractiveAppendObjectivesBeforeCreate(createOptions.STARNETHolon);

            result = await base.CreateAsync(createOptions, holonSubType, false, false, providerType: providerType);

            if (result != null)
            {
                if (result.Result != null && result.Result != null && !result.IsError)
                {
                    if (parentMission != null)
                    {
                        CLIEngine.ShowMessage($"You said this quest is a sub-quest of mission {parentMission.Name} so it now needs to be added as a dependency to the parent mission. In order to do so this quest first needs to be installed...");
                        //OASISResult<Quest> addResult = await AddDependencyAsync(parentSTARNETDNA: parentMission.STARNETDNA, dependencyType: "Mission", idOrNameOfDependency: result.Result.Id.ToString(), providerType: providerType);
                        OASISResult<Mission> addResult = await STARCLI.Missions.AddDependencyAsync(parentSTARNETDNA: parentMission.STARNETDNA, dependencyType: "Quest", idOrNameOfDependency: result.Result.Id.ToString(), providerType: providerType);
                    }

                    if (parentQuest != null)
                    {
                        CLIEngine.ShowMessage($"You said this quest is a sub-quest of quest {parentQuest.Name} so it now needs to be added as a dependency to the parent quest. In order to do so this quest first needs to be installed...");
                        OASISResult<Quest> addResult = await AddDependencyAsync(parentSTARNETDNA: parentQuest.STARNETDNA, dependencyType: "Quest", idOrNameOfDependency: result.Result.Id.ToString(), providerType: providerType);
                    }

                    if (CLIEngine.GetConfirmation($"Do you want to add any GeoHotSpot's to the '{result.Result.Name}' quest now?"))
                    {
                        do
                        {
                            Guid geoHotSpotId = Guid.Empty;
                            Console.WriteLine("");
                            if (!CLIEngine.GetConfirmation("Does the GeoHotSpot already exist?"))
                            {
                                Console.WriteLine("");
                                OASISResult<GeoHotSpot> geoHotSpotResult = await STARCLI.GeoHotSpots.CreateAsync(null, providerType: providerType);
                                
                                if (geoHotSpotResult != null && geoHotSpotResult.Result != null && !geoHotSpotResult.IsError)
                                    geoHotSpotId = geoHotSpotResult.Result.Id;
                            }
                            //else
                            //{
                            //    geoHotSpotId = CLIEngine.GetValidInputForGuid("What is the ")
                            //}

                            Console.WriteLine("");
                            OASISResult<Quest> addResult = await AddDependencyAsync(parentSTARNETDNA: result.Result.STARNETDNA, dependencyType: "GeoHotSpot", idOrNameOfDependency: geoHotSpotId.ToString(), providerType: providerType);
                        }
                        while (CLIEngine.GetConfirmation("Do you wish to add another GeoHotSpot?"));  
                    }
                    //else
                    //    Console.WriteLine("");

                    Console.WriteLine("");
                    if (CLIEngine.GetConfirmation($"Do you want to add any GeoNFT's to the '{result.Result.Name}' quest?"))
                    {
                        do
                        {
                            Guid geoNFTId = Guid.Empty;
                            Console.WriteLine("");
                            if (!CLIEngine.GetConfirmation("Does the GeoNFT already exist?"))
                            {
                                Console.WriteLine("");
                                OASISResult<STARGeoNFT> geoHotSpotResult = await STARCLI.GeoNFTs.CreateAsync(null, providerType: providerType);

                                if (geoHotSpotResult != null && geoHotSpotResult.Result != null && !geoHotSpotResult.IsError)
                                    geoNFTId = geoHotSpotResult.Result.Id;
                            }

                            Console.WriteLine("");
                            OASISResult<Quest> addResult = await AddDependencyAsync(parentSTARNETDNA: result.Result.STARNETDNA, dependencyType: "GeoNFT", idOrNameOfDependency: geoNFTId.ToString(), providerType: providerType);
                        }
                        while (CLIEngine.GetConfirmation("Do you wish to add another GeoNFT?"));
                        //Console.WriteLine("");
                    }
                    //else
                    //    Console.WriteLine("");

                    Console.WriteLine("");
                    if (CLIEngine.GetConfirmation($"Do you want to add any sub-quest's to the '{result.Result.Name}' quest?"))
                    {
                        do
                        {
                            Guid questId = Guid.Empty;
                            Console.WriteLine("");
                            if (!CLIEngine.GetConfirmation("Does the sub-quest already exist?"))
                            {
                                Console.WriteLine("");
                                OASISResult<Quest> questResult = await STARCLI.Quests.CreateAsync(null, providerType: providerType);

                                if (questResult != null && questResult.Result != null && !questResult.IsError)
                                    questId = questResult.Result.Id;
                            }

                            Console.WriteLine("");
                            OASISResult<Quest> addResult = await AddDependencyAsync(parentSTARNETDNA: result.Result.STARNETDNA, dependencyType: "Quest", idOrNameOfDependency: questId.ToString(), providerType: providerType);
                        }
                        while (CLIEngine.GetConfirmation("Do you wish to add another sub-quest?"));
                    }
                    //else
                    //    Console.WriteLine("");

                    Console.WriteLine("");
                    await AddDependenciesAsync(result.Result.STARNETDNA, providerType);
                }
            }
            
            return result;
        }

        protected override Task OnExtraUpdateFieldsAsync(OASISResult<Quest> loadResult, ref bool changesMade, ProviderType providerType)
        {
            if (CLIEngine.NonInteractive || loadResult?.Result == null)
                return Task.CompletedTask;

            if (!CLIEngine.GetConfirmation("Do you wish to edit quest objectives (checklist items: add or remove)?"))
            {
                Console.WriteLine("");
                return Task.CompletedTask;
            }

            InteractiveEditQuestObjectives(loadResult.Result, ref changesMade);
            return Task.CompletedTask;
        }

        private static void InteractiveAppendObjectivesBeforeCreate(Quest quest)
        {
            if (CLIEngine.NonInteractive || quest == null)
                return;

            quest.Objectives ??= new List<Objective>();
            if (!CLIEngine.GetConfirmation("Do you want to add checklist objectives (title and description) to this quest now?"))
            {
                Console.WriteLine("");
                return;
            }

            int nextOrder = quest.Objectives.Count == 0 ? 0 : quest.Objectives.Max(o => o.Order) + 1;
            do
            {
                Console.WriteLine("");
                string title = CLIEngine.GetValidInput("Objective title?");
                if (title == "exit")
                    break;
                string desc = CLIEngine.GetValidInput("Objective description?");
                if (desc == "exit")
                    break;
                quest.Objectives.Add(new Objective
                {
                    Id = Guid.NewGuid(),
                    Order = nextOrder++,
                    Title = title,
                    Description = desc
                });
            }
            while (CLIEngine.GetConfirmation("Add another objective?"));

            Console.WriteLine("");
        }

        private static void InteractiveEditQuestObjectives(Quest quest, ref bool changesMade)
        {
            quest.Objectives ??= new List<Objective>();

            while (true)
            {
                Console.WriteLine("");
                if (quest.Objectives.Count == 0)
                    CLIEngine.ShowMessage("No objectives yet.", ConsoleColor.DarkGray, false);
                else
                {
                    CLIEngine.ShowMessage("Current objectives:", ConsoleColor.Green, false);
                    foreach (Objective o in quest.Objectives.OrderBy(x => x.Order))
                        CLIEngine.ShowMessage($"  [{o.Order}] {o.Title}  ({o.Id})", ConsoleColor.Green, false);
                }

                Console.WriteLine("");
                string action = CLIEngine.GetValidInput("Objectives: type 'add' to add one, 'remove' to remove by order number, or 'done' to finish.").Trim().ToLowerInvariant();
                if (action == "exit" || action == "done")
                    break;

                if (action == "add")
                {
                    string title = CLIEngine.GetValidInput("Objective title?");
                    if (title == "exit")
                        break;
                    string desc = CLIEngine.GetValidInput("Objective description?");
                    if (desc == "exit")
                        break;
                    int next = quest.Objectives.Count == 0 ? 0 : quest.Objectives.Max(x => x.Order) + 1;
                    quest.Objectives.Add(new Objective
                    {
                        Id = Guid.NewGuid(),
                        Order = next,
                        Title = title,
                        Description = desc
                    });
                    changesMade = true;
                }
                else if (action == "remove")
                {
                    string ordStr = CLIEngine.GetValidInput("Order number to remove (as shown in [brackets])?");
                    if (ordStr == "exit")
                        break;
                    if (int.TryParse(ordStr, out int ord))
                    {
                        Objective rem = quest.Objectives.FirstOrDefault(x => x.Order == ord);
                        if (rem != null)
                        {
                            quest.Objectives.Remove(rem);
                            changesMade = true;
                        }
                        else
                            CLIEngine.ShowWarningMessage("No objective with that order.");
                    }
                }
            }

            Console.WriteLine("");
        }

        private async Task UpdateQuestObjectivesFromJsonFileAsync(string idOrName, string jsonPath, ProviderType providerType)
        {
            OASISResult<Quest> loadResult = await FindAsync("update", idOrName, default, true, providerType: providerType);
            if (loadResult == null || loadResult.IsError || loadResult.Result == null)
            {
                CLIEngine.ShowErrorMessage($"Could not load quest to update. Reason: {loadResult?.Message}");
                return;
            }

            if (!File.Exists(jsonPath))
            {
                CLIEngine.ShowErrorMessage($"Objectives JSON file not found: {jsonPath}");
                return;
            }

            string json = await File.ReadAllTextAsync(jsonPath).ConfigureAwait(false);
            OASISResult<List<Objective>> parsed = ParseObjectivesFromJsonContent(json);
            if (parsed.IsError || parsed.Result == null)
            {
                CLIEngine.ShowErrorMessage(parsed.Message ?? "Failed to parse objectives JSON.");
                return;
            }

            loadResult.Result.Objectives = parsed.Result;
            OASISResult<Quest> result = await STARNETManager.EditAsync(STAR.BeamedInAvatar.Id, loadResult.Result, (STARNETDNA)loadResult.Result.STARNETDNA, providerType);
            Console.WriteLine("");
            CLIEngine.ShowWorkingMessage("Saving quest...");

            if (result != null && !result.IsError && result.Result != null)
            {
                (result, bool saveResult) = ErrorHandling.HandleResponse(result, await STARNETManager.WriteDNAAsync(result.Result.STARNETDNA, result.Result.STARNETDNA.SourcePath).ConfigureAwait(false), "Error occured saving the STARNETDNA. Reason: ", "Quest Successfully Updated.");

                if (saveResult)
                    await ShowAsync(result.Result);
            }
            else
                CLIEngine.ShowErrorMessage($"An error occured updating the quest. Reason: {result?.Message}");
        }

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
                OASISResult<List<Objective>> parsed = ParseObjectivesFromJsonContent(json);
                if (parsed.IsError)
                {
                    OASISErrorHandling.HandleError(ref r, parsed.Message);
                    return r;
                }

                createOptions.STARNETHolon.Objectives = parsed.Result ?? new List<Objective>();
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
        }

        private static OASISResult<List<Objective>> ParseObjectivesFromJsonContent(string json)
        {
            OASISResult<List<Objective>> r = new OASISResult<List<Objective>>();
            try
            {
                var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                List<QuestObjectiveCliJson> dtos;

                using (JsonDocument doc = JsonDocument.Parse(json))
                {
                    if (doc.RootElement.ValueKind == JsonValueKind.Array)
                        dtos = JsonSerializer.Deserialize<List<QuestObjectiveCliJson>>(json, opts);
                    else if (doc.RootElement.TryGetProperty("objectives", out JsonElement arr) && arr.ValueKind == JsonValueKind.Array)
                        dtos = JsonSerializer.Deserialize<List<QuestObjectiveCliJson>>(arr.GetRawText(), opts);
                    else
                    {
                        OASISErrorHandling.HandleError(ref r, "Quest objectives JSON must be a JSON array or an object with an \"objectives\" array.");
                        return r;
                    }
                }

                if (dtos == null)
                {
                    r.Result = new List<Objective>();
                    return r;
                }

                var list = new List<Objective>();
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
                        Description = d.Description.Trim()
                    });
                }

                r.Result = list;
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
            CLIEngine.ShowDivider();
            foreach (IQuest iq in list)
            {
                WriteRuntimeQuestToConsole(iq, showDetailedInfo);

                // In "list detailed", include full STARNET holon + STARNET DNA info (mirrors the base list/show output).
                if (showDetailedInfo && iq != null)
                {
                    Console.WriteLine("");
                    await ShowAsync(iq, showHeader: false, showFooter: false, showNumbers: false, number: 0, showDetailedInfo: true);
                    Console.WriteLine("");
                }
            }

            CLIEngine.ShowDivider();
        }

        private static object BuildQuestListJsonObject(IQuest q, bool showDetailed)
        {
            var objectives = new List<object>();
            if (q.Objectives != null)
            {
                foreach (var o in q.Objectives.OrderBy(x => x.Order))
                {
                    if (o == null) continue;
                    objectives.Add(new
                    {
                        id = o.Id,
                        order = o.Order,
                        title = o.Title ?? string.Empty,
                        description = o.Description ?? string.Empty,
                        status = FormatObjectiveUiStatus(q, o),
                        progressPercent = GetObjectiveProgressPercent(o),
                        progressSummary = o.ProgressSummary ?? string.Empty
                    });
                }
            }

            List<string>? prereq = null;
            if (showDetailed && q is Quest qq && qq.PrerequisiteQuestIds != null && qq.PrerequisiteQuestIds.Count > 0)
                prereq = qq.PrerequisiteQuestIds.ToList();

            var row = new
            {
                id = q.Id,
                name = q.Name ?? string.Empty,
                description = q.Description ?? string.Empty,
                questType = GetRuntimeQuestTypeLabel(q),
                status = q.Status.ToString(),
                progressPercent = GetQuestProgressPercent(q),
                gameSource = q.GameSource ?? string.Empty,
                parentQuestId = q.ParentQuestId == Guid.Empty ? (string?)null : q.ParentQuestId.ToString(),
                parentMissionId = q.ParentMissionId == Guid.Empty ? (string?)null : q.ParentMissionId.ToString(),
                rewardKarma = showDetailed ? q.RewardKarma : (long?)null,
                rewardXP = showDetailed ? q.RewardXP : (long?)null,
                prerequisiteQuestIds = prereq,
                objectives,
                starnetDNA = showDetailed && q.STARNETDNA != null ? new
                {
                    id = q.STARNETDNA.Id,
                    name = q.STARNETDNA.Name,
                    description = q.STARNETDNA.Description,
                    starnetHolonType = q.STARNETDNA.STARNETHolonType,
                    starnetCategory = FormatStarnetDnaJsonValue(q.STARNETDNA.STARNETCategory),
                    starnetSubCategory = FormatStarnetDnaJsonValue(q.STARNETDNA.STARNETSubCategory),
                    version = q.STARNETDNA.Version,
                    versionSequence = q.STARNETDNA.VersionSequence,
                    createdOn = q.STARNETDNA.CreatedOn,
                    createdByAvatarUsername = q.STARNETDNA.CreatedByAvatarUsername,
                    publishedOn = q.STARNETDNA.PublishedOn,
                    publishedProviderType = q.STARNETDNA.PublishedProviderType,
                    launchTarget = q.STARNETDNA.LaunchTarget,
                    dependencies = q.STARNETDNA.Dependencies,
                    metaTagMappings = q.STARNETDNA.MetaTagMappings
                } : null
            };
            return row;
        }

        private static void WriteRuntimeQuestToConsole(IQuest q, bool showDetailed)
        {
            string typeLabel = GetRuntimeQuestTypeLabel(q);
            string parentNote = q.ParentQuestId != Guid.Empty
                ? $"  (sub-quest of {q.ParentQuestId})"
                : string.Empty;

            CLIEngine.ShowMessage(
                $"  {q.Name ?? "(unnamed)"}  [{typeLabel}]  {q.Status}  {GetQuestProgressPercent(q)}%{parentNote}",
                ConsoleColor.Green,
                false);

            if (!string.IsNullOrWhiteSpace(q.Description))
                CLIEngine.ShowMessage($"      {q.Description}", ConsoleColor.Green, false);

            if (!string.IsNullOrWhiteSpace(q.GameSource))
                CLIEngine.ShowMessage($"      GameSource: {q.GameSource}", ConsoleColor.Green, false);

            if (q.Objectives == null || q.Objectives.Count == 0)
            {
                CLIEngine.ShowMessage("      Objectives: (none)", ConsoleColor.DarkGray, false);
                Console.WriteLine("");
                return;
            }

            CLIEngine.ShowMessage("      Objectives:", ConsoleColor.Green, false);
            foreach (IObjective o in q.Objectives.OrderBy(x => x.Order))
            {
                if (o == null) continue;
                string st = FormatObjectiveUiStatus(q, o);
                int pct = GetObjectiveProgressPercent(o);
                string line = o.ProgressSummary ?? string.Empty;
                CLIEngine.ShowMessage(
                    $"        [{o.Order}] {o.Title}  |  {st}  |  {pct}%",
                    ConsoleColor.Green,
                    false);
                if (!string.IsNullOrWhiteSpace(line))
                    CLIEngine.ShowMessage($"            {line}", ConsoleColor.DarkGray, false);

                // Spacing between objective "items" (matches the inventory detailed UX).
                Console.WriteLine("");
            }

            Console.WriteLine("");
        }

        private static string GetRuntimeQuestTypeLabel(IQuest q)
        {
            if (q.QuestType != default(QuestType))
                return q.QuestType.ToString();
            return q.Type.ToString();
        }

        private static int GetQuestProgressPercent(IQuest q)
        {
            if (q is Quest qq)
                return qq.ProgressPercent;
            return 0;
        }

        private static int GetObjectiveProgressPercent(IObjective o)
        {
            if (o is Objective oj)
                return oj.ProgressPercent;
            return o.IsCompleted ? 100 : 0;
        }

        private static string FormatObjectiveUiStatus(IQuest quest, IObjective obj)
        {
            if (obj.IsCompleted) return "Completed";
            if (quest.Status == QuestStatus.NotStarted)
                return "NotStarted";
            return "InProgress";
        }

        private static string FormatStarnetDnaJsonValue(object? raw)
        {
            if (raw == null)
                return "None";

            if (raw is JsonElement je)
            {
                switch (je.ValueKind)
                {
                    case JsonValueKind.String:
                        return je.GetString() ?? "None";
                    case JsonValueKind.Number:
                        if (je.TryGetInt32(out var i32))
                            return i32.ToString(CultureInfo.InvariantCulture);
                        if (je.TryGetInt64(out var i64))
                            return i64.ToString(CultureInfo.InvariantCulture);
                        return je.GetRawText();
                    case JsonValueKind.Null:
                    case JsonValueKind.Undefined:
                        return "None";
                    default:
                        return je.GetRawText();
                }
            }

            try
            {
                string s = raw.ToString();
                if (string.IsNullOrWhiteSpace(s))
                    return "None";

                if (s.Contains("\"ValueKind\"", StringComparison.Ordinal))
                {
                    using var doc = JsonDocument.Parse(s);
                    if (doc.RootElement.ValueKind == JsonValueKind.Object &&
                        doc.RootElement.TryGetProperty("ValueKind", out JsonElement vk))
                    {
                        if (vk.ValueKind == JsonValueKind.Number)
                            return vk.GetRawText();
                        if (vk.ValueKind == JsonValueKind.String)
                            return vk.GetString() ?? "None";
                    }
                }

                return s;
            }
            catch
            {
                return raw.ToString() ?? "None";
            }
        }
    }
}