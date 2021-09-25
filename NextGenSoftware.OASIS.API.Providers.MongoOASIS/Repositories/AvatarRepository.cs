using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Managers;
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
            _dbContext = dbContext;
        }

        public async Task<Avatar> AddAsync(Avatar avatar)
        {
            try
            {
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

        public Avatar GetAvatar(Expression<Func<Avatar, bool>> expression)
        {
            var filter = Builders<Avatar>.Filter.Where(expression);
            return _dbContext.Avatar.Find(filter).FirstOrDefault();
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
                FilterDefinition<Avatar> filter = Builders<Avatar>.Filter.Where(x => x.Username == username);
                return await _dbContext.Avatar.FindAsync(filter).Result.FirstOrDefaultAsync();
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
                return _dbContext.Avatar.Find(filter).FirstOrDefault();
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

        public async Task<bool> DeleteAsync(Guid id, bool softDelete = true)
        {
            try
            {
                if (softDelete)
                {
                    Avatar avatar = await GetAvatarAsync(id);

                    if (AvatarManager.LoggedInAvatar != null)
                        avatar.DeletedByAvatarId = AvatarManager.LoggedInAvatar.Id.ToString();

                    avatar.DeletedDate = DateTime.Now;
                    await _dbContext.Avatar.ReplaceOneAsync(filter: g => g.HolonId == avatar.HolonId, replacement: avatar);
                    return true;
                }
                else
                {
                    FilterDefinition<Avatar> data = Builders<Avatar>.Filter.Where(x => x.HolonId == id);
                    await _dbContext.Avatar.DeleteOneAsync(data);
                    return true;
                }
            }
            catch
            {
                throw;
            }
        }
        
        public bool Delete(Expression<Func<Avatar, bool>> expression, bool softDelete = true)
        {
            try
            {
                if (softDelete)
                {
                    Avatar avatar = GetAvatar(expression);

                    if (AvatarManager.LoggedInAvatar != null)
                        avatar.DeletedByAvatarId = AvatarManager.LoggedInAvatar.Id.ToString();

                    avatar.DeletedDate = DateTime.Now;
                    _dbContext.Avatar.ReplaceOne(filter: g => g.HolonId == avatar.HolonId, replacement: avatar);
                    return true;
                }
                else
                {
                    FilterDefinition<Avatar> data = Builders<Avatar>.Filter.Where(expression);
                    _dbContext.Avatar.DeleteOne(data);
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }
        
        public async Task<bool> DeleteAsync(Expression<Func<Avatar, bool>> expression, bool softDelete = true)
        {
            try
            {
                if (softDelete)
                {
                    var avatar = await GetAvatarAsync(expression);

                    if (AvatarManager.LoggedInAvatar != null)
                        avatar.DeletedByAvatarId = AvatarManager.LoggedInAvatar.Id.ToString();
                    avatar.DeletedDate = DateTime.Now;
                    await _dbContext.Avatar.ReplaceOneAsync(filter: g => g.HolonId == avatar.HolonId, replacement: avatar);
                    return true;
                }
                else
                {
                    var data = Builders<Avatar>.Filter.Where(expression);
                    await _dbContext.Avatar.DeleteOneAsync(data);
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        public bool Delete(Guid id, bool softDelete = true)
        {
            try
            {
                if (softDelete)
                {
                    Avatar avatar = GetAvatar(id);

                    if (AvatarManager.LoggedInAvatar != null)
                        avatar.DeletedByAvatarId = AvatarManager.LoggedInAvatar.Id.ToString();

                    avatar.DeletedDate = DateTime.Now;
                    _dbContext.Avatar.ReplaceOne(filter: g => g.HolonId == avatar.HolonId, replacement: avatar);
                    return true;
                }
                else
                {
                    FilterDefinition<Avatar> data = Builders<Avatar>.Filter.Where(x => x.HolonId == id);
                    _dbContext.Avatar.DeleteOne(data);
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
                    Avatar avatar = await GetAvatarAsync(providerKey);

                    if (AvatarManager.LoggedInAvatar != null)
                        avatar.DeletedByAvatarId = AvatarManager.LoggedInAvatar.Id.ToString();

                    avatar.DeletedDate = DateTime.Now;
                    await _dbContext.Avatar.ReplaceOneAsync(filter: g => g.HolonId == avatar.HolonId, replacement: avatar);
                    return true;
                }
                else
                {
                    FilterDefinition<Avatar> data = Builders<Avatar>.Filter.Where(x => x.ProviderKey[Core.Enums.ProviderType.MongoDBOASIS] == providerKey);
                    await _dbContext.Avatar.DeleteOneAsync(data);
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
                    Avatar avatar = GetAvatar(providerKey);

                    if (AvatarManager.LoggedInAvatar != null)
                        avatar.DeletedByAvatarId = AvatarManager.LoggedInAvatar.Id.ToString();

                    avatar.DeletedDate = DateTime.Now;
                    _dbContext.Avatar.ReplaceOne(filter: g => g.HolonId == avatar.HolonId, replacement: avatar);
                    return true;
                }
                else
                {
                    FilterDefinition<Avatar> data = Builders<Avatar>.Filter.Where(x => x.ProviderKey[Core.Enums.ProviderType.MongoDBOASIS] == providerKey);
                    _dbContext.Avatar.DeleteOne(data);
                    return true;
                }
            }
            catch
            {
                throw;
            }
        }
    }
}