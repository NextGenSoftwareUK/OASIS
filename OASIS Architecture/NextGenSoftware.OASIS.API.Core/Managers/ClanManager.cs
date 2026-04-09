using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Avatar;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.Managers
{
    /// <summary>
    /// Manager for Clan entities: create, update, load, list, delete, send item to clan, add/remove avatar.
    /// </summary>
    public class ClanManager
    {
        private static ClanManager _instance;

        public static ClanManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new ClanManager();
                return _instance;
            }
        }

        public async Task<OASISResult<IClan>> CreateClanAsync(Guid ownerAvatarId, string name, string description = null, ProviderType providerType = ProviderType.Default)
        {
            var result = new OASISResult<IClan>();
            if (string.IsNullOrWhiteSpace(name))
            {
                result.IsError = true;
                result.Message = "Clan name is required.";
                return result;
            }

            var byName = await LoadClanByNameAsync(name.Trim(), providerType);
            if (byName.Result != null)
            {
                result.IsError = true;
                result.Message = "A clan with that name already exists.";
                return result;
            }

            var clan = new Clan
            {
                Name = name.Trim(),
                Description = description?.Trim() ?? "",
                OwnerAvatarId = ownerAvatarId,
                MemberIds = new List<Guid> { ownerAvatarId },
                Inventory = new List<IInventoryItem>()
            };

            var saveResult = await HolonManager.Instance.SaveHolonAsync(clan, true, true, 0, true, false, providerType);
            if (saveResult.IsError || saveResult.Result == null)
            {
                result.IsError = true;
                result.Message = saveResult.Message ?? "Failed to create clan.";
                return result;
            }

            result.Result = saveResult.Result as IClan;
            result.Message = "Clan created.";
            return result;
        }

        public async Task<OASISResult<IClan>> UpdateClanAsync(IClan clan, ProviderType providerType = ProviderType.Default)
        {
            var result = new OASISResult<IClan>();
            if (clan == null || clan.Id == Guid.Empty)
            {
                result.IsError = true;
                result.Message = "Valid clan is required.";
                return result;
            }

            var saveResult = await HolonManager.Instance.SaveHolonAsync(clan, true, true, 0, true, false, providerType);
            if (saveResult.IsError || saveResult.Result == null)
            {
                result.IsError = true;
                result.Message = saveResult.Message ?? "Failed to update clan.";
                return result;
            }

            result.Result = saveResult.Result as IClan;
            result.Message = "Clan updated.";
            return result;
        }

        public async Task<OASISResult<IClan>> LoadClanAsync(Guid clanId, ProviderType providerType = ProviderType.Default)
        {
            var result = new OASISResult<IClan>();
            var holonResult = await HolonManager.Instance.LoadHolonAsync<Clan>(clanId, false, false, 0, true, false, HolonType.Clan, 0, providerType);
            if (holonResult.IsError || holonResult.Result == null)
            {
                result.IsError = true;
                result.Message = holonResult.Message ?? "Clan not found.";
                return result;
            }

            result.Result = holonResult.Result;
            return result;
        }

        /// <summary>Load clan by name (case-insensitive). Uses list then filter.</summary>
        public async Task<OASISResult<IClan>> LoadClanByNameAsync(string name, ProviderType providerType = ProviderType.Default)
        {
            var result = new OASISResult<IClan>();
            if (string.IsNullOrWhiteSpace(name))
            {
                result.IsError = true;
                result.Message = "Clan name is required.";
                return result;
            }

            var listResult = await HolonManager.Instance.LoadAllHolonsAsync<Clan>(HolonType.Clan, false, false, 0, true, false, HolonType.Clan, 0, providerType);
            if (listResult.IsError || listResult.Result == null)
            {
                result.IsError = true;
                result.Message = listResult.Message ?? "Failed to list clans.";
                return result;
            }

            var clan = listResult.Result.FirstOrDefault(c => string.Equals(c.Name, name.Trim(), StringComparison.OrdinalIgnoreCase));
            if (clan == null)
            {
                result.IsError = true;
                result.Message = "Clan not found.";
                return result;
            }

            result.Result = clan;
            return result;
        }

        public async Task<OASISResult<IEnumerable<IClan>>> ListClansAsync(Guid? ownerAvatarId = null, ProviderType providerType = ProviderType.Default)
        {
            var result = new OASISResult<IEnumerable<IClan>>();
            var listResult = await HolonManager.Instance.LoadAllHolonsAsync<Clan>(HolonType.Clan, false, false, 0, true, false, HolonType.Clan, 0, providerType);
            if (listResult.IsError)
            {
                result.IsError = true;
                result.Message = listResult.Message ?? "Failed to list clans.";
                return result;
            }

            var list = (listResult.Result ?? Array.Empty<Clan>()).Cast<IClan>();
            if (ownerAvatarId.HasValue && ownerAvatarId.Value != Guid.Empty)
                list = list.Where(c => c.OwnerAvatarId == ownerAvatarId.Value);

            result.Result = list;
            return result;
        }

        public async Task<OASISResult<bool>> DeleteClanAsync(Guid clanId, ProviderType providerType = ProviderType.Default)
        {
            var result = new OASISResult<bool>();
            var loadResult = await LoadClanAsync(clanId, providerType);
            if (loadResult.IsError || loadResult.Result == null)
            {
                result.IsError = true;
                result.Message = loadResult.Message ?? "Clan not found.";
                return result;
            }

            var clanToDelete = loadResult.Result;
            var deleteResult = await HolonManager.Instance.DeleteHolonAsync(clanToDelete.Id, clanToDelete.OwnerAvatarId, true, providerType);
            if (deleteResult.IsError)
            {
                result.IsError = true;
                result.Message = deleteResult.Message ?? "Failed to delete clan.";
                return result;
            }

            result.Result = true;
            result.Message = "Clan deleted.";
            return result;
        }

        /// <summary>Send item from sender's inventory to clan treasury.</summary>
        public async Task<OASISResult<bool>> SendItemToClanAsync(Guid senderAvatarId, Guid clanId, string itemName, int quantity = 1, Guid? itemId = null, ProviderType providerType = ProviderType.Default)
        {
            var result = new OASISResult<bool>();
            if (quantity < 1) quantity = 1;

            var clanResult = await LoadClanAsync(clanId, providerType);
            if (clanResult.IsError || clanResult.Result == null)
            {
                result.IsError = true;
                result.Message = clanResult.Message ?? "Clan not found.";
                return result;
            }

            var senderDetailResult = await AvatarManager.Instance.LoadAvatarDetailAsync(senderAvatarId, providerType);
            if (senderDetailResult.IsError || senderDetailResult.Result == null)
            {
                result.IsError = true;
                result.Message = "Error loading sender inventory.";
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
                    result.Message = "Item not found in sender inventory.";
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
                result.Message = "Item not found in sender inventory or insufficient quantity.";
                return result;
            }

            foreach (var item in matching)
                senderDetail.Inventory.Remove(item);

            var saveSenderResult = await AvatarManager.Instance.SaveAvatarDetailAsync(senderDetail, AutoReplicationMode.UseGlobalDefaultInOASISDNA, AutoFailOverMode.UseGlobalDefaultInOASISDNA, AutoLoadBalanceMode.UseGlobalDefaultInOASISDNA, false, providerType);
            if (saveSenderResult.IsError)
            {
                result.IsError = true;
                result.Message = "Error updating sender inventory.";
                return result;
            }

            var clan = clanResult.Result;
            if (clan.Inventory == null)
                clan.Inventory = new List<IInventoryItem>();

            var template = matching[0];
            for (int i = 0; i < matching.Count; i++)
            {
                clan.Inventory.Add(new InventoryItem
                {
                    Id = Guid.NewGuid(),
                    Name = template.Name,
                    Description = template.Description,
                    HolonType = HolonType.InventoryItem,
                    MetaData = template.MetaData != null ? new Dictionary<string, object>(template.MetaData) : null
                });
            }

            var saveClanResult = await HolonManager.Instance.SaveHolonAsync(clan as IHolon, true, true, 0, true, false, providerType);
            if (saveClanResult.IsError)
            {
                result.IsError = true;
                result.Message = "Error updating clan inventory.";
                return result;
            }

            result.Result = true;
            result.Message = $"Sent {matching.Count} x '{itemName}' to clan.";
            return result;
        }

        public async Task<OASISResult<bool>> AddAvatarToClanAsync(Guid clanId, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            var result = new OASISResult<bool>();
            var clanResult = await LoadClanAsync(clanId, providerType);
            if (clanResult.IsError || clanResult.Result == null)
            {
                result.IsError = true;
                result.Message = clanResult.Message ?? "Clan not found.";
                return result;
            }

            var clan = clanResult.Result;
            if (clan.MemberIds == null)
                clan.MemberIds = new List<Guid>();

            if (clan.MemberIds.Contains(avatarId))
            {
                result.Result = true;
                result.Message = "Avatar is already a member.";
                return result;
            }

            clan.MemberIds.Add(avatarId);
            var saveResult = await UpdateClanAsync(clan, providerType);
            if (saveResult.IsError)
            {
                result.IsError = true;
                result.Message = saveResult.Message ?? "Failed to add avatar to clan.";
                return result;
            }

            result.Result = true;
            result.Message = "Avatar added to clan.";
            return result;
        }

        public async Task<OASISResult<bool>> RemoveAvatarFromClanAsync(Guid clanId, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            var result = new OASISResult<bool>();
            var clanResult = await LoadClanAsync(clanId, providerType);
            if (clanResult.IsError || clanResult.Result == null)
            {
                result.IsError = true;
                result.Message = clanResult.Message ?? "Clan not found.";
                return result;
            }

            var clan = clanResult.Result;
            if (clan.MemberIds == null || !clan.MemberIds.Contains(avatarId))
            {
                result.Result = true;
                result.Message = "Avatar is not a member.";
                return result;
            }

            if (clan.OwnerAvatarId == avatarId)
            {
                result.IsError = true;
                result.Message = "Cannot remove the clan owner. Transfer ownership or delete the clan.";
                return result;
            }

            clan.MemberIds.Remove(avatarId);
            var saveResult = await UpdateClanAsync(clan, providerType);
            if (saveResult.IsError)
            {
                result.IsError = true;
                result.Message = saveResult.Message ?? "Failed to remove avatar from clan.";
                return result;
            }

            result.Result = true;
            result.Message = "Avatar removed from clan.";
            return result;
        }

        /// <summary>Get list of avatar Ids that are members of the clan.</summary>
        public async Task<OASISResult<IEnumerable<Guid>>> GetClanMembersAsync(Guid clanId, ProviderType providerType = ProviderType.Default)
        {
            var result = new OASISResult<IEnumerable<Guid>>();
            var clanResult = await LoadClanAsync(clanId, providerType);
            if (clanResult.IsError || clanResult.Result == null)
            {
                result.IsError = true;
                result.Message = clanResult.Message ?? "Clan not found.";
                return result;
            }

            result.Result = clanResult.Result.MemberIds ?? new List<Guid>();
            return result;
        }
    }
}
