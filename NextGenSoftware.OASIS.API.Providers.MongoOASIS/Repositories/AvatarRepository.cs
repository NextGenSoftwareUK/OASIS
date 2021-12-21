using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Infrastructure.Singleton;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Interfaces;
using Avatar = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities.Avatar;
using AvatarDetail = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities.AvatarDetail;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Repositories
{
    public class AvatarRepository : IAvatarRepository
    {
        private MongoDbContext _dbContext;

        public AvatarRepository(MongoDbContext dbContext)
        {
            SerializerRegister.GetInstance().RegisterGuidBsonSerializer();
            _dbContext = dbContext;
        }

        public async Task<Avatar> AddAsync(Avatar avatar)
        {
            try
            {
                if (avatar.HolonId == Guid.Empty)
                    avatar.HolonId = Guid.NewGuid();

                avatar.CreatedProviderType = new Core.Helpers.EnumValue<Core.Enums.ProviderType>(Core.Enums.ProviderType.MongoDBOASIS);

                await _dbContext.Avatar.InsertOneAsync(avatar);
                avatar.ProviderKey[Core.Enums.ProviderType.MongoDBOASIS] = avatar.Id;
                
                await UpdateAsync(avatar);
                return avatar;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<AvatarDetail> AddAsync(AvatarDetail avatar)
        {
            try
            {
                if (avatar.HolonId == Guid.Empty)
                    avatar.HolonId = Guid.NewGuid();

                avatar.CreatedProviderType = new Core.Helpers.EnumValue<Core.Enums.ProviderType>(Core.Enums.ProviderType.MongoDBOASIS);

                await _dbContext.AvatarDetail.InsertOneAsync(avatar);
                avatar.ProviderKey[Core.Enums.ProviderType.MongoDBOASIS] = avatar.Id;

                await UpdateAsync(avatar);
                return avatar;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public Avatar Add(Avatar avatar)
        {
            try
            {
                if (avatar.HolonId == Guid.Empty)
                    avatar.HolonId = Guid.NewGuid();

                avatar.CreatedProviderType = new Core.Helpers.EnumValue<Core.Enums.ProviderType>(Core.Enums.ProviderType.MongoDBOASIS);

                _dbContext.Avatar.InsertOne(avatar);
                avatar.ProviderKey[Core.Enums.ProviderType.MongoDBOASIS] = avatar.Id;

                Update(avatar);
                return avatar;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public AvatarDetail Add(AvatarDetail avatar)
        {
            try
            {
                if (avatar.HolonId == Guid.Empty)
                    avatar.HolonId = Guid.NewGuid();

                avatar.CreatedProviderType = new Core.Helpers.EnumValue<Core.Enums.ProviderType>(Core.Enums.ProviderType.MongoDBOASIS);

                _dbContext.AvatarDetail.InsertOne(avatar);
                avatar.ProviderKey[Core.Enums.ProviderType.MongoDBOASIS] = avatar.Id;

                Update(avatar);
                return avatar;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<Avatar> GetAvatarAsync(Guid id)
        {
            try
            {
                FilterDefinition<Avatar> filter = Builders<Avatar>.Filter.Where(x => x.HolonId == id);
                return await _dbContext.Avatar.FindAsync(filter).Result.FirstOrDefaultAsync();
            }
            catch
            {
                throw;
            }
        }

        public Avatar GetAvatar(Guid id)
        {
            try
            {
                FilterDefinition<Avatar> filter = Builders<Avatar>.Filter.Where(x => x.HolonId == id);
                return _dbContext.Avatar.Find(filter).FirstOrDefault();
            }
            catch
            {
                throw;
            }
        }

        public OASISResult<Avatar> GetAvatar(Expression<Func<Avatar, bool>> expression)
        {
            OASISResult<Avatar> result = new OASISResult<Avatar>();

            try
            {
                var filter = Builders<Avatar>.Filter.Where(expression);
                result.Result = _dbContext.Avatar.Find(filter).FirstOrDefault();
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, $"Error in GetAvatar method in AvatarRepository loading Avatar. Reason: {ex}");
            }

            return result;
        }
        
        public async Task<Avatar> GetAvatarAsync(Expression<Func<Avatar, bool>> expression)
        {
            var filter = Builders<Avatar>.Filter.Where(expression);
            var findResult = await _dbContext.Avatar.FindAsync(filter);
            return await findResult.FirstOrDefaultAsync();
        }
        
        
        public AvatarDetail GetAvatarDetail(Expression<Func<AvatarDetail, bool>> expression)
        {
            var filter = Builders<AvatarDetail>.Filter.Where(expression);
            return _dbContext.AvatarDetail.Find(filter).FirstOrDefault();
        }
        
        public async Task<AvatarDetail> GetAvatarDetailAsync(Expression<Func<AvatarDetail, bool>> expression)
        {
            var filter = Builders<AvatarDetail>.Filter.Where(expression);
            var findResult = await _dbContext.AvatarDetail.FindAsync(filter);
            return await findResult.FirstOrDefaultAsync();
        }

        public async Task<Avatar> GetAvatarAsync(string username)
        {
            try
            {
                //TODO: Find out how mongo sorts descending by date! It works for non async fine (below)!
                //FilterDefinition<Avatar> filter = Builders<Avatar>.Sort.Descending(x => x.CreatedDate).(x => x.Username == username);
                FilterDefinition<Avatar> filter = Builders<Avatar>.Filter.Where(x => x.Username == username);
                //return await _dbContext.Avatar.FindAsync(filter).Result.FirstOrDefaultAsync();
                List<Avatar> avatars = await _dbContext.Avatar.FindAsync(filter).Result.ToListAsync();

                //Temp workaround till can find out how mongo sorts async collections!
                avatars.Sort((x, y) => x.CreatedDate.CompareTo(y.CreatedDate));
                avatars.Reverse();
                return avatars[0];
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public Avatar GetAvatar(string username)
        {
            try
            {
                FilterDefinition<Avatar> filter = Builders<Avatar>.Filter.Where(x => x.Username == username);
                return _dbContext.Avatar.Find(filter).SortByDescending(x => x.CreatedDate).FirstOrDefault();
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<Avatar> GetAvatarAsync(string username, string password)
        {
            try
            {
                FilterDefinition<Avatar> filter = Builders<Avatar>.Filter.Where(x => x.Username == username);
                Avatar avatar = await _dbContext.Avatar.FindAsync(filter).Result.FirstOrDefaultAsync();

                if (avatar != null && password != avatar.Password)
                    return null;

                return avatar;
            }
            catch
            {
                throw;
            }
        }

        public Avatar GetAvatar(string username, string password)
        {
            try
            {
                FilterDefinition<Avatar> filter = Builders<Avatar>.Filter.Where(x => x.Username == username);
                Avatar avatar = _dbContext.Avatar.FindAsync(filter).Result.FirstOrDefault();

                if (avatar != null && password != avatar.Password)
                    return null;

                return avatar;
            }
            catch
            {
                throw;
            }
        }

        public async Task<List<Avatar>> GetAvatarsAsync()
        {
            try
            {
                return await _dbContext.Avatar.FindAsync(_ => true).Result.ToListAsync();
            }
            catch
            {
                throw;
            }
        }

        public List<Avatar> GetAvatars()
        {
            try
            {
                return _dbContext.Avatar.Find(_ => true).ToList();
            }
            catch
            {
                throw;
            }
        }

        public async Task<Avatar> UpdateAsync(Avatar avatar)
        {
            try
            {
                await _dbContext.Avatar.ReplaceOneAsync(filter: g => g.HolonId == avatar.HolonId, replacement: avatar);
                return avatar;
            }
            catch
            {
                throw;
            }
        }

        public async Task<AvatarDetail> UpdateAsync(AvatarDetail avatar)
        {
            try
            {
                await _dbContext.AvatarDetail.ReplaceOneAsync(filter: g => g.HolonId == avatar.HolonId, replacement: avatar);
                return avatar;
            }
            catch
            {
                throw;
            }
        }

        public async Task<AvatarDetail> GetAvatarDetailAsync(Guid id)
        {
            var filter = Builders<AvatarDetail>.Filter.Where(x => x.HolonId == id);
            var findResult = await _dbContext.AvatarDetail.FindAsync(filter);
            var detailEntity = await findResult.FirstOrDefaultAsync();
            return detailEntity;
        }

        public AvatarDetail GetAvatarDetail(Guid id)
        {
            var filter = Builders<AvatarDetail>.Filter.Where(x => x.HolonId == id);
            return _dbContext.AvatarDetail.Find(filter).FirstOrDefault();
        }

        public async Task<AvatarDetail> GetAvatarDetailAsync(string username)
        {
            var filter = Builders<AvatarDetail>.Filter.Where(x => x.Username == username);
            var findResult = await _dbContext.AvatarDetail.FindAsync(filter);
            var detailEntity = await findResult.FirstOrDefaultAsync();
            return detailEntity;
        }

        public AvatarDetail GetAvatarDetail(string username)
        {
            var filter = Builders<AvatarDetail>.Filter.Where(x => x.Username == username);
            return _dbContext.AvatarDetail.Find(filter).FirstOrDefault();
        }

        public async Task<IEnumerable<AvatarDetail>> GetAvatarDetailsAsync()
        {
            var cursor = await _dbContext.AvatarDetail.FindAsync(_ => true);
            return cursor.ToEnumerable();
        }

        public IEnumerable<AvatarDetail> GetAvatarDetails()
        {
            return _dbContext.AvatarDetail.Find(_ => true).ToEnumerable();
        }

        //public async Task<AvatarThumbnail> GetAvatarThumbnailByIdAsync(Guid id)
        //{
        //    var filter = Builders<AvatarThumbnail>.Filter.Where(x => x.Id == id);
        //    var findResult = await _dbContext.AvatarThumbnail.FindAsync(filter);
        //    var detailEntity = await findResult.FirstOrDefaultAsync();
        //    return detailEntity;
        //}

        //public AvatarThumbnail GetAvatarThumbnailById(Guid id)
        //{            
        //    var filter = Builders<AvatarThumbnail>.Filter.Where(x => x.Id == id);
        //    return _dbContext.AvatarThumbnail.Find(filter).FirstOrDefault();
        //}

        public Avatar Update(Avatar avatar)
        {
            try
            {
                _dbContext.Avatar.ReplaceOne(filter: g => g.HolonId == avatar.HolonId, replacement: avatar);
                return avatar;
            }
            catch
            {
                throw;
            }
        }

        public AvatarDetail Update(AvatarDetail avatar)
        {
            try
            {
                _dbContext.AvatarDetail.ReplaceOne(filter: g => g.HolonId == avatar.HolonId, replacement: avatar);
                return avatar;
            }
            catch
            {
                throw;
            }
        }

        public async Task<OASISResult<bool>> DeleteAsync(Guid id, bool softDelete = true)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            string errorMessage = "Error occured in DeleteAsync method in AvatarRepository in MongoDBOASIS Provider.";

            try
            {
                if (softDelete)
                {
                    Avatar avatar = await GetAvatarAsync(id);

                    if (avatar == null)
                    {
                        ErrorHandling.HandleError(ref result, $"{errorMessage} The avatar with id {id} was not found.");
                        return result;
                    }

                    if (AvatarManager.LoggedInAvatar != null)
                        avatar.DeletedByAvatarId = AvatarManager.LoggedInAvatar.Id.ToString();

                    avatar.DeletedDate = DateTime.Now;
                    await _dbContext.Avatar.ReplaceOneAsync(filter: g => g.HolonId == avatar.HolonId, replacement: avatar);
                    result.Result = true;
                }
                else
                {
                    FilterDefinition<Avatar> data = Builders<Avatar>.Filter.Where(x => x.HolonId == id);
                    await _dbContext.Avatar.DeleteOneAsync(data);
                    result.Result = true;
                }
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                ErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured, error details: {ex.ToString()}");
            }

            return result;
        }
        
        public OASISResult<bool> Delete(Expression<Func<Avatar, bool>> expression, bool softDelete = true)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            string errorMessage = "Error occured in Delete method in AvatarRepository in MongoDBOASIS Provider.";

            try
            {
                if (softDelete)
                {
                    Avatar avatar = GetAvatar(expression).Result;

                    if (avatar == null)
                    {
                        ErrorHandling.HandleError(ref result, $"{errorMessage} The avatar with expression {expression} was not found.");
                        return result;
                    }

                    if (AvatarManager.LoggedInAvatar != null)
                        avatar.DeletedByAvatarId = AvatarManager.LoggedInAvatar.Id.ToString();

                    avatar.DeletedDate = DateTime.Now;
                    _dbContext.Avatar.ReplaceOne(filter: g => g.HolonId == avatar.HolonId, replacement: avatar);
                    result.Result = true;
                }
                else
                {
                    FilterDefinition<Avatar> data = Builders<Avatar>.Filter.Where(expression);
                    _dbContext.Avatar.DeleteOne(data);
                    result.Result = true;
                }
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                ErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured, error details: {ex.ToString()}");
            }

            return result;
        }
        
        public async Task<OASISResult<bool>> DeleteAsync(Expression<Func<Avatar, bool>> expression, bool softDelete = true)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            string errorMessage = "Error occured in Delete method in AvatarRepository in MongoDBOASIS Provider.";

            try
            {
                if (softDelete)
                {
                    Avatar avatar = await GetAvatarAsync(expression);

                    if (avatar == null)
                    {
                        ErrorHandling.HandleError(ref result, $"{errorMessage} The avatar with expression {expression} was not found.");
                        return result;
                    }

                    if (AvatarManager.LoggedInAvatar != null)
                        avatar.DeletedByAvatarId = AvatarManager.LoggedInAvatar.Id.ToString();

                    avatar.DeletedDate = DateTime.Now;
                    await _dbContext.Avatar.ReplaceOneAsync(filter: g => g.HolonId == avatar.HolonId, replacement: avatar);
                    result.Result = true;
                }
                else
                {
                    FilterDefinition<Avatar> data = Builders<Avatar>.Filter.Where(expression);
                    await _dbContext.Avatar.DeleteOneAsync(data);
                    result.Result = true;
                }
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                ErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured, error details: {ex.ToString()}");
            }

            return result;
        }

        public OASISResult<bool> Delete(Guid id, bool softDelete = true)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            string errorMessage = "Error occured in Delete method in AvatarRepository in MongoDBOASIS Provider.";

            try
            {
                if (softDelete)
                {
                    Avatar avatar = GetAvatar(id);

                    if (avatar == null)
                    {
                        ErrorHandling.HandleError(ref result, $"{errorMessage} The avatar with id {id} was not found.");
                        return result;
                    }

                    if (avatar.DeletedDate != DateTime.MinValue)
                    {
                        ErrorHandling.HandleError(ref result, $"{errorMessage} The avatar with id {id} was already soft deleted on {avatar.DeletedDate} by avatar with id {avatar.DeletedByAvatarId}.");
                        return result;
                    }

                    if (AvatarManager.LoggedInAvatar != null)
                        avatar.DeletedByAvatarId = AvatarManager.LoggedInAvatar.Id.ToString();

                    avatar.DeletedDate = DateTime.Now;
                    _dbContext.Avatar.ReplaceOne(filter: g => g.HolonId == avatar.HolonId, replacement: avatar);
                    result.Result = true;
                }
                else{
                    FilterDefinition<Avatar> data = Builders<Avatar>.Filter.Where(x => x.HolonId == id);
                    _dbContext.Avatar.DeleteOne(data);
                    result.Result = true;
                }
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                ErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured, error details: {ex.ToString()}");
            }

            return result;
        }

        public async Task<OASISResult<bool>> DeleteAsync(string providerKey, bool softDelete = true)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            string errorMessage = "Error occured in DeleteAsync method in AvatarRepository in MongoDBOASIS Provider.";

            try
            {
                if (softDelete)
                {
                    Avatar avatar = await GetAvatarAsync(providerKey);

                    if (avatar == null)
                    {
                        ErrorHandling.HandleError(ref result, $"{errorMessage} The avatar with providerKey {providerKey} was not found.");
                        return result;
                    }

                    if (AvatarManager.LoggedInAvatar != null)
                        avatar.DeletedByAvatarId = AvatarManager.LoggedInAvatar.Id.ToString();

                    avatar.DeletedDate = DateTime.Now;
                    await _dbContext.Avatar.ReplaceOneAsync(filter: g => g.HolonId == avatar.HolonId, replacement: avatar);
                    result.Result = true;
                }
                else
                {
                    FilterDefinition<Avatar> data = Builders<Avatar>.Filter.Where(x => x.ProviderKey[Core.Enums.ProviderType.MongoDBOASIS] == providerKey);
                    await _dbContext.Avatar.DeleteOneAsync(data);
                    result.Result = true;
                }
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                ErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured, error details: {ex.ToString()}");
            }

            return result;
        }

        public OASISResult<bool> Delete(string providerKey, bool softDelete = true)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            string errorMessage = "Error occured in Delete method in AvatarRepository in MongoDBOASIS Provider.";

            try
            {
                if (softDelete)
                {
                    Avatar avatar = GetAvatar(providerKey);

                    if (avatar == null)
                    {
                        ErrorHandling.HandleError(ref result, $"{errorMessage} The avatar with providerKey {providerKey} was not found.");
                        return result;
                    }

                    if (AvatarManager.LoggedInAvatar != null)
                        avatar.DeletedByAvatarId = AvatarManager.LoggedInAvatar.Id.ToString();

                    avatar.DeletedDate = DateTime.Now;
                    _dbContext.Avatar.ReplaceOne(filter: g => g.HolonId == avatar.HolonId, replacement: avatar);
                    result.Result = true;
                }
                else
                {
                    FilterDefinition<Avatar> data = Builders<Avatar>.Filter.Where(x => x.ProviderKey[Core.Enums.ProviderType.MongoDBOASIS] == providerKey);
                    _dbContext.Avatar.DeleteOne(data);
                    result.Result = true;
                }
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                ErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured, error details: {ex.ToString()}");
            }

            return result;
        }
    }
}