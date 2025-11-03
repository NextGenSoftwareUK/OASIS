using NextGenSoftware.CLI.Engine;
using NextGenSoftware.OASIS.API.Core.Enums;
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

        public override async Task<OASISResult<Quest>> CreateAsync(ISTARNETCreateOptions<Quest, STARNETDNA> createOptions = null, object holonSubType = null, bool showHeaderAndInro = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<Quest> result = new OASISResult<Quest>();
            Mission parentMission = null;
            Quest parentQuest = null;
            int order = 0;

            if (CLIEngine.GetConfirmation("Does this quest belong to a Mission?"))
            {
                Console.WriteLine("");
                OASISResult<InstalledMission> missionResult = await STARCLI.Missions.FindAndInstallIfNotInstalledAsync("use for the parent");

                if (missionResult != null && missionResult.Result != null && !missionResult.IsError)
                {
                    OASISResult<Mission> loadResult = await STAR.STARAPI.Missions.LoadAsync(STAR.BeamedInAvatar.Id, missionResult.Result.Id, providerType: providerType);

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
                    OASISResult<Quest> loadResult = await STAR.STARAPI.Quests.LoadAsync(STAR.BeamedInAvatar.Id, questResult.Result.Id, providerType: providerType);

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

            result = await base.CreateAsync(createOptions, holonSubType, showHeaderAndInro, providerType);

            if (result != null)
            {
                if (result.Result != null && result.Result != null && !result.IsError)
                {
                    if (parentMission != null)
                    {
                        //TODO: Need to find way to add dependency without it being installed first! ;-)
                        //await STAR.STARAPI.Missions.AddDependencyAsync<InstalledQuest>(STAR.BeamedInAvatar.Id, parentMission, result.Result, DependencyType.Quest, providerType: providerType);
                    }

                    if (parentQuest != null)
                    {
                        //TODO: Need to find way to add dependency without it being installed first! ;-)
                        //await STAR.STARAPI.Quests.AddDependencyAsync<InstalledQuest>(STAR.BeamedInAvatar.Id, parentQuest, result.Result, DependencyType.Quest, providerType: providerType);
                    }

                    if (CLIEngine.GetConfirmation("Do you want to add any GeoHotSpot's to this Quest now?"))
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

                            Console.WriteLine("");
                            OASISResult<Quest> addResult = await AddDependencyAsync(STARNETDNA: result.Result.STARNETDNA, dependencyType: "GeoHotSpot", idOrNameOfDependency: geoHotSpotId.ToString(), providerType: providerType);
                        }
                        while (CLIEngine.GetConfirmation("Do you wish to add another GeoHotSpot?"));  
                    }

                    Console.WriteLine("");
                    if (CLIEngine.GetConfirmation("Do you want to add any GeoNFT's to this Quest now?"))
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
                            OASISResult<Quest> addResult = await AddDependencyAsync(STARNETDNA: result.Result.STARNETDNA, dependencyType: "GeoNFT", idOrNameOfDependency: geoNFTId.ToString(), providerType: providerType);
                        }
                        while (CLIEngine.GetConfirmation("Do you wish to add another GeoNFT?"));
                    }

                    if (CLIEngine.GetConfirmation("Do you want to add any sub-quest's to this Quest now?"))
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
                            OASISResult<Quest> addResult = await AddDependencyAsync(STARNETDNA: result.Result.STARNETDNA, dependencyType: "Quest", idOrNameOfDependency: questId.ToString(), providerType: providerType);
                        }
                        while (CLIEngine.GetConfirmation("Do you wish to add another sub-quest?"));
                    }

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
    }
}