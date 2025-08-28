using Microsoft.Azure.Cosmos.Serialization.HybridRow;
using NextGenSoftware.CLI.Engine;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers;
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

        public async Task<OASISResult<IQuest>> AddGeoNFTToQuestAsync(string idOrNameOfQuest, string idOrNameOfGeoNFT, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IQuest> result = new OASISResult<IQuest>();
            OASISResult <Quest> parentResult = await FindAsync("use", idOrNameOfQuest, true, providerType: providerType);

            if (parentResult != null && !parentResult.IsError && parentResult.Result != null)
            {
                OASISResult<InstalledGeoNFT> installedGeoNFT = await STARCLI.GeoNFTs.FindAndInstallIfNotInstalledAsync("use", idOrNameOfGeoNFT, providerType: providerType);

                if (installedGeoNFT != null && installedGeoNFT.Result != null && !installedGeoNFT.IsError)
                {
                    OASISResult<IQuest> addResult = ((QuestManager)STARNETManager).AddGeoNFTToQuest(STAR.BeamedInAvatar.Id, parentResult.Result.Id, installedGeoNFT.Result.Id, providerType);

                    if (addResult != null && addResult.Result != null && !addResult.IsError)
                        CLIEngine.ShowSuccessMessage($"Successfully added GeoNFT {installedGeoNFT.Result.Name} to Quest {parentResult.Result.Name}.");
                    else
                        OASISErrorHandling.HandleError(ref result, $"Error occured adding GeoNFT {installedGeoNFT.Result.Name} to Quest {parentResult.Result.Name}. Reason: {addResult.Message}");
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"Error occured finding and installing GeoNFT {idOrNameOfGeoNFT}. Reason: {installedGeoNFT.Message}");
            }
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured finding Quest {idOrNameOfQuest}. Reason: {parentResult.Message}");

            return result;
        }

        public async Task<OASISResult<IQuest>> AddGeoNFTToQuestAsync(string idOrNameOfQuest, string idOrNameOfGeoNFT, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IQuest> result = new OASISResult<IQuest>();
            OASISResult<Quest> parentResult = await FindAsync("use", idOrNameOfQuest, true, providerType: providerType);

            if (parentResult != null && !parentResult.IsError && parentResult.Result != null)
            {
                OASISResult<InstalledGeoNFT> installedGeoNFT = await STARCLI.GeoNFTs.FindAndInstallIfNotInstalledAsync("use", idOrNameOfGeoNFT, providerType: providerType);

                if (installedGeoNFT != null && installedGeoNFT.Result != null && !installedGeoNFT.IsError)
                {
                    OASISResult<IQuest> addResult = ((QuestManager)STARNETManager).AddGeoNFTToQuest(STAR.BeamedInAvatar.Id, parentResult.Result.Id, installedGeoNFT.Result.Id, providerType);

                    if (addResult != null && addResult.Result != null && !addResult.IsError)
                        CLIEngine.ShowSuccessMessage($"Successfully added GeoNFT {installedGeoNFT.Result.Name} to Quest {parentResult.Result.Name}.");
                    else
                        OASISErrorHandling.HandleError(ref result, $"Error occured adding GeoNFT {installedGeoNFT.Result.Name} to Quest {parentResult.Result.Name}. Reason: {addResult.Message}");
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"Error occured finding and installing GeoNFT {idOrNameOfGeoNFT}. Reason: {installedGeoNFT.Message}");
            }
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured finding Quest {idOrNameOfQuest}. Reason: {parentResult.Message}");

            return result;
        }
        public async Task<OASISResult<IQuest>> RemoveGeoNFTFromQuestAsync(string idOrNameOfQuest, string idOrNameOfGeoNFT, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IQuest> result = new OASISResult<IQuest>();
            OASISResult<Quest> parentResult = await FindAsync("use", idOrNameOfQuest, true, providerType: providerType);

            if (parentResult != null && !parentResult.IsError && parentResult.Result != null)
            {
                //OASISResult<InstalledGeoNFT> installedGeoNFT = await STARCLI.GeoNFTs.FindAndInstallIfNotInstalledAsync("use", idOrNameOfGeoNFT, providerType: providerType);

                //if (installedGeoNFT != null && installedGeoNFT.Result != null && !installedGeoNFT.IsError)
                //{
                //    OASISResult<IQuest> addResult = ((QuestManager)STARNETManager).AddGeoNFTToQuest(STAR.BeamedInAvatar.Id, parentResult.Result.Id, installedGeoNFT.Result.Id, providerType);

                //    if (addResult != null && addResult.Result != null && !addResult.IsError)
                //        CLIEngine.ShowSuccessMessage($"Successfully removed GeoNFT {installedGeoNFT.Result.Name} from Quest {parentResult.Result.Name}.");
                //    else
                //        OASISErrorHandling.HandleError(ref result, $"Error occured removing GeoNFT {installedGeoNFT.Result.Name} from Quest {parentResult.Result.Name}. Reason: {addResult.Message}");
                //}
                //else
                //    OASISErrorHandling.HandleError(ref result, $"Error occured finding and installing GeoNFT {idOrNameOfGeoNFT}. Reason: {installedGeoNFT.Message}");
            }
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured finding Quest {idOrNameOfQuest}. Reason: {parentResult.Message}");

            return result;
        }
    }
}