using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Helpers;

namespace NextGenSoftware.OASIS.API.Core.Managers
{
    public partial class AvatarManager
    {
        #region Avatar Inventory Management

        /// <summary>
        /// Gets all inventory items owned by the avatar
        /// This is the avatar's actual inventory (items they own), not items they created
        /// </summary>
        public async Task<OASISResult<IEnumerable<IInventoryItem>>> GetAvatarInventoryAsync(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            var result = new OASISResult<IEnumerable<IInventoryItem>>();
            try
            {
                var avatarDetailResult = await LoadAvatarDetailAsync(avatarId, providerType);
                if (avatarDetailResult.IsError || avatarDetailResult.Result == null)
                {
                    result.IsError = true;
                    result.Message = $"Error loading avatar detail: {avatarDetailResult.Message}";
                    return result;
                }

                result.Result = avatarDetailResult.Result.Inventory ?? new List<IInventoryItem>();
                result.Message = $"Retrieved {result.Result.Count()} inventory items";
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error getting avatar inventory: {ex.Message}";
                result.Exception = ex;
            }

            return result;
        }

        /// <summary>
        /// Gets all inventory items owned by the avatar (synchronous version)
        /// </summary>
        public OASISResult<IEnumerable<IInventoryItem>> GetAvatarInventory(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            return GetAvatarInventoryAsync(avatarId, providerType).Result;
        }

        /// <summary>
        /// Adds an item to the avatar's inventory
        /// The item can be from the STARNET store (created by anyone) or a new item
        /// </summary>
        public async Task<OASISResult<IInventoryItem>> AddItemToAvatarInventoryAsync(Guid avatarId, IInventoryItem item, ProviderType providerType = ProviderType.Default)
        {
            var result = new OASISResult<IInventoryItem>();
            if (item == null)
            {
                result.IsError = true;
                result.Message = "The inventory item is required. Please provide a valid Inventory Item object in the request body.";
                return result;
            }
            // Promote MetaData to first-class GameSource/ItemType so the holon persists them (STAR client may send only MetaData)
            if (item is IHolonBase holonBase && holonBase.MetaData != null)
            {
                if (string.IsNullOrWhiteSpace(item.ItemType) && holonBase.MetaData.TryGetValue("ItemType", out var typeObj) && typeObj != null)
                    item.ItemType = typeObj.ToString();
                if (string.IsNullOrWhiteSpace(item.GameSource) && holonBase.MetaData.TryGetValue("GameSource", out var gsObj) && gsObj != null)
                    item.GameSource = gsObj.ToString();
            }
            try
            {
                var avatarDetailResult = await LoadAvatarDetailAsync(avatarId, providerType);
                if (avatarDetailResult.IsError || avatarDetailResult.Result == null)
                {
                    result.IsError = true;
                    result.Message = $"Error loading avatar detail: {avatarDetailResult.Message}";
                    return result;
                }

                var avatarDetail = avatarDetailResult.Result;
                if (avatarDetail.Inventory == null)
                    avatarDetail.Inventory = new List<IInventoryItem>();

                // If client did not send an Id (e.g. add-by-name flows), assign a new one so we don't false-match on Guid.Empty
                if (item.Id == Guid.Empty)
                    item.Id = Guid.NewGuid();

                int addQty = item.Quantity > 0 ? item.Quantity : 1;
                bool stack = item.Stack;

                // Check if item already exists by ID (client sent an explicit id that is already in inventory)
                var existingById = avatarDetail.Inventory.FirstOrDefault(i => i.Id == item.Id);
                if (existingById != null)
                {
                    result.IsError = true;
                    result.Message = "Item already exists in avatar inventory";
                    result.Result = existingById;
                    return result;
                }

                // If same name exists: stack = increment quantity; !stack = return error
                var existingByName = avatarDetail.Inventory.FirstOrDefault(i =>
                    i.Name?.Equals(item.Name, StringComparison.OrdinalIgnoreCase) == true);
                if (existingByName != null)
                {
                    if (!stack)
                    {
                        result.IsError = true;
                        result.Message = "Item already exists";
                        result.Result = existingByName;
                        return result;
                    }
                    int existingQty = existingByName.Quantity > 0 ? existingByName.Quantity : 1;
                    existingByName.Quantity = existingQty + addQty;
                    result.Result = existingByName;
                    result.Message = $"Item quantity updated; total quantity: {existingByName.Quantity}";
                    var saveResult = await SaveAvatarDetailAsync(avatarDetail);
                    if (saveResult.IsError)
                    {
                        result.IsError = true;
                        result.Message = $"Error saving avatar detail: {saveResult.Message}";
                        return result;
                    }
                    return result;
                }

                // New item: set quantity and add
                item.Quantity = addQty;
                avatarDetail.Inventory.Add(item);

                // Save the updated avatar detail
                var addSaveResult = await SaveAvatarDetailAsync(avatarDetail);
                if (addSaveResult.IsError)
                {
                    result.IsError = true;
                    result.Message = $"Error saving avatar detail: {addSaveResult.Message}";
                    return result;
                }

                result.Result = item;
                result.Message = "Item added to avatar inventory successfully";
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error adding item to avatar inventory: {ex.Message}";
                result.Exception = ex;
            }

            return result;
        }

        /// <summary>
        /// Adds an item to the avatar's inventory (synchronous version)
        /// </summary>
        public OASISResult<IInventoryItem> AddItemToAvatarInventory(Guid avatarId, IInventoryItem item, ProviderType providerType = ProviderType.Default)
        {
            return AddItemToAvatarInventoryAsync(avatarId, item, providerType).Result;
        }

        /// <summary>
        /// Removes an item from the avatar's inventory
        /// </summary>
        public async Task<OASISResult<bool>> RemoveItemFromAvatarInventoryAsync(Guid avatarId, Guid itemId, ProviderType providerType = ProviderType.Default)
        {
            var result = new OASISResult<bool>();
            try
            {
                var avatarDetailResult = await LoadAvatarDetailAsync(avatarId, providerType);
                if (avatarDetailResult.IsError || avatarDetailResult.Result == null)
                {
                    result.IsError = true;
                    result.Message = $"Error loading avatar detail: {avatarDetailResult.Message}";
                    return result;
                }

                var avatarDetail = avatarDetailResult.Result;
                if (avatarDetail.Inventory == null || avatarDetail.Inventory.Count == 0)
                {
                    result.IsError = true;
                    result.Message = "Avatar inventory is empty";
                    return result;
                }

                // Find and remove the item
                var itemToRemove = avatarDetail.Inventory.FirstOrDefault(i => i.Id == itemId);
                if (itemToRemove == null)
                {
                    result.IsError = true;
                    result.Message = "Item not found in avatar inventory";
                    return result;
                }

                avatarDetail.Inventory.Remove(itemToRemove);

                // Save the updated avatar detail
                var saveResult = await SaveAvatarDetailAsync(avatarDetail);
                if (saveResult.IsError)
                {
                    result.IsError = true;
                    result.Message = $"Error saving avatar detail: {saveResult.Message}";
                    return result;
                }

                result.Result = true;
                result.Message = "Item removed from avatar inventory successfully";
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error removing item from avatar inventory: {ex.Message}";
                result.Exception = ex;
            }

            return result;
        }

        /// <summary>
        /// Removes an item from the avatar's inventory (synchronous version)
        /// </summary>
        public OASISResult<bool> RemoveItemFromAvatarInventory(Guid avatarId, Guid itemId, ProviderType providerType = ProviderType.Default)
        {
            return RemoveItemFromAvatarInventoryAsync(avatarId, itemId, providerType).Result;
        }

        /// <summary>
        /// Checks if the avatar has a specific item in their inventory
        /// </summary>
        public async Task<OASISResult<bool>> AvatarHasItemAsync(Guid avatarId, Guid itemId, ProviderType providerType = ProviderType.Default)
        {
            var result = new OASISResult<bool>();
            try
            {
                var inventoryResult = await GetAvatarInventoryAsync(avatarId, providerType);
                if (inventoryResult.IsError)
                {
                    result.IsError = true;
                    result.Message = inventoryResult.Message;
                    return result;
                }

                result.Result = inventoryResult.Result?.Any(i => i.Id == itemId) ?? false;
                result.Message = result.Result ? "Avatar has the item" : "Avatar does not have the item";
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error checking if avatar has item: {ex.Message}";
                result.Exception = ex;
            }

            return result;
        }

        /// <summary>
        /// Checks if the avatar has a specific item in their inventory (synchronous version)
        /// </summary>
        public OASISResult<bool> AvatarHasItem(Guid avatarId, Guid itemId, ProviderType providerType = ProviderType.Default)
        {
            return AvatarHasItemAsync(avatarId, itemId, providerType).Result;
        }

        /// <summary>
        /// Checks if the avatar has a specific item by name in their inventory
        /// </summary>
        public async Task<OASISResult<bool>> AvatarHasItemByNameAsync(Guid avatarId, string itemName, ProviderType providerType = ProviderType.Default)
        {
            var result = new OASISResult<bool>();
            try
            {
                var inventoryResult = await GetAvatarInventoryAsync(avatarId, providerType);
                if (inventoryResult.IsError)
                {
                    result.IsError = true;
                    result.Message = inventoryResult.Message;
                    return result;
                }

                result.Result = inventoryResult.Result?.Any(i => 
                    i.Name?.Equals(itemName, StringComparison.OrdinalIgnoreCase) == true ||
                    i.Description?.Contains(itemName, StringComparison.OrdinalIgnoreCase) == true
                ) ?? false;

                result.Message = result.Result ? $"Avatar has item '{itemName}'" : $"Avatar does not have item '{itemName}'";
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error checking if avatar has item by name: {ex.Message}";
                result.Exception = ex;
            }

            return result;
        }

        /// <summary>
        /// Checks if the avatar has a specific item by name in their inventory (synchronous version)
        /// </summary>
        public OASISResult<bool> AvatarHasItemByName(Guid avatarId, string itemName, ProviderType providerType = ProviderType.Default)
        {
            return AvatarHasItemByNameAsync(avatarId, itemName, providerType).Result;
        }

        /// <summary>
        /// Searches the avatar's inventory by name or description
        /// </summary>
        public async Task<OASISResult<IEnumerable<IInventoryItem>>> SearchAvatarInventoryAsync(Guid avatarId, string searchTerm, ProviderType providerType = ProviderType.Default)
        {
            var result = new OASISResult<IEnumerable<IInventoryItem>>();
            try
            {
                var inventoryResult = await GetAvatarInventoryAsync(avatarId, providerType);
                if (inventoryResult.IsError)
                {
                    result.IsError = true;
                    result.Message = inventoryResult.Message;
                    return result;
                }

                var matchingItems = inventoryResult.Result?.Where(i =>
                    i.Name?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) == true ||
                    i.Description?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) == true
                ).ToList() ?? new List<IInventoryItem>();

                result.Result = matchingItems;
                result.Message = $"Found {matchingItems.Count} matching items";
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error searching avatar inventory: {ex.Message}";
                result.Exception = ex;
            }

            return result;
        }

        /// <summary>
        /// Searches the avatar's inventory by name or description (synchronous version)
        /// </summary>
        public OASISResult<IEnumerable<IInventoryItem>> SearchAvatarInventory(Guid avatarId, string searchTerm, ProviderType providerType = ProviderType.Default)
        {
            return SearchAvatarInventoryAsync(avatarId, searchTerm, providerType).Result;
        }

        /// <summary>
        /// Gets a specific item from the avatar's inventory by ID
        /// </summary>
        public async Task<OASISResult<IInventoryItem>> GetAvatarInventoryItemAsync(Guid avatarId, Guid itemId, ProviderType providerType = ProviderType.Default)
        {
            var result = new OASISResult<IInventoryItem>();
            try
            {
                var inventoryResult = await GetAvatarInventoryAsync(avatarId, providerType);
                if (inventoryResult.IsError)
                {
                    result.IsError = true;
                    result.Message = inventoryResult.Message;
                    return result;
                }

                var item = inventoryResult.Result?.FirstOrDefault(i => i.Id == itemId);
                if (item == null)
                {
                    result.IsError = true;
                    result.Message = "Item not found in avatar inventory";
                    return result;
                }

                result.Result = item;
                result.Message = "Item retrieved successfully";
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error getting avatar inventory item: {ex.Message}";
                result.Exception = ex;
            }

            return result;
        }

        /// <summary>
        /// Gets a specific item from the avatar's inventory by ID (synchronous version)
        /// </summary>
        public OASISResult<IInventoryItem> GetAvatarInventoryItem(Guid avatarId, Guid itemId, ProviderType providerType = ProviderType.Default)
        {
            return GetAvatarInventoryItemAsync(avatarId, itemId, providerType).Result;
        }

        /// <summary>
        /// Sends an item from the sender's inventory to another avatar (or to a clan when forClan is true).
        /// Target can be username or avatar Id (as string). When itemId is provided, that specific item is sent; otherwise items are matched by name.
        /// When forClan is true, target is treated as clan name and result messages use "clan" wording (e.g. "Clan not found").
        /// </summary>
        public async Task<OASISResult<bool>> SendItemToAvatarAsync(Guid senderAvatarId, string targetUsernameOrAvatarId, string itemName, int quantity = 1, Guid? itemId = null, ProviderType providerType = ProviderType.Default, bool forClan = false)
        {
            var result = new OASISResult<bool>();
            if (string.IsNullOrWhiteSpace(targetUsernameOrAvatarId) || string.IsNullOrWhiteSpace(itemName))
            {
                result.IsError = true;
                result.Message = forClan ? "Clan name and item name are required." : "Target (username or avatar id) and item name are required.";
                return result;
            }
            if (quantity < 1) quantity = 1;
            try
            {
                var senderDetailResult = await LoadAvatarDetailAsync(senderAvatarId, providerType);
                if (senderDetailResult.IsError || senderDetailResult.Result == null)
                {
                    result.IsError = true;
                    result.Message = $"Error loading sender avatar: {senderDetailResult.Message}";
                    return result;
                }
                var senderDetail = senderDetailResult.Result;
                if (senderDetail.Inventory == null || senderDetail.Inventory.Count == 0)
                {
                    result.IsError = true;
                    result.Message = "Sender inventory is empty.";
                    return result;
                }
                List<IInventoryItem> matching;
                if (itemId.HasValue && itemId.Value != Guid.Empty)
                {
                    var byId = senderDetail.Inventory.FirstOrDefault(i => i.Id == itemId.Value);
                    if (byId == null)
                    {
                        result.IsError = true;
                        result.Message = $"Item with Id {itemId.Value} not found in sender inventory.";
                        return result;
                    }
                    matching = new List<IInventoryItem> { byId };
                    quantity = 1;
                }
                else
                {
                    matching = senderDetail.Inventory
                        .Where(i => i.Name?.Equals(itemName, StringComparison.OrdinalIgnoreCase) == true)
                        .Take(quantity)
                        .ToList();
                }
                if (matching.Count == 0)
                {
                    result.IsError = true;
                    result.Message = $"Item '{itemName}' not found in sender inventory or insufficient quantity.";
                    return result;
                }
                Guid targetAvatarId;
                if (Guid.TryParse(targetUsernameOrAvatarId.Trim(), out var parsedId) && parsedId != Guid.Empty)
                {
                    targetAvatarId = parsedId;
                }
                else
                {
                    var targetAvatarResult = await LoadAvatarAsync(targetUsernameOrAvatarId.Trim(), false, true, providerType);
                    if (targetAvatarResult.IsError || targetAvatarResult.Result == null)
                    {
                        result.IsError = true;
                        result.Message = forClan ? $"Clan not found: {targetUsernameOrAvatarId}" : $"Target avatar not found: {targetUsernameOrAvatarId}";
                        return result;
                    }
                    targetAvatarId = targetAvatarResult.Result.Id;
                }
                if (targetAvatarId == senderAvatarId)
                {
                    result.IsError = true;
                    result.Message = "Cannot send item to yourself.";
                    return result;
                }
                var template = matching[0];
                foreach (var item in matching)
                {
                    senderDetail.Inventory.Remove(item);
                }
                var saveSenderResult = await SaveAvatarDetailAsync(senderDetail);
                if (saveSenderResult.IsError)
                {
                    result.IsError = true;
                    result.Message = $"Error updating sender inventory: {saveSenderResult.Message}";
                    return result;
                }
                var targetDetailResult = await LoadAvatarDetailAsync(targetAvatarId, providerType);
                if (targetDetailResult.IsError || targetDetailResult.Result == null)
                {
                    result.IsError = true;
                    result.Message = forClan ? $"Error loading clan: {targetDetailResult.Message}" : $"Error loading target avatar: {targetDetailResult.Message}";
                    return result;
                }
                var targetDetail = targetDetailResult.Result;
                if (targetDetail.Inventory == null) targetDetail.Inventory = new List<IInventoryItem>();
                for (int i = 0; i < matching.Count; i++)
                {
                    var newItem = new InventoryItem
                    {
                        Id = Guid.NewGuid(),
                        Name = template.Name,
                        Description = template.Description,
                        HolonType = template.HolonType,
                        MetaData = template.MetaData != null ? new Dictionary<string, object>(template.MetaData) : null,
                        Quantity = 1
                    };
                    targetDetail.Inventory.Add(newItem);
                }
                var saveTargetResult = await SaveAvatarDetailAsync(targetDetail);
                if (saveTargetResult.IsError)
                {
                    result.IsError = true;
                    result.Message = forClan ? $"Error updating clan inventory: {saveTargetResult.Message}" : $"Error updating target inventory: {saveTargetResult.Message}";
                    return result;
                }
                result.Result = true;
                result.Message = forClan ? $"Sent {matching.Count} x '{itemName}' to clan." : $"Sent {matching.Count} x '{itemName}' to avatar.";
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = forClan ? $"Error sending item to clan: {ex.Message}" : $"Error sending item to avatar: {ex.Message}";
                result.Exception = ex;
            }
            return result;
        }

        /// <summary>
        /// Sends an item from the sender's inventory to a clan (clan treasury).
        /// Resolves clan by name via ClanManager; uses clan-specific result messages.
        /// </summary>
        public async Task<OASISResult<bool>> SendItemToClanAsync(Guid senderAvatarId, string clanName, string itemName, int quantity = 1, Guid? itemId = null, ProviderType providerType = ProviderType.Default)
        {
            if (string.IsNullOrWhiteSpace(clanName))
            {
                return new OASISResult<bool> { IsError = true, Message = "Clan name is required." };
            }

            var clanResult = await ClanManager.Instance.LoadClanByNameAsync(clanName.Trim(), providerType).ConfigureAwait(false);
            if (clanResult.IsError || clanResult.Result == null)
            {
                return new OASISResult<bool> { IsError = true, Message = clanResult.Message ?? "Clan not found." };
            }

            return await ClanManager.Instance.SendItemToClanAsync(senderAvatarId, clanResult.Result.Id, itemName.Trim(), quantity, itemId, providerType).ConfigureAwait(false);
        }

        #endregion
    }
}



