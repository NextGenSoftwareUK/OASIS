using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.Managers
{
    public partial class HolonManager : OASISManager
    {
    // Raised after settings are successfully saved for an avatar/category
    public event Action<Guid, string> SettingsSaved;
        private static HolonManager _instance = null;
        private OASISResult<IEnumerable<IHolon>> _allHolonsCache = null;

        //public delegate void StorageProviderError(object sender, AvatarManagerErrorEventArgs e);

        public static HolonManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new HolonManager(ProviderManager.Instance.CurrentStorageProvider);

                return _instance;
            }
        }

        //TODO: In future more than one storage provider can be active at a time where each call can specify which provider to use.
        public HolonManager(IOASISStorageProvider OASISStorageProvider, OASISDNA OASISDNA = null) : base(OASISStorageProvider, OASISDNA)
        {

        }

        public void ClearCache()
        {
            _allHolonsCache.Result = null;
            _allHolonsCache = null;
        }

        /// <summary>
        /// Send's a given holon from one provider to another. 
        /// This method is only really needed if auto-replication is disabled or there is a use case for sending from one provider to another.
        /// By default this will NOT auto-replicate to any other provider (set autoReplicate to true if you wish it to). This param overrides the global auto-replication setting.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="id"></param>
        /// <param name="sourceProviderType"></param>
        /// <param name="destinationProviderType"></param>
        /// <param name="autoReplicate"></param>
        /// <returns></returns>
        public OASISResult<T> SendHolon<T>(Guid id, ProviderType sourceProviderType, ProviderType destinationProviderType, bool autoReplicate = false) where T : IHolon, new()
        {
            // TODO: Finish Implementing ASAP...
            // Needs to load the holon from the source provider and then save to the destination provider.


            return new OASISResult<T>();
        }

        public async Task<OASISResult<IHolon>> AddHolonToCollectionAsync(IHolon parentHolon, IHolon holon, List<IHolon> holons, Guid avatarId, bool saveHolon = true, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IHolon> result = new OASISResult<IHolon>();

            if (holons == null)
                holons = new List<IHolon>();

            else if (holons.Any(x => x.Name == holon.Name))
            {
                result.IsError = true;
                result.Message = string.Concat("The name ", holon.Name, " is already taken, please choose another.");
                return result;
            }

            holon.IsNewHolon = true; //TODO: I am pretty sure every holon being added to a collection using this method will be a new one?

            if (holon.ParentOmniverseId == Guid.Empty)
            {
                holon.ParentOmniverseId = parentHolon.ParentOmniverseId;
                holon.ParentOmniverse = parentHolon.ParentOmniverse;
            }

            if (holon.ParentMultiverseId == Guid.Empty)
            {
                holon.ParentMultiverseId = parentHolon.ParentMultiverseId;
                holon.ParentMultiverse = parentHolon.ParentMultiverse;
            }

            if (holon.ParentUniverseId == Guid.Empty)
            {
                holon.ParentUniverseId = parentHolon.ParentUniverseId;
                holon.ParentUniverse = parentHolon.ParentUniverse;
            }

            if (holon.ParentDimensionId == Guid.Empty)
            {
                holon.ParentDimensionId = parentHolon.ParentDimensionId;
                holon.ParentDimension = parentHolon.ParentDimension;
            }

            if (holon.ParentGalaxyClusterId == Guid.Empty)
            {
                holon.ParentGalaxyClusterId = parentHolon.ParentGalaxyClusterId;
                holon.ParentGalaxyCluster = parentHolon.ParentGalaxyCluster;
            }

            if (holon.ParentGalaxyId == Guid.Empty)
            {
                holon.ParentGalaxyId = parentHolon.ParentGalaxyId;
                holon.ParentGalaxy = parentHolon.ParentGalaxy;
            }

            if (holon.ParentSolarSystemId == Guid.Empty)
            {
                holon.ParentSolarSystemId = parentHolon.ParentSolarSystemId;
                holon.ParentSolarSystem = parentHolon.ParentSolarSystem;
            }

            if (holon.ParentGreatGrandSuperStarId == Guid.Empty)
            {
                holon.ParentGreatGrandSuperStarId = parentHolon.ParentGreatGrandSuperStarId;
                holon.ParentGreatGrandSuperStar = parentHolon.ParentGreatGrandSuperStar;
            }

            if (holon.ParentGrandSuperStarId == Guid.Empty)
            {
                holon.ParentGrandSuperStarId = parentHolon.ParentGrandSuperStarId;
                holon.ParentGrandSuperStar = parentHolon.ParentGrandSuperStar;
            }

            if (holon.ParentSuperStarId == Guid.Empty)
            {
                holon.ParentSuperStarId = parentHolon.ParentSuperStarId;
                holon.ParentSuperStar = parentHolon.ParentSuperStar;
            }

            if (holon.ParentStarId == Guid.Empty)
            {
                holon.ParentStarId = parentHolon.ParentStarId;
                holon.ParentStar = parentHolon.ParentStar;
            }

            if (holon.ParentPlanetId == Guid.Empty)
            {
                holon.ParentPlanetId = parentHolon.ParentPlanetId;
                holon.ParentPlanet = parentHolon.ParentPlanet;
            }

            if (holon.ParentMoonId == Guid.Empty)
            {
                holon.ParentMoonId = parentHolon.ParentMoonId;
                holon.ParentMoon = parentHolon.ParentMoon;
            }

            if (holon.ParentCelestialSpaceId == Guid.Empty)
            {
                holon.ParentCelestialSpaceId = parentHolon.ParentCelestialSpaceId;
                holon.ParentCelestialSpace = parentHolon.ParentCelestialSpace;
            }

            if (holon.ParentCelestialBodyId == Guid.Empty)
            {
                holon.ParentCelestialBodyId = parentHolon.ParentCelestialBodyId;
                holon.ParentCelestialBody = parentHolon.ParentCelestialBody;
            }

            if (holon.ParentZomeId == Guid.Empty)
            {
                holon.ParentZomeId = parentHolon.ParentZomeId;
                holon.ParentZome = parentHolon.ParentZome;
            }

            if (holon.ParentHolonId == Guid.Empty)
            {
                holon.ParentHolonId = parentHolon.ParentHolonId;
                holon.ParentHolon = parentHolon.ParentHolon;
            }

            switch (parentHolon.HolonType)
            {
                case HolonType.GreatGrandSuperStar:
                    holon.ParentGreatGrandSuperStarId = parentHolon.Id;
                    holon.ParentGreatGrandSuperStar = (IGreatGrandSuperStar)parentHolon;
                    holon.ParentCelestialBodyId = parentHolon.Id;
                    holon.ParentCelestialBody = (ICelestialBody)parentHolon;
                    holon.ParentHolonId = parentHolon.Id;
                    holon.ParentHolon = parentHolon; //ParentHolon;
                    break;

                case HolonType.GrandSuperStar:
                    holon.ParentGrandSuperStarId = parentHolon.Id;
                    holon.ParentGrandSuperStar = (IGrandSuperStar)parentHolon;
                    holon.ParentCelestialBodyId = parentHolon.Id;
                    holon.ParentCelestialBody = (ICelestialBody)parentHolon;
                    holon.ParentHolonId = parentHolon.Id;
                    holon.ParentHolon = parentHolon; //ParentHolon;
                    break;

                case HolonType.SuperStar:
                    holon.ParentSuperStarId = parentHolon.Id;
                    holon.ParentSuperStar = (ISuperStar)parentHolon;
                    holon.ParentCelestialBodyId = parentHolon.Id;
                    holon.ParentCelestialBody = (ICelestialBody)parentHolon;
                    holon.ParentHolonId = parentHolon.Id;
                    holon.ParentHolon = parentHolon; //ParentHolon;
                    break;

                case HolonType.Multiverse:
                    holon.ParentMultiverseId = parentHolon.Id;
                    holon.ParentMultiverse = (IMultiverse)parentHolon;
                    holon.ParentCelestialSpaceId = parentHolon.Id;
                    holon.ParentCelestialSpace = (ICelestialSpace)parentHolon;
                    holon.ParentHolonId = parentHolon.Id;
                    holon.ParentHolon = parentHolon; //ParentHolon;
                    break;

                case HolonType.Universe:
                    holon.ParentUniverseId = parentHolon.Id;
                    holon.ParentUniverse = (IUniverse)parentHolon;
                    holon.ParentCelestialSpaceId = parentHolon.Id;
                    holon.ParentCelestialSpace = (ICelestialSpace)parentHolon;
                    holon.ParentHolonId = parentHolon.Id;
                    holon.ParentHolon = parentHolon; //ParentHolon;
                    break;

                case HolonType.Dimension:
                    holon.ParentDimensionId = parentHolon.Id;
                    holon.ParentDimension = (IDimension)parentHolon;
                    holon.ParentCelestialSpaceId = parentHolon.Id;
                    holon.ParentCelestialSpace = (ICelestialSpace)parentHolon;
                    holon.ParentHolonId = parentHolon.Id;
                    holon.ParentHolon = parentHolon; //ParentHolon;
                    break;

                case HolonType.GalaxyCluster:
                    holon.ParentGalaxyClusterId = parentHolon.Id;
                    holon.ParentGalaxyCluster = (IGalaxyCluster)parentHolon;
                    holon.ParentCelestialSpaceId = parentHolon.Id;
                    holon.ParentCelestialSpace = (ICelestialSpace)parentHolon;
                    holon.ParentHolonId = parentHolon.Id;
                    holon.ParentHolon = parentHolon; //ParentHolon;
                    break;

                case HolonType.Galaxy:
                    holon.ParentGalaxyId = parentHolon.Id;
                    holon.ParentGalaxy = (IGalaxy)parentHolon;
                    holon.ParentCelestialSpaceId = parentHolon.Id;
                    holon.ParentCelestialSpace = (ICelestialSpace)parentHolon;
                    holon.ParentHolonId = parentHolon.Id;
                    holon.ParentHolon = parentHolon; //ParentHolon;
                    break;

                case HolonType.SolarSystem:
                    holon.ParentSolarSystemId = parentHolon.Id;
                    holon.ParentSolarSystem = (ISolarSystem)parentHolon;
                    holon.ParentCelestialSpaceId = parentHolon.Id;
                    holon.ParentCelestialSpace = (ICelestialSpace)parentHolon;
                    holon.ParentHolonId = parentHolon.Id;
                    holon.ParentHolon = parentHolon; //ParentHolon;
                    break;

                case HolonType.Star:
                    holon.ParentStarId = parentHolon.Id;
                    holon.ParentStar = (IStar)parentHolon;
                    holon.ParentCelestialBodyId = parentHolon.Id;
                    holon.ParentCelestialBody = (ICelestialBody)parentHolon;
                    holon.ParentHolonId = parentHolon.Id;
                    holon.ParentHolon = parentHolon; //ParentHolon;
                    break;

                case HolonType.Planet:
                    holon.ParentPlanetId = parentHolon.Id;
                    holon.ParentPlanet = (IPlanet)parentHolon;
                    holon.ParentCelestialBodyId = parentHolon.Id;
                    holon.ParentCelestialBody = (ICelestialBody)parentHolon;
                    holon.ParentHolonId = parentHolon.Id;
                    holon.ParentHolon = parentHolon; //ParentHolon;
                    break;

                case HolonType.Moon:
                    holon.ParentMoonId = parentHolon.Id;
                    holon.ParentMoon = (IMoon)parentHolon;
                    holon.ParentCelestialBodyId = parentHolon.Id;
                    holon.ParentCelestialBody = (ICelestialBody)parentHolon;
                    holon.ParentHolonId = parentHolon.Id;
                    holon.ParentHolon = parentHolon; //ParentHolon;
                    break;

                case HolonType.Zome:
                    holon.ParentZomeId = parentHolon.Id;
                    holon.ParentZome = (IZome)parentHolon;
                    holon.ParentHolonId = parentHolon.Id;
                    holon.ParentHolon = parentHolon; //ParentHolon;
                    break;

                case HolonType.Holon:
                    holon.ParentHolonId = parentHolon.Id;
                    holon.ParentHolon = parentHolon;
                    break;
            }

            holons.Add(holon);

            //OASISResult<IEnumerable<IHolon>> holonsResult = await base.SaveHolonsAsync(holons, false);
            //OASISResult<IEnumerable<IHolon>> holonsResult = await base.SaveHolonsAsync(holons, false); //TODO: Temp to test new code...

            if (saveHolon)
            {
                result = await SaveHolonAsync(holon, avatarId, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType); //TODO: WE ONLY NEED TO SAVE THE NEW HOLON, NO NEED TO RE-SAVE THE WHOLE COLLECTION AGAIN! ;-)
                result.IsSaved = true;
            }
            else
            {
                result.Message = "Holon was not saved due to saveHolon being set to false.";
                result.IsSaved = false;
                result.Result = holon;
            }

            return result;
        }

        #region Settings Management

        /// <summary>
        /// Save a setting for an avatar in a specific category
        /// </summary>
        /// <typeparam name="T">Type of the setting value</typeparam>
        /// <param name="avatarId">Avatar ID</param>
        /// <param name="category">Settings category (subscription, notifications, privacy, hyperdrive, custom)</param>
        /// <param name="key">Setting key</param>
        /// <param name="value">Setting value</param>
        /// <returns>Success result</returns>
        public async Task<OASISResult<bool>> SaveSettingAsync<T>(Guid avatarId, string category, string key, T value)
        {
            var result = new OASISResult<bool>();
            try
            {
                // Get or create the settings holon for this category
                var settingsHolon = await GetOrCreateSettingsHolonAsync(avatarId, category);
                
                // Update the setting in MetaData
                settingsHolon.MetaData[key] = value;
                settingsHolon.ModifiedDate = DateTime.UtcNow;
                
                // Save the updated holon
                var saveResult = await SaveHolonAsync(settingsHolon);
                if (saveResult.IsError)
                {
                    result.IsError = true;
                    result.Message = $"Failed to save setting: {saveResult.Message}";
                    return result;
                }
                
                result.Result = true;
                result.Message = $"Setting '{key}' saved successfully in category '{category}'";
                try { SettingsSaved?.Invoke(avatarId, category); } catch { }
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error saving setting: {ex.Message}";
                result.Exception = ex;
            }
            return result;
        }

        /// <summary>
        /// Save multiple settings for an avatar in a specific category in one operation
        /// </summary>
        /// <param name="avatarId">Avatar ID</param>
        /// <param name="category">Settings category (subscription, notifications, privacy, hyperdrive, custom)</param>
        /// <param name="settings">Dictionary of settings to save</param>
        /// <returns>Success result</returns>
        public async Task<OASISResult<bool>> SaveSettingsAsync(Guid avatarId, string category, Dictionary<string, object> settings)
        {
            var result = new OASISResult<bool>();
            try
            {
                // Get or create the settings holon for this category
                var settingsHolon = await GetOrCreateSettingsHolonAsync(avatarId, category);
                
                // Update all settings in MetaData in one go
                foreach (var setting in settings)
                {
                    settingsHolon.MetaData[setting.Key] = setting.Value;
                }
                // bump a simple version stamp for cache invalidation
                settingsHolon.MetaData["_versionStamp"] = DateTime.UtcNow.Ticks;
                settingsHolon.ModifiedDate = DateTime.UtcNow;
                
                // Save the updated holon
                var saveResult = await SaveHolonAsync(settingsHolon);
                if (saveResult.IsError)
                {
                    result.IsError = true;
                    result.Message = $"Failed to save settings: {saveResult.Message}";
                    return result;
                }
                
                result.Result = true;
                result.Message = $"{settings.Count} settings saved successfully in category '{category}'";

                try
                {
                    SettingsSaved?.Invoke(avatarId, category);
                }
                catch { }
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error saving settings: {ex.Message}";
                result.Exception = ex;
            }
            return result;
        }

        /// <summary>
        /// Load a setting for an avatar from a specific category
        /// </summary>
        /// <typeparam name="T">Type of the setting value</typeparam>
        /// <param name="avatarId">Avatar ID</param>
        /// <param name="category">Settings category (subscription, notifications, privacy, hyperdrive, custom)</param>
        /// <param name="key">Setting key</param>
        /// <param name="defaultValue">Default value if setting not found</param>
        /// <returns>Setting value or default</returns>
        public async Task<OASISResult<T>> LoadSettingAsync<T>(Guid avatarId, string category, string key, T defaultValue = default(T))
        {
            var result = new OASISResult<T>();
            try
            {
                // Get the settings holon for this category
                var settingsHolon = await GetOrCreateSettingsHolonAsync(avatarId, category);
                
                // Get the setting from MetaData
                if (settingsHolon.MetaData.ContainsKey(key))
                {
                    var value = settingsHolon.MetaData[key];
                    if (value is T)
                    {
                        result.Result = (T)value;
                    }
                    else
                    {
                        result.Result = defaultValue;
                    }
                }
                else
                {
                    result.Result = defaultValue;
                }
                
                result.Message = $"Setting '{key}' loaded from category '{category}'";
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error loading setting: {ex.Message}";
                result.Exception = ex;
                result.Result = defaultValue;
            }
            return result;
        }

        /// <summary>
        /// Get all settings for an avatar from a specific category
        /// </summary>
        /// <param name="avatarId">Avatar ID</param>
        /// <param name="category">Settings category</param>
        /// <returns>Dictionary of all settings in the category</returns>
        public async Task<OASISResult<Dictionary<string, object>>> GetAllSettingsAsync(Guid avatarId, string category)
        {
            var result = new OASISResult<Dictionary<string, object>>();
            try
            {
                // Get the settings holon for this category
                var settingsHolon = await GetOrCreateSettingsHolonAsync(avatarId, category);
                
                result.Result = new Dictionary<string, object>(settingsHolon.MetaData);
                result.Message = $"All settings loaded from category '{category}'";
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error loading all settings: {ex.Message}";
                result.Exception = ex;
                result.Result = new Dictionary<string, object>();
            }
            return result;
        }

        /// <summary>
        /// Get or create a settings holon for an avatar and category
        /// </summary>
        /// <param name="avatarId">Avatar ID</param>
        /// <param name="category">Settings category</param>
        /// <returns>Settings holon</returns>
        private async Task<IHolon> GetOrCreateSettingsHolonAsync(Guid avatarId, string category)
        {
            try
            {
                // Get lookup holon ID from OASISDNA
                var lookupHolonId = GetLookupHolonIdFromDNA();
                
                if (lookupHolonId == Guid.Empty)
                {
                    // Create new lookup holon if none exists
                    lookupHolonId = await CreateLookupHolonAsync();
                }
                
                // Load the lookup holon to get the mapping
                var lookupHolon = await LoadHolonAsync(lookupHolonId);
                
                if (lookupHolon.IsError || lookupHolon.Result == null)
                {
                    // Create new lookup holon if loading failed
                    lookupHolonId = await CreateLookupHolonAsync();
                    lookupHolon = await LoadHolonAsync(lookupHolonId);
                }
                
                // Create the mapping key
                var mappingKey = $"settings_avatar_{avatarId}_{category}";
                
                // Check if mapping exists in lookup holon
                if (lookupHolon.Result.MetaData.ContainsKey(mappingKey))
                {
                    var settingsHolonId = Guid.Parse(lookupHolon.Result.MetaData[mappingKey].ToString());
                    
                    // Load the existing settings holon
                    var settingsHolon = await LoadHolonAsync(settingsHolonId);
                    
                    if (!settingsHolon.IsError && settingsHolon.Result != null)
                    {
                        return settingsHolon.Result;
                    }
                }
                
                // Create new settings holon
                var newSettingsHolon = new Holon
                {
                    Id = Guid.NewGuid(),
                    Name = mappingKey,
                    Description = $"Settings for avatar {avatarId} in category {category}",
                    CreatedByAvatarId = avatarId,
                    CreatedDate = DateTime.UtcNow,
                    ModifiedDate = DateTime.UtcNow,
                    MetaData = new Dictionary<string, object>()
                };
                
                // Save the new settings holon
                await SaveHolonAsync(newSettingsHolon);
                
                // Update lookup holon with new mapping
                lookupHolon.Result.MetaData[mappingKey] = newSettingsHolon.Id.ToString();
                lookupHolon.Result.ModifiedDate = DateTime.UtcNow;
                await SaveHolonAsync(lookupHolon.Result);
                
                return newSettingsHolon;
            }
            catch (Exception ex)
            {
                // Fallback: create settings holon directly (without lookup)
                var holonName = $"settings_avatar_{avatarId}_{category}";
                var newHolon = new Holon
                {
                    Id = Guid.NewGuid(),
                    Name = holonName,
                    Description = $"Settings for avatar {avatarId} in category {category}",
                    CreatedByAvatarId = avatarId,
                    CreatedDate = DateTime.UtcNow,
                    ModifiedDate = DateTime.UtcNow,
                    MetaData = new Dictionary<string, object>()
                };
                
                await SaveHolonAsync(newHolon);
                return newHolon;
            }
        }
        
        /// <summary>
        /// Get lookup holon ID from OASISDNA
        /// </summary>
        /// <returns>Lookup holon ID</returns>
        private Guid GetLookupHolonIdFromDNA()
        {
            try
            {
                if (OASISDNA != null && OASISDNA.OASIS.SettingsLookupHolonId != Guid.Empty)
                {
                    return OASISDNA.OASIS.SettingsLookupHolonId;
                }
                return Guid.Empty;
            }
            catch
            {
                return Guid.Empty;
            }
        }
        
        /// <summary>
        /// Create a new lookup holon and store its ID in OASISDNA
        /// </summary>
        /// <returns>New lookup holon ID</returns>
        private async Task<Guid> CreateLookupHolonAsync()
        {
            try
            {
                var lookupHolon = new Holon
                {
                    Id = Guid.NewGuid(),
                    Name = "SettingsLookup",
                    Description = "Lookup holon for settings avatar/category to holon ID mappings",
                    CreatedDate = DateTime.UtcNow,
                    ModifiedDate = DateTime.UtcNow,
                    MetaData = new Dictionary<string, object>()
                };
                
                await SaveHolonAsync(lookupHolon);
                
                // Store lookup holon ID in OASISDNA
                if (OASISDNA != null)
                {
                    OASISDNA.OASIS.SettingsLookupHolonId = lookupHolon.Id;
                    // Note: OASISDNA will be saved when the application shuts down or when explicitly saved elsewhere
                }
                
                return lookupHolon.Id;
            }
            catch
            {
                return Guid.Empty;
            }
        }

        #endregion
    }
} 