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
        public IHolon LoadHolon(Guid id, HolonType type = HolonType.Holon, ProviderType providerType = ProviderType.Default)
        {
            bool needToChangeBack = false;
            ProviderType currentProviderType = ProviderManager.CurrentStorageProviderType.Value;
            IHolon holon = null;

            try
            {
                holon = ProviderManager.SetAndActivateCurrentStorageProvider(providerType).Result.LoadHolon(id, type);
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
                            holon = ProviderManager.SetAndActivateCurrentStorageProvider(providerTypeInternal.Value).Result.LoadHolon(id, type);
                            needToChangeBack = true;

                            if (holon != null)
                                break;
                        }
                        catch (Exception ex2)
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
     
        public Task<IHolon> LoadHolonAsync(Guid id, HolonType type = HolonType.Holon, ProviderType provider = ProviderType.Default)
        {
            return ProviderManager.SetAndActivateCurrentStorageProvider(provider).Result.LoadHolonAsync(id, type);
        }

        public IHolon LoadHolon(string providerKey, HolonType type = HolonType.Holon, ProviderType provider = ProviderType.Default)
        {
            return ProviderManager.SetAndActivateCurrentStorageProvider(provider).Result.LoadHolon(providerKey, type);
        }

        public Task<IHolon> LoadHolonAsync(string providerKey, HolonType type = HolonType.Holon, ProviderType provider = ProviderType.Default)
        {
            return ProviderManager.SetAndActivateCurrentStorageProvider(provider).Result.LoadHolonAsync(providerKey, type);
        }


        public IEnumerable<IHolon> LoadHolonsForParent(Guid id, HolonType type = HolonType.Holon, ProviderType provider = ProviderType.Default)
        {
            return ProviderManager.SetAndActivateCurrentStorageProvider(provider).Result.LoadHolonsForParent(id, type);
        }

        public Task<IEnumerable<IHolon>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.Holon, ProviderType provider = ProviderType.Default)
        {
            return ProviderManager.SetAndActivateCurrentStorageProvider(provider).Result.LoadHolonsForParentAsync(id, type);
        }

        public IEnumerable<IHolon> LoadHolonsForParent(string providerKey, HolonType type = HolonType.Holon, ProviderType provider = ProviderType.Default)
        {
            return ProviderManager.SetAndActivateCurrentStorageProvider(provider).Result.LoadHolonsForParent(providerKey, type);
        }

        public Task<IEnumerable<IHolon>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.Holon, ProviderType provider = ProviderType.Default)
        {
            return ProviderManager.SetAndActivateCurrentStorageProvider(provider).Result.LoadHolonsForParentAsync(providerKey, type);
        }

        public IEnumerable<IHolon> LoadAllHolons(HolonType type = HolonType.Holon, ProviderType provider = ProviderType.Default)
        {
            return ProviderManager.SetAndActivateCurrentStorageProvider(provider).Result.LoadAllHolons(type);
        }

        public Task<IEnumerable<IHolon>> LoadAllHolonsAsync(HolonType type = HolonType.Holon, ProviderType provider = ProviderType.Default)
        {
            return ProviderManager.SetAndActivateCurrentStorageProvider(provider).Result.LoadAllHolonsAsync(type);
        }

        public IHolon SaveHolon(IHolon holon, ProviderType providerType = ProviderType.Default)
        {
            bool needToChangeBack = false;
            ProviderType currentProviderType = ProviderManager.CurrentStorageProviderType.Value;

            try
            {
                holon = ProviderManager.SetAndActivateCurrentStorageProvider(providerType).Result.SaveHolon(PrepareHolonForSaving(holon));
            }
            catch (Exception ex)
            {
                holon = null;
            }

            if (holon == null)
            {
                // Only try the next provider if they are not set to auto-replicate.
                //  if (ProviderManager.ProvidersThatAreAutoReplicating.Count == 0)
                //  {
                foreach (EnumValue<ProviderType> type in ProviderManager.GetProviderAutoFailOverList())
                {
                    if (type.Value != providerType && type.Value != ProviderManager.CurrentStorageProviderType.Value)
                    {
                        try
                        {
                            holon = ProviderManager.SetAndActivateCurrentStorageProvider(type.Value).Result.SaveHolon(PrepareHolonForSaving(holon));
                            needToChangeBack = true;

                            if (holon != null)
                                break;
                        }
                        catch (Exception ex2)
                        {
                            holon = null;
                            //If the next provider errors then just continue to the next provider.
                        }
                    }
                }
                //   }
            }

            foreach (EnumValue<ProviderType> type in ProviderManager.GetProvidersThatAreAutoReplicating())
            {
                if (type.Value != providerType && type.Value != ProviderManager.CurrentStorageProviderType.Value)
                {
                    try
                    {
                        ProviderManager.SetAndActivateCurrentStorageProvider(type.Value).Result.SaveHolon(holon);
                        needToChangeBack = true;
                    }
                    catch (Exception ex)
                    {
                        // Add logging here.
                    }
                }
            }

            // Set the current provider back to the original provider.
          //  if (needToChangeBack)
                ProviderManager.SetAndActivateCurrentStorageProvider(currentProviderType);

            return holon;
        }

        //public async Task<IHolon> SaveHolonAsync(IHolon holon, ProviderType providerType = ProviderType.Default)
        public async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, ProviderType providerType = ProviderType.Default)
        {
            bool needToChangeBack = false;
            ProviderType currentProviderType = ProviderManager.CurrentStorageProviderType.Value;
            OASISResult<IHolon> result = new OASISResult<IHolon>();

            try
            {
                result.Result = await ProviderManager.SetAndActivateCurrentStorageProvider(providerType).Result.SaveHolonAsync(PrepareHolonForSaving(holon));
            }
            catch (Exception ex)
            {
                result.Result = null;
                LogError(holon, providerType, ex.ToString());
            }

            if (result.Result == null)
            {
                // Only try the next provider if they are not set to auto-replicate.
                //  if (ProviderManager.ProvidersThatAreAutoReplicating.Count == 0)
                //  {
                foreach (EnumValue<ProviderType> type in ProviderManager.GetProviderAutoFailOverList())
                {
                    if (type.Value != providerType && type.Value != ProviderManager.CurrentStorageProviderType.Value)
                    {
                        try
                        {
                            result.Result = await ProviderManager.SetAndActivateCurrentStorageProvider(type.Value).Result.SaveHolonAsync(PrepareHolonForSaving(holon));
                            needToChangeBack = true;

                            if (result.Result != null)
                                break;
                        }
                        catch (Exception ex2)
                        {
                            result.Result = null;
                            //If the next provider errors then just continue to the next provider.

                            LogError(holon, type.Value, ex2.ToString());
                        }
                    }
                }
                //   }
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
                {
                    try
                    {
                        await ProviderManager.SetAndActivateCurrentStorageProvider(type.Value).Result.SaveHolonAsync(holon);
                        needToChangeBack = true;
                    }
                    catch (Exception ex)
                    {
                         LogError(holon, type.Value, ex.ToString());
                    }
                }
            }

            // Set the current provider back to the original provider.
          //  if (needToChangeBack)
                ProviderManager.SetAndActivateCurrentStorageProvider(currentProviderType);

            result.IsSaved = result.Result != null && result.Result.Id != Guid.Empty;
            return result;
        }

        public IEnumerable<IHolon> SaveHolons(IEnumerable<IHolon> holons, ProviderType providerType = ProviderType.Default)
        {
            bool needToChangeBack = false;
            ProviderType currentProviderType = ProviderManager.CurrentStorageProviderType.Value;

            try
            {
                holons = ProviderManager.SetAndActivateCurrentStorageProvider(providerType).Result.SaveHolons(PrepareHolonsForSaving(holons));
            }
            catch (Exception ex)
            {
                holons = null;
            }

            if (holons == null)
            {
                // Only try the next provider if they are not set to auto-replicate.
                //  if (ProviderManager.ProvidersThatAreAutoReplicating.Count == 0)
                //  {
                foreach (EnumValue<ProviderType> type in ProviderManager.GetProviderAutoFailOverList())
                {
                    if (type.Value != providerType && type.Value != ProviderManager.CurrentStorageProviderType.Value)
                    {
                        try
                        {
                            holons = ProviderManager.SetAndActivateCurrentStorageProvider(type.Value).Result.SaveHolons(PrepareHolonsForSaving(holons));
                            needToChangeBack = true;

                            if (holons != null)
                                break;
                        }
                        catch (Exception ex2)
                        {
                            holons = null;
                            //If the next provider errors then just continue to the next provider.
                        }
                    }
                }
                //   }
            }

            foreach (EnumValue<ProviderType> type in ProviderManager.GetProvidersThatAreAutoReplicating())
            {
                if (type.Value != providerType && type.Value != ProviderManager.CurrentStorageProviderType.Value)
                {
                    try
                    {
                        ProviderManager.SetAndActivateCurrentStorageProvider(type.Value).Result.SaveHolons(holons);
                        needToChangeBack = true;
                    }
                    catch (Exception ex)
                    {
                        // Add logging here.
                    }
                }
            }

            // Set the current provider back to the original provider.
           // if (needToChangeBack)
                ProviderManager.SetAndActivateCurrentStorageProvider(currentProviderType);

            return holons;
        }

        public async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, ProviderType providerType = ProviderType.Default)
        {
            bool needToChangeBack = false;
            ProviderType currentProviderType = ProviderManager.CurrentStorageProviderType.Value;
            OASISResult<IEnumerable<IHolon>> result = new OASISResult<IEnumerable<IHolon>>();

            try
            {
                result.Result = await ProviderManager.SetAndActivateCurrentStorageProvider(providerType).Result.SaveHolonsAsync(PrepareHolonsForSaving(holons));
                result.IsSaved = true;
            }
            catch (Exception ex)
            {
                result.Result = null;
            }

            if (result.Result == null)
            {
                // Only try the next provider if they are not set to auto-replicate.
                //  if (ProviderManager.ProvidersThatAreAutoReplicating.Count == 0)
                //  {
                foreach (EnumValue<ProviderType> type in ProviderManager.GetProviderAutoFailOverList())
                {
                    if (type.Value != providerType && type.Value != ProviderManager.CurrentStorageProviderType.Value)
                    {
                        try
                        {
                            result.Result = await ProviderManager.SetAndActivateCurrentStorageProvider(type.Value).Result.SaveHolonsAsync(holons);
                            result.IsSaved = true;
                            needToChangeBack = true;

                            if (result.Result != null)
                                break;
                        }
                        catch (Exception ex2)
                        {
                            result.Result = null;
                            //If the next provider errors then just continue to the next provider.
                        }
                    }
                }
                //   }
            }

            if (result.Result == null)
            {
                result.IsError = true;
                result.Message = string.Concat("All Registered OASIS Providers In The AutoFailOverList Failed To Save The Holons. Providers in list are ", ProviderManager.GetProviderAutoFailOverListAsString());
            }

            foreach (EnumValue<ProviderType> type in ProviderManager.GetProvidersThatAreAutoReplicating())
            {
                if (type.Value != providerType && type.Value != ProviderManager.CurrentStorageProviderType.Value)
                {
                    try
                    {
                        await ProviderManager.SetAndActivateCurrentStorageProvider(type.Value).Result.SaveHolonsAsync(holons);
                        needToChangeBack = true;
                    }
                    catch (Exception ex)
                    {
                        // Add logging here.
                    }
                }
            }

            // Set the current provider back to the original provider.
         //   if (needToChangeBack)
                ProviderManager.SetAndActivateCurrentStorageProvider(currentProviderType);

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
    }
}
