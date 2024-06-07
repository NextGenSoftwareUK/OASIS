using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using NextGenSoftware.OASIS.API.Core.CustomAttrbiutes;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.Logging;
using NextGenSoftware.OASIS.Common;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.API.Core.Managers
{
    public partial class HolonManager : OASISManager
    {
        private IHolon PrepareHolonForSaving(IHolon holon, Guid avatarId, bool extractMetaData) //where T : IHolon, new()
        {
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
                holon.ParentHolonId = avatarId;

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

            // Retreive any custom properties and store in the holon metadata dictionary.
            // TODO: Would ideally like to find a better way to do this so we can avoid reflection if possible because of the potential overhead!
            // Need to do some perfomrnace tests with reflection turned on/off (so with this code enabled/disabled) to see what the overhead is exactly...

            // We only want to extract the meta data for sub-classes of Holon that are calling the Generic overloads.
            if (holon.GetType() != typeof(Holon) && extractMetaData)
            {
                PropertyInfo[] props = holon.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

                foreach (PropertyInfo propertyInfo in props)
                {
                    foreach (CustomAttributeData data in propertyInfo.CustomAttributes)
                    {
                        if (data.AttributeType == (typeof(CustomOASISProperty)))
                        {
                            holon.MetaData[propertyInfo.Name] = propertyInfo.GetValue(holon).ToString();
                            break;
                        }
                    }
                }
            }

            //TODO: Un-comment this later and fix issues from ParentHolon! ;-)
            //SetParentIdsForHolon(holon.ParentGreatGrandSuperStar, holon.ParentGrandSuperStar, holon.ParentSuperStar, holon.ParentStar, holon.ParentPlanet, holon.ParentMoon, holon.ParentZome, holon);
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

        private void SetParentIdsForHolon(IGreatGrandSuperStar greatGrandSuperStar, IGrandSuperStar grandSuperStar, ISuperStar superStar, IStar star, IPlanet planet, IMoon moon, IZome zome, IHolon holon)
        {
            if (holon.Children != null)
            {
                foreach (IHolon childHolon in holon.Children)
                    SetParentIdsForHolon(greatGrandSuperStar, grandSuperStar, superStar, star, planet, moon, zome, childHolon);
            }

            if (greatGrandSuperStar != null)
            {
                if (holon.ParentGreatGrandSuperStar == null) //TODO: Do we need a null check here? What if we want to override it? Same for all other props below...
                    holon.ParentGreatGrandSuperStar = greatGrandSuperStar;

                if (holon.ParentGreatGrandSuperStarId == Guid.Empty)
                    holon.ParentGreatGrandSuperStarId = greatGrandSuperStar.Id;
            }

            if (grandSuperStar != null)
            {
                if (holon.ParentGrandSuperStar == null)
                    holon.ParentGrandSuperStar = grandSuperStar;

                if (holon.ParentGrandSuperStarId == Guid.Empty)
                    holon.ParentGrandSuperStarId = grandSuperStar.Id;
            }

            if (superStar != null)
            {
                if (holon.ParentSuperStar == null)
                    holon.ParentSuperStar = superStar;

                if (holon.ParentSuperStarId == Guid.Empty)
                    holon.ParentSuperStarId = superStar.Id;
            }

            if (star != null)
            {
                if (holon.ParentStar == null)
                    holon.ParentStar = star;

                if (holon.ParentStarId == Guid.Empty)
                    holon.ParentStarId = star.Id;
            }

            if (planet != null)
            {
                if (holon.ParentStar == null)
                    holon.ParentPlanet = planet;

                holon.ParentPlanetId = planet.Id;
            }

            if (moon != null)
            {
                if (holon.ParentMoon == null)
                    holon.ParentMoon = moon;

                if (holon.ParentMoonId == Guid.Empty)
                    holon.ParentMoonId = moon.Id;
            }

            if (zome != null)
            {
                if (holon.ParentZome == null)
                    holon.ParentZome = zome;

                if (holon.ParentZomeId == Guid.Empty)
                    holon.ParentZomeId = zome.Id;
            }

            if (holon != null)
            {
                if (holon.ParentHolonId == Guid.Empty)
                    holon.ParentHolonId = holon.Id;

                if (holon.ParentHolon == null)
                    holon.ParentHolon = holon;
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

        private void MapMetaData<T>(OASISResult<IEnumerable<T>> result) where T : IHolon
        {
            List<T> holons = result.Result.ToList();
            for (int i = 0; i < holons.Count(); i++)
            {
                if (holons[i].MetaData != null)
                    holons[i] = (T)MapMetaData<T>(holons[i]);
            }
        }

        private IHolon MapMetaData<T>(IHolon holon) where T : IHolon
        {
            foreach (string key in holon.MetaData.Keys)
            {
                PropertyInfo propInfo = typeof(T).GetProperty(key);

                if (propInfo != null)
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

                    else
                        propInfo.SetValue(holon, holon.MetaData[key]);
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

        private List<IHolon> BuildChildHolonsList(IHolon holon, List<IHolon> childHolons, bool recursive = true, int maxChildDepth = 0, int currentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            currentChildDepth++;

            if ((recursive && currentChildDepth >= maxChildDepth) || (!recursive && currentChildDepth > 1))
                return childHolons;

            foreach (IHolon childHolon in holon.Children)
                childHolons = BuildChildHolonsList(childHolon, childHolons);

            return childHolons;
        }
    }
}