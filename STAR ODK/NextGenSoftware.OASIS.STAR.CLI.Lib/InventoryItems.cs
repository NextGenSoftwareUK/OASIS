using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Objects;

namespace NextGenSoftware.OASIS.STAR.CLI.Lib
{
    //public class InventoryItems : STARNETUIBase<InventoryItem, DownloadedInventoryItem, InstalledInventoryItem, InventoryItemDNA>
    public class InventoryItems : STARNETUIBase<InventoryItem, DownloadedInventoryItem, InstalledInventoryItem, STARNETDNA>
    {
        public InventoryItems(Guid avatarId) : base(new API.ONODE.Core.Managers.InventoryItemManager(avatarId),
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
    }
}