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
    //public class Chapters : STARNETUIBase<Chapter, DownloadedChapter, InstalledChapter, ChapterDNA>
    public class Chapters : STARNETUIBase<Chapter, DownloadedChapter, InstalledChapter, STARNETDNA>
    {
        public Chapters(Guid avatarId, STARDNA STARDNA) : base(new API.ONODE.Core.Managers.ChapterManager(avatarId, STARDNA),
            "Welcome to the Chapter Wizard", new List<string> 
            {
                "This wizard will allow you create a Chapter which contain Quest's. Chapter's belong to Mission's and allow larger quest's to be broken into Chapter's.",
                "Quest's can also have sub-quests.",
                "Quest's contain GeoNFT's & GeoHotSpot's which can reward you various InventoryItem's for the avatar who completes the quest, triggers the GeoHotSpot or collects the GeoNFT.",
                "The wizard will create an empty folder with a ChapterDNA.json file in it. You then simply place any files/folders you need for the assets (optional) for the mission into this folder.",
                "Finally you run the sub-command 'chapter publish' to convert the folder containing the chapter (can contain any number of files and sub-folders) into a OASIS Chapter file (.ochapter) as well as optionally upload to STARNET.",
                "You can then share the .ochapter file with others across any platform or OS, who can then install the Chapter from the file using the sub-command 'chapter install'.",
                "You can also optionally choose to upload the .ochapter file to the STARNET store so others can search, download and install the chapter."
            },
            STAR.STARDNA.DefaultChaptersSourcePath, "DefaultChaptersSourcePath",
            STAR.STARDNA.DefaultChaptersPublishedPath, "DefaultChaptersPublishedPath",
            STAR.STARDNA.DefaultChaptersDownloadedPath, "DefaultChaptersDownloadedPath",
            STAR.STARDNA.DefaultChaptersInstalledPath, "DefaultChaptersInstalledPath")
        { }

        //public override async Task<OASISResult<Chapter>> CreateAsync(object createParams, Chapter newHolon = null, bool showHeaderAndInro = true, bool checkIfSourcePathExists = true, object holonSubType = null, Dictionary<string, object> metaData = null, STARNETDNA STARNETDNA = default, ProviderType providerType = ProviderType.Default)
        public override async Task<OASISResult<Chapter>> CreateAsync(ISTARNETCreateOptions<Chapter, STARNETDNA> createOptions = null, object holonSubType = null, bool showHeaderAndInro = true, bool addDependencies = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<Chapter> result = new OASISResult<Chapter>();
            Mission parentMission = null;
            int order = 0;

            if (CLIEngine.GetConfirmation("Does this chapter belong to a Mission?"))
            {
                OASISResult<InstalledMission> missionResult = await STARCLI.Missions.FindAndInstallIfNotInstalledAsync("use for the parent");

                if (missionResult != null && missionResult.Result != null && !missionResult.IsError)
                {
                    OASISResult<Mission> loadResult = await STAR.STARAPI.Missions.LoadAsync(STAR.BeamedInAvatar.Id, missionResult.Result.Id, providerType: providerType);

                    if (loadResult != null && loadResult.Result != null && !loadResult.IsError)
                        parentMission = loadResult.Result;
                }
            }

            if (parentMission != null)
                order = parentMission.Chapters.Count() + 1;

            if (createOptions == null)
                createOptions = new STARNETCreateOptions<Chapter, STARNETDNA>() { STARNETHolon = new Chapter() };

            if (parentMission != null)
                createOptions.STARNETHolon.ParentMissionId = parentMission.Id;

            createOptions.STARNETHolon.Order = order;

            result = await base.CreateAsync(createOptions, holonSubType, showHeaderAndInro, false, providerType);

            if (result != null)
            {
                if (result.Result != null && result.Result != null && !result.IsError)
                {
                    if (parentMission != null)
                    {
                        //TODO: Need to find way to add dependency without it being installed first! ;-)
                        //await STAR.STARAPI.Missions.AddDependencyAsync<InstalledChapter>(STAR.BeamedInAvatar.Id, parentMission, result.Result, DependencyType.Chapter, providerType: providerType);
                    }

                    if (CLIEngine.GetConfirmation("Do you want to add any Quest's to this Chapter now?"))
                    {
                        do
                        {
                            Guid questId = Guid.Empty;
                            Console.WriteLine("");
                            if (!CLIEngine.GetConfirmation("Does the Quest already exist?"))
                            {
                                OASISResult<Quest> questResult = await STARCLI.Quests.CreateAsync(null, providerType: providerType);

                                if (questResult != null && questResult.Result != null && !questResult.IsError)
                                    questId = questResult.Result.Id;
                            }

                            Console.WriteLine("");
                            OASISResult<Chapter> addResult = await AddDependencyAsync(STARNETDNA: result.Result.STARNETDNA, dependencyType: "Quest", idOrNameOfDependency: questId.ToString(), providerType: providerType);
                        }
                        while (CLIEngine.GetConfirmation("Do you wish to add another Quest?"));
                    }

                    await AddDependenciesAsync(result.Result.STARNETDNA, providerType);
                }
            }

            return result;
        }
    }
}