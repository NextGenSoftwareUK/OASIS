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
    public class Games : STARNETUIBase<Game, DownloadedGame, InstalledGame, STARNETDNA>
    {
        public Games(Guid avatarId, STARDNA STARDNA) : base(new NextGenSoftware.OASIS.API.ONODE.Core.Managers.GameManager(avatarId, STARDNA),
            "Welcome to the Game Wizard", new List<string> 
            {
                "This wizard will allow you to create a OGame which can be published to STARNET and shared with others.",
                "Games can support cross-game interoperability, allowing players to share keycards, avatar SSO, karma scores, NFTs, and assets across different games.",
                "Games can be linked to OQuests from the Quest API, enabling quests to span multiple games.",
                "Games support shared inventory systems, allowing items collected in one game to be used in other games, apps, websites, and services.",
                "The wizard will create an empty folder with a GameDNA.json file in it. You then simply place any files/folders you need for the assets (optional) for the game into this folder.",
                "Finally you run the sub-command 'game publish' to convert the folder containing the game (can contain any number of files and sub-folders) into a OASIS Game file (.ogame) as well as optionally upload to STARNET.",
                "You can then share the .ogame file with others across any platform or OS, who can then install the Game from the file using the sub-command 'game install'.",
                "You can also optionally choose to upload the .ogame file to the STARNET store so others can search, download and install the game."
            },
            STAR.STARDNA.DefaultGamesSourcePath, "DefaultGamesSourcePath",
            STAR.STARDNA.DefaultGamesPublishedPath, "DefaultGamesPublishedPath",
            STAR.STARDNA.DefaultGamesDownloadedPath, "DefaultGamesDownloadedPath",
            STAR.STARDNA.DefaultGamesInstalledPath, "DefaultGamesInstalledPath")
        { }

        public override async Task<OASISResult<Game>> CreateAsync(ISTARNETCreateOptions<Game, STARNETDNA> createOptions = null, object holonSubType = null, bool showHeaderAndInro = true, bool addDependencies = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<Game> result = new OASISResult<Game>();

            result = await base.CreateAsync(createOptions, holonSubType, showHeaderAndInro, false, providerType);

            if (result != null)
            {
                if (result.Result != null && result.Result != null && !result.IsError)
                {
                    Console.WriteLine("");
                    if (CLIEngine.GetConfirmation($"Do you want to configure interoperability features for the '{result.Result.Name}' game?"))
                    {
                        Console.WriteLine("");
                        result.Result.SupportsCrossGameQuests = CLIEngine.GetConfirmation("Does this game support cross-game quests?");
                        result.Result.SupportsSharedInventory = CLIEngine.GetConfirmation("Does this game support shared inventory?");
                        result.Result.SupportsAvatarSSO = CLIEngine.GetConfirmation("Does this game support avatar SSO (Single Sign-On)?");
                        result.Result.SupportsKarmaScores = CLIEngine.GetConfirmation("Does this game support karma scores?");
                        result.Result.SupportsNFTs = CLIEngine.GetConfirmation("Does this game support NFTs?");
                        result.Result.SupportsKeycards = CLIEngine.GetConfirmation("Does this game support keycards?");

                        // Save the updated game
                        var saveResult = await STARNETManager.UpdateAsync(STAR.BeamedInAvatar.Id, result.Result, false, "Default", providerType);
                        
                        if (saveResult.IsError)
                            CLIEngine.ShowErrorMessage($"Error saving game interoperability settings: {saveResult.Message}");
                        else
                            CLIEngine.ShowSuccessMessage("Game interoperability settings saved successfully.");
                    }

                    Console.WriteLine("");
                    await AddDependenciesAsync(result.Result.STARNETDNA, providerType);
                }
            }

            return result;
        }
    }
}

