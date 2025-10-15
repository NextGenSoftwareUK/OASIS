//using MongoDB.Driver;
//using NextGenSoftware.OASIS.API.Core.Enums;
//using NextGenSoftware.OASIS.API.Core.Helpers;
//using NextGenSoftware.OASIS.API.Core.Managers;
//using OASIS_Onion.Model.Models;
//using OASIS_Onion.MongoDb.Interface;
//using OASIS_Onion.Repository.Entity;
//using OASIS_Onion.Repository.Interface;
//using System;
//using System.Collections.Generic;
//using System.Threading.Tasks;

//namespace OASIS_Onion.Repository.Repository
//{
//    public class HolonRepository : EntityRepository<Holon, IEntityProvider<Holon>>, IHolonRepository
//    {
//        private IEntityProvider<Holon> _testProvider;

//        public HolonRepository(IEntityProvider<Holon> testProvider) : base(testProvider)
//        {
//            this._testProvider = testProvider;
//        }

//        public async Task<OASISResult<Holon>> AddAsync(Holon holon)
//        {
//            OASISResult<Holon> result = new OASISResult<Holon>();

//            try
//            {
//                if (holon.HolonId == Guid.Empty)
//                    holon.HolonId = Guid.NewGuid();

//                holon.CreatedProviderType.Value = ProviderType.MongoDBOASIS;
//                await this._testProvider.InsertAsync(holon);
//                holon.ProviderKey[ProviderType.MongoDBOASIS] = holon.Id.ToString();
//                await UpdateAsync(holon);
//                result.Result = holon;
//            }
//            catch (Exception ex)
//            {
//                result.IsError = true;
//                result.Message = $"Error saving holon with id {holon.Id} and name {holon.Name} in AddAsync method in MongoDBOASIS Provider. Reason: {ex.ToString()}";
//            }

//            return result;
//        }

//        public OASISResult<Holon> Add(Holon holon)
//        {
//            OASISResult<Holon> result = new OASISResult<Holon>();

//            try
//            {
//                if (holon.HolonId == Guid.Empty)
//                    holon.HolonId = Guid.NewGuid();

//                holon.CreatedProviderType.Value = ProviderType.MongoDBOASIS;

//                this._testProvider.Insert(holon);
//                holon.ProviderKey[ProviderType.MongoDBOASIS] = holon.Id.ToString();

//                Update(holon);
//                result.Result = holon;
//            }
//            catch (Exception ex)
//            {
//                result.IsError = true;
//                result.Message = $"Error saving holon with id {holon.Id} and name {holon.Name} in Add method in MongoDBOASIS Provider. Reason: {ex.ToString()}";
//            }

//            return result;
//        }

//        public async Task<Holon> GetHolonAsync(Guid id)
//        {
//            try
//            {
//                return await this._testProvider.GetAsync(id);
//            }
//            catch
//            {
//                throw;
//            }
//        }

//        public Holon GetHolon(Guid id)
//        {
//            try
//            {
//                return this._testProvider.Get(id);
//            }
//            catch
//            {
//                throw;
//            }
//        }

//        public async Task<Holon> GetHolonAsync(string providerKey)
//        {
//            try
//            {
//                FilterDefinition<Holon> filter = Builders<Holon>.Filter.Where(x => x.ProviderKey[ProviderType.MongoDBOASIS] == providerKey);
//                return await this._testProvider.GetAsync(filter);
//            }
//            catch
//            {
//                throw;
//            }
//        }

//        public Holon GetHolon(string providerKey)
//        {
//            try
//            {
//                FilterDefinition<Holon> filter = Builders<Holon>.Filter.Where(x => x.ProviderKey[ProviderType.MongoDBOASIS] == providerKey);
//                return this._testProvider.Get(filter);
//            }
//            catch
//            {
//                throw;
//            }
//        }

//        public async Task<IEnumerable<Holon>> GetAllHolonsAsync(HolonType holonType = HolonType.All)
//        {
//            try
//            {
//                if (holonType != HolonType.All)
//                {
//                    return await this._testProvider.GetAllAsync();
//                }
//                else
//                {
//                    FilterDefinition<Holon> filter = Builders<Holon>.Filter.Where(x => x.HolonType == holonType);
//                    return await this._testProvider.GetAllAsync(filter);
//                }
//            }
//            catch
//            {
//                throw;
//            }
//        }

//        public IEnumerable<Holon> GetAllHolons(HolonType holonType = HolonType.All)
//        {
//            try
//            {
//                if (holonType != HolonType.All)
//                {
//                    return this._testProvider.GetAll();
//                }
//                else
//                {
//                    FilterDefinition<Holon> filter = Builders<Holon>.Filter.Where(x => x.HolonType == holonType);
//                    return this._testProvider.GetAll(filter);
//                }
//            }
//            catch
//            {
//                throw;
//            }
//        }

//        public async Task<IEnumerable<Holon>> GetAllHolonsForParentAsync(Guid id, HolonType holonType)
//        {
//            try
//            {
//                return await this._testProvider.GetAllAsync(BuildFilterForGetHolonsForParent(id, holonType));
//            }
//            catch
//            {
//                throw;
//            }
//        }

//        public IEnumerable<Holon> GetAllHolonsForParent(Guid id, HolonType holonType)
//        {
//            try
//            {
//                return this._testProvider.GetAll(BuildFilterForGetHolonsForParent(id, holonType));
//            }
//            catch
//            {
//                throw;
//            }
//        }

//        /*
//        public async OASISResult<Task<IEnumerable<Holon>>> GetAllHolonsForParentAsync(string providerKey, HolonType holonType)
//        {
//            OASISResult<Task<IEnumerable<Holon>>> result = new OASISResult<Task<IEnumerable<Holon>>>();

//            try
//            {
//                //return await _dbContext.Holon.FindAsync(BuildFilterForGetHolonsForParent(providerKey, holonType)).Result.ToListAsync();
//                result.Result = await _dbContext.Holon.FindAsync(BuildFilterForGetHolonsForParent(providerKey, holonType)).Result.ToListAsync();
//            }
//            catch (Exception ex)
//            {
//                result.IsError = true;
//                result.Message = string.Concat("Unknown error occured in GetAllHolonsForParentAsync method. providerKey: ", providerKey, ", holonType: ", Enum.GetName(typeof(HolonType), holonType), ". Error details: ", ex.ToString());
//                result.Exception = ex;
//            }
//        }*/

//        //TODO: Not sure we want to use OASISResult in the providers? because HolonManager, etc in OASIS.API.Core automatically catches, handles, logs all errors etc so no provider can ever take down the OASIS! ;-)  I guess it cannot hurt to handle at this level too...
//        public async Task<OASISResult<IEnumerable<Holon>>> GetAllHolonsForParentAsync(string providerKey, HolonType holonType)
//        {
//            OASISResult<IEnumerable<Holon>> result = new OASISResult<IEnumerable<Holon>>();

//            try
//            {
//                result.Result = await this._testProvider.GetAllAsync(BuildFilterForGetHolonsForParent(providerKey, holonType));
//            }
//            catch (Exception ex)
//            {
//                string errorMessage = string.Concat("Unknown error occured in GetAllHolonsForParentAsync method. providerKey: ", providerKey, ", holonType: ", Enum.GetName(typeof(HolonType), holonType), ". Error details: ", ex.ToString());
//                result.IsError = true;
//                result.Message = errorMessage;
//                LoggingManager.Log(errorMessage, LogType.Error);
//                result.Exception = ex;
//            }

//            return result;
//        }

//        public IEnumerable<Holon> GetAllHolonsForParent(string providerKey, HolonType holonType)
//        {
//            try
//            {
//                return this._testProvider.GetAll(BuildFilterForGetHolonsForParent(providerKey, holonType));
//            }
//            catch
//            {
//                throw;
//            }
//        }

//        public async Task<OASISResult<Holon>> UpdateAsync(Holon holon)
//        {
//            OASISResult<Holon> result = new OASISResult<Holon>();

//            try
//            {
//                //TODO: Cant remember why I was doing this?! lol
//                if (holon.Id == null)
//                {
//                    Holon originalHolon = await GetHolonAsync(holon.HolonId);

//                    if (originalHolon != null)
//                    {
//                        holon.Id = originalHolon.Id;
//                        holon.CreatedByAvatarId = originalHolon.CreatedByAvatarId;
//                        holon.CreatedDate = originalHolon.CreatedDate;
//                        holon.HolonType = originalHolon.HolonType;
//                        holon.ParentZome = originalHolon.ParentZome;
//                        holon.ParentZomeId = originalHolon.ParentZomeId;
//                        holon.ParentMoon = originalHolon.ParentMoon;
//                        holon.ParentPlanet = originalHolon.ParentPlanet;
//                        holon.ParentMoonId = originalHolon.ParentMoonId;
//                        holon.ParentPlanetId = originalHolon.ParentPlanetId;
//                        holon.Children = originalHolon.Children;
//                        holon.DeletedByAvatarId = originalHolon.DeletedByAvatarId;
//                        holon.DeletedDate = originalHolon.DeletedDate;

//                        //TODO: Needs more thought!
//                    }
//                }

//                await this._testProvider.UpdateAsync(holon);
//                result.Result = holon;
//            }
//            catch (Exception ex)
//            {
//                result.IsError = true;
//                result.Message = $"Error saving holon with id {holon.Id} and name {holon.Name} in Update method in MongoDBOASIS Provider. Reason: {ex.ToString()}";
//            }

//            return result;
//        }

//        public OASISResult<Holon> Update(Holon holon)
//        {
//            OASISResult<Holon> result = new OASISResult<Holon>();

//            try
//            {
//                //TODO: Cant remember why I was doing this?! lol
//                if (holon.Id == null)
//                {
//                    Holon originalHolon = GetHolon(holon.HolonId);

//                    if (originalHolon != null)
//                    {
//                        holon.Id = originalHolon.Id;
//                        holon.CreatedByAvatarId = originalHolon.CreatedByAvatarId;
//                        holon.CreatedDate = originalHolon.CreatedDate;
//                        holon.HolonType = originalHolon.HolonType;
//                        holon.ParentZome = originalHolon.ParentZome;
//                        holon.ParentZomeId = originalHolon.ParentZomeId;
//                        holon.ParentMoon = originalHolon.ParentMoon;
//                        holon.ParentPlanet = originalHolon.ParentPlanet;
//                        holon.ParentMoonId = originalHolon.ParentMoonId;
//                        holon.ParentPlanetId = originalHolon.ParentPlanetId;
//                        holon.Children = originalHolon.Children;
//                        holon.DeletedByAvatarId = originalHolon.DeletedByAvatarId;
//                        holon.DeletedDate = originalHolon.DeletedDate;

//                        //TODO: SOMEONE PLEASE FINISH THIS ASAP!!!
//                    }
//                }

//                this._testProvider.Update(holon);
//                result.Result = holon;
//            }
//            catch (Exception ex)
//            {
//                result.IsError = true;
//                result.Message = $"Error saving holon with id {holon.Id} and name {holon.Name} in Update method in MongoDBOASIS Provider. Reason: {ex.ToString()}";
//            }

//            return result;
//        }

//        public async Task<bool> DeleteAsync(Guid id, bool softDelete = true)
//        {
//            try
//            {
//                if (softDelete)
//                {
//                    Holon holon = await GetHolonAsync(id);
//                    return await SoftDeleteAsync(holon);
//                }
//                else
//                {
//                    FilterDefinition<Holon> data = Builders<Holon>.Filter.Where(x => x.HolonId == id);
//                    await this._testProvider.RemoveAsync(data);
//                    return true;
//                }
//            }
//            catch
//            {
//                throw;
//            }
//        }

//        public bool Delete(Guid id, bool softDelete = true)
//        {
//            try
//            {
//                if (softDelete)
//                {
//                    Holon holon = GetHolon(id);
//                    return SoftDelete(holon);
//                }
//                else
//                {
//                    FilterDefinition<Holon> data = Builders<Holon>.Filter.Where(x => x.HolonId == id);
//                    this._testProvider.Remove(data);
//                    return true;
//                }
//            }
//            catch
//            {
//                throw;
//            }
//        }

//        public async Task<bool> DeleteAsync(string providerKey, bool softDelete = true)
//        {
//            try
//            {
//                if (softDelete)
//                {
//                    Holon holon = await GetHolonAsync(providerKey);
//                    return await SoftDeleteAsync(holon);
//                }
//                else
//                {
//                    FilterDefinition<Holon> data = Builders<Holon>.Filter.Where(x => x.ProviderKey[ProviderType.MongoDBOASIS] == providerKey);
//                    await this._testProvider.RemoveAsync(data);
//                    return true;
//                }
//            }
//            catch
//            {
//                throw;
//            }
//        }

//        public bool Delete(string providerKey, bool softDelete = true)
//        {
//            try
//            {
//                if (softDelete)
//                {
//                    Holon holon = GetHolon(providerKey);
//                    return SoftDelete(holon);
//                }
//                else
//                {
//                    FilterDefinition<Holon> data = Builders<Holon>.Filter.Where(x => x.ProviderKey[ProviderType.MongoDBOASIS] == providerKey);
//                    this._testProvider.Remove(data);
//                    return true;
//                }
//            }
//            catch
//            {
//                throw;
//            }
//        }

//        private async Task<bool> SoftDeleteAsync(Holon holon)
//        {
//            try
//            {
//                if (AvatarManager.LoggedInAvatar != null)
//                    holon.DeletedByAvatarId = AvatarManager.LoggedInAvatar.Id.ToString();

//                holon.DeletedDate = DateTime.Now;
//                await this._testProvider.UpdateAsync(holon);
//                return true;
//            }
//            catch
//            {
//                throw;
//            }
//        }

//        private bool SoftDelete(Holon holon)
//        {
//            try
//            {
//                if (AvatarManager.LoggedInAvatar != null)
//                    holon.DeletedByAvatarId = AvatarManager.LoggedInAvatar.Id.ToString();

//                holon.DeletedDate = DateTime.Now;
//                this._testProvider.Update(holon);
//                return true;
//            }
//            catch
//            {
//                throw;
//            }
//        }

//        private FilterDefinition<Holon> BuildFilterForGetHolonsForParent(string providerKey, HolonType holonType)
//        {
//            FilterDefinition<Holon> filter = null;
//            Holon holon = GetHolon(providerKey);
//            return BuildFilterForGetHolonsForParent(holon.HolonId, holonType);
//        }

//        private FilterDefinition<Holon> BuildFilterForGetHolonsForParent(Guid id, HolonType holonType)
//        {
//            FilterDefinition<Holon> filter = null;

//            if (holonType != HolonType.All)
//            {
//                filter = Builders<Holon>.Filter.And(
//                Builders<Holon>.Filter.Where(p => p.ParentHolonId == id),
//                Builders<Holon>.Filter.Where(p => p.HolonType == holonType));
//            }
//            else
//            {
//                filter = Builders<Holon>.Filter.And(
//                Builders<Holon>.Filter.Where(p => p.ParentHolonId == id));
//            }

//            return filter;
//        }

//        private void HandleError(string message)
//        {
//            LoggingManager.Log(message, LogType.Error);
//        }
//    }
//}