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

namespace NextGenSoftware.OASIS.API.Core.Managers
{
    public partial class HolonManager : OASISManager
    {
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

            if (holon.Id == Guid.Empty || holon.CreatedDate == DateTime.MinValue)
            {
                if (holon.Id == Guid.Empty)
                    holon.Id = Guid.NewGuid();

                holon.IsNewHolon = true;
            }
            else if (holon.CreatedDate != DateTime.MinValue)
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

            bool storeAsJsonString = false;

            // We only want to extract the meta data for sub-classes of Holon that are calling the Generic overloads.
            if (holon.GetType() != typeof(Holon) && extractMetaData)
            {
                PropertyInfo[] props = holon.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
                var classAttributes = holon.GetType().GetCustomAttributes();

                //foreach (CustomOASISProperty attribute in classAttributes)
                //{
                //    Console.WriteLine($"StoreAsJsonString: {attribute.StoreAsJsonString}");

                //    storeAsJsonString = attribute.StoreAsJsonString;


                //}

                foreach (PropertyInfo propertyInfo in props)
                {
                    foreach (CustomAttributeData data in propertyInfo.CustomAttributes)
                    {
                        if (data.AttributeType == (typeof(CustomOASISProperty)))
                        {
                            //if (data.NamedArguments)

                            for (int i = 0; i < data.NamedArguments.Count() ; i++)
                            {
                                if (data.NamedArguments[i].MemberName == propertyInfo.Name)
                                {
                                    if (Convert.ToBoolean(data.NamedArguments[i].TypedValue.Value))
                                    {

                                    }
                                }
                            }

                           //ustomOASISProperty oasisProperty = data as CustomOASISProperty;

                            //holon.MetaData[propertyInfo.Name] = propertyInfo.GetValue(holon).ToString();
                            if (storeAsJsonString)
                                holon.MetaData[propertyInfo.Name] = JsonSerializer.Serialize(propertyInfo.GetValue(holon)) ;
                            else
                                holon.MetaData[propertyInfo.Name] = propertyInfo.GetValue(holon);

                            break;
                        }
                    }
                }
            }

            //if (holon.AllChildren == null)
            //    holon.AllChildren = new List<IHolon>(holon.Children);
                //holon.AllChildren = new List<Holon>(holon.Children.ToImmutableList()); //TODO: Investigate ImmutableList...

            SetParentIdsForHolon(avatarId, extractMetaData, holon);
            RemoveCelesialBodies(holon);

            return holon;
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

        private void LogError(IHolon holon, ProviderType providerType, string errorMessage)
        {
            LoggingManager.Log(string.Concat("An error occured attempting to save the ", LoggingHelper.GetHolonInfoForLogging(holon), " using the ", Enum.GetName(providerType), " provider. Error Details: ", errorMessage), LogType.Error);
        }

        private OASISResult<T> HandleSaveHolonForListOfProviderError<T>(OASISResult<T> result, OASISResult<T> holonSaveResult, string listName, string providerName) where T : IHolon
        {
            holonSaveResult.Message = GetSaveHolonForListOfProvidersErrorMessage(listName, providerName, holonSaveResult.Message);
            OASISErrorHandling.HandleError(ref holonSaveResult, holonSaveResult.Message);
            result.InnerMessages.Add(holonSaveResult.Message);
            result.IsWarning = true;
            result.IsError = false;
            return result;
        }

        //private OASISResult<T> HandleSaveHolonForListOfProviderError<T>(OASISResult<T> result, OASISResult<T> holonSaveResult, string listName, string providerName) where T : IHolon
        //{
        //    holonSaveResult.Message = GetSaveHolonForListOfProvidersErrorMessage(listName, providerName, holonSaveResult.Message);
        //    OASISErrorHandling.HandleError(ref holonSaveResult, holonSaveResult.Message);
        //    result.InnerMessages.Add(holonSaveResult.Message);
        //    result.IsWarning = true;
        //    result.IsError = false;
        //    return result;
        //}

        private OASISResult<IEnumerable<T>> HandleSaveHolonForListOfProviderError<T>(OASISResult<IEnumerable<T>> result, OASISResult<IEnumerable<T>> holonSaveResult, string listName, string providerName) where T : IHolon
        {
            holonSaveResult.Message = GetSaveHolonForListOfProvidersErrorMessage(listName, providerName, holonSaveResult.Message);
            OASISErrorHandling.HandleError(ref holonSaveResult, holonSaveResult.Message);
            result.InnerMessages.Add(holonSaveResult.Message);
            result.IsWarning = true;
            result.IsError = false;
            return result;
        }

        private string GetSaveHolonForListOfProvidersErrorMessage(string listName, string providerName, string holoSaveResultErrorMessage)
        {
            return $"Error attempting to save in {listName} list for provider {providerName}. Reason: {holoSaveResultErrorMessage}";
        }

        //private void SwitchBackToCurrentProvider<T>(ProviderType currentProviderType, ref OASISResult<T> result)
        //{
        //    OASISResult<IOASISStorageProvider> providerResult = ProviderManager.Instance.SetAndActivateCurrentStorageProvider(currentProviderType);

        //    if (providerResult.IsError)
        //    {
        //        result.IsWarning = true; //TODO: Not sure if this should be an error or a warning? Because there was no error saving the holons but an error switching back to the current provider.                
        //        //result.InnerMessages.Add(string.Concat("The holons saved but there was an error switching the default provider back to ", Enum.GetName(typeof(ProviderType), currentProviderType), " provider. Error Details: ", providerResult.Message));
        //        result.Message = string.Concat(result.Message, ". The holons saved but there was an error switching the default provider back to ", Enum.GetName(typeof(ProviderType), currentProviderType), " provider. Error Details: ", providerResult.Message);
        //    }
        //}

        private async Task SwitchBackToCurrentProviderAsync<T>(ProviderType currentProviderType, OASISResult<T> result)
        {
            OASISResult<IOASISStorageProvider> providerResult = await ProviderManager.Instance.SetAndActivateCurrentStorageProviderAsync(currentProviderType);

            if (providerResult.IsError)
            {
                result.IsWarning = true; //TODO: Not sure if this should be an error or a warning? Because there was no error saving the holons but an error switching back to the current provider.                
                //result.InnerMessages.Add(string.Concat("The holons saved but there was an error switching the default provider back to ", Enum.GetName(typeof(ProviderType), currentProviderType), " provider. Error Details: ", providerResult.Message));
                result.Message = string.Concat(result.Message, ". The holons saved but there was an error switching the default provider back to ", Enum.GetName(typeof(ProviderType), currentProviderType), " provider. Error Details: ", providerResult.Message);
            }
        }

        private void SwitchBackToCurrentProvider<T>(ProviderType currentProviderType, ref OASISResult<T> result)
        {
            OASISResult<IOASISStorageProvider> providerResult = ProviderManager.Instance.SetAndActivateCurrentStorageProvider(currentProviderType);

            if (providerResult.IsError)
            {
                result.IsWarning = true; //TODO: Not sure if this should be an error or a warning? Because there was no error saving the holons but an error switching back to the current provider.                
                //result.InnerMessages.Add(string.Concat("The holons saved but there was an error switching the default provider back to ", Enum.GetName(typeof(ProviderType), currentProviderType), " provider. Error Details: ", providerResult.Message));
                result.Message = string.Concat(result.Message, ". The holons saved but there was an error switching the default provider back to ", Enum.GetName(typeof(ProviderType), currentProviderType), " provider. Error Details: ", providerResult.Message);
            }
        }

        public void MapMetaData<T>(OASISResult<IEnumerable<T>> result) where T : IHolon
        {
            List<T> holons = result.Result.ToList();
            for (int i = 0; i < holons.Count(); i++)
            {
                if (holons[i].MetaData != null)
                    holons[i] = (T)MapMetaData<T>(holons[i]);
            }
        }

        public IHolon MapMetaData<T>(IHolon holon) where T : IHolon
        {
            foreach (string key in holon.MetaData.Keys)
            {
                try
                {
                    PropertyInfo propInfo = typeof(T).GetProperty(key);

                    if (propInfo != null && holon.MetaData[key] != null)
                    {
                        if (propInfo.PropertyType == typeof(Guid))
                            propInfo.SetValue(holon, new Guid(holon.MetaData[key].ToString()));

                        else if (propInfo.PropertyType == typeof(bool))
                            propInfo.SetValue(holon, Convert.ToBoolean(holon.MetaData[key]));

                        else if (propInfo.PropertyType == typeof(DateTime))
                            propInfo.SetValue(holon, Convert.ToDateTime(holon.MetaData[key]));

                        else if (propInfo.PropertyType == typeof(int))
                            propInfo.SetValue(holon, Convert.ToInt32(holon.MetaData[key]));

                        else if (propInfo.PropertyType == typeof(long))
                            propInfo.SetValue(holon, Convert.ToInt64(holon.MetaData[key]));

                        else if (propInfo.PropertyType == typeof(float))
                            propInfo.SetValue(holon, Convert.ToDouble(holon.MetaData[key])); //TODO: Check if this is right?! :)

                        else if (propInfo.PropertyType == typeof(double))
                            propInfo.SetValue(holon, Convert.ToDouble(holon.MetaData[key]));

                        else if (propInfo.PropertyType == typeof(decimal))
                            propInfo.SetValue(holon, Convert.ToDecimal(holon.MetaData[key]));

                        else if (propInfo.PropertyType == typeof(UInt16))
                            propInfo.SetValue(holon, Convert.ToUInt16(holon.MetaData[key]));

                        else if (propInfo.PropertyType == typeof(UInt32))
                            propInfo.SetValue(holon, Convert.ToUInt32(holon.MetaData[key]));

                        else if (propInfo.PropertyType == typeof(UInt64))
                            propInfo.SetValue(holon, Convert.ToUInt64(holon.MetaData[key]));

                        else if (propInfo.PropertyType == typeof(Single))
                            propInfo.SetValue(holon, Convert.ToSingle(holon.MetaData[key]));

                        else if (propInfo.PropertyType == typeof(char))
                            propInfo.SetValue(holon, Convert.ToChar(holon.MetaData[key]));

                        else if (propInfo.PropertyType == typeof(byte))
                            propInfo.SetValue(holon, Convert.ToByte(holon.MetaData[key]));

                        else if (propInfo.PropertyType == typeof(sbyte))
                            propInfo.SetValue(holon, Convert.ToSByte(holon.MetaData[key]));

                        else if (propInfo.PropertyType == typeof(Color))
                            propInfo.SetValue(holon, ColorTranslator.FromHtml(holon.MetaData[key].ToString()));

                        else if (propInfo.PropertyType == typeof(Enum))
                            propInfo.SetValue(holon, holon.MetaData[key]);
                            //propInfo.SetValue(holon, (Color)(holon.MetaData[key]));

                        //else if (propInfo.Attributes.)
                        //    propInfo.SetValue(holon, holon.MetaData[key]);
                            
                        else if (propInfo.PropertyType == typeof(string) && holon.MetaData[key] != null)
                            propInfo.SetValue(holon, holon.MetaData[key].ToString());

                        else
                            propInfo.SetValue(holon, holon.MetaData[key]);
                    }
                }
                catch (Exception ex)
                {

                }
                //TODO: Add any other missing types...
            }

            return holon;
        }

        //private string BuildInnerMessageError(List<string> innerMessages)
        //{
        //    string result = "";
        //    foreach (string innerMessage in innerMessages)
        //        result = string.Concat(result, innerMessage, "\n\n");

        //    return result;
        //}

        private OASISResult<T> HasHolonChanged<T>(IHolon holon, ref OASISResult<T> result)
        {
            //TODO: TEMP! REMOVE ONCE FINISH IMPLEMENTING HASHOLONCHANGED METHOD BELOW...
            result.HasAnyHolonsChanged = true;
            return result;

            if (!holon.HasHolonChanged())
            {
                result.Message = "No changes need saving";
                result.HasAnyHolonsChanged = false;
            }
            else
                result.HasAnyHolonsChanged = true;

            return result;
        }

        private OASISResult<IEnumerable<T>> HasAnyHolonsChanged<T>(IEnumerable<T> holons, ref OASISResult<IEnumerable<T>> result)
        {
            //TODO: TEMP! REMOVE ONCE FINISH IMPLEMENTING HASHOLONCHANGED METHOD BELOW...
            result.HasAnyHolonsChanged = true;
            return result;

            foreach (IHolon holon in holons)
            {
                if (holon.HasHolonChanged())
                {
                    result.HasAnyHolonsChanged = true;
                    break;
                }
            }

            if (!result.HasAnyHolonsChanged)
                result.Message = "No changes need saving";

            return result;
        }

        private string BuildSaveHolonAutoFailOverErrorMessage(List<string> innerMessages, IHolon holon = null)
        {
            return string.Concat("All registered OASIS Providers in the AutoFailOver List failed to save ", holon != null ? LoggingHelper.GetHolonInfoForLogging(holon) : "", ". Reason: ", OASISResultHelper.BuildInnerMessageError(innerMessages), "Please view the logs and InnerMessages property for more information. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString());
        }

        private string BuildSaveHolonAutoReplicateErrorMessage(List<string> innerMessages, IHolon holon = null)
        {
            return string.Concat("One or more registered OASIS Providers in the AutoReplicate List failed to save ", holon != null ? LoggingHelper.GetHolonInfoForLogging(holon) : "", ". Reason: ", OASISResultHelper.BuildInnerMessageError(innerMessages), "Please view the logs and InnerMessages property for more information. Providers in the list are: ", ProviderManager.Instance.GetProvidersThatAreAutoReplicatingAsString());
        }

        private void HandleSaveHolonsErrorForAutoFailOverList<T>(ref OASISResult<IEnumerable<T>> result, IHolon holon = null) where T : IHolon
        {
            OASISErrorHandling.HandleError(ref result, BuildSaveHolonAutoFailOverErrorMessage(result.InnerMessages, holon));
        }

        private void HandleSaveHolonErrorForAutoFailOverList<T>(ref OASISResult<T> result, IHolon holon = null) where T : IHolon
        {
            OASISErrorHandling.HandleError(ref result, BuildSaveHolonAutoFailOverErrorMessage(result.InnerMessages, holon));
        }

        private void HandleSaveHolonsErrorForAutoReplicateList<T>(ref OASISResult<IEnumerable<T>> result, IHolon holon = null) where T : IHolon
        {
            OASISErrorHandling.HandleWarning(ref result, BuildSaveHolonAutoReplicateErrorMessage(result.InnerMessages, holon));
        }

        private void HandleSaveHolonErrorForAutoReplicateList<T>(ref OASISResult<T> result, IHolon holon = null) where T : IHolon
        {
            OASISErrorHandling.HandleWarning(ref result, BuildSaveHolonAutoReplicateErrorMessage(result.InnerMessages, holon));
        }

        private List<IHolon> BuildAllChildHolonsList(IHolon holon, List<IHolon> childHolons, bool recursive = true, int maxChildDepth = 0, int currentChildDepth = 0, bool continueOnError = true)
        {
            currentChildDepth++;

            if ((recursive && currentChildDepth >= maxChildDepth && maxChildDepth > 0) || (!recursive && currentChildDepth > 1))
                return childHolons;

            //foreach (IHolon child in holon.Children) 
            foreach (IHolon child in holon.AllChildren)
            { 
                if (child.Id == Guid.Empty)
                    child.Id = Guid.NewGuid();

                if (child.ParentHolonId == Guid.Empty)
                    child.ParentHolonId = holon.Id;

                if (!childHolons.Where(x => x.Id == child.Id).Any())
                    childHolons.Add(child);
            }

            //foreach (IHolon childHolon in holon.Children)
            foreach (IHolon childHolon in holon.AllChildren)
            {
                foreach (IHolon innerChildHolon in BuildAllChildHolonsList(childHolon, childHolons, recursive, maxChildDepth, currentChildDepth, continueOnError))
                {
                    if (innerChildHolon.Id == Guid.Empty)
                        innerChildHolon.Id = Guid.NewGuid();

                    if (innerChildHolon.ParentHolonId == Guid.Empty)
                        innerChildHolon.ParentHolonId = holon.Id;

                    if (!childHolons.Where(x => x.Id == innerChildHolon.Id).Any())
                        childHolons.Add(innerChildHolon);
                }
            }

            return childHolons;
        }

        private List<IHolon> BuildAllChildHolonsList(IEnumerable<IHolon> holons, List<IHolon> childHolons, bool recursive = true, int maxChildDepth = 0, int currentChildDepth = 0, bool continueOnError = true)
        {
            foreach (IHolon holon in holons)
                BuildAllChildHolonsList(holon, childHolons, recursive, maxChildDepth, currentChildDepth, continueOnError);

            return childHolons;
        }

        private string BuildChildHolonIdList(IHolon holon)
        {
            string ids = "";

            foreach (IHolon child in holon.Children) 
                ids = string.Concat(ids, ",", child.Id);

            if (ids.Length > 1)
                ids = ids.Substring(1);

            return ids;
        }

        private string BuildAllChildHolonIdList(List<IHolon> allchildHolons)
        {
            string ids = "";

            foreach (IHolon holon in allchildHolons)
                ids = string.Concat(ids, ",", holon.Id);

            if (ids.Length > 1)
                ids = ids.Substring(1);

            return ids;
        }

        public T RemoveCelesialBodies<T>(T holon) where T : IHolon
        {
            //if (holon.Id == Guid.Empty)
            //{
            //    holon.Id = Guid.NewGuid();
            //    holon.IsNewHolon = true;
            //}

            ICelestialBody celestialBody = holon as ICelestialBody;

            if (celestialBody != null)
            {
                if (celestialBody.CelestialBodyCore != null)
                {
                    _core[holon.Id] = celestialBody.CelestialBodyCore;
                    celestialBody.Children = celestialBody.CelestialBodyCore.AllChildren.ToList(); //We need to set the children holons before the core is removed during saving... The AllChildren property of CelestialBody will default to Children if the core is not found.
                }
                
                celestialBody.CelestialBodyCore = null;
            }

            if (holon.ParentOmniverse != null)
                _parentOmiverse[holon.Id] = holon.ParentOmniverse;

            if (holon.ParentDimension != null)
                _parentDimension[holon.Id] = holon.ParentDimension;

            if (holon.ParentMultiverse != null)
                _parentMultiverse[holon.Id] = holon.ParentMultiverse;

            if (holon.ParentUniverse != null)
                _parentUniverse[holon.Id] = holon.ParentUniverse;

            if (holon.ParentGalaxyCluster != null)
                _parentGalaxyCluster[holon.Id] = holon.ParentGalaxyCluster;

            if (holon.ParentGalaxy != null)
                _parentGalaxy[holon.Id] = holon.ParentGalaxy;

            if (holon.ParentSolarSystem != null)
                _parentSolarSystem[holon.Id] = holon.ParentSolarSystem;

            if (holon.ParentGreatGrandSuperStar != null)
                _parentGreatGrandSuperStar[holon.Id] = holon.ParentGreatGrandSuperStar;

            if (holon.ParentGrandSuperStar != null)
                _parentGrandSuperStar[holon.Id] = holon.ParentGrandSuperStar;

            if (holon.ParentSuperStar != null)
                _parentSuperStar[holon.Id] = holon.ParentSuperStar;

            if (holon.ParentStar != null)
                _parentStar[holon.Id] = holon.ParentStar;

            if (holon.ParentPlanet != null)
                _parentPlanet[holon.Id] = holon.ParentPlanet;

            if (holon.ParentMoon != null)
                _parentMoon[holon.Id] = holon.ParentMoon;

            if (holon.ParentCelestialSpace != null)
                _parentCelestialSpace[holon.Id] = holon.ParentCelestialSpace;

            if (holon.ParentCelestialBody != null)
                _parentCelestialBody[holon.Id] = holon.ParentCelestialBody;

            if (holon.ParentZome != null)
                _parentZome[holon.Id] = holon.ParentZome;

            if (holon.ParentHolon != null)
                _parentHolon[holon.Id] = holon.ParentHolon;

            holon.ParentOmniverse = null;
            holon.ParentDimension = null;
            holon.ParentMultiverse = null;
            holon.ParentUniverse = null;
            holon.ParentGalaxyCluster = null;
            holon.ParentGalaxy = null;
            holon.ParentSolarSystem = null;
            holon.ParentGreatGrandSuperStar = null;
            holon.ParentGrandSuperStar = null;
            holon.ParentSuperStar = null;
            holon.ParentStar = null;
            holon.ParentPlanet = null;
            holon.ParentMoon = null;
            holon.ParentCelestialBody = null;
            holon.ParentCelestialSpace = null;
            holon.ParentZome = null;
            holon.ParentHolon = null;

            return holon;
        }

        public IEnumerable<IHolon> RemoveCelesialBodies(IEnumerable<IHolon> holons)
        {
            List<IHolon> holonsList = holons.ToList();

            for (int i = 0; i < holonsList.Count(); i++)
                holonsList[i] = RemoveCelesialBodies(holonsList[i]);

            return holonsList;
        }

        public IEnumerable<T> RemoveCelesialBodies<T>(IEnumerable<T> holons) where T : IHolon
        {
            List<T> holonsList = holons.ToList();

            for (int i = 0; i < holonsList.Count(); i++)
                holonsList[i] = (T)RemoveCelesialBodies(holonsList[i]);

            return holonsList;
        }

        //private IHolon RestoreCelesialBodies(IHolon originalHolon)
        public T RestoreCelesialBodies<T>(T originalHolon) where T : IHolon
        {
            if (originalHolon != null)
            {
                //dynamic paramsObject = new ExpandoObject();
                originalHolon.IsNewHolon = false;

                if (_parentOmiverse.ContainsKey(originalHolon.Id))
                    originalHolon.ParentOmniverse = _parentOmiverse[originalHolon.Id];

                if (_parentDimension.ContainsKey(originalHolon.Id))
                    originalHolon.ParentDimension = _parentDimension[originalHolon.Id];

                if (_parentMultiverse.ContainsKey(originalHolon.Id))
                    originalHolon.ParentMultiverse = _parentMultiverse[originalHolon.Id];

                if (_parentUniverse.ContainsKey(originalHolon.Id))
                    originalHolon.ParentUniverse = _parentUniverse[originalHolon.Id];

                if (_parentGalaxyCluster.ContainsKey(originalHolon.Id))
                    originalHolon.ParentGalaxyCluster = _parentGalaxyCluster[originalHolon.Id];

                if (_parentGalaxy.ContainsKey(originalHolon.Id))
                    originalHolon.ParentGalaxy = _parentGalaxy[originalHolon.Id];

                if (_parentSolarSystem.ContainsKey(originalHolon.Id))
                    originalHolon.ParentSolarSystem = _parentSolarSystem[originalHolon.Id];

                if (_parentGreatGrandSuperStar.ContainsKey(originalHolon.Id))
                    originalHolon.ParentGreatGrandSuperStar = _parentGreatGrandSuperStar[originalHolon.Id];

                if (_parentGrandSuperStar.ContainsKey(originalHolon.Id))
                    originalHolon.ParentGrandSuperStar = _parentGrandSuperStar[originalHolon.Id];

                if (_parentSuperStar.ContainsKey(originalHolon.Id))
                    originalHolon.ParentSuperStar = _parentSuperStar[originalHolon.Id];

                if (_parentStar.ContainsKey(originalHolon.Id))
                    originalHolon.ParentStar = _parentStar[originalHolon.Id];

                if (_parentPlanet.ContainsKey(originalHolon.Id))
                    originalHolon.ParentPlanet = _parentPlanet[originalHolon.Id];

                if (_parentMoon.ContainsKey(originalHolon.Id))
                    originalHolon.ParentMoon = _parentMoon[originalHolon.Id];

                if (_parentCelestialSpace.ContainsKey(originalHolon.Id))
                    originalHolon.ParentCelestialSpace = _parentCelestialSpace[originalHolon.Id];

                if (_parentCelestialBody.ContainsKey(originalHolon.Id))
                    originalHolon.ParentCelestialBody = _parentCelestialBody[originalHolon.Id];

                if (_parentZome.ContainsKey(originalHolon.Id))
                    originalHolon.ParentZome = _parentZome[originalHolon.Id];

                if (_parentHolon.ContainsKey(originalHolon.Id))
                    originalHolon.ParentHolon = _parentHolon[originalHolon.Id];

                _parentOmiverse.Remove(originalHolon.Id);
                _parentDimension.Remove(originalHolon.Id);
                _parentMultiverse.Remove(originalHolon.Id);
                _parentUniverse.Remove(originalHolon.Id);
                _parentGalaxyCluster.Remove(originalHolon.Id);
                _parentGalaxy.Remove(originalHolon.Id);
                _parentSolarSystem.Remove(originalHolon.Id);
                _parentGreatGrandSuperStar.Remove(originalHolon.Id);
                _parentGrandSuperStar.Remove(originalHolon.Id);
                _parentSuperStar.Remove(originalHolon.Id);
                _parentStar.Remove(originalHolon.Id);
                _parentPlanet.Remove(originalHolon.Id);
                _parentMoon.Remove(originalHolon.Id);
                _parentCelestialSpace.Remove(originalHolon.Id);
                _parentCelestialBody.Remove(originalHolon.Id);
                _parentZome.Remove(originalHolon.Id);
                _parentHolon.Remove(originalHolon.Id);

                ICelestialBody celestialBody = originalHolon as ICelestialBody;

                if (celestialBody != null)
                {
                    if (_core.ContainsKey(originalHolon.Id))
                        celestialBody.CelestialBodyCore = _core[originalHolon.Id];

                    _core.Remove(originalHolon.Id);
                    return (T)celestialBody;
                }

                //switch (originalHolon.HolonType)
                //{
                //    case HolonType.GreatGrandSuperStar:
                //        {
                //            if (_core.ContainsKey(originalHolon.Id))
                //            {
                //                GreatGrandSuperStar celestialBody = JsonConvert.DeserializeObject<GreatGrandSuperStar>(JsonConvert.SerializeObject(originalHolon));
                //                celestialBody.CelestialBodyCore = _core[originalHolon.Id];
                //                originalHolon = JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(celestialBody));
                //            }
                //        }
                //        break;
                //}

                //if (originalHolon.HolonType == HolonType.GreatGrandSuperStar ||
                //    originalHolon.HolonType == HolonType.GrandSuperStar ||
                //    originalHolon.HolonType == HolonType.SuperStar ||
                //    originalHolon.HolonType == HolonType.Star ||
                //    originalHolon.HolonType == HolonType.Planet ||
                //    originalHolon.HolonType == HolonType.Moon)
                //{
                //    //celestialBody = originalHolon as ICelestialBody;

                //    // celestialBody = (ICelestialBody)originalHolon;

                //    if (_core.ContainsKey(originalHolon.Id))
                //    {
                //        PlayerData player = JsonConvert.DeserializeObject<PlayerData>(JsonConvert.SerializeObject(item));

                //        //ExpandoObjectHelpers.AddProperty(paramsObject, "CelestialBodyCore", _core[originalHolon.Id]);
                //        //ExpandoObjectHelpers.AddProperty(paramsObject, "Id", originalHolon.Id);
                //        //ExpandoObjectHelpers.AddProperty(paramsObject, "ParentHolonId", originalHolon.ParentHolonId);
                //        //ExpandoObjectHelpers.AddProperty(paramsObject, "ProviderUniqueStorageKey", originalHolon.ProviderUniqueStorageKey);
                //        //ExpandoObjectHelpers.AddProperty(paramsObject, "Name", originalHolon.Name);
                //        //ExpandoObjectHelpers.AddProperty(paramsObject, "Description", originalHolon.Description);
                //        //ExpandoObjectHelpers.AddProperty(paramsObject, "HolonType", originalHolon.HolonType);
                //        //ExpandoObjectHelpers.AddProperty(paramsObject, "Children", originalHolon.Children);
                //        //ExpandoObjectHelpers.AddProperty(paramsObject, "CreatedByAvatar", originalHolon.CreatedByAvatar);
                //        //ExpandoObjectHelpers.AddProperty(paramsObject, "CreatedByAvatarId", originalHolon.CreatedByAvatarId);
                //        //ExpandoObjectHelpers.AddProperty(paramsObject, "CreatedDate", originalHolon.Name);
                //        //ExpandoObjectHelpers.AddProperty(paramsObject, "ModifiedByAvatar", originalHolon.ModifiedByAvatar);
                //        //ExpandoObjectHelpers.AddProperty(paramsObject, "ModifiedByAvatarId", originalHolon.ModifiedByAvatarId);
                //        //ExpandoObjectHelpers.AddProperty(paramsObject, "DeletedDate", originalHolon.DeletedDate);
                //        //ExpandoObjectHelpers.AddProperty(paramsObject, "Version", originalHolon.Version);
                //        //ExpandoObjectHelpers.AddProperty(paramsObject, "IsActive", originalHolon.IsActive);
                //        //ExpandoObjectHelpers.AddProperty(paramsObject, "IsChanged", originalHolon.IsChanged);
                //        //ExpandoObjectHelpers.AddProperty(paramsObject, "IsNewHolon", originalHolon.IsNewHolon);
                //        //ExpandoObjectHelpers.AddProperty(paramsObject, "MetaData", originalHolon.MetaData);
                //        //ExpandoObjectHelpers.AddProperty(paramsObject, "ProviderMetaData", originalHolon.ProviderMetaData);
                //        //ExpandoObjectHelpers.AddProperty(paramsObject, "Original", originalHolon.Original);
                //        //ExpandoObjectHelpers.AddProperty(paramsObject, "ParentGreatGrandSuperStar", originalHolon.ParentGreatGrandSuperStar);
                //        //ExpandoObjectHelpers.AddProperty(paramsObject, "ParentGreatGrandSuperStarId", originalHolon.ParentGreatGrandSuperStarId);
                //        //ExpandoObjectHelpers.AddProperty(paramsObject, "ParentGrandSuperStar", originalHolon.ParentGrandSuperStar);
                //        //ExpandoObjectHelpers.AddProperty(paramsObject, "ParentGrandSuperStarId", originalHolon.ParentGrandSuperStarId);
                //        //ExpandoObjectHelpers.AddProperty(paramsObject, "ParentStar", originalHolon.ParentStar);
                //        //ExpandoObjectHelpers.AddProperty(paramsObject, "ParentStarId", originalHolon.ParentStarId);
                //        //ExpandoObjectHelpers.AddProperty(paramsObject, "ParentPlanet", originalHolon.ParentPlanet);
                //        //ExpandoObjectHelpers.AddProperty(paramsObject, "ParentPlanetId", originalHolon.ParentPlanetId);
                //        //ExpandoObjectHelpers.AddProperty(paramsObject, "ParentMoon", originalHolon.ParentGreatGrandSuperStarId);
                //        //ExpandoObjectHelpers.AddProperty(paramsObject, "ParentMoonId", originalHolon.ParentMoonId);
                //        //ExpandoObjectHelpers.AddProperty(paramsObject, "ParentCelestialSpace", originalHolon.ParentCelestialSpace);
                //        //ExpandoObjectHelpers.AddProperty(paramsObject, "ParentCelestialSpaceId", originalHolon.ParentCelestialSpaceId);
                //        //ExpandoObjectHelpers.AddProperty(paramsObject, "ParentCelestialBody", originalHolon.ParentCelestialBody);
                //        //ExpandoObjectHelpers.AddProperty(paramsObject, "ParentCelestialBodyId", originalHolon.ParentCelestialBodyId);
                //        //ExpandoObjectHelpers.AddProperty(paramsObject, "ParentZome", originalHolon.ParentZome);
                //        //ExpandoObjectHelpers.AddProperty(paramsObject, "ParentZomeId", originalHolon.ParentZomeId);
                //        //ExpandoObjectHelpers.AddProperty(paramsObject, "ParentHolon", originalHolon.ParentHolon);
                //        //ExpandoObjectHelpers.AddProperty(paramsObject, "ParentHolonId", originalHolon.ParentHolonId);
                //        //ExpandoObjectHelpers.AddProperty(paramsObject, "ParentOmniverse", originalHolon.ParentOmniverse);
                //        //ExpandoObjectHelpers.AddProperty(paramsObject, "ParentOmniverseId", originalHolon.ParentOmniverseId);
                //        //ExpandoObjectHelpers.AddProperty(paramsObject, "ParentMultiverse", originalHolon.ParentMultiverse);
                //        //ExpandoObjectHelpers.AddProperty(paramsObject, "ParentMultiverseId", originalHolon.ParentMultiverseId);
                //        //ExpandoObjectHelpers.AddProperty(paramsObject, "ParentDimension", originalHolon.ParentDimension);
                //        //ExpandoObjectHelpers.AddProperty(paramsObject, "ParentDimensionId", originalHolon.ParentDimensionId);
                //        //ExpandoObjectHelpers.AddProperty(paramsObject, "ParentUniverse", originalHolon.ParentUniverse);
                //        //ExpandoObjectHelpers.AddProperty(paramsObject, "ParentUniverseId", originalHolon.ParentUniverseId);
                //        //ExpandoObjectHelpers.AddProperty(paramsObject, "ParentGalaxyCluster", originalHolon.ParentGalaxyCluster);
                //        //ExpandoObjectHelpers.AddProperty(paramsObject, "ParentGalaxyClusterId", originalHolon.ParentGalaxyClusterId);
                //        //ExpandoObjectHelpers.AddProperty(paramsObject, "ParentGalaxy", originalHolon.ParentGalaxy);
                //        //ExpandoObjectHelpers.AddProperty(paramsObject, "ParentGalaxyId", originalHolon.ParentGalaxyId);
                //        //ExpandoObjectHelpers.AddProperty(paramsObject, "ParentSolarSystem", originalHolon.ParentSolarSystem);
                //        //ExpandoObjectHelpers.AddProperty(paramsObject, "ParentSolarSystemId", originalHolon.ParentSolarSystemId);

                //        //return (T)paramsObject;
                //    }
            }

            return originalHolon;
        }

        public IEnumerable<IHolon> RestoreCelesialBodies(IEnumerable<IHolon> holons)
        {
            List<IHolon> restoredHolons = new List<IHolon>();

            if (holons != null)
            {
                foreach (IHolon holon in holons)
                    restoredHolons.Add(RestoreCelesialBodies(holon));
            }
            //else
            //    LoggingManager.Log("The list of holons to restore celestial bodies for is null.", LogType.Warning);

            return restoredHolons;
        }

        public IEnumerable<T> RestoreCelesialBodies<T>(IEnumerable<T> holons) where T : IHolon
        {
            List<T> restoredHolons = new List<T>();

            if (holons != null)
            {
                foreach (T holon in holons)
                    restoredHolons.Add(RestoreCelesialBodies(holon));
            }
            //else
            //    LoggingManager.Log("The list of holons to restore celestial bodies for is null.", LogType.Warning);

            return restoredHolons;
        }
    }
}