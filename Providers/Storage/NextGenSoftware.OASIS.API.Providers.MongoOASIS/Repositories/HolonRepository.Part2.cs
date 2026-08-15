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

        //public async Task<OASISResult<IEnumerable<Holon>>> GetAllHolonsForParentByCustomKeyAsync(string customKey, HolonType holonType)
        //{
        //    OASISResult<IEnumerable<Holon>> result = new OASISResult<IEnumerable<Holon>>();

        //    try
        //    {
        //        result.Result = await _dbContext.Holon.FindAsync(BuildFilterForGetHolonsForParentByCustomKey(customKey, holonType)).Result.ToListAsync();
        //    }
        //    catch (Exception ex)
        //    {
        //        string errorMessage = string.Concat("Unknown error occured in GetAllHolonsForParentByCustomKeyAsync method. customKey: ", customKey, ", holonType: ", Enum.GetName(typeof(HolonType), holonType), ". Error details: ", ex.ToString());
        //        result.IsError = true;
        //        result.Message = errorMessage;
        //        LoggingManager.Log(errorMessage, LogType.Error);
        //        result.Exception = ex;
        //    }

        //    return result;
        //}

        //public IEnumerable<Holon> GetAllHolonsForParentByCustomKey(string customKey, HolonType holonType)
        //{
        //    try
        //    {
        //        return _dbContext.Holon.Find(BuildFilterForGetHolonsForParentByCustomKey(customKey, holonType)).ToList();
        //    }
        //    catch
        //    {
        //        throw;
        //    }
        //}

        // Builds a filter for HolonType + soft-delete guard.
        private static FilterDefinition<Holon> BuildHolonTypeFilter(HolonType holonType) =>
            holonType == HolonType.All
                ? Builders<Holon>.Filter.Where(x => x.DeletedDate == DateTime.MinValue)
                : Builders<Holon>.Filter.Where(x => x.HolonType == holonType && x.DeletedDate == DateTime.MinValue);

        // Uses $expr+$getField so dotted key names (e.g. "NFT.MintedByAvatarId") are treated as
        // literal dictionary keys rather than nested field paths (MongoDB dot-notation limitation).
        // $toString handles any BSON value type, matching the original .ToString() comparison.
        private static FilterDefinition<Holon> BuildMetaDataFilter(string metaKey, string metaValue) =>
            new BsonDocumentFilterDefinition<Holon>(new BsonDocument("$expr", new BsonDocument("$eq", new BsonArray
            {
                new BsonDocument("$toString", new BsonDocument("$getField", new BsonDocument
                {
                    { "field", metaKey },
                    { "input", "$MetaData" }
                })),
                metaValue
            })));

        private static FilterDefinition<Holon> BuildMetaDataMultiFilter(Dictionary<string, string> pairs, MetaKeyValuePairMatchMode mode)
        {
            var filters = pairs.Select(kv => BuildMetaDataFilter(kv.Key, kv.Value)).ToList();
            return mode == MetaKeyValuePairMatchMode.All
                ? Builders<Holon>.Filter.And(filters)
                : Builders<Holon>.Filter.Or(filters);
        }

        public async Task<OASISResult<IEnumerable<Holon>>> GetHolonsByMetaDataAsync(string metaKey, string metaValue, HolonType holonType)
        {
            OASISResult<IEnumerable<Holon>> result = new OASISResult<IEnumerable<Holon>>();

            try
            {
                //var collection = _dbContext.MongoDB.GetCollection<Holon>("Holon");

                //if (holonType == HolonType.All)
                //{
                //    var query = from doc in collection.AsQueryable<Holon>()
                //                //where doc.MetaData.ContainsKey(metaKey) && doc.MetaData[metaKey] != null && doc.MetaData[metaKey].ToString() == metaValue
                //                where doc.MetaData[metaKey] != null && doc.MetaData[metaKey].ToString() == metaValue
                //                where doc.DeletedDate == DateTime.MinValue
                //                select doc;

                //    result.Result = query.ToList();
                //}
                //else
                //{
                //    var query = from doc in collection.AsQueryable<Holon>()
                //                //where doc.MetaData.ContainsKey(metaKey) && doc.MetaData[metaKey] != null && doc.MetaData[metaKey].ToString() == metaValue
                //                where doc.MetaData[metaKey] != null && doc.MetaData[metaKey].ToString() == metaValue
                //                where doc.HolonType == holonType
                //                where doc.DeletedDate == DateTime.MinValue
                //                select doc;

                //    result.Result = query.ToList();
                //}

                // Use $expr+$getField to match dotted key names literally (not as nested paths)
                // and $toString to handle any BSON value type — all server-side.
                FilterDefinition<Holon> filter = BuildHolonTypeFilter(holonType);
                filter = Builders<Holon>.Filter.And(filter, BuildMetaDataFilter(metaKey, metaValue));

                result.Result = await _dbContext.Holon.FindAsync(filter).Result.ToListAsync();
            }
            catch (Exception ex)
            {
                string errorMessage = string.Concat("Unknown error occured in GetHolonsByMetaDataAsync method. metaKey: ", metaKey, ", metaValue:, ", metaValue, "holonType: ", Enum.GetName(typeof(HolonType), holonType), ". Error details: ", ex.ToString());
                result.IsError = true;
                result.Message = errorMessage;
                LoggingManager.Log(errorMessage, LogType.Error);
                result.Exception = ex;
            }

            return result;
        }

        public OASISResult<IEnumerable<Holon>> GetHolonsByMetaData(string metaKey, string metaValue, HolonType holonType)
        {
            OASISResult<IEnumerable<Holon>> result = new OASISResult<IEnumerable<Holon>>();

            try
            {
                //var collection = _dbContext.MongoDB.GetCollection<Holon>("Holon");

                //if (holonType == HolonType.All)
                //{
                //    var query = from doc in collection.AsQueryable<Holon>()
                //                where doc.MetaData.ContainsKey(metaKey) && doc.MetaData[metaKey] != null && doc.MetaData[metaKey].ToString() == metaValue
                //                where doc.DeletedDate == DateTime.MinValue
                //                select doc;

                //    result.Result = query.ToList();
                //}
                //else
                //{
                //    var query = from doc in collection.AsQueryable<Holon>()
                //                where doc.MetaData.ContainsKey(metaKey) && doc.MetaData[metaKey] != null && doc.MetaData[metaKey].ToString() == metaValue
                //                where doc.HolonType == holonType
                //                where doc.DeletedDate == DateTime.MinValue
                //                select doc;

                //    result.Result = query.ToList();
                //}

                FilterDefinition<Holon> filter = Builders<Holon>.Filter.And(
                    BuildHolonTypeFilter(holonType), BuildMetaDataFilter(metaKey, metaValue));

                result.Result = _dbContext.Holon.Find(filter).ToList();
            }
            catch (Exception ex)
            {
                string errorMessage = string.Concat("Unknown error occured in GetHolonsByMetaDataAsync method. metaKey: ", metaKey, ", metaValue:, ", metaValue, "holonType: ", Enum.GetName(typeof(HolonType), holonType), ". Error details: ", ex.ToString());
                result.IsError = true;
                result.Message = errorMessage;
                LoggingManager.Log(errorMessage, LogType.Error);
                result.Exception = ex;
            }

            return result;
        }

        public async Task<OASISResult<IEnumerable<Holon>>> GetHolonsByMetaDataAsync(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType holonType)
        {
            OASISResult<IEnumerable<Holon>> result = new OASISResult<IEnumerable<Holon>>();

            try
            {
                //TODO: Need to finish later! ;-)
                //var collection = _dbContext.MongoDB.GetCollection<Holon>("Holon");

                //if (holonType == HolonType.All)
                //{
                //    var query = from doc in collection.AsQueryable<Holon>()
                //                where doc.MetaData.ContainsKey(metaKey) && doc.MetaData[metaKey] != null && doc.MetaData[metaKey].ToString() == metaValue
                //                select doc;

                //    result.Result = query.ToList();
                //}
                //else
                //{
                //    var query = from doc in collection.AsQueryable<Holon>()
                //                where doc.MetaData.ContainsKey(metaKey) && doc.MetaData[metaKey] != null && doc.MetaData[metaKey].ToString() == metaValue
                //                where doc.HolonType == holonType
                //                select doc;

                //    result.Result = query.ToList();
                //}



                //TODO: Need to write a query to load by meta data so is more efficent! :)
                //result.Result = await _dbContext.Holon.FindAsync(BuildFilterForGetHolonsForParentByMetaData(metaKey, metaValue, holonType)).Result.ToListAsync();

                FilterDefinition<Holon> filter = Builders<Holon>.Filter.And(
                    BuildHolonTypeFilter(holonType),
                    BuildMetaDataMultiFilter(metaKeyValuePairs, metaKeyValuePairMatchMode));

                result.Result = await _dbContext.Holon.FindAsync(filter).Result.ToListAsync();

                //if (holonType != HolonType.All)
                //    result.Result = matchedHolons.Where(x => x.HolonType == holonType).ToList();
                //else
                //    result.Result = matchedHolons;
            }
            catch (Exception ex)
            {
                string errorMessage = string.Concat("Unknown error occured in GetHolonsByMetaDataAsync method. holonType: ", Enum.GetName(typeof(HolonType), holonType), ". Error details: ", ex.ToString());
                result.IsError = true;
                result.Message = errorMessage;
                LoggingManager.Log(errorMessage, LogType.Error);
                result.Exception = ex;
            }

            return result; 
        }

        public OASISResult<IEnumerable<Holon>> GetHolonsByMetaData(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType holonType)
        {
            OASISResult<IEnumerable<Holon>> result = new OASISResult<IEnumerable<Holon>>();

            try
            {
                FilterDefinition<Holon> filter = Builders<Holon>.Filter.And(
                    BuildHolonTypeFilter(holonType),
                    BuildMetaDataMultiFilter(metaKeyValuePairs, metaKeyValuePairMatchMode));

                result.Result = _dbContext.Holon.Find(filter).ToList();

                //if (holonType != HolonType.All)
                //    result.Result = matchedHolons.Where(x => x.HolonType == holonType).ToList();
                //else
                //    result.Result = matchedHolons;
            }
            catch (Exception ex)
            {
                string errorMessage = string.Concat("Unknown error occured in GetHolonsByMetaData method. holonType: ", Enum.GetName(typeof(HolonType), holonType), ". Error details: ", ex.ToString());
                result.IsError = true;
                result.Message = errorMessage;
                LoggingManager.Log(errorMessage, LogType.Error);
                result.Exception = ex;
            }

            return result;
        }

        public async Task<OASISResult<Holon>> UpdateAsync(Holon holon)
        {
            OASISResult<Holon> result = new OASISResult<Holon>();

            try
            {
                // If the caller did not supply the MongoDB ObjectId (_id), look it up by HolonId
                // so that ReplaceOneAsync does not try to write _id: null onto the existing document
                // (MongoDB rejects this with error code 66 "immutable field _id was altered").
                // This happens when stateless REST/JS clients construct a holon from scratch — they
                // know the OASIS GUID (HolonId) but have no way to know the internal MongoDB _id.
                if (string.IsNullOrEmpty(holon.Id))
                {
                    Holon originalHolon = await GetHolonAsync(holon.HolonId);
                    if (originalHolon != null)
                        holon.Id = originalHolon.Id;
                }

                // Old code (kept for reference — was previously commented out and never ran):
                // if (holon.Id == null)
                // {
                //     //Holon originalHolon = await GetHolonAsync(holon.HolonId);
                //     //if (originalHolon != null)
                //     //{
                //     //    holon.Id = originalHolon.Id;
                //     //    holon.CreatedByAvatarId = originalHolon.CreatedByAvatarId;
                //     //    holon.CreatedDate = originalHolon.CreatedDate;
                //     //    holon.HolonType = originalHolon.HolonType;
                //     //    holon.ParentZome = originalHolon.ParentZome;
                //     //    holon.ParentZomeId = originalHolon.ParentZomeId;
                //     //    holon.ParentMoon = originalHolon.ParentMoon;
                //     //    holon.ParentPlanet = originalHolon.ParentPlanet;
                //     //    holon.ParentMoonId = originalHolon.ParentMoonId;
                //     //    holon.ParentPlanetId = originalHolon.ParentPlanetId;
                //     //    holon.Children = originalHolon.Children;
                //     //    holon.DeletedByAvatarId = originalHolon.DeletedByAvatarId;
                //     //    holon.DeletedDate = originalHolon.DeletedDate;
                //     //    holon.MetaData = originalHolon.MetaData;
                //     //    //TODO: Needs more thought!
                //     //}
                // }

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

        //public async Task<OASISResult<IHolon>> DeleteAsync(Guid id, Guid avatarId, bool softDelete = true)
        //{
        //    OASISResult<IHolon> result = new OASISResult<IHolon>();

        //    try
        //    {
        //        Holon holon = await GetHolonAsync(id);

        //        if (holon != null)
        //        {
        //            if (softDelete)
        //            {
        //                result = Helpers.DataHelper.ConvertMongoEntityToOASISHolon(await SoftDeleteAsync(holon, avatarId));

        //                if (result.Result != null)
        //                {
        //                    result.IsDeleted = true;
        //                    result.DeletedCount = 1;
        //                }
        //            }
        //            else
        //            {
        //                FilterDefinition<Holon> data = Builders<Holon>.Filter.Where(x => x.HolonId == id);
        //                await _dbContext.Holon.DeleteOneAsync(data);
        //                result.IsDeleted = true;
        //                result.DeletedCount = 1;
        //                result.Result = Helpers.DataHelper.ConvertMongoEntityToOASISHolon(holon);
        //            }
        //        }
        //        else
        //        {
        //            result.IsError = true;
        //            result.Message = $"Holon with id {id} not found.";
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"Error Occured In MongoDBOASIS Provider.HolonRepository.DeleteAsync. Reason: {e}");
        //    }

        //    return result;
        //}

        //public OASISResult<IHolon> Delete(Guid id, Guid avatarId, bool softDelete = true)
        //{
        //    OASISResult<IHolon> result = new OASISResult<IHolon>();

        //    try
        //    {
        //        Holon holon = GetHolon(id);

        //        if (holon != null)
        //        {
        //            if (softDelete)
        //            {
        //                result = Helpers.DataHelper.ConvertMongoEntityToOASISHolon(SoftDelete(holon, avatarId));

        //                if (result.Result != null)
        //                {
        //                    result.IsDeleted = true;
        //                    result.DeletedCount = 1;
        //                }
        //            }
        //            else
        //            {
        //                FilterDefinition<Holon> data = Builders<Holon>.Filter.Where(x => x.HolonId == id);
        //                _dbContext.Holon.DeleteOne(data);
        //                result.IsDeleted = true;
        //                result.DeletedCount = 1;
        //                result.Result = Helpers.DataHelper.ConvertMongoEntityToOASISHolon(holon);
        //            }
        //        }
        //        else
        //        {
        //            result.IsError = true;
        //            result.Message = $"Holon with id {id} not found.";
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"Error Occured In MongoDBOASIS Provider.HolonRepository.Delete. Reason: {e}");
        //    }

        //    return result;
        //}

        //public async Task<OASISResult<IHolon>> DeleteAsync(Guid avatarId, string providerKey, bool softDelete = true)
        //{
        //    OASISResult<IHolon> result = new OASISResult<IHolon>();

        //    try
        //    {
        //        Holon holon = await GetHolonAsync(providerKey);

        //        if (holon != null)
        //        {
        //            if (softDelete)
        //            {
        //                result = Helpers.DataHelper.ConvertMongoEntityToOASISHolon(await SoftDeleteAsync(holon, avatarId));

        //                if (result.Result != null)
        //                {
        //                    result.IsDeleted = true;
        //                    result.DeletedCount = 1;
        //                }
        //            }
        //            else
        //            {
        //                FilterDefinition<Holon> data = Builders<Holon>.Filter.Where(x => x.ProviderUniqueStorageKey[ProviderType.MongoDBOASIS] == providerKey);
        //                await _dbContext.Holon.DeleteOneAsync(data);
        //                result.IsDeleted = true;
        //                result.DeletedCount = 1;
        //                result.Result = Helpers.DataHelper.ConvertMongoEntityToOASISHolon(holon);
        //            }
        //        }
        //        else
        //        {
        //            result.IsError = true;
        //            result.Message = $"Holon with providerKey {providerKey} not found.";
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"Error Occured In MongoDBOASIS Provider.HolonRepository.DeleteAsync. Reason: {e}");
        //    }

        //    return result;
        //}

        //public OASISResult<IHolon> Delete(Guid avatarId, string providerKey, bool softDelete = true)
        //{
        //    OASISResult<IHolon> result = new OASISResult<IHolon>();

        //    try
        //    {
        //        Holon holon = GetHolon(providerKey);

        //        if (holon != null)
        //        {
        //            if (softDelete)
        //            {
        //                result = Helpers.DataHelper.ConvertMongoEntityToOASISHolon(SoftDelete(holon, avatarId));

        //                if (result.Result != null)
        //                {
        //                    result.IsDeleted = true;
        //                    result.DeletedCount = 1;
        //                }
        //            }
        //            else
        //            {
        //                FilterDefinition<Holon> data = Builders<Holon>.Filter.Where(x => x.ProviderUniqueStorageKey[ProviderType.MongoDBOASIS] == providerKey);
        //                _dbContext.Holon.DeleteOne(data);
        //                result.IsDeleted = true;
        //                result.DeletedCount = 1;
        //                result.Result = Helpers.DataHelper.ConvertMongoEntityToOASISHolon(holon);
        //            }
        //        }
        //        else
        //        {
        //            result.IsError = true;
        //            result.Message = $"Holon with providerKey {providerKey} not found.";
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"Error Occured In MongoDBOASIS Provider.HolonRepository.Delete. Reason: {e}");
        //    }

        //    return result;
        //}

        public async Task<OASISResult<IHolon>> DeleteAsync(Guid id)
        {
            OASISResult<IHolon> result = new OASISResult<IHolon>();

            try
            {
                Holon holon = await GetHolonAsync(id);

                if (holon != null)
                {
                    FilterDefinition<Holon> data = Builders<Holon>.Filter.Where(x => x.HolonId == id);
                    await _dbContext.Holon.DeleteOneAsync(data);
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
                OASISErrorHandling.HandleError(ref result, $"Error Occured In MongoDBOASIS Provider.HolonRepository.DeleteAsync. Reason: {e}");
            }

            return result;
        }
    }
}
