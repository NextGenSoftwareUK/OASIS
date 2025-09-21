using NextGenSoftware.CLI.Engine;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Objects;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.STAR.CLI.Lib.Objects;
using NextGenSoftware.OASIS.STAR.DNA;

namespace NextGenSoftware.OASIS.STAR.CLI.Lib
{
    //public class InventoryItems : STARNETUIBase<InventoryItem, DownloadedInventoryItem, InstalledInventoryItem, InventoryItemDNA>
    public class InventoryItems : STARNETUIBase<InventoryItem, DownloadedInventoryItem, InstalledInventoryItem, STARNETDNA>
    {
        public InventoryItems(Guid avatarId, STARDNA STARDNA) : base(new API.ONODE.Core.Managers.InventoryItemManager(avatarId, STARDNA),
            "Welcome to the Geo-HotSpot Wizard", new List<string> 
            {
                "This wizard will allow you create a InventoryItem which are rewarded when you complete quest's, collect GeoNFT's or trigger GeoHotSpot's.",
                "The wizard will create an empty folder with a InventoryItemDNA.json file in it. You then simply place any files/folders you need for the assets (optional) for the inventory item into this folder.",
                "Finally you run the sub-command 'inventoryitem publish' to convert the folder containing the inventory item (can contain any number of files and sub-folders) into a OASIS InventoryItem file (.oinventoryitem) as well as optionally upload to STARNET.",
                "You can then share the .oinventoryitem file with others across any platform or OS, who can then install the InventoryItem from the file using the sub-command 'inventoryitem install'.",
                "You can also optionally choose to upload the .oinventoryitem file to the STARNET store so others can search, download and install the inventory item."
            },
            STAR.STARDNA.DefaultInventoryItemsSourcePath, "DefaultInventoryItemsSourcePath",
            STAR.STARDNA.DefaultInventoryItemsPublishedPath, "DefaultInventoryItemsPublishedPath",
            STAR.STARDNA.DefaultInventoryItemsDownloadedPath, "DefaultInventoryItemsDownloadedPath",
            STAR.STARDNA.DefaultInventoryItemsInstalledPath, "DefaultInventoryItemsInstalledPath")
        { }

        public override async Task<OASISResult<InventoryItem>> CreateAsync(object createParams, InventoryItem newHolon = null, bool showHeaderAndInro = true, bool checkIfSourcePathExists = true, object holonSubType = null, Dictionary<string, object> metaData = null, STARNETDNA STARNETDNA = default, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<InventoryItem> result = new OASISResult<InventoryItem>();

            if (newHolon == null)
                newHolon = new InventoryItem();

            OASISResult<ImageObjectResult> imageObjectResult = await ProcessImageOrObjectAsync("InventoryItem");

            if (imageObjectResult != null && imageObjectResult.Result != null && !imageObjectResult.IsError)
            {
                newHolon.Image2D = imageObjectResult.Result.Image2D;
                newHolon.Image2DURI = imageObjectResult.Result.Image2DURI;
                newHolon.Object3D = imageObjectResult.Result.Object3D;
                newHolon.Object3DURI = imageObjectResult.Result.Object3DURI;
            }
            else
            {
                result.IsError = true;
                result.Message = "Error processing image or object!";
                return result;
            }

            result = await base.CreateAsync(createParams, newHolon, showHeaderAndInro, checkIfSourcePathExists, holonSubType, metaData, STARNETDNA, providerType);

            if (result != null)
            {
                if (result.Result != null && result.Result != null && !result.IsError)
                {
                    //CLIEngine.ShowMessage("Add any dependencies to the InventoryItem below. If for example you want items to be rewarded when it is triggered then add a InventoryItem dependency, if you want it to unlock a new quest then add a Quest dependency and so on. If however this GeoHotSpot belongs to another Quest then you will need to add it as a dependency to that Quest (or use the quest create/edit sub-command).", ConsoleColor.Yellow);
                    await AddDependenciesAsync(result.Result.STARNETDNA, providerType);
                }
            }

            return result;
        }
    }
}