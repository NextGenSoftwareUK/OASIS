using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Generic;
using NextGenSoftware.Logging;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.CustomAttrbiutes;
using System.Collections.Immutable;
using System.Drawing;
using System.Reflection.Metadata;
using System.Text.Json;
using NextGenSoftware.OASIS.API.DNA;

namespace NextGenSoftware.OASIS.API.Core.Managers
{
    public partial class HolonManager : OASISManager
    {
        /// <summary>
        /// Used when hydrating holon MetaData JSON into CLR types (e.g. quest objectives list).
        /// Must be case-insensitive: persisted rows often use Pascal <c>Title</c>/<c>Description</c> while the ONODE objective type maps JSON names <c>title</c>/<c>description</c>;
        /// default System.Text.Json would leave authored strings empty while requirement dictionaries may still deserialize.
        /// </summary>
        private static readonly JsonSerializerOptions MetaDataComplexTypeDeserializeOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        private Dictionary<Guid, IOmiverse> _parentOmiverse = new Dictionary<Guid, IOmiverse>();
        private Dictionary<Guid, IDimension> _parentDimension = new Dictionary<Guid, IDimension>();
        private Dictionary<Guid, IMultiverse> _parentMultiverse = new Dictionary<Guid, IMultiverse>();
        private Dictionary<Guid, IUniverse> _parentUniverse = new Dictionary<Guid, IUniverse>();
        private Dictionary<Guid, IGalaxyCluster> _parentGalaxyCluster = new Dictionary<Guid, IGalaxyCluster>();
        private Dictionary<Guid, IGalaxy> _parentGalaxy = new Dictionary<Guid, IGalaxy>();
        private Dictionary<Guid, ISolarSystem> _parentSolarSystem = new Dictionary<Guid, ISolarSystem>();
        private Dictionary<Guid, IGreatGrandSuperStar> _parentGreatGrandSuperStar = new Dictionary<Guid, IGreatGrandSuperStar>();
        private Dictionary<Guid, IGrandSuperStar> _parentGrandSuperStar = new Dictionary<Guid, IGrandSuperStar>();
        private Dictionary<Guid, ISuperStar> _parentSuperStar = new Dictionary<Guid, ISuperStar>();
        private Dictionary<Guid, IStar> _parentStar = new Dictionary<Guid, IStar>();
        private Dictionary<Guid, IPlanet> _parentPlanet = new Dictionary<Guid, IPlanet>();
        private Dictionary<Guid, IMoon> _parentMoon = new Dictionary<Guid, IMoon>();
        private Dictionary<Guid, ICelestialSpace> _parentCelestialSpace = new Dictionary<Guid, ICelestialSpace>();
        private Dictionary<Guid, ICelestialBody> _parentCelestialBody = new Dictionary<Guid, ICelestialBody>();
        private Dictionary<Guid, IZome> _parentZome = new Dictionary<Guid, IZome>();
        private Dictionary<Guid, IHolon> _parentHolon = new Dictionary<Guid, IHolon>();
        private Dictionary<Guid, ICelestialBodyCore> _core = new Dictionary<Guid, ICelestialBodyCore>();

        public Dictionary<Guid, ICelestialBodyCore> CelestialBodyCoreCache
        {
            get
            {
                return _core;
            }
        }

        private IHolon PrepareHolonForSaving(IHolon holon, Guid avatarId, bool extractMetaData)
        {
            // Callers (SaveHolon overloads) validate holon != null and return OASISResult before calling here.
            // TODO: I think it's best to include audit stuff here so the providers do not need to worry about it?
            // Providers could always override this behaviour if they choose...

            // CHANGED: Previously IsNewHolon was set to true if EITHER Id == Guid.Empty OR
            // CreatedDate == DateTime.MinValue. The CreatedDate check was problematic for
            // stateless REST/JS clients (e.g. Vercel functions) that construct a holon
            // object from scratch and never set CreatedDate — causing every save to be
            // treated as an insert, creating a new MongoDB document every time instead of
            // updating the existing one. Id == Guid.Empty is the correct and sufficient
            // signal that a holon has not been persisted yet. CreatedDate is set below for
            // new holons; for existing ones the caller simply won't know it, and that is fine.
            //
            // Old code (kept for reference):
            // if (holon.Id == Guid.Empty || holon.CreatedDate == DateTime.MinValue)
            // {
            //     if (holon.Id == Guid.Empty)
            //         holon.Id = Guid.NewGuid();
            //     holon.IsNewHolon = true;
            // }
            // else if (holon.CreatedDate != DateTime.MinValue)
            //     holon.IsNewHolon = false;

            if (holon.Id == Guid.Empty)
            {
                holon.Id = Guid.NewGuid();
                holon.IsNewHolon = true;
            }
            else
                holon.IsNewHolon = false;

            //if (holon.Id != Guid.Empty)
            if (!holon.IsNewHolon)
            {
                holon.ModifiedDate = DateTime.Now;
                holon.ModifiedByAvatarId = avatarId;
                //if (AvatarManager.LoggedInAvatar != null)
                // holon.ModifiedByAvatarId = AvatarManager.LoggedInAvatar.Id;

                holon.Version++;
                holon.PreviousVersionId = holon.VersionId;
                holon.VersionId = Guid.NewGuid();
            }
            else
            {
                holon.IsActive = true;
                holon.CreatedDate = DateTime.Now;
                holon.CreatedByAvatarId = avatarId;

                //if (AvatarManager.LoggedInAvatar != null)
                //{
                //    holon.CreatedByAvatarId = AvatarManager.LoggedInAvatar.Id;
                //    holon.ParentHolonId = AvatarManager.LoggedInAvatar.Id;
                //}

                holon.Version = 1;
                holon.VersionId = Guid.NewGuid();
            }

            //If the parentHolonId hasn't been set then default it to the CreatedByAvatarId.
            if (holon.ParentHolonId == Guid.Empty)
                holon.ParentHolonId = holon.CreatedByAvatarId;

            holon.Original = null;

            // Retreive any custom properties and store in the holon metadata dictionary.
            // TODO: Would ideally like to find a better way to do this so we can avoid reflection if possible because of the potential overhead!
            // Need to do some perfomrnace tests with reflection turned on/off (so with this code enabled/disabled) to see what the overhead is exactly...

            // We only want to extract the meta data for sub-classes of Holon that are calling the Generic overloads.
            if (holon.GetType() != typeof(Holon) && extractMetaData)
            {
                PropertyInfo[] props = holon.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

                foreach (PropertyInfo propertyInfo in props)
                {
                    var customAttr = propertyInfo.GetCustomAttribute<CustomOASISProperty>();
                    if (customAttr != null)
                    {
                        bool useJson = customAttr.StoreAsJsonString;
                        var value = propertyInfo.GetValue(holon);
                        if (useJson && value != null)
                            holon.MetaData[propertyInfo.Name] = JsonSerializer.Serialize(value);
                        else
                            holon.MetaData[propertyInfo.Name] = value;
                    }
                }
            }

            /* Ensure CreatedByAvatarId and Active are in MetaData so LoadHolonsByMetaData (e.g. by CreatedByAvatarId + Active) returns this holon, including child quests/sub-quests saved via SaveAsync (not only via STARNETManagerBase.CreateAsync). */
            if (holon.MetaData == null)
                holon.MetaData = new Dictionary<string, object>();

            holon.MetaData["CreatedByAvatarId"] = holon.CreatedByAvatarId.ToString();

            if (holon.IsNewHolon) 
                holon.IsActive = true;

            holon.MetaData["Active"] = holon.IsActive ? "1" : "0";

            //if (holon.AllChildren == null)
            //    holon.AllChildren = new List<IHolon>(holon.Children);
                //holon.AllChildren = new List<Holon>(holon.Children.ToImmutableList()); //TODO: Investigate ImmutableList...

            SetParentIdsForHolon(avatarId, extractMetaData, holon);
            RemoveCelesialBodies(holon);

            EncryptHolonMetaData(holon);

            return holon;
        }

        // Keys that must always remain plain text in MetaData so provider queries and internal
        // framework lookups continue to work when HolonDataEncryption is enabled.
        //
        // Sources:
        //   PrepareHolonForSaving sets: CreatedByAvatarId, Active
        //   MongoDB provider queries: HolonType (geo/radius searches)
        //   SettingsManager: _versionStamp (optimistic concurrency)
        //   SaveFile/LoadFile: data (binary blob)
        //   Provider avatar lookups (Zcash, Aztec, LocalFile, etc.): Username, Email, Name,
        //     Description, ParentId, ParentProviderKey, SearchQuery, parkType, env
        //   Provider NFT lookups: web3TokenId, NFT.MintedByAvatarId, NFT.MintWalletAddress,
        //     NFT.ParentWeb4NFTId, GEONFT.PlacedByAvatarId, GEONFT.MintedByAvatarId,
        //     GEONFT.OriginalOASISNFT.MintWalletAddress, GEONFT.LatLong
        //   [CustomOASISProperty] keys commonly queried via LoadHolonsByMetaData:
        //     Status (QuestBase), ActiveQuestId, ActiveObjectiveId (AvatarDetail)
        //
        // Add any app-specific queryable keys to HolonDataEncryption.AdditionalQueryableKeys
        // in OASIS_DNA.json rather than hardcoding them here.
        private static readonly HashSet<string> _systemMetaKeys = new()
        {
            // Framework-set
            "CreatedByAvatarId", "Active", "HolonType", "_versionStamp", "data",
            // Provider avatar/search lookups
            "Username", "Email", "Name", "Description",
            "ParentId", "ParentProviderKey", "SearchQuery", "parkType", "env",
            // Provider NFT lookups
            "web3TokenId",
            "NFT.MintedByAvatarId", "NFT.MintWalletAddress", "NFT.ParentWeb4NFTId",
            "GEONFT.PlacedByAvatarId", "GEONFT.MintedByAvatarId",
            "GEONFT.OriginalOASISNFT.MintWalletAddress", "GEONFT.LatLong",
            // [CustomOASISProperty] keys queried by the quest/avatar system
            "Status", "ActiveQuestId", "ActiveObjectiveId", "ParentMissionId",
            // MongoDB avatar type discriminator value
            "Avatar",
        };
        private const string EncMetaKey = "__oasis_enc__";

        private void EncryptHolonMetaData(IHolon holon)
        {
            var encSettings = (holon as Holon)?.DataEncryptionOverride
                              ?? OASISDNAManager.OASISDNA?.OASIS?.Security?.HolonDataEncryption;

            if (encSettings == null || holon.MetaData == null || holon.MetaData.Count == 0) return;
            if (encSettings.Rijndael256EncryptionEnabled != true && encSettings.QuantumEncryptionEnabled != true) return;
            if (holon.MetaData.ContainsKey(EncMetaKey)) return; // already encrypted

            // Build the effective exempt set: built-in system keys + any deployment-configured queryable keys.
            var exemptKeys = encSettings.AdditionalQueryableKeys?.Count > 0
                ? new HashSet<string>(_systemMetaKeys.Concat(encSettings.AdditionalQueryableKeys))
                : _systemMetaKeys;

            var userKeys = holon.MetaData.Keys.Where(k => !exemptKeys.Contains(k)).ToList();
            if (userKeys.Count == 0) return;

            var toEncrypt = userKeys.ToDictionary(k => k, k => holon.MetaData[k]);
            var json = JsonSerializer.Serialize(toEncrypt);
            var encrypted = PasswordEncryptionHelper.EncryptValue(json, encSettings);

            foreach (var key in userKeys)
                holon.MetaData.Remove(key);

            holon.MetaData[EncMetaKey] = encrypted;
        }

        internal void DecryptHolonMetaData(IHolon holon)
        {
            if (holon?.MetaData == null || !holon.MetaData.ContainsKey(EncMetaKey)) return;

            var encSettings = OASISDNAManager.OASISDNA?.OASIS?.Security?.HolonDataEncryption;
            if (encSettings == null) return;

            try
            {
                var encrypted = holon.MetaData[EncMetaKey]?.ToString();
                if (string.IsNullOrEmpty(encrypted)) return;

                var json = PasswordEncryptionHelper.DecryptValue(encrypted, encSettings);
                var restored = JsonSerializer.Deserialize<Dictionary<string, object>>(json);

                holon.MetaData.Remove(EncMetaKey);
                if (restored != null)
                    foreach (var kv in restored)
                        holon.MetaData[kv.Key] = kv.Value;
            }
            catch { }
        }

        /// <summary>
        /// Copies [CustomOASISProperty] fields from the holon into its MetaData so providers persist them (e.g. AvatarDetail.ActiveQuestId, ActiveObjectiveId).
        /// Call before saving a holon when the save path does not go through SaveHolon (e.g. SaveAvatarDetailAsync).
        /// </summary>
        public void ExtractCustomPropertiesToMetaData(IHolon holon)
        {
            if (holon == null) return;
            if (holon.MetaData == null)
                holon.MetaData = new Dictionary<string, object>();
            if (holon.GetType() == typeof(Holon)) return;

            var props = holon.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var propertyInfo in props)
            {
                var customAttr = propertyInfo.GetCustomAttribute<CustomOASISProperty>();
                if (customAttr == null) continue;
                var value = propertyInfo.GetValue(holon);
                if (customAttr.StoreAsJsonString && value != null)
                    holon.MetaData[propertyInfo.Name] = JsonSerializer.Serialize(value);
                else
                    holon.MetaData[propertyInfo.Name] = value;
            }
        }

        private IEnumerable<IHolon> PrepareHolonsForSaving(IEnumerable<IHolon> holons, Guid avatarId, bool extractMetaData)
        {
            List<IHolon> holonsToReturn = new List<IHolon>();

            foreach (IHolon holon in holons)
                holonsToReturn.Add(PrepareHolonForSaving(holon, avatarId, extractMetaData));

            return holonsToReturn;
        }

        private IEnumerable<IHolon> PrepareHolonsForSaving<T>(IEnumerable<T> holons, Guid avatarId, bool extractMetaData)
        {
            List<IHolon> holonsToReturn = new List<IHolon>();

            foreach (T holon in holons)
                holonsToReturn.Add(PrepareHolonForSaving((IHolon)holon, avatarId, extractMetaData));

            return holonsToReturn;
        }

        //private void SetParentIdsForZome(IGreatGrandSuperStar greatGrandSuperStar, IGrandSuperStar grandSuperStar, ISuperStar superStar, IStar star, IPlanet planet, IMoon moon, IZome zome)
        //{
        //    //TODO: Not sure if we even need this?!
        //    //SetParentIdsForHolon(greatGrandSuperStar, grandSuperStar, superStar, star, planet, moon, null, zome); //A zome is also a holon (everything is a holon).
        //    //SetParentIdsForHolon(greatGrandSuperStar, grandSuperStar, superStar, star, planet, moon, zome, null); //A zome is also a holon (everything is a holon).

        //    if (zome.Holons != null)
        //    {
        //        foreach (IHolon holon in zome.Holons)
        //            SetParentIdsForHolon(greatGrandSuperStar, grandSuperStar, superStar, star, planet, moon, zome, holon);
        //    }
        //}

        private void SetParentIdsForHolon(Guid avatarId, bool extractMetaData, IHolon holon)
        {
            //Make sure all ids's are set.
            if (holon.ParentCelestialBody != null)
                holon.ParentCelestialBodyId = holon.ParentCelestialBody.Id;

            if (holon.ParentCelestialSpace != null)
                holon.ParentCelestialSpaceId = holon.ParentCelestialSpace.Id;

            if (holon.ParentDimension != null)
                holon.ParentDimensionId = holon.ParentDimension.Id;

            if (holon.ParentOmniverse != null)
                holon.ParentOmniverseId = holon.ParentOmniverse.Id;

            if (holon.ParentMultiverse != null)
                holon.ParentMultiverseId = holon.ParentMultiverse.Id;

            if (holon.ParentUniverse != null)
                holon.ParentUniverseId = holon.ParentUniverse.Id;

            if (holon.ParentGalaxyCluster != null)
                holon.ParentGalaxyClusterId = holon.ParentGalaxyCluster.Id;

            if (holon.ParentGalaxy != null)
                holon.ParentGalaxyId = holon.ParentGalaxy.Id;

            if (holon.ParentSolarSystem != null)
                holon.ParentSolarSystemId = holon.ParentSolarSystem.Id;

            if (holon.ParentGreatGrandSuperStar != null)
                holon.ParentGreatGrandSuperStarId = holon.ParentGreatGrandSuperStar.Id;

            if (holon.ParentGrandSuperStar != null)
                holon.ParentGrandSuperStarId = holon.ParentGrandSuperStar.Id;

            if (holon.ParentSuperStar != null)
                holon.ParentSuperStarId = holon.ParentSuperStar.Id;

            if (holon.ParentStar != null)
                holon.ParentStarId = holon.ParentStar.Id;

            if (holon.ParentPlanet != null)
                holon.ParentPlanetId = holon.ParentPlanet.Id;

            if (holon.ParentMoon != null)
                holon.ParentMoonId = holon.ParentMoon.Id;

            if (holon.ParentZome != null)
                holon.ParentZomeId = holon.ParentZome.Id;

            if (holon.ParentHolon != null)
                holon.ParentHolonId = holon.ParentHolon.Id;

            //If there is a parentHolon then it will set any missing celestial spaces/bodies to the same as the parent (ideally these will already be set but this is a fail-safe/fallback just in case).
            if (holon.ParentHolon != null)
            {
                if (holon.ParentHolon.ParentGreatGrandSuperStar != null)
                {
                    if (holon.ParentGreatGrandSuperStarId == Guid.Empty && holon.Id != holon.ParentHolon.ParentGreatGrandSuperStar.Id)
                    {
                        holon.ParentMultiverseId = holon.ParentHolon.ParentGreatGrandSuperStar.Id;

                        if (holon.ParentGreatGrandSuperStar == null)
                            holon.ParentGreatGrandSuperStar = holon.ParentHolon.ParentGreatGrandSuperStar;
                    }
                }
                else if (holon.ParentGreatGrandSuperStarId == Guid.Empty && holon.ParentHolon.ParentGreatGrandSuperStarId != holon.Id)
                    holon.ParentGreatGrandSuperStarId = holon.ParentHolon.ParentGreatGrandSuperStarId;


                //TODO: Apply above to all below ASAP!
                if (holon.ParentHolon.ParentGrandSuperStar != null)
                {
                    if (holon.ParentGrandSuperStar == null)
                        holon.ParentGrandSuperStar = holon.ParentHolon.ParentGrandSuperStar;

                    if (holon.ParentGrandSuperStarId == Guid.Empty)
                        holon.ParentGrandSuperStarId = holon.ParentHolon.ParentGrandSuperStar.Id;
                }
                else if (holon.ParentGrandSuperStarId == Guid.Empty)
                    holon.ParentGrandSuperStarId = holon.ParentHolon.ParentGrandSuperStarId;

                if (holon.ParentHolon.ParentSuperStar != null)
                {
                    if (holon.ParentSuperStar == null)
                        holon.ParentSuperStar = holon.ParentHolon.ParentSuperStar;

                    if (holon.ParentSuperStarId == Guid.Empty)
                        holon.ParentSuperStarId = holon.ParentHolon.ParentSuperStar.Id;
                }
                else if (holon.ParentSuperStarId == Guid.Empty)
                    holon.ParentSuperStarId = holon.ParentHolon.ParentSuperStarId;

                if (holon.ParentHolon.ParentStar != null)
                {
                    if (holon.ParentStar == null)
                        holon.ParentStar = holon.ParentHolon.ParentStar;

                    if (holon.ParentStarId == Guid.Empty)
                        holon.ParentStarId = holon.ParentHolon.ParentStar.Id;
                }
                else if (holon.ParentStarId == Guid.Empty)
                    holon.ParentStarId = holon.ParentHolon.ParentStarId;

                if (holon.ParentHolon.ParentPlanet != null)
                {
                    if (holon.ParentPlanet == null)
                        holon.ParentPlanet = holon.ParentHolon.ParentPlanet;

                    if (holon.ParentPlanetId == Guid.Empty)
                        holon.ParentPlanetId = holon.ParentHolon.ParentPlanet.Id;
                }
                else if (holon.ParentPlanetId == Guid.Empty)
                    holon.ParentPlanetId = holon.ParentHolon.ParentPlanetId;

                if (holon.ParentHolon.ParentMoon != null)
                {
                    if (holon.ParentMoon == null)
                        holon.ParentMoon = holon.ParentHolon.ParentMoon;

                    if (holon.ParentMoonId == Guid.Empty)
                        holon.ParentMoonId = holon.ParentHolon.ParentMoon.Id;
                }
                else if (holon.ParentMoonId == Guid.Empty)
                    holon.ParentMoonId = holon.ParentHolon.ParentMoonId;

                if (holon.ParentHolon.ParentZome != null)
                {
                    if (holon.ParentZome == null)
                        holon.ParentZome = holon.ParentHolon.ParentZome;

                    if (holon.ParentZomeId == Guid.Empty)
                        holon.ParentZomeId = holon.ParentHolon.ParentZome.Id;
                }
                else if (holon.ParentZomeId == Guid.Empty)
                    holon.ParentZomeId = holon.ParentHolon.ParentZomeId;

                if (holon.ParentHolon.ParentCelestialBody != null)
                {
                    if (holon.ParentCelestialBody == null)
                        holon.ParentCelestialBody = holon.ParentHolon.ParentCelestialBody;

                    if (holon.ParentCelestialBodyId == Guid.Empty)
                        holon.ParentCelestialBodyId = holon.ParentHolon.ParentCelestialBody.Id;
                }
                else if (holon.ParentCelestialBodyId == Guid.Empty)
                    holon.ParentCelestialBodyId = holon.ParentHolon.ParentCelestialBodyId;



                if (holon.ParentHolon.ParentCelestialSpace != null)
                {
                    if (holon.ParentCelestialSpaceId == Guid.Empty && holon.Id != holon.ParentHolon.ParentCelestialSpace.Id)
                    {
                        holon.ParentCelestialSpaceId = holon.ParentHolon.ParentCelestialSpace.Id;

                        if (holon.ParentCelestialSpace == null)
                            holon.ParentCelestialSpace = holon.ParentHolon.ParentCelestialSpace;
                    }
                }
                else if (holon.ParentCelestialSpaceId == Guid.Empty && holon.ParentHolon.ParentCelestialSpaceId != holon.Id)
                    holon.ParentCelestialSpaceId = holon.ParentHolon.ParentCelestialSpaceId;


                if (holon.ParentHolon.ParentOmniverse != null)
                {
                    if (holon.ParentOmniverseId == Guid.Empty && holon.Id != holon.ParentHolon.ParentOmniverse.Id)
                    {
                        holon.ParentOmniverseId = holon.ParentHolon.ParentOmniverse.Id;

                        if (holon.ParentOmniverse == null)
                            holon.ParentOmniverse = holon.ParentHolon.ParentOmniverse;
                    }
                }
                else if (holon.ParentOmniverseId == Guid.Empty && holon.ParentHolon.ParentOmniverseId != holon.Id)
                    holon.ParentOmniverseId = holon.ParentHolon.ParentOmniverseId;


                if (holon.ParentHolon.ParentMultiverse != null)
                {
                    if (holon.ParentMultiverseId == Guid.Empty && holon.Id != holon.ParentHolon.ParentMultiverse.Id)
                    {
                        holon.ParentMultiverseId = holon.ParentHolon.ParentMultiverse.Id;

                        if (holon.ParentMultiverse == null)
                            holon.ParentMultiverse = holon.ParentHolon.ParentMultiverse;
                    }
                }
                else if (holon.ParentMultiverseId == Guid.Empty && holon.ParentHolon.ParentMultiverseId != holon.Id)
                    holon.ParentMultiverseId = holon.ParentHolon.ParentMultiverseId;

       
                if (holon.ParentHolon.ParentUniverse != null)
                {
                    if (holon.ParentUniverse == null)
                        holon.ParentUniverse = holon.ParentHolon.ParentUniverse;

                    if (holon.ParentUniverseId == Guid.Empty)
                        holon.ParentUniverseId = holon.ParentHolon.ParentUniverse.Id;
                }
                else if (holon.ParentUniverseId == Guid.Empty)
                    holon.ParentUniverseId = holon.ParentHolon.ParentUniverseId;

                if (holon.ParentHolon.ParentDimension != null)
                {
                    if (holon.ParentDimension == null)
                        holon.ParentDimension = holon.ParentHolon.ParentDimension;

                    if (holon.ParentDimensionId == Guid.Empty)
                        holon.ParentDimensionId = holon.ParentHolon.ParentDimension.Id;
                }
                else if (holon.ParentDimensionId == Guid.Empty)
                    holon.ParentDimensionId = holon.ParentHolon.ParentDimensionId;

                if (holon.ParentHolon.ParentGalaxyCluster != null)
                {
                    if (holon.ParentGalaxyCluster == null)
                        holon.ParentGalaxyCluster = holon.ParentHolon.ParentGalaxyCluster;

                    if (holon.ParentGalaxyClusterId == Guid.Empty)
                        holon.ParentGalaxyClusterId = holon.ParentHolon.ParentGalaxyCluster.Id;
                }
                else if (holon.ParentGalaxyClusterId == Guid.Empty)
                    holon.ParentGalaxyClusterId = holon.ParentHolon.ParentGalaxyClusterId;

                if (holon.ParentHolon.ParentGalaxy != null)
                {
                    if (holon.ParentGalaxy == null)
                        holon.ParentGalaxy = holon.ParentHolon.ParentGalaxy;

                    if (holon.ParentGalaxyId == Guid.Empty)
                        holon.ParentGalaxyId = holon.ParentHolon.ParentGalaxy.Id;
                }
                else if (holon.ParentGalaxyId == Guid.Empty)
                    holon.ParentGalaxyId = holon.ParentHolon.ParentGalaxyId;

                if (holon.ParentHolon.ParentSolarSystem != null)
                {
                    if (holon.ParentSolarSystem == null)
                        holon.ParentSolarSystem = holon.ParentHolon.ParentSolarSystem;

                    if (holon.ParentSolarSystemId == Guid.Empty)
                        holon.ParentSolarSystemId = holon.ParentHolon.ParentSolarSystem.Id;
                }
                else if (holon.ParentSolarSystemId == Guid.Empty)
                    holon.ParentSolarSystemId = holon.ParentHolon.ParentSolarSystemId;
            }


            if (holon.ParentHolonId == Guid.Empty)
            {
                //holon.ParentHolonId = holon.Id;
            }
                

            if (holon.ParentHolon == null)
            {
                //holon.ParentHolon = holon;
            }

            //if (holon.Children != null)
            if (holon.AllChildren != null)
            {
                //foreach (IHolon childHolon in holon.Children)
                foreach (IHolon childHolon in holon.AllChildren)
                {
                    if (childHolon.ParentHolon == null)
                        childHolon.ParentHolon = holon;

                    if (childHolon.ParentHolonId == Guid.Empty)
                    {
                        if (childHolon.ParentHolon != null)
                            childHolon.ParentHolonId = childHolon.ParentHolon.Id;
                        else
                            childHolon.ParentHolonId = holon.Id;
                    }

                    PrepareHolonForSaving(childHolon, avatarId, extractMetaData);
                }
            }
        }

    }
}
