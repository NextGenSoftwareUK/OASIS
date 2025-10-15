using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Core.Managers
{
    /// <summary>
    /// Manages social media integration, feeds, and social provider connections
    /// </summary>
    public partial class SocialManager : OASISManager
    {
        private static SocialManager _instance;
        private readonly Dictionary<string, SocialProvider> _registeredProviders;
        private readonly Dictionary<Guid, List<SocialPost>> _socialFeeds;

        public static SocialManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new SocialManager(ProviderManager.Instance.CurrentStorageProvider);
                return _instance;
            }
        }

        public SocialManager(IOASISStorageProvider OASISStorageProvider, OASISDNA OASISDNA = null) : base(OASISStorageProvider, OASISDNA)
        {
            _registeredProviders = new Dictionary<string, SocialProvider>();
            _socialFeeds = new Dictionary<Guid, List<SocialPost>>();
        }

        /// <summary>
        /// Get social feed from all registered providers for an avatar
        /// </summary>
        public async Task<OASISResult<List<SocialPost>>> GetSocialFeedAsync(Guid avatarId)
        {
            var result = new OASISResult<List<SocialPost>>();
            try
            {
                if (!_socialFeeds.ContainsKey(avatarId))
                {
                    _socialFeeds[avatarId] = new List<SocialPost>();
                }

                // Get posts from all registered providers
                var allPosts = new List<SocialPost>();
                foreach (var provider in _registeredProviders.Values)
                {
                    if (provider.IsActive)
                    {
                        var providerPosts = await GetPostsFromProvider(provider, avatarId);
                        allPosts.AddRange(providerPosts);
                    }
                }

                // Sort by timestamp (newest first)
                allPosts = allPosts.OrderByDescending(p => p.Timestamp).ToList();

                result.Result = allPosts;
                result.Message = "Social feed retrieved successfully";
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error retrieving social feed: {ex.Message}";
                result.Exception = ex;
            }
            return result;
        }

        /// <summary>
        /// Register a social provider for an avatar
        /// </summary>
        public async Task<OASISResult<bool>> RegisterSocialProviderAsync(Guid avatarId, string providerName, string accessToken, Dictionary<string, object> providerSettings = null)
        {
            var result = new OASISResult<bool>();
            try
            {
                var provider = new SocialProvider
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = providerName,
                    AccessToken = accessToken,
                    AvatarId = avatarId,
                    IsActive = true,
                    RegisteredAt = DateTime.UtcNow,
                    Settings = providerSettings ?? new Dictionary<string, object>()
                };

                _registeredProviders[provider.Id] = provider;

                result.Result = true;
                result.Message = $"Social provider {providerName} registered successfully";
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Result = false;
                result.Message = $"Error registering social provider: {ex.Message}";
                result.Exception = ex;
            }
            return await Task.FromResult(result);
        }

        /// <summary>
        /// Share a holon to social media
        /// </summary>
        public async Task<OASISResult<bool>> ShareHolonAsync(Guid avatarId, Guid holonId, string message, List<string> providerIds = null)
        {
            var result = new OASISResult<bool>();
            try
            {
                var providersToUse = providerIds != null && providerIds.Any() 
                    ? _registeredProviders.Where(p => providerIds.Contains(p.Key)).Select(p => p.Value)
                    : _registeredProviders.Values.Where(p => p.IsActive);

                var shareTasks = new List<Task<bool>>();
                foreach (var provider in providersToUse)
                {
                    shareTasks.Add(ShareToProvider(provider, holonId, message));
                }

                var results = await Task.WhenAll(shareTasks);
                var successCount = results.Count(r => r);

                result.Result = successCount > 0;
                result.Message = $"Shared to {successCount} of {shareTasks.Count} providers";
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Result = false;
                result.Message = $"Error sharing holon: {ex.Message}";
                result.Exception = ex;
            }
            return result;
        }

        /// <summary>
        /// Get registered social providers for an avatar
        /// </summary>
        public async Task<OASISResult<List<SocialProvider>>> GetRegisteredProvidersAsync(Guid avatarId)
        {
            var result = new OASISResult<List<SocialProvider>>();
            try
            {
                var providers = _registeredProviders.Values
                    .Where(p => p.AvatarId == avatarId)
                    .ToList();

                result.Result = providers;
                result.Message = "Registered providers retrieved successfully";
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error retrieving registered providers: {ex.Message}";
                result.Exception = ex;
            }
            return await Task.FromResult(result);
        }

        private async Task<List<SocialPost>> GetPostsFromProvider(SocialProvider provider, Guid avatarId)
        {
            // Simulate fetching posts from social provider
            await Task.Delay(100);
            
            return new List<SocialPost>
            {
                new SocialPost
                {
                    Id = Guid.NewGuid().ToString(),
                    ProviderId = provider.Id,
                    ProviderName = provider.Name,
                    Content = $"Sample post from {provider.Name}",
                    Timestamp = DateTime.UtcNow.AddHours(-1),
                    AvatarId = avatarId
                }
            };
        }

        private async Task<bool> ShareToProvider(SocialProvider provider, Guid holonId, string message)
        {
            // Simulate sharing to social provider
            await Task.Delay(200);
            
            // In a real implementation, this would call the actual social media API
            return true;
        }
    }

    public class SocialProvider
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string AccessToken { get; set; }
        public Guid AvatarId { get; set; }
        public bool IsActive { get; set; }
        public DateTime RegisteredAt { get; set; }
        public Dictionary<string, object> Settings { get; set; } = new Dictionary<string, object>();
    }

    public class SocialPost
    {
        public string Id { get; set; }
        public string ProviderId { get; set; }
        public string ProviderName { get; set; }
        public string Content { get; set; }
        public DateTime Timestamp { get; set; }
        public Guid AvatarId { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }
}
