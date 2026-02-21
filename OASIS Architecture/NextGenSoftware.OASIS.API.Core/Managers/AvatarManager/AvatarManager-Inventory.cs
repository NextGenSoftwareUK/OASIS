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

                // Check if item already exists by ID (client sent an explicit id that is already in inventory)
                var existingById = avatarDetail.Inventory.FirstOrDefault(i => i.Id == item.Id);
                if (existingById != null)
                {
                    result.IsError = true;
                    result.Message = "Item already exists in avatar inventory";
                    result.Result = existingById;
                    return result;
                }

                // Optionally treat add-by-name as idempotent: if same name exists, return success with existing item
                var existingByName = avatarDetail.Inventory.FirstOrDefault(i =>
                    i.Name?.Equals(item.Name, StringComparison.OrdinalIgnoreCase) == true);
                if (existingByName != null)
                {
                    result.Result = existingByName;
                    result.Message = "Item already exists in avatar inventory (matched by name)";
                    return result;
                }

                // Add the item
                avatarDetail.Inventory.Add(item);

                // Save the updated avatar detail
                var saveResult = await SaveAvatarDetailAsync(avatarDetail);
                if (saveResult.IsError)
                {
                    result.IsError = true;
                    result.Message = $"Error saving avatar detail: {saveResult.Message}";
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

        #endregion
    }
}



