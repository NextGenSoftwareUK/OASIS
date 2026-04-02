using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
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

        public override async Task<OASISResult<Quest>> CreateAsync(ISTARNETCreateOptions<Quest, STARNETDNA> createOptions = null, object holonSubType = null, bool showHeaderAndInro = true, bool addDependencies = true, ProviderType providerType = ProviderType.Default)
        {
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
                WriteRuntimeQuestToConsole(iq, showDetailedInfo);

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
                description = showDetailed ? (q.Description ?? string.Empty) : null,
                questType = GetRuntimeQuestTypeLabel(q),
                status = q.Status.ToString(),
                progressPercent = GetQuestProgressPercent(q),
                gameSource = q.GameSource ?? string.Empty,
                parentQuestId = q.ParentQuestId == Guid.Empty ? (string?)null : q.ParentQuestId.ToString(),
                parentMissionId = q.ParentMissionId == Guid.Empty ? (string?)null : q.ParentMissionId.ToString(),
                rewardKarma = showDetailed ? q.RewardKarma : (long?)null,
                rewardXP = showDetailed ? q.RewardXP : (long?)null,
                prerequisiteQuestIds = prereq,
                objectives
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

            if (showDetailed && !string.IsNullOrWhiteSpace(q.Description))
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
    }
}