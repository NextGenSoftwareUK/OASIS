using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Configuration;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.Managers
{
    /// <summary>
    /// Manages OASIS settings and user preferences
    /// </summary>
    public class SettingsManager : OASISManager
    {
        private static SettingsManager _instance;

        public static SettingsManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new SettingsManager(ProviderManager.Instance.CurrentStorageProvider, ProviderManager.Instance.OASISDNA);

                return _instance;
            }
        }

        private SettingsManager(IOASISStorageProvider OASISStorageProvider, OASISDNA OASISDNA = null) : base(OASISStorageProvider, OASISDNA)
        {
        }

        /// <summary>
        /// Get all OASIS settings for an avatar
        /// </summary>
        /// <param name="avatarId">Avatar ID</param>
        /// <returns>Complete settings object</returns>
        public async Task<OASISResult<Dictionary<string, object>>> GetAllSettingsAsync(Guid avatarId)
        {
            var result = new OASISResult<Dictionary<string, object>>();
            try
            {
                var hyperDriveSettings = await GetHyperDriveSettingsAsync(avatarId);
                var notificationSettings = await GetNotificationSettingsAsync(avatarId);
                var privacySettings = await GetPrivacySettingsAsync(avatarId);
                var systemSettings = await GetSystemSettingsAsync(avatarId);
                var subscriptionSettings = await GetSubscriptionSettingsAsync(avatarId);

                var allSettings = new Dictionary<string, object>
                {
                    ["hyperDrive"] = hyperDriveSettings.Result,
                    ["notifications"] = notificationSettings.Result,
                    ["privacy"] = privacySettings.Result,
                    ["system"] = systemSettings.Result,
                    ["subscription"] = subscriptionSettings.Result,
                    ["lastUpdated"] = DateTime.UtcNow,
                    ["avatarId"] = avatarId
                };

                result.Result = allSettings;
                result.Message = "All OASIS settings retrieved successfully.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error retrieving all settings: {ex.Message}", ex);
            }
            return result;
        }

        /// <summary>
        /// Get HyperDrive settings for an avatar
        /// </summary>
        /// <param name="avatarId">Avatar ID</param>
        /// <returns>HyperDrive settings</returns>
        public async Task<OASISResult<Dictionary<string, object>>> GetHyperDriveSettingsAsync(Guid avatarId)
        {
            var result = new OASISResult<Dictionary<string, object>>();
            try
            {
                // Get global HyperDrive configuration from DNA
                var hyperDriveConfig = OASISDNA?.OASIS?.OASISHyperDriveConfig;
                
                // Get avatar-specific preferences
                var avatarPreferences = await GetAvatarPreferencesAsync(avatarId);
                
                var settings = new Dictionary<string, object>
                {
                    // Global HyperDrive Settings
                    ["isEnabled"] = hyperDriveConfig?.IsEnabled ?? true,
                    ["defaultStrategy"] = hyperDriveConfig?.DefaultStrategy ?? "Auto",
                    ["hyperDriveMode"] = _OASISDNA?.OASIS?.HyperDriveMode ?? "Legacy",
                    
                    // Auto-Failover Settings
                    ["autoFailoverEnabled"] = hyperDriveConfig?.AutoFailoverEnabled ?? true,
                    ["autoReplicationEnabled"] = hyperDriveConfig?.AutoReplicationEnabled ?? true,
                    ["autoLoadBalancingEnabled"] = hyperDriveConfig?.AutoLoadBalancingEnabled ?? true,
                    
                    // Performance Settings
                    ["maxRetryAttempts"] = hyperDriveConfig?.MaxRetryAttempts ?? 3,
                    ["requestTimeoutMs"] = hyperDriveConfig?.RequestTimeoutMs ?? 5000,
                    ["healthCheckIntervalMs"] = hyperDriveConfig?.HealthCheckIntervalMs ?? 30000,
                    ["maxConcurrentRequests"] = hyperDriveConfig?.MaxConcurrentRequests ?? 100,
                    
                    // Weight Settings
                    ["performanceWeight"] = hyperDriveConfig?.PerformanceWeight ?? 0.4,
                    ["costWeight"] = hyperDriveConfig?.CostWeight ?? 0.3,
                    ["geographicWeight"] = hyperDriveConfig?.GeographicWeight ?? 0.2,
                    ["availabilityWeight"] = hyperDriveConfig?.AvailabilityWeight ?? 0.1,
                    
                    // Threshold Settings
                    ["maxLatencyThresholdMs"] = hyperDriveConfig?.MaxLatencyThresholdMs ?? 200,
                    ["maxErrorRateThreshold"] = hyperDriveConfig?.MaxErrorRateThreshold ?? 0.05,
                    ["minUptimeThreshold"] = hyperDriveConfig?.MinUptimeThreshold ?? 99.0,
                    
                    // Provider Settings
                    ["enabledProviders"] = hyperDriveConfig?.EnabledProviders ?? new List<string>(),
                    ["autoFailoverProviders"] = hyperDriveConfig?.AutoFailoverProviders ?? new List<string>(),
                    ["autoReplicationProviders"] = hyperDriveConfig?.AutoReplicationProviders ?? new List<string>(),
                    ["loadBalancingProviders"] = hyperDriveConfig?.LoadBalancingProviders ?? new List<string>(),
                    
                    // Avatar-Specific Preferences
                    ["preferredProvider"] = avatarPreferences.GetValueOrDefault("preferredProvider", "Auto"),
                    ["autoSync"] = avatarPreferences.GetValueOrDefault("autoSync", true),
                    ["geographicOptimization"] = avatarPreferences.GetValueOrDefault("geographicOptimization", true),
                    ["costOptimization"] = avatarPreferences.GetValueOrDefault("costOptimization", false),
                    ["performanceOptimization"] = avatarPreferences.GetValueOrDefault("performanceOptimization", true)
                };

                result.Result = settings;
                result.Message = "HyperDrive settings retrieved successfully.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error retrieving HyperDrive settings: {ex.Message}", ex);
            }
            return result;
        }

        /// <summary>
        /// Update HyperDrive settings for an avatar
        /// </summary>
        /// <param name="avatarId">Avatar ID</param>
        /// <param name="settings">Settings to update</param>
        /// <returns>Success status</returns>
        public async Task<OASISResult<bool>> UpdateHyperDriveSettingsAsync(Guid avatarId, Dictionary<string, object> settings)
        {
            var result = new OASISResult<bool>();
            try
            {
                // Update avatar-specific preferences
                await UpdateAvatarPreferencesAsync(avatarId, "hyperDrive", settings);
                
                result.Result = true;
                result.Message = "HyperDrive settings updated successfully.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error updating HyperDrive settings: {ex.Message}", ex);
            }
            return result;
        }

        /// <summary>
        /// Get notification settings for an avatar
        /// </summary>
        /// <param name="avatarId">Avatar ID</param>
        /// <returns>Notification settings</returns>
        public async Task<OASISResult<Dictionary<string, object>>> GetNotificationSettingsAsync(Guid avatarId)
        {
            var result = new OASISResult<Dictionary<string, object>>();
            try
            {
                var avatarPreferences = await GetAvatarPreferencesAsync(avatarId);
                
                var settings = new Dictionary<string, object>
                {
                    ["emailNotifications"] = avatarPreferences.GetValueOrDefault("emailNotifications", true),
                    ["pushNotifications"] = avatarPreferences.GetValueOrDefault("pushNotifications", true),
                    ["karmaUpdates"] = avatarPreferences.GetValueOrDefault("karmaUpdates", true),
                    ["achievementNotifications"] = avatarPreferences.GetValueOrDefault("achievementNotifications", true),
                    ["giftNotifications"] = avatarPreferences.GetValueOrDefault("giftNotifications", true),
                    ["chatNotifications"] = avatarPreferences.GetValueOrDefault("chatNotifications", true),
                    ["questUpdates"] = avatarPreferences.GetValueOrDefault("questUpdates", true),
                    ["systemUpdates"] = avatarPreferences.GetValueOrDefault("systemUpdates", true),
                    ["marketingEmails"] = avatarPreferences.GetValueOrDefault("marketingEmails", false)
                };

                result.Result = settings;
                result.Message = "Notification settings retrieved successfully.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error retrieving notification settings: {ex.Message}", ex);
            }
            return result;
        }

        /// <summary>
        /// Update notification settings for an avatar
        /// </summary>
        /// <param name="avatarId">Avatar ID</param>
        /// <param name="settings">Settings to update</param>
        /// <returns>Success status</returns>
        public async Task<OASISResult<bool>> UpdateNotificationSettingsAsync(Guid avatarId, Dictionary<string, object> settings)
        {
            var result = new OASISResult<bool>();
            try
            {
                await UpdateAvatarPreferencesAsync(avatarId, "notifications", settings);
                
                result.Result = true;
                result.Message = "Notification settings updated successfully.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error updating notification settings: {ex.Message}", ex);
            }
            return result;
        }

        /// <summary>
        /// Get privacy settings for an avatar
        /// </summary>
        /// <param name="avatarId">Avatar ID</param>
        /// <returns>Privacy settings</returns>
        public async Task<OASISResult<Dictionary<string, object>>> GetPrivacySettingsAsync(Guid avatarId)
        {
            var result = new OASISResult<Dictionary<string, object>>();
            try
            {
                var avatarPreferences = await GetAvatarPreferencesAsync(avatarId);
                
                var settings = new Dictionary<string, object>
                {
                    ["profileVisibility"] = avatarPreferences.GetValueOrDefault("profileVisibility", "public"),
                    ["showKarma"] = avatarPreferences.GetValueOrDefault("showKarma", true),
                    ["showAchievements"] = avatarPreferences.GetValueOrDefault("showAchievements", true),
                    ["showGifts"] = avatarPreferences.GetValueOrDefault("showGifts", true),
                    ["showStats"] = avatarPreferences.GetValueOrDefault("showStats", true),
                    ["showLocation"] = avatarPreferences.GetValueOrDefault("showLocation", false),
                    ["allowFriendRequests"] = avatarPreferences.GetValueOrDefault("allowFriendRequests", true),
                    ["allowGifts"] = avatarPreferences.GetValueOrDefault("allowGifts", true),
                    ["allowChat"] = avatarPreferences.GetValueOrDefault("allowChat", true)
                };

                result.Result = settings;
                result.Message = "Privacy settings retrieved successfully.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error retrieving privacy settings: {ex.Message}", ex);
            }
            return result;
        }

        /// <summary>
        /// Update privacy settings for an avatar
        /// </summary>
        /// <param name="avatarId">Avatar ID</param>
        /// <param name="settings">Settings to update</param>
        /// <returns>Success status</returns>
        public async Task<OASISResult<bool>> UpdatePrivacySettingsAsync(Guid avatarId, Dictionary<string, object> settings)
        {
            var result = new OASISResult<bool>();
            try
            {
                await UpdateAvatarPreferencesAsync(avatarId, "privacy", settings);
                
                result.Result = true;
                result.Message = "Privacy settings updated successfully.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error updating privacy settings: {ex.Message}", ex);
            }
            return result;
        }

        /// <summary>
        /// Get system settings for an avatar
        /// </summary>
        /// <param name="avatarId">Avatar ID</param>
        /// <returns>System settings</returns>
        public async Task<OASISResult<Dictionary<string, object>>> GetSystemSettingsAsync(Guid avatarId)
        {
            var result = new OASISResult<Dictionary<string, object>>();
            try
            {
                var avatarPreferences = await GetAvatarPreferencesAsync(avatarId);
                
                var settings = new Dictionary<string, object>
                {
                    // Theme Settings
                    ["theme"] = avatarPreferences.GetValueOrDefault("theme", "dark"),
                    ["language"] = avatarPreferences.GetValueOrDefault("language", "en"),
                    ["timezone"] = avatarPreferences.GetValueOrDefault("timezone", "UTC"),
                    
                    // Display Settings
                    ["showKarma"] = avatarPreferences.GetValueOrDefault("showKarma", true),
                    ["showAchievements"] = avatarPreferences.GetValueOrDefault("showAchievements", true),
                    ["showGifts"] = avatarPreferences.GetValueOrDefault("showGifts", true),
                    ["showStats"] = avatarPreferences.GetValueOrDefault("showStats", true),
                    ["showLocation"] = avatarPreferences.GetValueOrDefault("showLocation", false),
                    
                    // API Settings
                    ["defaultProvider"] = avatarPreferences.GetValueOrDefault("defaultProvider", "holochain"),
                    ["autoSync"] = avatarPreferences.GetValueOrDefault("autoSync", true),
                    ["enableCaching"] = avatarPreferences.GetValueOrDefault("enableCaching", true),
                    ["cacheTimeout"] = avatarPreferences.GetValueOrDefault("cacheTimeout", 300),
                    
                    // Security Settings
                    ["requireTwoFactor"] = avatarPreferences.GetValueOrDefault("requireTwoFactor", false),
                    ["sessionTimeout"] = avatarPreferences.GetValueOrDefault("sessionTimeout", 3600),
                    ["allowMultipleSessions"] = avatarPreferences.GetValueOrDefault("allowMultipleSessions", true),
                    
                    // Performance Settings
                    ["enableCompression"] = avatarPreferences.GetValueOrDefault("enableCompression", true),
                    ["enableEncryption"] = avatarPreferences.GetValueOrDefault("enableEncryption", true),
                    ["maxFileSize"] = avatarPreferences.GetValueOrDefault("maxFileSize", 10485760),
                    ["maxConcurrentRequests"] = avatarPreferences.GetValueOrDefault("maxConcurrentRequests", 10)
                };

                result.Result = settings;
                result.Message = "System settings retrieved successfully.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error retrieving system settings: {ex.Message}", ex);
            }
            return result;
        }

        /// <summary>
        /// Update system settings for an avatar
        /// </summary>
        /// <param name="avatarId">Avatar ID</param>
        /// <param name="settings">Settings to update</param>
        /// <returns>Success status</returns>
        public async Task<OASISResult<bool>> UpdateSystemSettingsAsync(Guid avatarId, Dictionary<string, object> settings)
        {
            var result = new OASISResult<bool>();
            try
            {
                await UpdateAvatarPreferencesAsync(avatarId, "system", settings);
                
                result.Result = true;
                result.Message = "System settings updated successfully.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error updating system settings: {ex.Message}", ex);
            }
            return result;
        }

        /// <summary>
        /// Get subscription settings for an avatar
        /// </summary>
        /// <param name="avatarId">Avatar ID</param>
        /// <returns>Subscription settings</returns>
        public async Task<OASISResult<Dictionary<string, object>>> GetSubscriptionSettingsAsync(Guid avatarId)
        {
            var result = new OASISResult<Dictionary<string, object>>();
            try
            {
                // Get avatar details for email
                var avatarResult = await AvatarManager.Instance.LoadAvatarAsync(avatarId);
                var avatar = avatarResult.Result;
                
                // Get subscription info from subscription system
                var subscriptionInfo = await GetSubscriptionInfoAsync(avatarId);
                
                var settings = new Dictionary<string, object>
                {
                    // Subscription Info
                    ["currentPlan"] = subscriptionInfo.GetValueOrDefault("currentPlan", "Bronze"),
                    ["planType"] = subscriptionInfo.GetValueOrDefault("planType", "Bronze"),
                    ["isActive"] = subscriptionInfo.GetValueOrDefault("isActive", true),
                    ["expiresAt"] = subscriptionInfo.GetValueOrDefault("expiresAt", DateTime.UtcNow.AddDays(30)),
                    
                    // Plan Limits
                    ["maxRequestsPerMonth"] = subscriptionInfo.GetValueOrDefault("maxRequestsPerMonth", 10000),
                    ["maxStorageGB"] = subscriptionInfo.GetValueOrDefault("maxStorageGB", 10),
                    ["maxAvatars"] = subscriptionInfo.GetValueOrDefault("maxAvatars", 5),
                    ["maxOAPPs"] = subscriptionInfo.GetValueOrDefault("maxOAPPs", 3),
                    
                    // Usage Stats
                    ["requestsUsedThisMonth"] = subscriptionInfo.GetValueOrDefault("requestsUsedThisMonth", 0),
                    ["storageUsedGB"] = subscriptionInfo.GetValueOrDefault("storageUsedGB", 0),
                    ["avatarsCreated"] = subscriptionInfo.GetValueOrDefault("avatarsCreated", 1),
                    ["oappsCreated"] = subscriptionInfo.GetValueOrDefault("oappsCreated", 0),
                    
                    // Billing Settings
                    ["billingEmail"] = avatar?.Email ?? "unknown@example.com",
                    ["autoRenew"] = subscriptionInfo.GetValueOrDefault("autoRenew", true),
                    ["paymentMethod"] = subscriptionInfo.GetValueOrDefault("paymentMethod", "card"),
                    
                    // Feature Access
                    ["hyperDriveEnabled"] = subscriptionInfo.GetValueOrDefault("hyperDriveEnabled", true),
                    ["advancedAnalytics"] = subscriptionInfo.GetValueOrDefault("advancedAnalytics", false),
                    ["prioritySupport"] = subscriptionInfo.GetValueOrDefault("prioritySupport", false),
                    ["customDomains"] = subscriptionInfo.GetValueOrDefault("customDomains", false),
                    ["apiKeys"] = subscriptionInfo.GetValueOrDefault("apiKeys", 1),
                    ["webhooks"] = subscriptionInfo.GetValueOrDefault("webhooks", 0)
                };

                result.Result = settings;
                result.Message = "Subscription settings retrieved successfully.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error retrieving subscription settings: {ex.Message}", ex);
            }
            return result;
        }

        /// <summary>
        /// Update subscription settings for an avatar
        /// </summary>
        /// <param name="avatarId">Avatar ID</param>
        /// <param name="settings">Settings to update</param>
        /// <returns>Success status</returns>
        public async Task<OASISResult<bool>> UpdateSubscriptionSettingsAsync(Guid avatarId, Dictionary<string, object> settings)
        {
            var result = new OASISResult<bool>();
            try
            {
                await UpdateSubscriptionInfoAsync(avatarId, settings);
                
                result.Result = true;
                result.Message = "Subscription settings updated successfully.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error updating subscription settings: {ex.Message}", ex);
            }
            return result;
        }

        #region Private Helper Methods

        /// <summary>
        /// Get avatar preferences from storage
        /// </summary>
        /// <param name="avatarId">Avatar ID</param>
        /// <returns>Avatar preferences</returns>
        private async Task<Dictionary<string, object>> GetAvatarPreferencesAsync(Guid avatarId)
        {
            try
            {
                // Load avatar and get preferences from metadata
                var avatarResult = await AvatarManager.Instance.LoadAvatarAsync(avatarId);
                if (avatarResult.IsError || avatarResult.Result == null)
                    return new Dictionary<string, object>();

                var avatar = avatarResult.Result;
                
                // Get preferences from avatar metadata or create default
                if (avatar.MetaData != null && avatar.MetaData.ContainsKey("preferences"))
                {
                    return avatar.MetaData["preferences"] as Dictionary<string, object> ?? new Dictionary<string, object>();
                }
                
                return new Dictionary<string, object>();
            }
            catch
            {
                return new Dictionary<string, object>();
            }
        }

        /// <summary>
        /// Update avatar preferences in storage
        /// </summary>
        /// <param name="avatarId">Avatar ID</param>
        /// <param name="category">Settings category</param>
        /// <param name="settings">Settings to update</param>
        private async Task UpdateAvatarPreferencesAsync(Guid avatarId, string category, Dictionary<string, object> settings)
        {
            try
            {
                // Load avatar
                var avatarResult = await AvatarManager.Instance.LoadAvatarAsync(avatarId);
                if (avatarResult.IsError || avatarResult.Result == null)
                    return;

                var avatar = avatarResult.Result;
                
                // Initialize metadata if needed
                if (avatar.MetaData == null)
                    avatar.MetaData = new Dictionary<string, object>();
                
                // Initialize preferences if needed
                if (!avatar.MetaData.ContainsKey("preferences"))
                    avatar.MetaData["preferences"] = new Dictionary<string, object>();
                
                var preferences = avatar.MetaData["preferences"] as Dictionary<string, object>;
                if (preferences == null)
                {
                    preferences = new Dictionary<string, object>();
                    avatar.MetaData["preferences"] = preferences;
                }
                
                // Update category settings
                preferences[category] = settings;
                
                // Save avatar with updated preferences
                await AvatarManager.Instance.SaveAvatarAsync(avatar);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating avatar preferences: {ex.Message}");
            }
        }

        /// <summary>
        /// Get subscription information
        /// </summary>
        /// <param name="avatarId">Avatar ID</param>
        /// <returns>Subscription info</returns>
        private async Task<Dictionary<string, object>> GetSubscriptionInfoAsync(Guid avatarId)
        {
            try
            {
                // Get subscription data from HolonManager
                var subscriptionHolon = await HolonManager.Instance.LoadHolonAsync($"subscription_{avatarId}");
                
                if (subscriptionHolon.IsError || subscriptionHolon.Result == null)
                {
                    // Return default subscription info if no data found
                    return new Dictionary<string, object>
                    {
                        ["currentPlan"] = "Bronze",
                        ["planType"] = "Bronze",
                        ["isActive"] = true,
                        ["expiresAt"] = DateTime.UtcNow.AddDays(30),
                        ["maxRequestsPerMonth"] = 10000,
                        ["maxStorageGB"] = 10,
                        ["maxAvatars"] = 5,
                        ["maxOAPPs"] = 3,
                        ["requestsUsedThisMonth"] = 0,
                        ["storageUsedGB"] = 0,
                        ["avatarsCreated"] = 1,
                        ["oappsCreated"] = 0,
                        ["autoRenew"] = true,
                        ["paymentMethod"] = "card",
                        ["hyperDriveEnabled"] = true,
                        ["advancedAnalytics"] = false,
                        ["prioritySupport"] = false,
                        ["customDomains"] = false,
                        ["apiKeys"] = 1,
                        ["webhooks"] = 0
                    };
                }

                // Parse subscription data from holon
                var subscriptionData = subscriptionHolon.Result.MetaData;
                return new Dictionary<string, object>
                {
                    ["currentPlan"] = subscriptionData.GetValueOrDefault("currentPlan", "Bronze"),
                    ["planType"] = subscriptionData.GetValueOrDefault("planType", "Bronze"),
                    ["isActive"] = subscriptionData.GetValueOrDefault("isActive", true),
                    ["expiresAt"] = subscriptionData.GetValueOrDefault("expiresAt", DateTime.UtcNow.AddDays(30)),
                    ["maxRequestsPerMonth"] = subscriptionData.GetValueOrDefault("maxRequestsPerMonth", 10000),
                    ["maxStorageGB"] = subscriptionData.GetValueOrDefault("maxStorageGB", 10),
                    ["maxAvatars"] = subscriptionData.GetValueOrDefault("maxAvatars", 5),
                    ["maxOAPPs"] = subscriptionData.GetValueOrDefault("maxOAPPs", 3),
                    ["requestsUsedThisMonth"] = subscriptionData.GetValueOrDefault("requestsUsedThisMonth", 0),
                    ["storageUsedGB"] = subscriptionData.GetValueOrDefault("storageUsedGB", 0),
                    ["avatarsCreated"] = subscriptionData.GetValueOrDefault("avatarsCreated", 1),
                    ["oappsCreated"] = subscriptionData.GetValueOrDefault("oappsCreated", 0),
                    ["autoRenew"] = subscriptionData.GetValueOrDefault("autoRenew", true),
                    ["paymentMethod"] = subscriptionData.GetValueOrDefault("paymentMethod", "card"),
                    ["hyperDriveEnabled"] = subscriptionData.GetValueOrDefault("hyperDriveEnabled", true),
                    ["advancedAnalytics"] = subscriptionData.GetValueOrDefault("advancedAnalytics", false),
                    ["prioritySupport"] = subscriptionData.GetValueOrDefault("prioritySupport", false),
                    ["customDomains"] = subscriptionData.GetValueOrDefault("customDomains", false),
                    ["apiKeys"] = subscriptionData.GetValueOrDefault("apiKeys", 1),
                    ["webhooks"] = subscriptionData.GetValueOrDefault("webhooks", 0)
                };
            }
            catch
            {
                return new Dictionary<string, object>();
            }
        }

        /// <summary>
        /// Update subscription information
        /// </summary>
        /// <param name="avatarId">Avatar ID</param>
        /// <param name="settings">Settings to update</param>
        private async Task UpdateSubscriptionInfoAsync(Guid avatarId, Dictionary<string, object> settings)
        {
            try
            {
                // Get existing subscription holon or create new one
                var subscriptionHolon = await HolonManager.Instance.LoadHolonAsync($"subscription_{avatarId}");
                
                if (subscriptionHolon.IsError || subscriptionHolon.Result == null)
                {
                    // Create new subscription holon
                    var newHolon = new Holon
                    {
                        Id = Guid.NewGuid(),
                        Name = $"Subscription_{avatarId}",
                        Description = $"Subscription settings for avatar {avatarId}",
                        MetaData = settings,
                        CreatedByAvatarId = avatarId,
                        CreatedDate = DateTime.UtcNow,
                        ModifiedDate = DateTime.UtcNow
                    };
                    
                    await HolonManager.Instance.SaveHolonAsync(newHolon);
                }
                else
                {
                    // Update existing subscription holon
                    var existingHolon = subscriptionHolon.Result;
                    
                    // Update metadata with new settings
                    foreach (var setting in settings)
                    {
                        existingHolon.MetaData[setting.Key] = setting.Value;
                    }
                    
                    existingHolon.ModifiedDate = DateTime.UtcNow;
                    await HolonManager.Instance.SaveHolonAsync(existingHolon);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating subscription info: {ex.Message}");
            }
        }

        #endregion
    }
}
