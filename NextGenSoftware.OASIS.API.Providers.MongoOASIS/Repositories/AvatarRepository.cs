using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Interfaces;

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

        public Avatar Add(Avatar avatar)
        {
            try
            {
                avatar.HolonId = Guid.NewGuid();
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
                await _dbContext.Avatar.ReplaceOneAsync(filter: g => g.Id == avatar.Id, replacement: avatar);
                return avatar;
            }
            catch
            {
                throw;
            }
        }

        public Avatar Update(Avatar avatar)
        {
            try
            {
                _dbContext.Avatar.ReplaceOne(filter: g => g.Id == avatar.Id, replacement: avatar);
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
                    await _dbContext.Avatar.ReplaceOneAsync(filter: g => g.Id == avatar.Id, replacement: avatar);
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
                    _dbContext.Avatar.ReplaceOne(filter: g => g.Id == avatar.Id, replacement: avatar);
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
                    await _dbContext.Avatar.ReplaceOneAsync(filter: g => g.Id == avatar.Id, replacement: avatar);
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
                    _dbContext.Avatar.ReplaceOne(filter: g => g.Id == avatar.Id, replacement: avatar);
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