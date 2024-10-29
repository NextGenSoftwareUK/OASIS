using System;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.ONode.Core.Managers
{
    public class PublishManagerBase : COSMICManagerBase
    {
        public PublishManagerBase(Guid avatarId, OASISDNA OASISDNA = null) : base(avatarId, OASISDNA) { }
        public PublishManagerBase(IOASISStorageProvider OASISStorageProvider, Guid avatarId, OASISDNA OASISDNA = null) : base(OASISStorageProvider, avatarId, OASISDNA) { }

        protected async Task<OASISResult<T>> PublishHolonAsync<T>(IPublishableHolon holon, Guid avatarId, string methodName = "PublishManagerBase.PublishHolonAsync", ProviderType providerType = ProviderType.Default) where T : IPublishableHolon, new()
        {
            OASISResult<T> result = new OASISResult<T>();
            string errorMessage = $"Error occured in {methodName}. Reason:";

            try
            {
                //TODO: Soon will implement a version history (with date, avatarid and version that was published) for previous published versions along with rollback options etc.
                holon.PublishedOn = DateTime.Now;
                holon.PublishedByAvatarId = avatarId;
                result = await SaveHolonAsync<T>(holon, avatarId, providerType);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }

        protected OASISResult<T> PublishHolon<T>(IPublishableHolon holon, Guid avatarId, string methodName = "PublishManagerBase.PublishHolonAsync", ProviderType providerType = ProviderType.Default) where T : IPublishableHolon, new()
        {
            OASISResult<T> result = new OASISResult<T>();
            string errorMessage = $"Error occured in {methodName}. Reason:";

            try
            {
                holon.PublishedOn = DateTime.Now;
                holon.PublishedByAvatarId = avatarId;
                result = SaveHolon<T>(holon, avatarId, providerType);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }

        protected async Task<OASISResult<T>> PublishHolonAsync<T>(Guid holonId, Guid avatarId, string methodName = "PublishManagerBase.PublishHolonAsync", ProviderType providerType = ProviderType.Default) where T : IPublishableHolon, new()
        {
            OASISResult<T> result = new OASISResult<T>();
            string errorMessage = $"Error occured in {methodName}. Reason:";

            try
            {
                OASISResult<T> loadResult = await LoadHolonAsync<T>(holonId, providerType);

                if (loadResult != null && !loadResult.IsError && loadResult.Result != null)
                    result = await PublishHolonAsync<T>(loadResult.Result, avatarId, methodName, providerType);
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the mission with COSMICManagerBase.LoadHolonAsync. Reason: {result.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }

        protected OASISResult<T> PublishHolon<T>(Guid holonId, Guid avatarId, string methodName = "PublishManagerBase.PublishHolon", ProviderType providerType = ProviderType.Default) where T : IPublishableHolon, new()
        {
            OASISResult<T> result = new OASISResult<T>();
            string errorMessage = $"Error occured in {methodName}. Reason:";

            try
            {
                OASISResult<T> loadResult = LoadHolon<T>(holonId, providerType);

                if (loadResult != null && !loadResult.IsError && loadResult.Result != null)
                    result = PublishHolon<T>(loadResult.Result, avatarId, methodName, providerType);
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the mission with COSMICManagerBase.LoadHolon. Reason: {result.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }

        protected async Task<OASISResult<T>> UnpublishHolonAsync<T>(IPublishableHolon holon, Guid avatarId, string methodName = "PublishManagerBase.UnpublishHolonAsync", ProviderType providerType = ProviderType.Default) where T : IPublishableHolon, new()
        {
            OASISResult<T> result = new OASISResult<T>();
            string errorMessage = $"Error occured in {methodName}. Reason:";

            try
            {
                holon.PublishedOn = DateTime.MinValue;
                holon.PublishedByAvatarId = Guid.Empty;
                result = await SaveHolonAsync<T>(holon, avatarId, providerType);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }

        protected OASISResult<T> UnpublishHolon<T>(IPublishableHolon holon, Guid avatarId, string methodName = "PublishManagerBase.UnpublishHolonAsync", ProviderType providerType = ProviderType.Default) where T : IPublishableHolon, new()
        {
            OASISResult<T> result = new OASISResult<T>();
            string errorMessage = $"Error occured in {methodName}. Reason:";

            try
            {
                holon.PublishedOn = DateTime.MinValue;
                holon.PublishedByAvatarId = Guid.Empty;
                result = SaveHolon<T>(holon, avatarId, providerType);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }

        protected async Task<OASISResult<T>> UnpublishHolonAsync<T>(Guid holonId, Guid avatarId, string methodName = "PublishManagerBase.UnpublishHolonAsync", ProviderType providerType = ProviderType.Default) where T : IPublishableHolon, new()
        {
            OASISResult<T> result = new OASISResult<T>();
            string errorMessage = $"Error occured in {methodName}. Reason:";

            try
            {
                OASISResult<T> loadResult = await LoadHolonAsync<T>(holonId, providerType);

                if (loadResult != null && !loadResult.IsError && loadResult.Result != null)
                    result = await UnpublishHolonAsync<T>(loadResult.Result, avatarId, methodName, providerType);
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the mission with COSMICManagerBase.LoadHolonAsync. Reason: {result.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }

        protected OASISResult<T> UnpublishHolon<T>(Guid holonId, Guid avatarId, string methodName = "PublishManagerBase.UnpublishHolon", ProviderType providerType = ProviderType.Default) where T : IPublishableHolon, new()
        {
            OASISResult<T> result = new OASISResult<T>();
            string errorMessage = $"Error occured in {methodName}. Reason:";

            try
            {
                OASISResult<T> loadResult = LoadHolon<T>(holonId, providerType);

                if (loadResult != null && !loadResult.IsError && loadResult.Result != null)
                    result = UnpublishHolon<T>(loadResult.Result, avatarId, methodName, providerType);
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured loading the mission with COSMICManagerBase.LoadHolon. Reason: {result.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured. Reason: {ex}");
            }

            return result;
        }
    }
}
