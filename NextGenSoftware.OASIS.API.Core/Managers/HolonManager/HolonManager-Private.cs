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

namespace NextGenSoftware.OASIS.API.Core.Managers
{
    public partial class HolonManager : OASISManager
    {
        private IHolon PrepareHolonForSaving(IHolon holon, bool extractMetaData)
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

                if (AvatarManager.LoggedInAvatar != null)
                    holon.ModifiedByAvatarId = AvatarManager.LoggedInAvatar.Id;

                holon.Version++;
                holon.PreviousVersionId = holon.VersionId;
                holon.VersionId = Guid.NewGuid();
            }
            else
            {
                holon.IsActive = true;
                holon.CreatedDate = DateTime.Now;

                if (AvatarManager.LoggedInAvatar != null)
                    holon.CreatedByAvatarId = AvatarManager.LoggedInAvatar.Id;

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

            return holon;
        }

        private IEnumerable<IHolon> PrepareHolonsForSaving(IEnumerable<IHolon> holons, bool extractMetaData)
        {
            List<IHolon> holonsToReturn = new List<IHolon>();

            foreach (IHolon holon in holons)
                holonsToReturn.Add(PrepareHolonForSaving(holon, extractMetaData));

            return holonsToReturn;
        }

        private void LogError(IHolon holon, ProviderType providerType, string errorMessage)
        {
            LoggingManager.Log(string.Concat("An error occured attempting to save the ", LoggingHelper.GetHolonInfoForLogging(holon), " using the ", Enum.GetName(providerType), " provider. Error Details: ", errorMessage), LogType.Error);
        }

        private OASISResult<T> HandleSaveHolonForListOfProviderError<T>(OASISResult<T> result, OASISResult<IHolon> holonSaveResult, string listName, string providerName) where T : IHolon
        {
            holonSaveResult.Message = GetSaveHolonForListOfProvidersErrorMessage(listName, providerName, holonSaveResult.Message);
            OASISErrorHandling.HandleError(ref holonSaveResult, holonSaveResult.Message);
            result.InnerMessages.Add(holonSaveResult.Message);
            result.IsWarning = true;
            result.IsError = false;
            return result;
        }

        private OASISResult<IEnumerable<T>> HandleSaveHolonForListOfProviderError<T>(OASISResult<IEnumerable<T>> result, OASISResult<IEnumerable<IHolon>> holonSaveResult, string listName, string providerName) where T : IHolon
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

        private void SwitchBackToCurrentProvider<T>(ProviderType currentProviderType, ref OASISResult<T> result)
        {
            OASISResult<IOASISStorageProvider> providerResult = ProviderManager.SetAndActivateCurrentStorageProvider(currentProviderType);

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

        private IHolon MapMetaData<T>(IHolon holon)
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

        private OASISResult<T> HasAnyHolonsChanged<T>(IEnumerable<IHolon> holons, ref OASISResult<T> result)
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
            return string.Concat("All registered OASIS Providers in the AutoFailOver List failed to save ", holon != null ? LoggingHelper.GetHolonInfoForLogging(holon) : "", ". Reason: ", OASISResultHelper.BuildInnerMessageError(innerMessages), "Please view the logs and InnerMessages property for more information. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString());
        }

        private string BuildSaveHolonAutoReplicateErrorMessage(List<string> innerMessages, IHolon holon = null)
        {
            return string.Concat("One or more registered OASIS Providers in the AutoReplicate List failed to save ", holon != null ? LoggingHelper.GetHolonInfoForLogging(holon) : "", ". Reason: ", OASISResultHelper.BuildInnerMessageError(innerMessages), "Please view the logs and InnerMessages property for more information. Providers in the list are: ", ProviderManager.GetProvidersThatAreAutoReplicatingAsString());
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
    }
}