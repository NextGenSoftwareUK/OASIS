using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects.Game;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Managers;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers.Base;
using NextGenSoftware.OASIS.STAR.DNA;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Managers
{
    public partial class GameManager : STARNETManagerBase<Game, DownloadedGame, InstalledGame, STARNETDNA>, IGameManager
    {

        /// <summary>
        /// Gets the sound volume
        /// </summary>
        public async Task<OASISResult<double>> GetSoundVolumeAsync(Guid gameId, Guid avatarId)
        {
            var result = new OASISResult<double>();
            try
            {
                var session = _activeSessions.Values.FirstOrDefault(s => s.GameId == gameId && s.AvatarId == avatarId);
                if (session != null)
                {
                    result.Result = session.SoundVolume;
                }
                else
                {
                    var settingsResult = await HolonManager.Instance.GetAllSettingsAsync(avatarId, "game");
                    if (!settingsResult.IsError && settingsResult.Result != null && settingsResult.Result.ContainsKey("soundVolume"))
                    {
                        result.Result = Convert.ToDouble(settingsResult.Result["soundVolume"]);
                    }
                    else
                    {
                        result.Result = 1.0;
                    }
                }

                result.Message = "Sound volume retrieved successfully";
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error getting sound volume: {ex.Message}";
                result.Exception = ex;
            }

            return result;
        }



        /// <summary>
        /// Sets the video quality setting
        /// </summary>
        public async Task<OASISResult<bool>> SetVideoSettingAsync(Guid gameId, Guid avatarId, VideoSetting setting)
        {
            var result = new OASISResult<bool>();
            try
            {
                var session = _activeSessions.Values.FirstOrDefault(s => s.GameId == gameId && s.AvatarId == avatarId);
                if (session != null)
                {
                    session.VideoSetting = setting;
                }

                await HolonManager.Instance.SaveSettingsAsync(avatarId, "game", new Dictionary<string, object> { { "videoSetting", setting.ToString() } });

                result.Result = true;
                result.Message = $"Video setting set to {setting}";
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error setting video setting: {ex.Message}";
                result.Exception = ex;
            }

            return result;
        }

        /// <summary>
        /// Gets the video quality setting
        /// </summary>
        public async Task<OASISResult<VideoSetting>> GetVideoSettingAsync(Guid gameId, Guid avatarId)
        {
            var result = new OASISResult<VideoSetting>();
            try
            {
                var session = _activeSessions.Values.FirstOrDefault(s => s.GameId == gameId && s.AvatarId == avatarId);
                if (session != null)
                {
                    result.Result = session.VideoSetting;
                }
                else
                {
                    var settingsResult = await HolonManager.Instance.GetAllSettingsAsync(avatarId, "game");
                    if (!settingsResult.IsError && settingsResult.Result != null && settingsResult.Result.ContainsKey("videoSetting"))
                    {
                        Enum.TryParse<VideoSetting>(settingsResult.Result["videoSetting"].ToString(), out var setting);
                        result.Result = setting;
                    }
                    else
                    {
                        result.Result = VideoSetting.Medium; // Default
                    }
                }

                result.Message = "Video setting retrieved successfully";
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error getting video setting: {ex.Message}";
                result.Exception = ex;
            }

            return result;
        }



        /// <summary>
        /// Binds keys to actions
        /// </summary>
        public async Task<OASISResult<bool>> BindKeysAsync(Guid gameId, Guid avatarId, Dictionary<string, string> keyBindings)
        {
            var result = new OASISResult<bool>();
            try
            {
                var session = _activeSessions.Values.FirstOrDefault(s => s.GameId == gameId && s.AvatarId == avatarId);
                if (session != null)
                {
                    foreach (var binding in keyBindings)
                    {
                        session.KeyBindings[binding.Key] = binding.Value;
                    }
                }

                // Save to avatar settings
                await HolonManager.Instance.SaveSettingsAsync(avatarId, "game", new Dictionary<string, object> { { "keyBindings", keyBindings } });

                result.Result = true;
                result.Message = "Keys bound successfully";
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error binding keys: {ex.Message}";
                result.Exception = ex;
            }

            return result;
        }

        /// <summary>
        /// Gets current key bindings
        /// </summary>
        public async Task<OASISResult<Dictionary<string, string>>> GetKeyBindingsAsync(Guid gameId, Guid avatarId)
        {
            var result = new OASISResult<Dictionary<string, string>>();
            try
            {
                var session = _activeSessions.Values.FirstOrDefault(s => s.GameId == gameId && s.AvatarId == avatarId);
                if (session != null && session.KeyBindings.Count > 0)
                {
                    result.Result = session.KeyBindings;
                }
                else
                {
                    var settingsResult = await HolonManager.Instance.GetAllSettingsAsync(avatarId, "game");
                    if (!settingsResult.IsError && settingsResult.Result != null && settingsResult.Result.ContainsKey("keyBindings"))
                    {
                        result.Result = (Dictionary<string, string>)settingsResult.Result["keyBindings"];
                    }
                    else
                    {
                        result.Result = new Dictionary<string, string>(); // Default empty
                    }
                }

                result.Message = "Key bindings retrieved successfully";
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error getting key bindings: {ex.Message}";
                result.Exception = ex;
            }

            return result;
        }



        /// <summary>
        /// Gets shared inventory items (keycards, items, etc.) that can be used across games, apps, websites, services
        /// Uses the AvatarDetail.Inventory property - the avatar's actual owned inventory
        /// This inventory is shared across ALL games, apps, websites, and services - enabling true cross-platform interoperability
        /// </summary>
        public async Task<OASISResult<List<IInventoryItem>>> GetSharedAssetsAsync(Guid avatarId)
        {
            var result = new OASISResult<List<IInventoryItem>>();
            try
            {
                // Get inventory from AvatarDetail - this is the avatar's actual owned inventory
                var inventoryResult = await AvatarManager.Instance.GetAvatarInventoryAsync(avatarId);
                
                if (inventoryResult.IsError)
                {
                    result.IsError = true;
                    result.Message = $"Error loading avatar inventory: {inventoryResult.Message}";
                    return result;
                }

                result.Result = inventoryResult.Result?.ToList() ?? new List<IInventoryItem>();
                result.Message = $"Retrieved {result.Result.Count} shared inventory items for avatar";
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error getting shared assets: {ex.Message}";
                result.Exception = ex;
            }

            return result;
        }

        /// <summary>
        /// Adds an item to the avatar's shared inventory (can be used across all games, apps, websites, services)
        /// Uses AvatarManager to add to AvatarDetail.Inventory
        /// </summary>
        public async Task<OASISResult<IInventoryItem>> AddItemToInventoryAsync(Guid avatarId, IInventoryItem item)
        {
            var result = new OASISResult<IInventoryItem>();
            try
            {
                var addResult = await AvatarManager.Instance.AddItemToAvatarInventoryAsync(avatarId, item);
                
                if (addResult.IsError)
                {
                    result.IsError = true;
                    result.Message = addResult.Message;
                    return result;
                }

                result.Result = addResult.Result;
                result.Message = "Item added to shared inventory successfully";
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error adding item to inventory: {ex.Message}";
                result.Exception = ex;
            }

            return result;
        }

        /// <summary>
        /// Removes an item from the avatar's shared inventory
        /// Uses AvatarManager to remove from AvatarDetail.Inventory
        /// </summary>
        public async Task<OASISResult<bool>> RemoveItemFromInventoryAsync(Guid avatarId, Guid itemId)
        {
            var result = new OASISResult<bool>();
            try
            {
                var removeResult = await AvatarManager.Instance.RemoveItemFromAvatarInventoryAsync(avatarId, itemId);
                
                if (removeResult.IsError)
                {
                    result.IsError = true;
                    result.Message = removeResult.Message;
                    return result;
                }

                result.Result = removeResult.Result;
                result.Message = "Item removed from shared inventory successfully";
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error removing item from inventory: {ex.Message}";
                result.Exception = ex;
            }

            return result;
        }

        /// <summary>
        /// Checks if the avatar has a specific item in their shared inventory
        /// Uses AvatarManager to check AvatarDetail.Inventory
        /// </summary>
        public async Task<OASISResult<bool>> HasItemAsync(Guid avatarId, Guid itemId)
        {
            var result = new OASISResult<bool>();
            try
            {
                var hasItemResult = await AvatarManager.Instance.AvatarHasItemAsync(avatarId, itemId);
                
                if (hasItemResult.IsError)
                {
                    result.IsError = true;
                    result.Message = hasItemResult.Message;
                    return result;
                }

                result.Result = hasItemResult.Result;
                result.Message = hasItemResult.Message;
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error checking for item: {ex.Message}";
                result.Exception = ex;
            }

            return result;
        }

        /// <summary>
        /// Checks if the avatar has a specific item by name in their shared inventory
        /// Uses AvatarManager to check AvatarDetail.Inventory
        /// </summary>
        public async Task<OASISResult<bool>> HasItemByNameAsync(Guid avatarId, string itemName)
        {
            var result = new OASISResult<bool>();
            try
            {
                var hasItemResult = await AvatarManager.Instance.AvatarHasItemByNameAsync(avatarId, itemName);
                
                if (hasItemResult.IsError)
                {
                    result.IsError = true;
                    result.Message = hasItemResult.Message;
                    return result;
                }

                result.Result = hasItemResult.Result;
                result.Message = hasItemResult.Message;
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error checking for item by name: {ex.Message}";
                result.Exception = ex;
            }

            return result;
        }

        /// <summary>
        /// Gets active quests that span multiple games
        /// </summary>
        public async Task<OASISResult<List<IQuestBase>>> GetCrossGameQuestsAsync(Guid avatarId)
        {
            var result = new OASISResult<List<IQuestBase>>();
            try
            {
                if (QuestManager == null)
                {
                    result.IsError = true;
                    result.Message = "Quest manager not initialized";
                    return result;
                }

                // Get all active quests for the avatar
                var questsResult = await QuestManager.LoadAllForAvatarAsync(avatarId);
                if (questsResult.IsError)
                {
                    result.IsError = true;
                    result.Message = questsResult.Message;
                    return result;
                }

                // Filter for cross-game quests (quests that can span multiple games)
                var crossGameQuests = questsResult.Result?.Where(q => 
                    q.MetaData != null && 
                    q.MetaData.ContainsKey("CrossGame") && 
                    Convert.ToBoolean(q.MetaData["CrossGame"])
                ).Cast<IQuestBase>().ToList() ?? new List<IQuestBase>();

                result.Result = crossGameQuests;
                result.Message = "Cross-game quests retrieved successfully";
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error getting cross-game quests: {ex.Message}";
                result.Exception = ex;
            }

            return result;
        }

        /// <summary>
        /// Gets avatar's karma score (shared across all games)
        /// </summary>
        public async Task<OASISResult<int>> GetAvatarKarmaAsync(Guid avatarId)
        {
            var result = new OASISResult<int>();
            try
            {
                if (KarmaManager == null)
                {
                    result.IsError = true;
                    result.Message = "Karma manager not initialized";
                    return result;
                }

                var karmaResult = await KarmaManager.GetKarmaAsync(avatarId);
                if (karmaResult.IsError)
                {
                    result.IsError = true;
                    result.Message = karmaResult.Message;
                    return result;
                }

                result.Result = (int)karmaResult.Result;
                result.Message = "Karma retrieved successfully";
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error getting karma: {ex.Message}";
                result.Exception = ex;
            }

            return result;
        }

    }
}
