using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Helpers;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    [ApiController]
    [Route("api/settings")]
    public class SettingsController : OASISControllerBase
    {
        private readonly OASISDNA _OASISDNA;

        public SettingsController()
        {
            _OASISDNA = OASISBootLoader.OASISBootLoader.OASISDNA;
        }

        /// <summary>
        /// Get all OASIS settings for the currently logged in avatar
        /// </summary>
        /// <returns>Comprehensive OASIS settings including HyperDrive, notifications, privacy, etc.</returns>
        [Authorize]
        [HttpGet("get-all-settings-for-current-logged-in-avatar")]
        public async Task<OASISResult<Dictionary<string, object>>> GetAllSettingsForCurrentLoggedInAvatar()
        {
            try
            {
                if (Avatar == null)
                {
                    return new OASISResult<Dictionary<string, object>> 
                    { 
                        IsError = true, 
                        Message = "Avatar not found. Please ensure you are logged in." 
                    };
                }

                OASISResult<Dictionary<string, object>> result = null;
                try
                {
                    result = await Program.SettingsManager.GetAllSettingsAsync(Avatar.Id);
                }
                catch
                {
                    // If real data unavailable, use test data
                }

                // Return test data if setting is enabled and result is null, has error, or result is null
                if (UseTestDataWhenLiveDataNotAvailable && (result == null || result.IsError || result.Result == null))
                {
                    return new OASISResult<Dictionary<string, object>>
                    {
                        Result = new Dictionary<string, object>(),
                        IsError = false,
                        Message = "Settings retrieved successfully (using test data)"
                    };
                }

                return result;
            }
            catch (Exception ex)
            {
                // Return test data if setting is enabled, otherwise return error
                if (UseTestDataWhenLiveDataNotAvailable)
                {
                    return new OASISResult<Dictionary<string, object>>
                    {
                        Result = new Dictionary<string, object>(),
                        IsError = false,
                        Message = "Settings retrieved successfully (using test data)"
                    };
                }
                return new OASISResult<Dictionary<string, object>>
                {
                    IsError = true,
                    Message = $"Error retrieving settings: {ex.Message}",
                    Exception = ex
                };
            }
        }

        /// <summary>
        /// Get HyperDrive settings for the current avatar
        /// </summary>
        /// <returns>HyperDrive configuration settings</returns>
        [Authorize]
        [HttpGet("hyperdrive-settings")]
        public async Task<OASISResult<Dictionary<string, object>>> GetHyperDriveSettings()
        {
            if (Avatar == null)
            {
                return new OASISResult<Dictionary<string, object>> 
                { 
                    IsError = true, 
                    Message = "Avatar not found. Please ensure you are logged in." 
                };
            }

            return await Program.SettingsManager.GetHyperDriveSettingsAsync(Avatar.Id);
        }

        /// <summary>
        /// Update HyperDrive settings
        /// </summary>
        /// <param name="settings">HyperDrive settings to update</param>
        /// <returns>Success status</returns>
        [Authorize]
        [HttpPut("hyperdrive-settings")]
        public async Task<OASISResult<bool>> UpdateHyperDriveSettings([FromBody] Dictionary<string, object> settings)
        {
            if (settings == null)
                return new OASISResult<bool> { IsError = true, Message = "The request body is required. Please provide a valid JSON object with HyperDrive settings." };
            if (Avatar == null)
            {
                return new OASISResult<bool> 
                { 
                    IsError = true, 
                    Message = "Avatar not found. Please ensure you are logged in." 
                };
            }

            return await Program.SettingsManager.UpdateHyperDriveSettingsAsync(Avatar.Id, settings);
        }

        /// <summary>
        /// Get system settings for the current avatar
        /// </summary>
        /// <returns>System configuration settings</returns>
        [Authorize]
        [HttpGet("system-settings")]
        public async Task<OASISResult<Dictionary<string, object>>> GetSystemSettings()
        {
            if (Avatar == null)
            {
                return new OASISResult<Dictionary<string, object>> 
                { 
                    IsError = true, 
                    Message = "Avatar not found. Please ensure you are logged in." 
                };
            }

            return await Program.SettingsManager.GetSystemSettingsAsync(Avatar.Id);
        }

        /// <summary>
        /// Update system settings
        /// </summary>
        /// <param name="settings">System settings to update</param>
        /// <returns>Success status</returns>
        [Authorize]
        [HttpPut("system-settings")]
        public async Task<OASISResult<bool>> UpdateSystemSettings([FromBody] Dictionary<string, object> settings)
        {
            if (settings == null)
                return new OASISResult<bool> { IsError = true, Message = "The request body is required. Please provide a valid JSON object with system settings." };
            if (Avatar == null)
            {
                return new OASISResult<bool> 
                { 
                    IsError = true, 
                    Message = "Avatar not found. Please ensure you are logged in." 
                };
            }

            return await Program.SettingsManager.UpdateSystemSettingsAsync(Avatar.Id, settings);
        }

        /// <summary>
        /// Get subscription settings for the current avatar
        /// </summary>
        /// <returns>Subscription and plan settings</returns>
        [Authorize]
        [HttpGet("subscription-settings")]
        public async Task<OASISResult<Dictionary<string, object>>> GetSubscriptionSettings()
        {
            if (Avatar == null)
            {
                return new OASISResult<Dictionary<string, object>> 
                { 
                    IsError = true, 
                    Message = "Avatar not found. Please ensure you are logged in." 
                };
            }

            return await Program.SettingsManager.GetSubscriptionSettingsAsync(Avatar.Id);
        }

        /// <summary>
        /// Update subscription settings
        /// </summary>
        /// <param name="settings">Subscription settings to update</param>
        /// <returns>Success status</returns>
        [Authorize]
        [HttpPut("subscription-settings")]
        public async Task<OASISResult<bool>> UpdateSubscriptionSettings([FromBody] Dictionary<string, object> settings)
        {
            if (settings == null)
                return new OASISResult<bool> { IsError = true, Message = "The request body is required. Please provide a valid JSON object with subscription settings." };
            if (Avatar == null)
            {
                return new OASISResult<bool> 
                { 
                    IsError = true, 
                    Message = "Avatar not found. Please ensure you are logged in." 
                };
            }

            return await Program.SettingsManager.UpdateSubscriptionSettingsAsync(Avatar.Id, settings);
        }

        /// <summary>
        /// Update avatar settings
        /// </summary>
        /// <param name="settings">Settings to update</param>
        /// <returns>Updated avatar</returns>
        [Authorize]
        [HttpPut("update-settings")]
        public async Task<OASISResult<IAvatar>> UpdateSettings([FromBody] Dictionary<string, object> settings)
        {
            var result = new OASISResult<IAvatar>();
            if (settings == null)
            {
                result.IsError = true;
                result.Message = "The request body is required. Please provide a valid JSON object with settings (e.g. firstName, lastName, title).";
                return result;
            }
            try
            {
                if (Avatar == null)
                {
                    result.IsError = true;
                    result.Message = "Avatar not found. Please ensure you are logged in.";
                    return result;
                }

                // Load current avatar
                var avatarResult = await Program.AvatarManager.LoadAvatarAsync(Avatar.Id);
                if (avatarResult.IsError || avatarResult.Result == null)
                {
                    result.IsError = true;
                    result.Message = "Failed to load avatar details.";
                    return result;
                }

                var avatar = avatarResult.Result;
                bool hasChanges = false;

                // Update basic info
                if (settings.ContainsKey("firstName") && settings["firstName"] != null)
                {
                    avatar.FirstName = settings["firstName"].ToString();
                    hasChanges = true;
                }
                if (settings.ContainsKey("lastName") && settings["lastName"] != null)
                {
                    avatar.LastName = settings["lastName"].ToString();
                    hasChanges = true;
                }
                if (settings.ContainsKey("title") && settings["title"] != null)
                {
                    avatar.Title = settings["title"].ToString();
                    hasChanges = true;
                }

                // Update contact information
                /* 
                {
                    avatar.Address = settings["address"].ToString();
                    hasChanges = true;
                }
                if (settings.ContainsKey("town") && settings["town"] != null)
                {
                    avatar.Town = settings["town"].ToString();
                    hasChanges = true;
                }
                if (settings.ContainsKey("county") && settings["county"] != null)
                {
                    avatar.County = settings["county"].ToString();
                    hasChanges = true;
                }
                if (settings.ContainsKey("country") && settings["country"] != null)
                {
                    avatar.Country = settings["country"].ToString();
                    hasChanges = true;
                }
                if (settings.ContainsKey("postcode") && settings["postcode"] != null)
                {
                    avatar.Postcode = settings["postcode"].ToString();
                    hasChanges = true;
                }
                if (settings.ContainsKey("mobile") && settings["mobile"] != null)
                {
                    avatar.Mobile = settings["mobile"].ToString();
                    hasChanges = true;
                }
                if (settings.ContainsKey("landline") && settings["landline"] != null)
                {
                    avatar.Landline = settings["landline"].ToString();
                    hasChanges = true;
                }

                // Update appearance
                if (settings.ContainsKey("portrait") && settings["portrait"] != null)
                {
                    avatar.Portrait = settings["portrait"].ToString();
                    hasChanges = true;
                }
                if (settings.ContainsKey("model3D") && settings["model3D"] != null)
                {
                    avatar.Model3D = settings["model3D"].ToString();
                    hasChanges = true;
                }

                // Update personal details
                if (settings.ContainsKey("dob") && settings["dob"] != null && DateTime.TryParse(settings["dob"].ToString(), out DateTime dob))
                {
                    avatar.DOB = dob;
                    hasChanges = true;
                }
                if (settings.ContainsKey("umaJson") && settings["umaJson"] != null)
                {
                    avatar.UmaJson = settings["umaJson"].ToString();
                    hasChanges = true;
                }
                */

                if (hasChanges)
                {
                    // Save updated avatar
                    var saveResult = await Program.AvatarManager.SaveAvatarAsync(avatar);
                    if (saveResult.IsError)
                    {
                        result.IsError = true;
                        result.Message = $"Failed to save avatar settings: {saveResult.Message}";
                        return result;
                    }

                    result.Result = saveResult.Result;
                    result.Message = "Avatar settings updated successfully.";
                }
                else
                {
                    result.Result = avatar;
                    result.Message = "No changes detected.";
                }
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error updating avatar settings: {ex.Message}";
                result.Exception = ex;
            }
            return result;
        }

        /// <summary>
        /// Get notification preferences
        /// </summary>
        /// <returns>Notification settings</returns>
        [Authorize]
        [HttpGet("notification-preferences")]
        public async Task<OASISResult<Dictionary<string, object>>> GetNotificationPreferences()
        {
            if (Avatar == null)
            {
                return new OASISResult<Dictionary<string, object>> 
                { 
                    IsError = true, 
                    Message = "Avatar not found. Please ensure you are logged in." 
                };
            }

            return await Program.SettingsManager.GetNotificationSettingsAsync(Avatar.Id);
        }

        /// <summary>
        /// Update notification preferences
        /// </summary>
        /// <param name="preferences">Notification preferences to update</param>
        /// <returns>Success status</returns>
        [Authorize]
        [HttpPut("notification-preferences")]
        public async Task<OASISResult<bool>> UpdateNotificationPreferences([FromBody] Dictionary<string, object> preferences)
        {
            if (preferences == null)
                return new OASISResult<bool> { IsError = true, Message = "The request body is required. Please provide a valid JSON object with notification preferences." };
            if (Avatar == null)
            {
                return new OASISResult<bool> 
                { 
                    IsError = true, 
                    Message = "Avatar not found. Please ensure you are logged in." 
                };
            }

            return await Program.SettingsManager.UpdateNotificationSettingsAsync(Avatar.Id, preferences);
        }

        /// <summary>
        /// Get privacy settings
        /// </summary>
        /// <returns>Privacy settings</returns>
        [Authorize]
        [HttpGet("privacy-settings")]
        public async Task<OASISResult<Dictionary<string, object>>> GetPrivacySettings()
        {
            if (Avatar == null)
            {
                return new OASISResult<Dictionary<string, object>> 
                { 
                    IsError = true, 
                    Message = "Avatar not found. Please ensure you are logged in." 
                };
            }

            return await Program.SettingsManager.GetPrivacySettingsAsync(Avatar.Id);
        }

        /// <summary>
        /// Update privacy settings
        /// </summary>
        /// <param name="privacySettings">Privacy settings to update</param>
        /// <returns>Success status</returns>
        [Authorize]
        [HttpPut("privacy-settings")]
        public async Task<OASISResult<bool>> UpdatePrivacySettings([FromBody] Dictionary<string, object> privacySettings)
        {
            if (privacySettings == null)
                return new OASISResult<bool> { IsError = true, Message = "The request body is required. Please provide a valid JSON object with privacy settings." };
            if (Avatar == null)
            {
                return new OASISResult<bool> 
                { 
                    IsError = true, 
                    Message = "Avatar not found. Please ensure you are logged in." 
                };
            }

            return await Program.SettingsManager.UpdatePrivacySettingsAsync(Avatar.Id, privacySettings);
        }

        /// <summary>
        /// Returns the current OASIS API Version
        /// </summary>
        /// <returns>API version string</returns>
        [HttpGet("version")]
        public string GetVersion()
        {
            return $"OASIS API {OASISBootLoader.OASISBootLoader.OASISRuntimeVersion}";
        }

        /// <summary>
        /// Get system configuration
        /// </summary>
        /// <returns>System configuration</returns>
        [HttpGet("system-config")]
        public OASISResult<Dictionary<string, object>> GetSystemConfig()
        {
            var result = new OASISResult<Dictionary<string, object>>();
            try
            {
                var config = new Dictionary<string, object>
                {
                    ["version"] = OASISBootLoader.OASISBootLoader.OASISRuntimeVersion,
                    ["environment"] = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production",
                    ["timestamp"] = DateTime.UtcNow,
                    ["features"] = new Dictionary<string, object>
                    {
                        ["karmaSystem"] = true,
                        ["giftSystem"] = true,
                        ["chatSystem"] = true,
                        ["questSystem"] = true,
                        ["achievementSystem"] = true,
                        ["leaderboardSystem"] = true
                    }
                };

                result.Result = config;
                result.Message = "System configuration retrieved successfully.";
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error retrieving system configuration: {ex.Message}";
                result.Exception = ex;
            }
            return result;
        }

    }
}
