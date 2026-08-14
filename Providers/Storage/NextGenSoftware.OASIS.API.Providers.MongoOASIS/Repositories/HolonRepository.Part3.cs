using System;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Threading.Tasks;
using System.Collections.Generic;
using NextGenSoftware.Logging;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Repositories
{
    public partial class HolonRepository
    {

        public OASISResult<IHolon> Delete(Guid id)
        {
            OASISResult<IHolon> result = new OASISResult<IHolon>();

            try
            {
                Holon holon = GetHolon(id);

                if (holon != null)
                {
                    FilterDefinition<Holon> data = Builders<Holon>.Filter.Where(x => x.HolonId == id);
                    _dbContext.Holon.DeleteOne(data);
                    result.IsDeleted = true;
                    result.DeletedCount = 1;
                    result.Result = Helpers.DataHelper.ConvertMongoEntityToOASISHolon(holon);
                }
                else
                {
                    result.IsError = true;
                    result.Message = $"Holon with id {id} not found.";
                }
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"Error Occured In MongoDBOASIS Provider.HolonRepository.Delete. Reason: {e}");
            }

            return result;
        }

        public async Task<OASISResult<IHolon>> DeleteAsync(string providerKey)
        {
            OASISResult<IHolon> result = new OASISResult<IHolon>();

            try
            {
                Holon holon = await GetHolonAsync(providerKey);

                if (holon != null)
                {
                    FilterDefinition<Holon> data = Builders<Holon>.Filter.Where(x => x.ProviderUniqueStorageKey[ProviderType.MongoDBOASIS] == providerKey);
                    await _dbContext.Holon.DeleteOneAsync(data);
                    result.IsDeleted = true;
                    result.DeletedCount = 1;
                    result.Result = Helpers.DataHelper.ConvertMongoEntityToOASISHolon(holon);
                }
                else
                {
                    result.IsError = true;
                    result.Message = $"Holon with providerKey {providerKey} not found.";
                }
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"Error Occured In MongoDBOASIS Provider.HolonRepository.DeleteAsync. Reason: {e}");
            }

            return result;
        }

        public OASISResult<IHolon> Delete(string providerKey)
        {
            OASISResult<IHolon> result = new OASISResult<IHolon>();

            try
            {
                Holon holon = GetHolon(providerKey);

                if (holon != null)
                {
                    FilterDefinition<Holon> data = Builders<Holon>.Filter.Where(x => x.ProviderUniqueStorageKey[ProviderType.MongoDBOASIS] == providerKey);
                    _dbContext.Holon.DeleteOne(data);
                    result.IsDeleted = true;
                    result.DeletedCount = 1;
                    result.Result = Helpers.DataHelper.ConvertMongoEntityToOASISHolon(holon);
                }
                else
                {
                    result.IsError = true;
                    result.Message = $"Holon with providerKey {providerKey} not found.";
                }
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"Error Occured In MongoDBOASIS Provider.HolonRepository.Delete. Reason: {e}");
            }

            return result;
        }

        //private async Task<OASISResult<Holon>> SoftDeleteAsync(Holon holon, Guid avatarId)
        //{
        //    OASISResult<Holon> result = new OASISResult<Holon>();

        //    try
        //    {
        //        if (holon != null)
        //        {
        //            //if (AvatarManager.LoggedInAvatar != null)
        //            //    holon.DeletedByAvatarId = AvatarManager.LoggedInAvatar.Id.ToString();

        //            holon.DeletedByAvatarId = avatarId.ToString();
        //            holon.DeletedDate = DateTime.Now;
        //            await _dbContext.Holon.ReplaceOneAsync(filter: g => g.Id == holon.Id, replacement: holon);
        //            //return (IHolon)holon;
        //            result.Result = holon;
        //        }
        //        else
        //            return null;
        //    }
        //    catch (Exception e)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"Error occured in MongoDBOASIS Provider.HolonReoisitory.SoftDeleteAsync. Reason: {e}");
        //    }

        //    return result;
        //}

        //private OASISResult<Holon> SoftDelete(Holon holon, Guid avatarId)
        //{
        //    OASISResult<Holon> result = new OASISResult<Holon>();

        //    try
        //    {
        //        if (holon != null)
        //        {
        //            //if (AvatarManager.LoggedInAvatar != null)
        //            //    holon.DeletedByAvatarId = AvatarManager.LoggedInAvatar.Id.ToString();

        //            holon.DeletedByAvatarId = avatarId.ToString();
        //            holon.DeletedDate = DateTime.Now;
        //             _dbContext.Holon.ReplaceOne(filter: g => g.Id == holon.Id, replacement: holon);
        //            result.Result = holon;
        //        }
        //        else
        //            return null;
        //    }
        //    catch (Exception e)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"Error occured in MongoDBOASIS Provider.HolonReoisitory.SoftDelete. Reason: {e}");
        //    }

        //    return result;
        //}

        private FilterDefinition<Holon> BuildFilterForGetHolonsForParent(string providerKey, HolonType holonType)
        {
            FilterDefinition<Holon> filter = null;
            Holon holon = GetHolon(providerKey);

            if (holon != null)
                return BuildFilterForGetHolonsForParent(holon.HolonId, holonType);
            else
                return null;
        }

        //private FilterDefinition<Holon> BuildFilterForGetHolonsForParentByCustomKey(string customKey, HolonType holonType)
        //{
        //    FilterDefinition<Holon> filter = null;
        //    Holon holon = GetHolonByCustomKey(customKey);

        //    if (holon != null)
        //        return BuildFilterForGetHolonsForParent(holon.HolonId, holonType);
        //    else
        //        return null;
        //}

        //private FilterDefinition<Holon> BuildFilterForGetHolonsForParentByMetaData(string metaKey, string metaValue, HolonType holonType)
        //{
        //    FilterDefinition<Holon> filter = null;
        //    Holon holon = GetHolonByMetaData(metaKey, metaValue);

        //    if (holon != null)
        //        return BuildFilterForGetHolonsForParent(holon.HolonId, holonType);
        //    else
        //        return null;
        //}

        private FilterDefinition<Holon> BuildFilterForGetHolonsForParent(Guid id, HolonType holonType)
        {
            FilterDefinition<Holon> filter = null;

            if (holonType != HolonType.All)
            {
                filter = Builders<Holon>.Filter.And(
                Builders<Holon>.Filter.Where(p => p.ParentHolonId == id),
                Builders<Holon>.Filter.Where(p => p.HolonType == holonType));
                Builders<Holon>.Filter.Where(p => p.DeletedDate == DateTime.MinValue);
            }
            else
            {
                filter = Builders<Holon>.Filter.And(
                Builders<Holon>.Filter.Where(p => p.ParentHolonId == id));
                Builders<Holon>.Filter.Where(p => p.DeletedDate == DateTime.MinValue);
            }

            return filter;
        }

        private void HandleError(string message)
        {
            LoggingManager.Log(message, LogType.Error);
        }
    }
}
