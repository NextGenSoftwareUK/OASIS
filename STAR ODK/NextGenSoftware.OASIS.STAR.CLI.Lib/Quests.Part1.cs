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

                TryApplyQuestHandoffFromScriptedKeys(createOptions);

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
            InteractiveAppendQuestLevelGeoAndHandoffForCreate(createOptions.STARNETHolon);

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

            if (CLIEngine.GetConfirmation("Do you wish to edit quest-level linked GeoHotSpot or external handoff URI?"))
            {
                Console.WriteLine("");
                PromptQuestLevelGeoAndHandoffFields(loadResult.Result, withIntroConfirmation: false);
                changesMade = true;
            }

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
                var obj = new Objective
                {
                    Id = Guid.NewGuid(),
                    Order = nextOrder++,
                    Title = title,
                    Description = desc
                };
                PromptOptionalObjectiveLinks(obj);
                quest.Objectives.Add(obj);
            }
            while (CLIEngine.GetConfirmation("Add another objective?"));

            Console.WriteLine("");
        }

        private static void PromptOptionalObjectiveLinks(Objective obj)
        {
            if (CLIEngine.NonInteractive || obj == null)
                return;

            string gh = CLIEngine.GetValidInput("Optional: linked GeoHotSpot id (GUID) for this objective (blank = skip):")?.Trim();
            if (!string.IsNullOrEmpty(gh) && gh != "exit" && Guid.TryParse(gh, out Guid g))
                obj.LinkedGeoHotSpotId = g;
            else if (!string.IsNullOrEmpty(gh) && gh != "exit")
                CLIEngine.ShowWarningMessage("Invalid GUID; objective GeoHotSpot link skipped.");

            string uri = CLIEngine.GetValidInput("Optional: external handoff URI for this objective (blank = skip):")?.Trim();
            if (!string.IsNullOrEmpty(uri) && uri != "exit")
                obj.ExternalHandoffUri = uri;
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
                    var newObj = new Objective
                    {
                        Id = Guid.NewGuid(),
                        Order = next,
                        Title = title,
                        Description = desc
                    };
                    PromptOptionalObjectiveLinks(newObj);
                    quest.Objectives.Add(newObj);
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
            OASISResult<QuestObjectivesJsonParseResult> parsed = ParseQuestObjectivesJsonFile(json, allowObjectivesOmitted: true);
            if (parsed.IsError || parsed.Result == null)
            {
                CLIEngine.ShowErrorMessage(parsed.Message ?? "Failed to parse objectives JSON.");
                return;
            }

            if (parsed.Result.Objectives == null && !parsed.Result.QuestLinkedGeoHotSpotId.HasValue && string.IsNullOrWhiteSpace(parsed.Result.QuestExternalHandoffUri))
            {
                CLIEngine.ShowErrorMessage("JSON must include an \"objectives\" array and/or linkedGeoHotSpotId and/or externalHandoffUri.");
                return;
            }

            if (parsed.Result.Objectives != null)
                loadResult.Result.Objectives = parsed.Result.Objectives;
            if (parsed.Result.QuestLinkedGeoHotSpotId.HasValue)
                loadResult.Result.LinkedGeoHotSpotId = parsed.Result.QuestLinkedGeoHotSpotId;
            if (!string.IsNullOrWhiteSpace(parsed.Result.QuestExternalHandoffUri))
                loadResult.Result.ExternalHandoffUri = parsed.Result.QuestExternalHandoffUri.Trim();
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
    }
}
