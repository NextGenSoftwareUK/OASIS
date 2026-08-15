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
                FilterDefinition<Holon> filter = Builders<Holon>.Filter.Where(x => x.HolonId == id && x.DeletedDate == DateTime.MinValue);
                //return await _dbContext.Holon.FindAsync(filter).Result.FirstOrDefaultAsync();

                Holon holon = await _dbContext.Holon.FindAsync(filter).Result.FirstOrDefaultAsync();

                if (holon != null)
                {
                    //if (holon.DeletedDate == DateTime.MinValue)
                    //{

                    //}

                    if ((holon.ProviderUniqueStorageKey != null && holon.ProviderUniqueStorageKey.ContainsKey(ProviderType.MongoDBOASIS) && holon.ProviderUniqueStorageKey[ProviderType.MongoDBOASIS] != holon.Id)
                        || (holon.ProviderUniqueStorageKey != null && !holon.ProviderUniqueStorageKey.ContainsKey(ProviderType.MongoDBOASIS))
                        || holon.ProviderUniqueStorageKey == null)
                    {
                        if (holon.ProviderUniqueStorageKey == null)
                            holon.ProviderUniqueStorageKey = new Dictionary<ProviderType, string>();

                        holon.ProviderUniqueStorageKey[ProviderType.MongoDBOASIS] = holon.Id;
                        await UpdateAsync(holon);
                    }
                }

                return holon;
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
                FilterDefinition<Holon> filter = Builders<Holon>.Filter.Where(x => x.HolonId == id && x.DeletedDate == DateTime.MinValue);
                //return _dbContext.Holon.Find(filter).FirstOrDefault();

                Holon holon = _dbContext.Holon.Find(filter).FirstOrDefault();

                if (holon != null)
                {
                    if ((holon.ProviderUniqueStorageKey != null && holon.ProviderUniqueStorageKey.ContainsKey(ProviderType.MongoDBOASIS) && holon.ProviderUniqueStorageKey[ProviderType.MongoDBOASIS] != holon.Id)
                        || (holon.ProviderUniqueStorageKey != null && !holon.ProviderUniqueStorageKey.ContainsKey(ProviderType.MongoDBOASIS))
                        || holon.ProviderUniqueStorageKey == null)
                    {
                        if (holon.ProviderUniqueStorageKey == null)
                            holon.ProviderUniqueStorageKey = new Dictionary<ProviderType, string>();

                        holon.ProviderUniqueStorageKey[ProviderType.MongoDBOASIS] = holon.Id;
                        Update(holon);
                    }
                }

                return holon;
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
                FilterDefinition<Holon> filter = Builders<Holon>.Filter.Where(x => x.ProviderUniqueStorageKey[ProviderType.MongoDBOASIS] == providerKey && x.DeletedDate == DateTime.MinValue);
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
                FilterDefinition<Holon> filter = Builders<Holon>.Filter.Where(x => x.ProviderUniqueStorageKey[ProviderType.MongoDBOASIS] == providerKey && x.DeletedDate == DateTime.MinValue);
                return _dbContext.Holon.Find(filter).FirstOrDefault();
            }
            catch
            {
                throw;
            }
        }

        //public async Task<Holon> GetHolonByMetaDataAsync(string metaKey, string metaValue)
        //{
        //    try
        //    {
        //        FilterDefinition<Holon> filter = Builders<Holon>.Filter.Where(x => x.MetaData[metaKey].ToString() == metaValue);
        //        return await _dbContext.Holon.FindAsync(filter).Result.FirstOrDefaultAsync();
        //    }
        //    catch
        //    {
        //        throw;
        //    }
        //}

        //public Holon GetHolonByMetaData(string metaKey, string metaValue)
        //{
        //    try
        //    {
        //        var documents = _dbContext.Holon.Find(Builders<Holon>.Filter.Empty).ToList();
        //        Holon matchedHolon = null;

        //        foreach (Holon holon in documents)
        //        {
        //            if (holon.MetaData.ContainsKey(metaKey) && holon.MetaData[metaKey].ToString() == metaValue)
        //            {
        //                matchedHolon = holon;
        //                break;
        //            }
        //        }

        //        return matchedHolon;

        //        //FilterDefinition<Holon> filter = Builders<Holon>.Filter.Where(x => x.MetaData[metaKey].ToString() == metaValue);

        //        //var filter = Builders<Holon>.Filter.Lte("MetaData.NFTMintWalletAddress", metaValue);
        //        //var filter = Builders<Holon>.Filter.AnyEq("MetaData", new BsonDocument { { "NFTMintWalletAddress", metaValue } });
        //        //var filter = Builders<Holon>.Filter.ElemMatch<BsonValue>("MetaData", new BsonDocument { { "NFTMintWalletAddress", metaValue }});
        //        //var result = _dbContext.Holon.Find(filter).ToList();



        //        //var c = _dbContext.Holon.Find(x => x.MetaData["NFTMintWalletAddress"].ToString() == metaValue).FirstOrDefault();
        //        //var e = _dbContext.Holon.Find(x => x.MetaData["NFTMintWalletAddress"])
        //        //_dbContext.Holon.Find( { $text: { $search: "On" } } );

        //        //var c = _dbContext.Holon.Find(x => x.MetaData[metaKey].ToString() == metaKey).FirstOrDefault();
        //        //var c = _dbContext.Holon.Find(x => x.MetaData[metaKey].ToString() == metaValue).FirstOrDefault();
        //        //var d = _dbContext.Holon.Find(x => x.MetaData["NFTMintWalletAddress"].ToString() == metaValue);
        //        //var e = _dbContext.Holon.Find(x => x.MetaData["NFTMintWalletAddress"].ToString() == metaValue).FirstOrDefault();

        //        //var c = _dbContext.Holon.Find({"comments.user": "AaravSingh" }).FirstOrDefault();
        //        //var c = _dbContext.Holon.Find({"comments.user": "AaravSingh" }).FirstOrDefault();

        //        //return _dbContext.Holon.Find(filter).FirstOrDefault();
        //        return null;
        //    }
        //    catch
        //    {
        //        throw;
        //    }
        //}

        //public async Task<Holon> GetHolonByCustomKeyAsync(string customKey)
        //{
        //    try
        //    {
        //        FilterDefinition<Holon> filter = Builders<Holon>.Filter.Where(x => x.CustomKey == customKey);
        //        return await _dbContext.Holon.FindAsync(filter).Result.FirstOrDefaultAsync();
        //    }
        //    catch
        //    {
        //        throw;
        //    }
        //}

        //public Holon GetHolonByCustomKey(string customKey)
        //{
        //    try
        //    {
        //        FilterDefinition<Holon> filter = Builders<Holon>.Filter.Where(x => x.CustomKey == customKey);
        //        return _dbContext.Holon.Find(filter).FirstOrDefault();
        //    }
        //    catch
        //    {
        //        throw;
        //    }
        //}

        public async Task<IEnumerable<Holon>> GetAllHolonsAsync(HolonType holonType = HolonType.All)
        {
            try
            {
                if (holonType == HolonType.All)
                {
                    //return await _dbContext.Holon.FindAsync(_ => true).Result.ToListAsync();
                    FilterDefinition<Holon> filter = Builders<Holon>.Filter.Where(x => x.DeletedDate == DateTime.MinValue);
                    return await _dbContext.Holon.FindAsync(filter).Result.ToListAsync();
                }
                else
                {
                    FilterDefinition<Holon> filter = Builders<Holon>.Filter.Where(x => x.HolonType == holonType && x.DeletedDate == DateTime.MinValue);
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
                    //return await _dbContext.Holon.FindAsync(_ => true).Result.ToListAsync();
                    FilterDefinition<Holon> filter = Builders<Holon>.Filter.Where(x => x.DeletedDate == DateTime.MinValue);
                    return _dbContext.Holon.Find(filter).ToList();
                }
                else
                {
                    FilterDefinition<Holon> filter = Builders<Holon>.Filter.Where(x => x.HolonType == holonType && x.DeletedDate == DateTime.MinValue);
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
    }
}
