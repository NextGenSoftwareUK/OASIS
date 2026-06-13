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
    //public class Missions : STARNETUIBase<Mission, DownloadedMission, InstalledMission, MissionDNA>
    public class Missions : STARNETUIBase<Mission, DownloadedMission, InstalledMission, STARNETDNA>
    {
        public Missions(Guid avatarId, STARDNA STARDNA) : base(new API.ONODE.Core.Managers.MissionManager(avatarId, STARDNA),
            "Welcome to the Mission Wizard", new List<string> 
            {
                "This wizard will allow you create a Mission which contains Quest's. Larger Quest's can be broken into Chapter's.",
                "Mission's can contain both Quest's and Chapter's. Quest's can also have sub-quests.",
                "Quest's contain GeoNFT's & GeoHotSpot's which can reward you various InventoryItem's for the avatar who completes the quest, triggers the GeoHotSpot or collects the GeoNFT.",
                "Mission's can optionally be linked to OAPP's.",
                "The wizard will create an empty folder with a MissionDNA.json file in it. You then simply place any files/folders you need for the assets (optional) for the mission into this folder.",
                "Finally you run the sub-command 'mission publish' to convert the folder containing the mission (can contain any number of files and sub-folders) into a OASIS Mission file (.omission) as well as optionally upload to STARNET.",
                "You can then share the .omission file with others across any platform or OS, who can then install the Mission from the file using the sub-command 'mission install'.",
                "You can also optionally choose to upload the .omission file to the STARNET store so others can search, download and install the mission."
            },
            STAR.STARDNA.DefaultMissionsSourcePath, "DefaultMissionsSourcePath",
            STAR.STARDNA.DefaultMissionsPublishedPath, "DefaultMissionsPublishedPath",
            STAR.STARDNA.DefaultMissionsDownloadedPath, "DefaultMissionsDownloadedPath",
            STAR.STARDNA.DefaultMissionsInstalledPath, "DefaultMissionsInstalledPath")
        { }

        public override async Task<OASISResult<Mission>> CreateAsync(ISTARNETCreateOptions<Mission, STARNETDNA> createOptions = null, object holonSubType = null, bool showHeaderAndInro = true, bool addDependencies = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<Mission> result = new OASISResult<Mission>();

            result = await base.CreateAsync(createOptions, holonSubType, showHeaderAndInro, false, providerType);

            if (result != null)
            {
                if (result.Result != null && result.Result != null && !result.IsError)
                {
                    if (CLIEngine.GetConfirmation("Do you want to add any Chapter's to this Mission now?"))
                    {
                        do
                        {
                            Guid chapterId = Guid.Empty;
                            Console.WriteLine("");
                            if (!CLIEngine.GetConfirmation("Does the Chapter already exist?"))
                            {
                                OASISResult<Chapter> chapterResult = await STARCLI.Chapters.CreateAsync(null, providerType: providerType);

                                if (chapterResult != null && chapterResult.Result != null && !chapterResult.IsError)
                                    chapterId = chapterResult.Result.Id;
                            }

                            Console.WriteLine("");
                            OASISResult<Mission> addResult = await AddDependencyAsync(parentSTARNETDNA: result.Result.STARNETDNA, dependencyType: "Chapter", idOrNameOfDependency: chapterId.ToString(), providerType: providerType);
                        }
                        while (CLIEngine.GetConfirmation("Do you wish to add another Chapter?"));
                    }

                    Console.WriteLine();
                    if (CLIEngine.GetConfirmation("Do you want to add any Quest's to this Mission now?"))
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
                            OASISResult<Mission> addResult = await AddDependencyAsync(parentSTARNETDNA: result.Result.STARNETDNA, dependencyType: "Quest", idOrNameOfDependency: questId.ToString(), providerType: providerType);
                        }
                        while (CLIEngine.GetConfirmation("Do you wish to add another Quest?"));
                    }

                    Console.WriteLine();
                    await AddDependenciesAsync(result.Result.STARNETDNA, providerType);
                }
            }

            return result;
        }
    }
}