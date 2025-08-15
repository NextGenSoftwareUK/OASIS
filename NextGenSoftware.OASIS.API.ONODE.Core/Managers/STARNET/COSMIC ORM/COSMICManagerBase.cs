using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Managers;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Managers.Base
{
    public abstract class COSMICManagerBase : OASISManager//, ICOSMICManagerBase
    {
        public COSMICManagerBase(Guid avatarId, OASISDNA OASISDNA = null) : base(avatarId, OASISDNA)
        {

        }

        public COSMICManagerBase(IOASISStorageProvider OASISStorageProvider, Guid avatarId, OASISDNA OASISDNA = null) : base(OASISStorageProvider, avatarId, OASISDNA)
        {

        }

        protected async Task<OASISResult<T>> SaveHolonAsync<T>(IHolon holon, Guid avatarId, ProviderType providerType = ProviderType.Default, string methodName = "COSMICManager.SaveHolonAsync", bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false) where T : IHolon, new()
        {
            OASISResult<T> result = new OASISResult<T>();
            string errorMessage = $"Error occured in {methodName}. Reason:";

            try
            {
                if (holon != null)
                {
                    //TODO: This is done automatically in the PreparetoSaveHolon method in HolonManager but because this Manager can also be used in the REST API we need to pass the avatarId in to every method call to make sure the avatarId is correct).
                    if (holon.Id == Guid.Empty)
                    {
                        holon.CreatedByAvatarId = avatarId;
                        holon.CreatedDate = DateTime.Now;
                    }
                    else
                    {
                        holon.ModifiedByAvatarId = avatarId;
                        holon.ModifiedDate = DateTime.Now;
                    }

                    //TODO: Eventually when all methods in HolonManager and Holon have been updated to take avatarId then the code above will not be needed anymore.
                    result = await holon.SaveAsync<T>(saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType);
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} holon is null!");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured: {ex}");
            }

            return result;
        }

        protected OASISResult<T> SaveHolon<T>(IHolon holon, Guid avatarId, ProviderType providerType = ProviderType.Default, string methodName = "COSMICManager.SaveHolon", bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false) where T : IHolon, new()
        {
            OASISResult<T> result = new OASISResult<T>();
            string errorMessage = $"Error occured in {methodName}. Reason:";

            try
            {
                if (holon != null)
                {
                    //TODO: This is done automatically in the PreparetoSaveHolon method in HolonManager but because this Manager can also be used in the REST API we need to pass the avatarId in to every method call to make sure the avatarId is correct).
                    if (holon.Id == Guid.Empty)
                    {
                        holon.CreatedByAvatarId = avatarId;
                        holon.CreatedDate = DateTime.Now;
                    }
                    else
                    {
                        holon.ModifiedByAvatarId = avatarId;
                        holon.ModifiedDate = DateTime.Now;
                    }

                    //TODO: Eventually when all methods in HolonManager and Holon have been updated to take avatarId then the code above will not be needed anymore.
                    result = holon.Save<T>(saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType);
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} holon is null!");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured: {ex}");
            }

            return result;
        }

        protected async Task<OASISResult<T>> LoadHolonAsync<T>(Guid holonId, ProviderType providerType = ProviderType.Default, string methodName = "COSMICManager.LoadHolonAsync", bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, HolonType childHolonType = HolonType.All, int version = 0) where T : IHolon, new()
        {
            OASISResult<T> result = new OASISResult<T>();
            string errorMessage = $"Error occured in {methodName}. Reason:";

            try
            {
                result = await Data.LoadHolonAsync<T>(holonId, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, HolonType.All, 0, providerType);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured: {ex}");
            }

            return result;
        }

        protected OASISResult<T> LoadHolon<T>(Guid holonId, ProviderType providerType = ProviderType.Default, string methodName = "COSMICManager.LoadHolon", bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, HolonType childHolonType = HolonType.All, int version = 0) where T : IHolon, new()
        {
            OASISResult<T> result = new OASISResult<T>();
            string errorMessage = $"Error occured in {methodName}. Reason:";

            try
            {
                result = Data.LoadHolon<T>(holonId, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, HolonType.All, 0, providerType);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured: {ex}");
            }

            return result;
        }

        protected async Task<OASISResult<IEnumerable<T>>> LoadAllHolonsAsync<T>(ProviderType providerType = ProviderType.Default, string methodName = "COSMICManager.LoadAllHolonsAsync", HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, HolonType childHolonType = HolonType.All, int version = 0) where T : IHolon, new()
        {
            OASISResult<IEnumerable<T>> result = new OASISResult<IEnumerable<T>>();
            string errorMessage = $"Error occured in {methodName}. Reason:";

            try
            {
                result = await Data.LoadAllHolonsAsync<T>(holonType, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, childHolonType, version, providerType);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured: {ex}");
            }

            return result;
        }

        protected OASISResult<IEnumerable<T>> LoadAllHolons<T>(ProviderType providerType = ProviderType.Default, string methodName = "COSMICManager.LoadAllHolonsForAvatar", HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, HolonType childHolonType = HolonType.All, int version = 0) where T : IHolon, new()
        {
            OASISResult<IEnumerable<T>> result = new OASISResult<IEnumerable<T>>();
            string errorMessage = $"Error occured in {methodName}. Reason:";

            try
            {
                result = Data.LoadAllHolons<T>(holonType, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, childHolonType, version, providerType);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured: {ex}");
            }

            return result;
        }

        protected async Task<OASISResult<IEnumerable<T>>> SearchHolonsAsync<T>(string searchTerm, Guid avatarId, bool searchOnlyForCurrentAvatar = true, ProviderType providerType = ProviderType.Default, string methodName = "COSMICManager.LoadAllHolonsAsync", HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, HolonType childHolonType = HolonType.All, int version = 0) where T : IHolon, new()
        {
            OASISResult<IEnumerable<T>> result = new OASISResult<IEnumerable<T>>();
            string errorMessage = $"Error occured in {methodName}. Reason:";

            try
            {
                result = await Data.SearchHolonsAsync<T>(searchTerm, avatarId, searchOnlyForCurrentAvatar, holonType, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, childHolonType, version, providerType);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured: {ex}");
            }

            return result;
        }

        protected OASISResult<IEnumerable<T>> SearchHolons<T>(string searchTerm, Guid avatarId, bool searchOnlyForCurrentAvatar = true, ProviderType providerType = ProviderType.Default, string methodName = "COSMICManager.LoadAllHolonsAsync", HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, HolonType childHolonType = HolonType.All, int version = 0) where T : IHolon, new()
        {
            OASISResult<IEnumerable<T>> result = new OASISResult<IEnumerable<T>>();
            string errorMessage = $"Error occured in {methodName}. Reason:";

            try
            {
                result = Data.SearchHolons<T>(searchTerm, avatarId, searchOnlyForCurrentAvatar, holonType, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, childHolonType, version, providerType);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured: {ex}");
            }

            return result;
        }

        protected async Task<OASISResult<IEnumerable<T>>> LoadAllHolonsForAvatarAsync<T>(Guid avatarId, ProviderType providerType = ProviderType.Default, string methodName = "COSMICManager.LoadAllHolonsForAvatarAsync", HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, HolonType childHolonType = HolonType.All, int version = 0) where T : IHolon, new()
        {
            OASISResult<IEnumerable<T>> result = new OASISResult<IEnumerable<T>>();
            string errorMessage = $"Error occured in {methodName}. Reason:";

            try
            {
                result = await Data.LoadHolonsForParentAsync<T>(avatarId, holonType, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, 0, childHolonType, version, providerType);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured: {ex}");
            }

            return result;
        }

        protected OASISResult<IEnumerable<T>> LoadAllHolonsForAvatar<T>(Guid avatarId, ProviderType providerType = ProviderType.Default, string methodName = "COSMICManager.LoadAllHolonsForAvatar", HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, HolonType childHolonType = HolonType.All, int version = 0) where T : IHolon, new()
        {
            OASISResult<IEnumerable<T>> result = new OASISResult<IEnumerable<T>>();
            string errorMessage = $"Error occured in {methodName}. Reason:";

            try
            {
                result = Data.LoadHolonsForParent<T>(avatarId, holonType, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, 0, childHolonType, version, providerType);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured: {ex}");
            }

            return result;
        }

        protected async Task<OASISResult<T>> DeleteHolonAsync<T>(Guid holonId, Guid avatarId, bool softDelete = true, ProviderType providerType = ProviderType.Default, string methodName = "COSMICManager.DeleteHolonAsync") where T : IHolon, new()
        {
            OASISResult<T> result = new OASISResult<T>();
            string errorMessage = $"Error occured in {methodName}. Reason:";

            try
            {
                result = await Data.DeleteHolonAsync<T>(holonId, avatarId, softDelete, providerType);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }

        protected OASISResult<T> DeleteHolon<T>(Guid holonId, Guid avatarId, bool softDelete = true, ProviderType providerType = ProviderType.Default, string methodName = "COSMICManager.DeleteHolon") where T : IHolon, new()
        {
            OASISResult<T> result = new OASISResult<T>();
            string errorMessage = $"Error occured in {methodName}. Reason:";

            try
            {
                result = Data.DeleteHolon<T>(holonId, avatarId, softDelete, providerType);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }
    }
}