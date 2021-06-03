using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Events;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Core.Managers
{
    public class HolonManager : OASISManager
    {
       // public List<IOASISStorage> OASISStorageProviders { get; set; }
        
        public delegate void StorageProviderError(object sender, AvatarManagerErrorEventArgs e);

        //TODO: In future more than one storage provider can be active at a time where each call can specify which provider to use.
        public HolonManager(IOASISStorage OASISStorageProvider) : base(OASISStorageProvider)
        {

        }
        public IHolon LoadHolon(Guid id, ProviderType providerType = ProviderType.Default)
        {
            bool needToChangeBack = false;
            ProviderType currentProviderType = ProviderManager.CurrentStorageProviderType.Value;
            IHolon holon = null;

            try
            {
                holon = ProviderManager.SetAndActivateCurrentStorageProvider(providerType).Result.LoadHolon(id);
            }
            catch (Exception ex)
            {
                holon = null;
            }

            if (holon == null)
            {
                // Only try the next provider if they are not set to auto-replicate.
                //   if (ProviderManager.ProvidersThatAreAutoReplicating.Count == 0)
                // {
                foreach (EnumValue<ProviderType> providerTypeInternal in ProviderManager.GetProviderAutoFailOverList())
                {
                    if (providerTypeInternal.Value != providerType && providerTypeInternal.Value != ProviderManager.CurrentStorageProviderType.Value)
                    {
                        try
                        {
                            holon = ProviderManager.SetAndActivateCurrentStorageProvider(providerTypeInternal.Value).Result.LoadHolon(id);
                            needToChangeBack = true;

                            if (holon != null)
                                break;
                        }
                        catch (Exception ex)
                        {
                            holon = null;
                            //If the next provider errors then just continue to the next provider.
                        }
                    }
                }
                //   }
            }

            // Set the current provider back to the original provider.
          //  if (needToChangeBack)
                ProviderManager.SetAndActivateCurrentStorageProvider(currentProviderType);

            return holon;
        }
     
        public Task<IHolon> LoadHolonAsync(Guid id, ProviderType provider = ProviderType.Default)
        {
            return ProviderManager.SetAndActivateCurrentStorageProvider(provider).Result.LoadHolonAsync(id);
        }

        public IHolon LoadHolon(string providerKey, ProviderType provider = ProviderType.Default)
        {
            return ProviderManager.SetAndActivateCurrentStorageProvider(provider).Result.LoadHolon(providerKey);
        }

        public Task<IHolon> LoadHolonAsync(string providerKey, ProviderType provider = ProviderType.Default)
        {
            return ProviderManager.SetAndActivateCurrentStorageProvider(provider).Result.LoadHolonAsync(providerKey);
        }

        public IEnumerable<IHolon> LoadHolonsForParent(Guid id, HolonType type = HolonType.All, ProviderType provider = ProviderType.Default)
        {
            return ProviderManager.SetAndActivateCurrentStorageProvider(provider).Result.LoadHolonsForParent(id, type);
        }

        public Task<IEnumerable<IHolon>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All, ProviderType provider = ProviderType.Default)
        {
            return ProviderManager.SetAndActivateCurrentStorageProvider(provider).Result.LoadHolonsForParentAsync(id, type);
        }

        public IEnumerable<IHolon> LoadHolonsForParent(string providerKey, HolonType type = HolonType.All, ProviderType provider = ProviderType.Default)
        {
            return ProviderManager.SetAndActivateCurrentStorageProvider(provider).Result.LoadHolonsForParent(providerKey, type);
        }

        //TODO: Need to implement this proper way of calling an OASIS method across the entire OASIS...
        public async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType holonType = HolonType.All, ProviderType provider = ProviderType.Default)
        {
            OASISResult<IEnumerable<IHolon>> result = new OASISResult<IEnumerable<IHolon>>();

            try
            {
                OASISResult<IOASISStorage> providerResult = ProviderManager.SetAndActivateCurrentStorageProvider(provider);

                if (providerResult.IsError)
                {
                    result.IsError = true;
                    result.Message = providerResult.Message;
                }
                else
                    result = await providerResult.Result.LoadHolonsForParentAsync(providerKey, holonType);
            }
            catch (Exception ex)
            {
                string errorMessage = string.Concat("An unknown error occured in LoadHolonsForParentAsync method. providerKey: ", providerKey, ", holonType: ", Enum.GetName(typeof(HolonType), holonType), ", providerType = ", Enum.GetName(typeof(ProviderType), provider), " Error details: ", ex.ToString());
                result.IsError = true;
                result.Message = errorMessage;
                LoggingManager.Log(errorMessage, LogType.Error);
                result.Exception = ex;
            }

            return result;
        }

        public IEnumerable<IHolon> LoadAllHolons(HolonType type = HolonType.All, ProviderType provider = ProviderType.Default)
        {
            return ProviderManager.SetAndActivateCurrentStorageProvider(provider).Result.LoadAllHolons(type);
        }

        public Task<IEnumerable<IHolon>> LoadAllHolonsAsync(HolonType type = HolonType.All, ProviderType provider = ProviderType.Default)
        {
            return ProviderManager.SetAndActivateCurrentStorageProvider(provider).Result.LoadAllHolonsAsync(type);
        }

        public OASISResult<IHolon> SaveHolon(IHolon holon, ProviderType providerType = ProviderType.Default)
        {
            ProviderType currentProviderType = ProviderManager.CurrentStorageProviderType.Value;
            OASISResult<IHolon> result = new OASISResult<IHolon>();

            result = SaveHolonForProviderType(holon, providerType, result);

            if (result.Result == null)
            {
                foreach (EnumValue<ProviderType> type in ProviderManager.GetProviderAutoFailOverList())
                {
                    if (type.Value != providerType && type.Value != ProviderManager.CurrentStorageProviderType.Value)
                    {
                        result = SaveHolonForProviderType(holon, type.Value, result);

                        if (result.Result != null)
                            break;
                    }
                }
            }

            if (result.Result == null)
            {
                result.IsError = true;
                string errorMessage = string.Concat("All registered OASIS Providers in the AutoFailOverList failed to save ", GetHolonInfoForLogging(holon), ". Please view the logs for more information. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString());
                result.Message = errorMessage;
                LoggingManager.Log(errorMessage, LogType.Error);
            }

            foreach (EnumValue<ProviderType> type in ProviderManager.GetProvidersThatAreAutoReplicating())
            {
                if (type.Value != providerType && type.Value != ProviderManager.CurrentStorageProviderType.Value)
                    SaveHolonForProviderType(holon, type.Value);
            }

            SwitchBackToCurrentProvider(currentProviderType, ref result);
            result.IsSaved = result.Result != null && result.Result.Id != Guid.Empty;

            return result;
        }

        //TODO: Need to implement this format to ALL other Holon/Avatar Manager methods with OASISResult, etc.
        public async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, ProviderType providerType = ProviderType.Default)
        {
            ProviderType currentProviderType = ProviderManager.CurrentStorageProviderType.Value;
            OASISResult<IHolon> result = new OASISResult<IHolon>();

            result = await SaveHolonForProviderTypeAsync(holon, providerType, result);

            if (result.Result == null)
            {
                foreach (EnumValue<ProviderType> type in ProviderManager.GetProviderAutoFailOverList())
                {
                    if (type.Value != providerType && type.Value != ProviderManager.CurrentStorageProviderType.Value)
                        result = await SaveHolonForProviderTypeAsync(holon, type.Value, result);
                }
            }

            if (result.Result == null)
            {
                result.IsError = true;
                string errorMessage = string.Concat("All registered OASIS Providers in the AutoFailOverList failed to save ", GetHolonInfoForLogging(holon), ". Please view the logs for more information. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString());
                result.Message = errorMessage;
                LoggingManager.Log(errorMessage, LogType.Error);
            }

            foreach (EnumValue<ProviderType> type in ProviderManager.GetProvidersThatAreAutoReplicating())
            {
                if (type.Value != providerType && type.Value != ProviderManager.CurrentStorageProviderType.Value)
                    await SaveHolonForProviderTypeAsync(holon, type.Value);
            }

            SwitchBackToCurrentProvider(currentProviderType, ref result);
            result.IsSaved = result.Result != null && result.Result.Id != Guid.Empty;

            return result;
        }

        public OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, ProviderType providerType = ProviderType.Default)
        {
            ProviderType currentProviderType = ProviderManager.CurrentStorageProviderType.Value;
            OASISResult<IEnumerable<IHolon>> result = new OASISResult<IEnumerable<IHolon>>();

            result = SaveHolonsForProviderType(holons, providerType, result);

            if (result.Result == null)
            {
                foreach (EnumValue<ProviderType> type in ProviderManager.GetProviderAutoFailOverList())
                {
                    if (type.Value != providerType && type.Value != ProviderManager.CurrentStorageProviderType.Value)
                        result = SaveHolonsForProviderType(holons, type.Value, result);
                }
            }

            if (result.Result == null)
            {
                result.IsError = true;
                result.Message = string.Concat("All Registered OASIS Providers In The AutoFailOverList Failed To Save The Holons. Providers in list are ", ProviderManager.GetProviderAutoFailOverListAsString());
            }

            foreach (EnumValue<ProviderType> type in ProviderManager.GetProvidersThatAreAutoReplicating())
            {
                if (type.Value != providerType && type.Value != ProviderManager.CurrentStorageProviderType.Value)
                    SaveHolonsForProviderType(holons, type.Value);
            }

            SwitchBackToCurrentProvider(currentProviderType, ref result);
            return result;
        }

        //TODO: Need to implement this format to ALL other Holon/Avatar Manager methods with OASISResult, etc.
        public async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, ProviderType providerType = ProviderType.Default)
        {
            ProviderType currentProviderType = ProviderManager.CurrentStorageProviderType.Value;
            OASISResult<IEnumerable<IHolon>> result = new OASISResult<IEnumerable<IHolon>>();

            result = await SaveHolonsForProviderTypeAsync(holons, providerType, result);

            if (result.Result == null)
            {
                foreach (EnumValue<ProviderType> type in ProviderManager.GetProviderAutoFailOverList())
                {
                    if (type.Value != providerType && type.Value != ProviderManager.CurrentStorageProviderType.Value)
                        result = await SaveHolonsForProviderTypeAsync(holons, type.Value, result);
                }
            }

            if (result.Result == null)
            {
                result.IsError = true;
                result.Message = string.Concat("All Registered OASIS Providers In The AutoFailOverList Failed To Save The Holons. Providers in list are ", ProviderManager.GetProviderAutoFailOverListAsString());
            }

            foreach (EnumValue<ProviderType> type in ProviderManager.GetProvidersThatAreAutoReplicating())
            {
                if (type.Value != providerType && type.Value != ProviderManager.CurrentStorageProviderType.Value)
                    await SaveHolonsForProviderTypeAsync(holons, type.Value);
            }

            SwitchBackToCurrentProvider(currentProviderType, ref result);
            return result;
        }

        public bool DeleteHolon(Guid id, bool softDelete = true, ProviderType provider = ProviderType.Default)
        {
            return ProviderManager.SetAndActivateCurrentStorageProvider(provider).Result.DeleteHolon(id, softDelete);
        }

        public Task<bool> DeleteHolonAsync(Guid id, bool softDelete = true, ProviderType provider = ProviderType.Default)
        {
            return ProviderManager.SetAndActivateCurrentStorageProvider(provider).Result.DeleteHolonAsync(id, softDelete);
        }

        public bool DeleteHolon(string providerKey, bool softDelete = true, ProviderType provider = ProviderType.Default)
        {
            return ProviderManager.SetAndActivateCurrentStorageProvider(provider).Result.DeleteHolon(providerKey, softDelete);
        }

        public Task<bool> DeleteHolonAsync(string providerKey, bool softDelete = true, ProviderType provider = ProviderType.Default)
        {
            return ProviderManager.SetAndActivateCurrentStorageProvider(provider).Result.DeleteHolonAsync(providerKey, softDelete);
        }

        private IHolon PrepareHolonForSaving(IHolon holon)
        {
            // TODO: I think it's best to include audit stuff here so the providers do not need to worry about it?
            // Providers could always override this behaviour if they choose...
            if (holon.Id != Guid.Empty)
            {
                holon.ModifiedDate = DateTime.Now;

                if (AvatarManager.LoggedInAvatar != null)
                    holon.ModifiedByAvatarId = AvatarManager.LoggedInAvatar.Id;
            }
            else
            {
                holon.IsActive = true;
                holon.CreatedDate = DateTime.Now;

                if (AvatarManager.LoggedInAvatar != null)
                    holon.CreatedByAvatarId = AvatarManager.LoggedInAvatar.Id;
            }

            return holon;
        }

        private IEnumerable<IHolon> PrepareHolonsForSaving(IEnumerable<IHolon> holons)
        {
            List<IHolon> holonsToReturn = new List<IHolon>();

            foreach (IHolon holon in holons)
                holonsToReturn.Add(PrepareHolonForSaving(holon));

            return holonsToReturn;
        }

        private void LogError(IHolon holon, ProviderType providerType, string errorMessage)
        {
            LoggingManager.Log(string.Concat("An error occured attempting to save the ", GetHolonInfoForLogging(holon), " using the ", Enum.GetName(providerType), " provider. Error Details: ", errorMessage), LogType.Error);
        }

        private string GetHolonInfoForLogging(IHolon holon)
        {
            return string.Concat("holon with id ", holon.Id, " and name ", holon.Name, " of type ", Enum.GetName(holon.HolonType));
        }

        private OASISResult<IHolon> SaveHolonForProviderType(IHolon holon, ProviderType providerType, OASISResult<IHolon> result = null)
        {
            try
            {
                OASISResult<IOASISStorage> providerResult = ProviderManager.SetAndActivateCurrentStorageProvider(providerType);

                if (providerResult.IsError)
                {
                    LoggingManager.Log(providerResult.Message, LogType.Error);

                    if (result != null)
                    {
                        result.IsError = true;
                        result.Message = providerResult.Message;
                    }

                    //TODO: In future will return these extra error messages in the OASISResult.
                }
                else if (result != null)
                {
                    result.Result = providerResult.Result.SaveHolon(PrepareHolonForSaving(holon));
                    result.IsSaved = true;
                }
            }
            catch (Exception ex)
            {
                if (result != null)
                    result.Result = null;

                LogError(holon, providerType, ex.ToString());
            }

            return result;
        }

        private async Task<OASISResult<IHolon>> SaveHolonForProviderTypeAsync(IHolon holon, ProviderType providerType, OASISResult<IHolon> result = null)
        {
            try
            {
                OASISResult<IOASISStorage> providerResult = ProviderManager.SetAndActivateCurrentStorageProvider(providerType);

                if (providerResult.IsError)
                {
                    LoggingManager.Log(providerResult.Message, LogType.Error);

                    if (result != null)
                    {
                        result.IsError = true;
                        result.Message = providerResult.Message;
                    }

                    //TODO: In future will return these extra error messages in the OASISResult.
                }
                else if (result != null)
                {
                    result.Result = await providerResult.Result.SaveHolonAsync(PrepareHolonForSaving(holon));
                    result.IsSaved = true;
                }
            }
            catch (Exception ex)
            {
                if (result != null)
                    result.Result = null;

                LogError(holon, providerType, ex.ToString());
            }

            return result;
        }

        private OASISResult<IEnumerable<IHolon>> SaveHolonsForProviderType(IEnumerable<IHolon> holons, ProviderType providerType, OASISResult<IEnumerable<IHolon>> result = null)
        {
            try
            {
                OASISResult<IOASISStorage> providerResult = ProviderManager.SetAndActivateCurrentStorageProvider(providerType);

                if (providerResult.IsError)
                {
                    LoggingManager.Log(providerResult.Message, LogType.Error);

                    if (result != null)
                    {
                        result.IsError = true;
                        result.Message = providerResult.Message;
                    }

                    //TODO: In future will return these extra error messages in the OASISResult.
                }
                else if (result != null)
                {
                    result.Result = providerResult.Result.SaveHolons(PrepareHolonsForSaving(holons));
                    result.IsSaved = true;
                }
            }
            catch (Exception ex)
            {
                if (result != null)
                    result.Result = null;

                LoggingManager.Log(string.Concat("An error occured attempting to save the holons using the ", Enum.GetName(providerType), " provider. Error Details: ", ex.ToString()), LogType.Error);
            }

            return result;
        }

        private async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsForProviderTypeAsync(IEnumerable<IHolon> holons, ProviderType providerType, OASISResult<IEnumerable<IHolon>> result = null)
        {
            try
            {
                OASISResult<IOASISStorage> providerResult = ProviderManager.SetAndActivateCurrentStorageProvider(providerType);

                if (providerResult.IsError)
                {
                    LoggingManager.Log(providerResult.Message, LogType.Error);

                    if (result != null)
                    {
                        result.IsError = true;
                        result.Message = providerResult.Message;
                    }

                    //TODO: In future will return these extra error messages in the OASISResult.
                }
                else if (result != null)
                {
                    result.Result = await providerResult.Result.SaveHolonsAsync(PrepareHolonsForSaving(holons));
                    result.IsSaved = true;
                }
            }
            catch (Exception ex)
            {
                if (result != null)
                    result.Result = null;

                LoggingManager.Log(string.Concat("An error occured attempting to save the holons using the ", Enum.GetName(providerType), " provider. Error Details: ", ex.ToString()), LogType.Error);
            }

            return result;
        }

        private void SwitchBackToCurrentProvider<T>(ProviderType currentProviderType, ref OASISResult<T> result)
        {
            OASISResult<IOASISStorage> providerResult = ProviderManager.SetAndActivateCurrentStorageProvider(currentProviderType);

            if (providerResult.IsError)
            {
                result.IsWarning = true; //TODO: Not sure if this should be an error or a warning? Because there was no error saving the holons but an error switching back to the current provider.                
                //result.InnerMessages.Add(string.Concat("The holons saved but there was an error switching the default provider back to ", Enum.GetName(typeof(ProviderType), currentProviderType), " provider. Error Details: ", providerResult.Message));
                result.Message = string.Concat(result.Message, ". The holons saved but there was an error switching the default provider back to ", Enum.GetName(typeof(ProviderType), currentProviderType), " provider. Error Details: ", providerResult.Message);
            }
        }
    }
}
