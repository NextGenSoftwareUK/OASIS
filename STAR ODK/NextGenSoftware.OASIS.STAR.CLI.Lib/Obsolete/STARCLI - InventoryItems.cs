//using NextGenSoftware.CLI.Engine;
//using NextGenSoftware.OASIS.Common;
//using NextGenSoftware.OASIS.API.Core.Enums;
//using NextGenSoftware.OASIS.API.Core.Objects;
//using NextGenSoftware.OASIS.API.Core.Interfaces;
//using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;

//namespace NextGenSoftware.OASIS.STAR.CLI.Lib
//{
//    public static partial class STARCLI
//    {
//        public static async Task<OASISResult<IInventoryItem>> CreateInventoryItemAsync(object createParams = null, ProviderType providerType = ProviderType.Default)
//        {
//            OASISResult<IInventoryItem> result = new OASISResult<IInventoryItem>();
//            byte[] object3D = null;
//            byte[] image2D = null;
//            string name = CLIEngine.GetValidInput("What is the name of the inventory item?");
//            string description = CLIEngine.GetValidInput("What is the description of the inventory item?");
            
//            if (CLIEngine.GetConfirmation("The inventory item can have either a 2D Image/Sprite and/or a 3D Object to represent it (but you must enter at least one of them). Would you like a 3D Object to represent the item?"))
//                object3D = CLIEngine.GetValidFileAndUpload("What is the full path to the 3D Object that will represent this inventory item?");
            
//            if (CLIEngine.GetConfirmation("Would you like a 2D Image/Sprite to represent the item?"))
//                image2D = CLIEngine.GetValidFileAndUpload("What is the full path to the 2D Sprite/Image that will represent this inventory item?");

//            result = await STAR.OASISAPI.Inventory.SaveInventoryItemAsync(new InventoryItem()
//            {
//                Name = name,
//                Description = description,
//                //InventoryItemData = new InventoryItemData() { Image2D = image2D, Object3D = object3D} //TODO: Trying to decide which approach is best? This way (adding custom props to a seperate class) or the way it is currently (custom props on the root of the holon but may be hard to find?) or the OAPP/NFT way of have a seperate object added to the metadata of the Holon...
//                Image2D = image2D,
//                Object3D = object3D
//            }, STAR.BeamedInAvatar.Id, providerType);;

//            if (result != null && !result.IsError && result.Result != null)
//                CLIEngine.ShowSuccessMessage("Inventory Item Successfully Created.");
//            else
//                CLIEngine.ShowErrorMessage($"Error occured creating the inventory item. Reason: {result.Message}");

//            return result;
//        }

//        //TODO: Make all Publish methods generic so can be re-used for other areas in CLI (like the backend managers are).
//        public static async Task<OASISResult<IInventoryItem>> PublishInventoryItemAsync(Guid inventoryItemId, ProviderType providerType = ProviderType.Default)
//        {
//            OASISResult<IInventoryItem> result = await STAR.OASISAPI.Inventory.PublishInventoryItemAsync(inventoryItemId, STAR.BeamedInAvatar.Id, providerType);

//            if (result != null && !result.IsError && result.Result != null)
//                CLIEngine.ShowSuccessMessage("Inventory Item Successfully Published.");
//            else
//                CLIEngine.ShowErrorMessage($"Error occured publishing the inventory item. Reason: {result.Message}");

//            return result;
//        }

//        public static async Task<OASISResult<IInventoryItem>> UnpublishInventoryItemAsync(Guid inventoryItemId, ProviderType providerType = ProviderType.Default)
//        {
//            OASISResult<IInventoryItem> result = await STAR.OASISAPI.Inventory.UnpublishInventoryItemAsync(inventoryItemId, STAR.BeamedInAvatar.Id, providerType);

//            if (result != null && !result.IsError && result.Result != null)
//                CLIEngine.ShowSuccessMessage("Inventory Item Successfully Unpublished.");
//            else
//                CLIEngine.ShowErrorMessage($"Error occured unpublishing the inventory item. Reason: {result.Message}");

//            return result;
//        }

//        public static async Task<OASISResult<IEnumerable<IInventoryItem>>> ListAllInventoryItemsForBeamedInAvatar(ProviderType providerType = ProviderType.Default)
//        {
//            OASISResult<IEnumerable<IInventoryItem>> result = await STAR.OASISAPI.Inventory.LoadAllInventoryItemsForAvatarAsync(STAR.BeamedInAvatar.Id, providerType);
//            ListInventoryItems(result);
//            return result;
//        }

//        public static async Task<OASISResult<IEnumerable<IInventoryItem>>> ListAllInventoryItems(ProviderType providerType = ProviderType.Default)
//        {
//            OASISResult<IEnumerable<IInventoryItem>> result = await STAR.OASISAPI.Inventory.LoadAllInventoryItemsAsync(providerType);
//            ListInventoryItems(result);
//            return result;
//        }

//        public static async Task SearchInventoryItemsAsync(string searchTerm, bool searchOnlyForCurrentAvatar = true, ProviderType providerType = ProviderType.Default)
//        {
//            ListInventoryItems(await STAR.OASISAPI.Inventory.SearchInventoryItemsAsync(searchTerm, STAR.BeamedInAvatar.Id, searchOnlyForCurrentAvatar, providerType));
//        }

//        public static async Task<OASISResult<IInventoryItem>> ShowInventoryItem(Guid inventoryItemId, ProviderType providerType = ProviderType.Default)
//        {
//            OASISResult<IInventoryItem> result = await STAR.OASISAPI.Inventory.LoadInventoryItemAsync(inventoryItemId, STAR.BeamedInAvatar.Id, providerType);

//            if (result != null && result.Result != null && !result.IsError)
//                ShowInventoryItem(result.Result);

//            return result;
//        }

//        public static void ShowInventoryItem(IInventoryItem inventoryItem)
//        {
//            CLIEngine.ShowMessage(string.Concat($"Id: ", inventoryItem.Id != Guid.Empty ? inventoryItem.Id : "None"));
//            CLIEngine.ShowMessage(string.Concat($"Name: ", !string.IsNullOrEmpty(inventoryItem.Name) ? inventoryItem.Name : "None"));
//            CLIEngine.ShowMessage(string.Concat($"Description: ", !string.IsNullOrEmpty(inventoryItem.Name) ? inventoryItem.Name : "None"));
//            CLIEngine.ShowMessage(string.Concat($"Created On: ", inventoryItem.CreatedDate != DateTime.MinValue ? inventoryItem.CreatedDate.ToString() : "None"));
//            CLIEngine.ShowMessage(string.Concat($"Created By: ", inventoryItem.CreatedByAvatarId != Guid.Empty ? string.Concat(inventoryItem.CreatedByAvatar.Username, " (", inventoryItem.CreatedByAvatarId.ToString(), ")") : "None"));
//            CLIEngine.ShowMessage(string.Concat($"Published On: ", inventoryItem.PublishedOn != DateTime.MinValue ? inventoryItem.PublishedOn.ToString() : "None"));
//            CLIEngine.ShowMessage(string.Concat($"Published By: ", inventoryItem.PublishedByAvatarId != Guid.Empty ? string.Concat(inventoryItem.PublishedByAvatar.Username, " (", inventoryItem.PublishedByAvatarId.ToString(), ")") : "None"));
//            CLIEngine.ShowMessage(string.Concat($"Version: ", inventoryItem.Version));
//        }

//        private static void ListInventoryItems(OASISResult<IEnumerable<IInventoryItem>> inventoryItems)
//        {
//            if (inventoryItems != null)
//            {
//                if (!inventoryItems.IsError)
//                {
//                    if (inventoryItems.Result != null && inventoryItems.Result.Count() > 0)
//                    {
//                        Console.WriteLine();

//                        if (inventoryItems.Result.Count() == 1)
//                            CLIEngine.ShowMessage($"{inventoryItems.Result.Count()} Inventory Item Found:");
//                        else
//                            CLIEngine.ShowMessage($"{inventoryItems.Result.Count()} Inventory Item's Found:");

//                        CLIEngine.ShowDivider();

//                        foreach (IOAPP oapp in inventoryItems.Result)
//                            ShowOAPP(oapp);
//                    }
//                    else
//                        CLIEngine.ShowWarningMessage("No Inventory Item's Found.");
//                }
//                else
//                    CLIEngine.ShowErrorMessage($"Error occured loading Inventory Item's. Reason: {inventoryItems.Message}");
//            }
//            else
//                CLIEngine.ShowErrorMessage($"Unknown error occured loading Inventory Item's.");
//        }
//    }
//}

