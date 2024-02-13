using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Driver;
using NextGenSoftware.Logging;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Repositories
{
    public class HolonRepository : IHolonRepository
    {
        private MongoDbContext _dbContext;

        public HolonRepository(MongoDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<OASISResult<Holon>> AddAsync(Holon holon)
        {
            OASISResult<Holon> result = new OASISResult<Holon>();

            try
            {
                if (holon.HolonId == Guid.Empty)
                    holon.HolonId = Guid.NewGuid();

                //holon.IsNewHolon = false;
                holon.CreatedProviderType = new EnumValue<ProviderType>(ProviderType.MongoDBOASIS);

                await _dbContext.Holon.InsertOneAsync(holon);
                holon.ProviderUniqueStorageKey[ProviderType.MongoDBOASIS] = holon.Id;

                await UpdateAsync(holon);
                result.Result = holon;
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error saving holon with id {holon.Id} and name {holon.Name} in AddAsync method in MongoDBOASIS Provider. Reason: {ex.ToString()}";
            }

            return result;
        }

        public OASISResult<Holon> Add(Holon holon)
        {
            OASISResult<Holon> result = new OASISResult<Holon>();

            try
            {
                if (holon.HolonId == Guid.Empty)
                    holon.HolonId = Guid.NewGuid();

                holon.CreatedProviderType = new EnumValue<ProviderType>(ProviderType.MongoDBOASIS);

                _dbContext.Holon.InsertOne(holon);
                holon.ProviderUniqueStorageKey[ProviderType.MongoDBOASIS] = holon.Id;

                Update(holon);
                result.Result = holon;
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error saving holon with id {holon.Id} and name {holon.Name} in Add method in MongoDBOASIS Provider. Reason: {ex.ToString()}";
            }

            return result;
        }

        public async Task<Holon> GetHolonAsync(Guid id)
        {
            try
            {
                FilterDefinition<Holon> filter = Builders<Holon>.Filter.Where(x => x.HolonId == id);
                return await _dbContext.Holon.FindAsync(filter).Result.FirstOrDefaultAsync();
            }
            catch
            {
                throw;
            }
        }

        public Holon GetHolon(Guid id)
        {
            try
            {
                FilterDefinition<Holon> filter = Builders<Holon>.Filter.Where(x => x.HolonId == id);
                return _dbContext.Holon.Find(filter).FirstOrDefault();
            }
            catch
            {
                throw;
            }
        }

        //public T GetHolon<T>(Guid id) where T : IHolon
        //{
        //    try
        //    {
        //        FilterDefinition<IHolon> filter = Builders<IHolon>.Filter.Where(x => x.Id == id);
        //        return _dbContext.Holon.Find(filter).FirstOrDefault();
        //    }
        //    catch
        //    {
        //        throw;
        //    }
        //}

        public async Task<Holon> GetHolonAsync(string providerKey)
        {
            try
            {
                FilterDefinition<Holon> filter = Builders<Holon>.Filter.Where(x => x.ProviderUniqueStorageKey[ProviderType.MongoDBOASIS] == providerKey);
                return await _dbContext.Holon.FindAsync(filter).Result.FirstOrDefaultAsync();
            }
            catch
            {
                throw;
            }
        }

        public Holon GetHolon(string providerKey)
        {
            try
            {
                FilterDefinition<Holon> filter = Builders<Holon>.Filter.Where(x => x.ProviderUniqueStorageKey[ProviderType.MongoDBOASIS] == providerKey);
                return _dbContext.Holon.Find(filter).FirstOrDefault();
            }
            catch
            {
                throw;
            }
        }

        public async Task<Holon> GetHolonByMetaDataAsync(string metaKey, string metaValue)
        {
            try
            {
                FilterDefinition<Holon> filter = Builders<Holon>.Filter.Where(x => x.MetaData[metaKey].ToString() == metaValue);
                return await _dbContext.Holon.FindAsync(filter).Result.FirstOrDefaultAsync();
            }
            catch
            {
                throw;
            }
        }

        public Holon GetHolonByMetaData(string metaKey, string metaValue)
        {
            try
            {
                FilterDefinition<Holon> filter = Builders<Holon>.Filter.Where(x => x.MetaData[metaKey].ToString() == metaValue);
                return _dbContext.Holon.Find(filter).FirstOrDefault();
            }
            catch
            {
                throw;
            }
        }

        public async Task<Holon> GetHolonByCustomKeyAsync(string customKey)
        {
            try
            {
                FilterDefinition<Holon> filter = Builders<Holon>.Filter.Where(x => x.CustomKey == customKey);
                return await _dbContext.Holon.FindAsync(filter).Result.FirstOrDefaultAsync();
            }
            catch
            {
                throw;
            }
        }

        public Holon GetHolonByCustomKey(string customKey)
        {
            try
            {
                FilterDefinition<Holon> filter = Builders<Holon>.Filter.Where(x => x.CustomKey == customKey);
                return _dbContext.Holon.Find(filter).FirstOrDefault();
            }
            catch
            {
                throw;
            }
        }

        public async Task<IEnumerable<Holon>> GetAllHolonsAsync(HolonType holonType = HolonType.All)
        {
            try
            {
                if (holonType == HolonType.All)
                {
                    return await _dbContext.Holon.FindAsync(_ => true).Result.ToListAsync();
                }
                else
                {
                    FilterDefinition<Holon> filter = Builders<Holon>.Filter.Where(x => x.HolonType == holonType);
                    return await _dbContext.Holon.FindAsync(filter).Result.ToListAsync();
                }
            }
            catch
            {
                throw;
            }
        }

        public IEnumerable<Holon> GetAllHolons(HolonType holonType = HolonType.All)
        {
            try
            {
                if (holonType == HolonType.All)
                {
                    return _dbContext.Holon.Find(_ => true).ToList();
                }
                else
                {
                    FilterDefinition<Holon> filter = Builders<Holon>.Filter.Where(x => x.HolonType == holonType);
                    return _dbContext.Holon.Find(filter).ToList();
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<IEnumerable<Holon>> GetAllHolonsForParentAsync(Guid id, HolonType holonType)
        {
            try
            {
                return await _dbContext.Holon.FindAsync(BuildFilterForGetHolonsForParent(id, holonType)).Result.ToListAsync();
            }
            catch
            {
                throw;
            }
        }

        public IEnumerable<Holon> GetAllHolonsForParent(Guid id, HolonType holonType)
        {
            try
            {
                return _dbContext.Holon.Find(BuildFilterForGetHolonsForParent(id, holonType)).ToList();
            }
            catch
            {
                throw;
            }
        }

        /*
        public async OASISResult<Task<IEnumerable<Holon>>> GetAllHolonsForParentAsync(string providerKey, HolonType holonType)
        {
            OASISResult<Task<IEnumerable<Holon>>> result = new OASISResult<Task<IEnumerable<Holon>>>();

            try
            {
                //return await _dbContext.Holon.FindAsync(BuildFilterForGetHolonsForParent(providerKey, holonType)).Result.ToListAsync();
                result.Result = await _dbContext.Holon.FindAsync(BuildFilterForGetHolonsForParent(providerKey, holonType)).Result.ToListAsync();

            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = string.Concat("Unknown error occured in GetAllHolonsForParentAsync method. providerKey: ", providerKey, ", holonType: ", Enum.GetName(typeof(HolonType), holonType), ". Error details: ", ex.ToString());
                result.Exception = ex;
            }
        }*/

        //TODO: Not sure we want to use OASISResult in the providers? because HolonManager, etc in OASIS.API.Core automatically catches, handles, logs all errors etc so no provider can ever take down the OASIS! ;-)  I guess it cannot hurt to handle at this level too...
        public async Task<OASISResult<IEnumerable<Holon>>> GetAllHolonsForParentAsync(string providerKey, HolonType holonType)
        {
            OASISResult<IEnumerable<Holon>> result = new OASISResult<IEnumerable<Holon>>();

            try
            {
                result.Result = await _dbContext.Holon.FindAsync(BuildFilterForGetHolonsForParent(providerKey, holonType)).Result.ToListAsync();
            }
            catch (Exception ex)
            {
                string errorMessage = string.Concat("Unknown error occured in GetAllHolonsForParentAsync method. providerKey: ", providerKey, ", holonType: ", Enum.GetName(typeof(HolonType), holonType), ". Error details: ", ex.ToString());
                result.IsError = true;
                result.Message = errorMessage;
                LoggingManager.Log(errorMessage, LogType.Error);
                result.Exception = ex;
            }

            return result;
        }

        public IEnumerable<Holon> GetAllHolonsForParent(string providerKey, HolonType holonType)
        {
            try
            {
                return _dbContext.Holon.Find(BuildFilterForGetHolonsForParent(providerKey, holonType)).ToList();
            }
            catch
            {
                throw;
            }
        }

        public async Task<OASISResult<IEnumerable<Holon>>> GetAllHolonsForParentByCustomKeyAsync(string customKey, HolonType holonType)
        {
            OASISResult<IEnumerable<Holon>> result = new OASISResult<IEnumerable<Holon>>();

            try
            {
                result.Result = await _dbContext.Holon.FindAsync(BuildFilterForGetHolonsForParentByCustomKey(customKey, holonType)).Result.ToListAsync();
            }
            catch (Exception ex)
            {
                string errorMessage = string.Concat("Unknown error occured in GetAllHolonsForParentByCustomKeyAsync method. customKey: ", customKey, ", holonType: ", Enum.GetName(typeof(HolonType), holonType), ". Error details: ", ex.ToString());
                result.IsError = true;
                result.Message = errorMessage;
                LoggingManager.Log(errorMessage, LogType.Error);
                result.Exception = ex;
            }

            return result;
        }

        public IEnumerable<Holon> GetAllHolonsForParentByCustomKey(string customKey, HolonType holonType)
        {
            try
            {
                return _dbContext.Holon.Find(BuildFilterForGetHolonsForParentByCustomKey(customKey, holonType)).ToList();
            }
            catch
            {
                throw;
            }
        }

        public async Task<OASISResult<IEnumerable<Holon>>> GetAllHolonsForParentByMetaDataAsync(string metaKey, string metaValue, HolonType holonType)
        {
            OASISResult<IEnumerable<Holon>> result = new OASISResult<IEnumerable<Holon>>();

            try
            {
                result.Result = await _dbContext.Holon.FindAsync(BuildFilterForGetHolonsForParentByMetaData(metaKey, metaValue, holonType)).Result.ToListAsync();
            }
            catch (Exception ex)
            {
                string errorMessage = string.Concat("Unknown error occured in GetAllHolonsForParentByMetaDataAsync method. metaKey: ", metaKey, ", metaValue:, ", metaValue, "holonType: ", Enum.GetName(typeof(HolonType), holonType), ". Error details: ", ex.ToString());
                result.IsError = true;
                result.Message = errorMessage;
                LoggingManager.Log(errorMessage, LogType.Error);
                result.Exception = ex;
            }

            return result;
        }

        public IEnumerable<Holon> GetAllHolonsForParentByMetaData(string metaKey, string metaValue, HolonType holonType)
        {
            try
            {
                return _dbContext.Holon.Find(BuildFilterForGetHolonsForParentByMetaData(metaKey, metaValue, holonType)).ToList();
            }
            catch
            {
                throw;
            }
        }

        public async Task<OASISResult<Holon>> UpdateAsync(Holon holon)
        {
            OASISResult<Holon> result = new OASISResult<Holon>();

            try
            {
                //TODO: Cant remember why I was doing this?! lol
                if (holon.Id == null)
                {
                    Holon originalHolon = await GetHolonAsync(holon.HolonId);

                    if (originalHolon != null)
                    {
                        holon.Id = originalHolon.Id;
                        holon.CreatedByAvatarId = originalHolon.CreatedByAvatarId;
                        holon.CreatedDate = originalHolon.CreatedDate;
                        holon.HolonType = originalHolon.HolonType;
                        holon.ParentZome = originalHolon.ParentZome;
                        holon.ParentZomeId = originalHolon.ParentZomeId;
                        holon.ParentMoon = originalHolon.ParentMoon;
                        holon.ParentPlanet = originalHolon.ParentPlanet;
                        holon.ParentMoonId = originalHolon.ParentMoonId;
                        holon.ParentPlanetId = originalHolon.ParentPlanetId;
                        holon.Children = originalHolon.Children;
                        holon.DeletedByAvatarId = originalHolon.DeletedByAvatarId;
                        holon.DeletedDate = originalHolon.DeletedDate;

                        //TODO: Needs more thought!
                    }
                }

                await _dbContext.Holon.ReplaceOneAsync(filter: g => g.HolonId == holon.HolonId, replacement: holon);
                result.Result = holon;
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error saving holon with id {holon.Id} and name {holon.Name} in Update method in MongoDBOASIS Provider. Reason: {ex.ToString()}";
            }

            return result;
        }

        public OASISResult<Holon> Update(Holon holon)
        {
            OASISResult<Holon> result = new OASISResult<Holon>();

            try
            {
                //TODO: Cant remember why I was doing this?! lol
                if (holon.Id == null)
                {
                    Holon originalHolon = GetHolon(holon.HolonId);

                    if (originalHolon != null)
                    {
                        holon.Id = originalHolon.Id;
                        holon.CreatedByAvatarId = originalHolon.CreatedByAvatarId;
                        holon.CreatedDate = originalHolon.CreatedDate;
                        holon.HolonType = originalHolon.HolonType;
                        holon.ParentZome = originalHolon.ParentZome;
                        holon.ParentZomeId = originalHolon.ParentZomeId;
                        holon.ParentMoon = originalHolon.ParentMoon;
                        holon.ParentPlanet = originalHolon.ParentPlanet;
                        holon.ParentMoonId = originalHolon.ParentMoonId;
                        holon.ParentPlanetId = originalHolon.ParentPlanetId;
                        holon.Children = originalHolon.Children;
                        holon.DeletedByAvatarId = originalHolon.DeletedByAvatarId;
                        holon.DeletedDate = originalHolon.DeletedDate;

                        //TODO: SOMEONE PLEASE FINISH THIS ASAP!!!
                    }
                }

                _dbContext.Holon.ReplaceOne(filter: g => g.HolonId == holon.HolonId, replacement: holon);
                result.Result = holon;
            }

            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error saving holon with id {holon.Id} and name {holon.Name} in Update method in MongoDBOASIS Provider. Reason: {ex.ToString()}";
            }

            return result;
        }

        public async Task<bool> DeleteAsync(Guid id, bool softDelete = true)
        {
            try
            {
                if (softDelete)
                {
                    Holon holon = await GetHolonAsync(id);
                    return await SoftDeleteAsync(holon);
                }
                else
                {
                    FilterDefinition<Holon> data = Builders<Holon>.Filter.Where(x => x.HolonId == id);
                    await _dbContext.Holon.DeleteOneAsync(data);
                    return true;
                }
            }
            catch
            {
                throw;
            }
        }

        public bool Delete(Guid id, bool softDelete = true)
        {
            try
            {
                if (softDelete)
                {
                    Holon holon = GetHolon(id);
                    return SoftDelete(holon);
                }
                else
                {
                    FilterDefinition<Holon> data = Builders<Holon>.Filter.Where(x => x.HolonId == id);
                    _dbContext.Holon.DeleteOne(data);
                    return true;
                }
            }
            catch
            {
                throw;
            }
        }

        public async Task<bool> DeleteAsync(string providerKey, bool softDelete = true)
        {
            try
            {
                if (softDelete)
                {
                    Holon holon = await GetHolonAsync(providerKey);
                    return await SoftDeleteAsync(holon);
                }
                else
                {
                    FilterDefinition<Holon> data = Builders<Holon>.Filter.Where(x => x.ProviderUniqueStorageKey[ProviderType.MongoDBOASIS] == providerKey);
                    await _dbContext.Holon.DeleteOneAsync(data);
                    return true;
                }
            }
            catch
            {
                throw;
            }
        }

        public bool Delete(string providerKey, bool softDelete = true)
        {
            try
            {
                if (softDelete)
                {
                    Holon holon = GetHolon(providerKey);
                    return SoftDelete(holon);
                }
                else
                {
                    FilterDefinition<Holon> data = Builders<Holon>.Filter.Where(x => x.ProviderUniqueStorageKey[ProviderType.MongoDBOASIS] == providerKey);
                    _dbContext.Holon.DeleteOne(data);
                    return true;
                }
            }
            catch
            {
                throw;
            }
        }

        private async Task<bool> SoftDeleteAsync(Holon holon)
        {
            try
            {
                if (holon != null)
                {
                    if (AvatarManager.LoggedInAvatar != null)
                        holon.DeletedByAvatarId = AvatarManager.LoggedInAvatar.Id.ToString();

                    holon.DeletedDate = DateTime.Now;
                    await _dbContext.Holon.ReplaceOneAsync(filter: g => g.Id == holon.Id, replacement: holon);
                    return true;
                }
                else
                    return false;
            }
            catch
            {
                throw;
            }
        }

        private bool SoftDelete(Holon holon)
        {
            try
            {
                if (holon != null)
                {
                    if (AvatarManager.LoggedInAvatar != null)
                        holon.DeletedByAvatarId = AvatarManager.LoggedInAvatar.Id.ToString();

                    holon.DeletedDate = DateTime.Now;
                    _dbContext.Holon.ReplaceOne(filter: g => g.Id == holon.Id, replacement: holon);
                    return true;
                }
                else
                    return false;
            }
            catch
            {
                throw;
            }
        }

        private FilterDefinition<Holon> BuildFilterForGetHolonsForParent(string providerKey, HolonType holonType)
        {
            FilterDefinition<Holon> filter = null;
            Holon holon = GetHolon(providerKey);

            if (holon != null)
                return BuildFilterForGetHolonsForParent(holon.HolonId, holonType);
            else
                return null;
        }

        private FilterDefinition<Holon> BuildFilterForGetHolonsForParentByCustomKey(string customKey, HolonType holonType)
        {
            FilterDefinition<Holon> filter = null;
            Holon holon = GetHolonByCustomKey(customKey);

            if (holon != null)
                return BuildFilterForGetHolonsForParent(holon.HolonId, holonType);
            else
                return null;
        }

        private FilterDefinition<Holon> BuildFilterForGetHolonsForParentByMetaData(string metaKey, string metaValue, HolonType holonType)
        {
            FilterDefinition<Holon> filter = null;
            Holon holon = GetHolonByMetaData(metaKey, metaValue);

            if (holon != null)
                return BuildFilterForGetHolonsForParent(holon.HolonId, holonType);
            else
                return null;
        }

        private FilterDefinition<Holon> BuildFilterForGetHolonsForParent(Guid id, HolonType holonType)
        {
            FilterDefinition<Holon> filter = null;

            if (holonType != HolonType.All)
            {
                filter = Builders<Holon>.Filter.And(
                Builders<Holon>.Filter.Where(p => p.ParentHolonId == id),
                Builders<Holon>.Filter.Where(p => p.HolonType == holonType));
            }
            else
            {
                filter = Builders<Holon>.Filter.And(
                Builders<Holon>.Filter.Where(p => p.ParentHolonId == id));
            }

            return filter;
        }

        private void HandleError(string message)
        {
            LoggingManager.Log(message, LogType.Error);
        }
    }
}