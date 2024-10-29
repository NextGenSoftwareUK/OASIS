using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.ONode.Core.Managers
{
    public class InventoryManager : PublishManagerBase
    {
        public InventoryManager(Guid avatarId, OASISDNA OASISDNA = null) : base(avatarId, OASISDNA) { }
        public InventoryManager(IOASISStorageProvider OASISStorageProvider, Guid avatarId, OASISDNA OASISDNA = null) : base(OASISStorageProvider, avatarId, OASISDNA) { }

        //TODO: Make all other Managers follow this pattern/design (so use Save instead of Create and Update)

        #region COSMICManagerBase
        public async Task<OASISResult<IInventoryItem>> SaveInventoryItemAsync(IInventoryItem inventoryItem, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IInventoryItem> result = new OASISResult<IInventoryItem>();
            OASISResult<InventoryItem> saveResult = await SaveHolonAsync<InventoryItem>(inventoryItem, avatarId, providerType, "InventoryManager.SaveInventoryItemAsync");
            result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(saveResult, result);
            result.Result = saveResult.Result;
            return result;
        }

        public OASISResult<IInventoryItem> SaveInventoryItem(IInventoryItem inventoryItem, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IInventoryItem> result = new OASISResult<IInventoryItem>();
            OASISResult<InventoryItem> saveResult = SaveHolon<InventoryItem>(inventoryItem, avatarId, providerType, "InventoryManager.SaveInventoryItem");
            result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(saveResult, result);
            result.Result = saveResult.Result;
            return result;
        }

        public async Task<OASISResult<IInventoryItem>> LoadInventoryItemAsync(Guid inventoryItemId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IInventoryItem> result = new OASISResult<IInventoryItem>();
            OASISResult<InventoryItem> loadResult = await LoadHolonAsync<InventoryItem>(inventoryItemId, providerType, "InventoryManager.LoadInventoryItemAsync");
            result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(loadResult, result);
            result.Result = loadResult.Result;
            return result;
        }

        public OASISResult<IInventoryItem> LoadInventoryItem(Guid inventoryItemId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IInventoryItem> result = new OASISResult<IInventoryItem>();
            OASISResult<InventoryItem> loadResult = LoadHolon<InventoryItem>(inventoryItemId, providerType, "InventoryManager.LoadInventoryItem");
            result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(loadResult, result);
            result.Result = loadResult.Result;
            return result;
        }

        public async Task<OASISResult<IEnumerable<IInventoryItem>>> LoadAllInventoryItemsAsync(ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IInventoryItem>> result = new OASISResult<IEnumerable<IInventoryItem>>();
            OASISResult<IEnumerable<InventoryItem>> loadHolonsResult = await LoadAllHolonsAsync<InventoryItem>(providerType, "InventoryManager.LoadAllInventoryItemsAsync", HolonType.InventoryItem);
            result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(loadHolonsResult, result);
            result.Result = loadHolonsResult.Result;
            return result;
        }

        public OASISResult<IEnumerable<IInventoryItem>> LoadAllInventoryItems(ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IInventoryItem>> result = new OASISResult<IEnumerable<IInventoryItem>>();
            OASISResult<IEnumerable<InventoryItem>> loadHolonsResult = LoadAllHolons<InventoryItem>(providerType, "InventoryManager.LoadAllInventoryItems", HolonType.InventoryItem);
            result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(loadHolonsResult, result);
            result.Result = loadHolonsResult.Result;
            return result;
        }

        public async Task<OASISResult<IEnumerable<IInventoryItem>>> LoadAllInventoryItemsForAvatarAsync(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IInventoryItem>> result = new OASISResult<IEnumerable<IInventoryItem>>();
            OASISResult<IEnumerable<InventoryItem>> loadHolonsResult = await LoadAllHolonsForAvatarAsync<InventoryItem>(avatarId, providerType, "InventoryManager.LoadAllInventoryItemsForAvatarAsync", HolonType.InventoryItem);
            result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(loadHolonsResult, result);
            result.Result = loadHolonsResult.Result;
            return result;
        }

        public OASISResult<IEnumerable<IInventoryItem>> LoadAllInventoryItemsForAvatar(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IInventoryItem>> result = new OASISResult<IEnumerable<IInventoryItem>>();
            OASISResult<IEnumerable<InventoryItem>> loadHolonsResult = LoadAllHolonsForAvatar<InventoryItem>(avatarId, providerType, "InventoryManager.LoadAllInventoryItemsForAvatarAsync", HolonType.InventoryItem);
            result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(loadHolonsResult, result);
            result.Result = loadHolonsResult.Result;
            return result;
        }

        public async Task<OASISResult<IInventoryItem>> DeleteInventoryItemAsync(Guid inventoryId, bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IInventoryItem> result = new OASISResult<IInventoryItem>();
            OASISResult<InventoryItem> loadHolonsResult = await DeleteHolonAsync<InventoryItem>(inventoryId, softDelete, providerType, "InventoryManager.DeleteInventoryItemAsync");
            result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(loadHolonsResult, result);
            result.Result = loadHolonsResult.Result;
            return result;
        }

        public OASISResult<IInventoryItem> DeleteInventoryItem(Guid inventoryId, bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IInventoryItem> result = new OASISResult<IInventoryItem>();
            OASISResult<InventoryItem> loadHolonsResult = DeleteHolon<InventoryItem>(inventoryId, softDelete, providerType, "InventoryManager.DeleteInventoryItem");
            result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(loadHolonsResult, result);
            result.Result = loadHolonsResult.Result;
            return result;
        }
        #endregion

        #region PublishManagerBase
        public async Task<OASISResult<IInventoryItem>> PublishInventoryItemAsync(Guid inventoryId, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IInventoryItem> result = new OASISResult<IInventoryItem>();
            OASISResult<InventoryItem> saveResult = await PublishHolonAsync<InventoryItem>(inventoryId, avatarId, "InventoryManager.PublishInventoryItemAsync", providerType);
            result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(saveResult, result);
            result.Result = saveResult.Result;
            return result;
        }

        public OASISResult<IInventoryItem> PublishInventoryItem(Guid inventoryId, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IInventoryItem> result = new OASISResult<IInventoryItem>();
            OASISResult<InventoryItem> saveResult = PublishHolon<InventoryItem>(inventoryId, avatarId, "InventoryManager.PublishInventoryItem", providerType);
            result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(saveResult, result);
            result.Result = saveResult.Result;
            return result;
        }

        public async Task<OASISResult<IInventoryItem>> PublishInventoryItemAsync(IInventoryItem inventoryItem, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IInventoryItem> result = new OASISResult<IInventoryItem>();
            OASISResult<InventoryItem> saveResult = await PublishHolonAsync<InventoryItem>(inventoryItem, avatarId, "InventoryManager.PublishInventoryItemAsync", providerType);
            result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(saveResult, result);
            result.Result = saveResult.Result;
            return result;
        }

        public OASISResult<IInventoryItem> PublishInventoryItem(IInventoryItem inventoryItem, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IInventoryItem> result = new OASISResult<IInventoryItem>();
            OASISResult<InventoryItem> saveResult = PublishHolon<InventoryItem>(inventoryItem, avatarId, "InventoryManager.PublishInventoryItem", providerType);
            result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(saveResult, result);
            result.Result = saveResult.Result;
            return result;
        }

        public async Task<OASISResult<IInventoryItem>> UnpublishInventoryItemAsync(Guid inventoryId, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IInventoryItem> result = new OASISResult<IInventoryItem>();
            OASISResult<InventoryItem> saveResult = await UnpublishHolonAsync<InventoryItem>(inventoryId, avatarId, "InventoryManager.UnpublishInventoryItemAsync", providerType);
            result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(saveResult, result);
            result.Result = saveResult.Result;
            return result;
        }

        public OASISResult<IInventoryItem> UnpublishInventoryItem(Guid inventoryId, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IInventoryItem> result = new OASISResult<IInventoryItem>();
            OASISResult<InventoryItem> saveResult = UnpublishHolon<InventoryItem>(inventoryId, avatarId, "InventoryManager.UnpublishInventoryItem", providerType);
            result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(saveResult, result);
            result.Result = saveResult.Result;
            return result;
        }

        public async Task<OASISResult<IInventoryItem>> UnpublishInventoryItemAsync(IInventoryItem inventoryItem, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IInventoryItem> result = new OASISResult<IInventoryItem>();
            OASISResult<InventoryItem> saveResult = await UnpublishHolonAsync<InventoryItem>(inventoryItem, avatarId, "InventoryManager.UnpublishInventoryItemAsync", providerType);
            result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(saveResult, result);
            result.Result = saveResult.Result;
            return result;
        }

        public OASISResult<IInventoryItem> UnpublishInventoryItem(IInventoryItem inventoryItem, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IInventoryItem> result = new OASISResult<IInventoryItem>();
            OASISResult<InventoryItem> saveResult = UnpublishHolon<InventoryItem>(inventoryItem, avatarId, "InventoryManager.UnpublishInventoryItem", providerType);
            result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(saveResult, result);
            result.Result = saveResult.Result;
            return result;
        }
        #endregion
    }
}