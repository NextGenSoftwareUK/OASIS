using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS
{
    public class AvatarRepository : IAvatarRepository
    {
        private MongoDbContext _dbContext;

        public AvatarRepository(MongoDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<Avatar> Add(Avatar avatar)
        {
            try
            {
                avatar.AvatarId = Guid.NewGuid().ToString();

                //if (AvatarManager.LoggedInAvatar != null)
                //    avatar.CreatedByAvatarId = AvatarManager.LoggedInAvatar.Id.ToString();

                //avatar.CreatedDate = DateTime.Now;

                await _dbContext.Avatar.InsertOneAsync(avatar);
                
                //avatar.Id =  //TODO: Check if Mongo populates the id automatically or if we need to re-load it...
                return avatar;
            }
            catch
            {
                throw;
            }
        }
        public async Task<Avatar> GetAvatar(Guid id)
        {
            try
            {
                //FilterDefinition<Avatar> filter = Builders<Avatar>.Filter.Eq("Id", id);
                FilterDefinition<Avatar> filter = Builders<Avatar>.Filter.Eq("AvatarId", id.ToString());
                return await _dbContext.Avatar.Find(filter).FirstOrDefaultAsync();
            }
            catch
            {
                throw;
            }
        }

        public async Task<Avatar> GetAvatar(string username)
        {
            try
            {
                //TODO: (MONGOFIX) Better if can query more than field at once in Mongo? Must be possible.... Can a Mongo dev PLEASE sort this... thanks... :)
                FilterDefinition<Avatar> filter = Builders<Avatar>.Filter.Eq("Username", username);
                //FilterDefinition<Avatar> filter = Builders<Avatar>.Filter.AnyEq(new FieldDefinition<TDocument>)

                Avatar avatar = await _dbContext.Avatar.Find(filter).FirstOrDefaultAsync();
                return avatar;
            }
            catch
            {
                throw;
            }
        }

        public async Task<Avatar> GetAvatar(string username, string password)
        {
            try
            {
                //TODO: (MONGOFIX) Better if can query more than field at once in Mongo? Must be possible.... Can a Mongo dev PLEASE sort this... thanks... :)
                FilterDefinition<Avatar> filter = Builders<Avatar>.Filter.Eq("Username", username);
                //FilterDefinition<Avatar> filter = Builders<Avatar>.Filter.AnyEq(new FieldDefinition<TDocument>)

                Avatar avatar = await _dbContext.Avatar.Find(filter).FirstOrDefaultAsync();

                if (avatar != null && password != avatar.Password)
                    return null;

                return avatar;
            }
            catch
            {
                throw;
            }
        }

        public async Task<List<Avatar>> GetAvatars()
        {
            try
            {
                return await _dbContext.Avatar.Find(_ => true).ToListAsync();
            }
            catch
            {
                throw;
            }
        }
        public async Task<Avatar> Update(Avatar avatar)
        {
            try
            {
               // if (AvatarManager.LoggedInAvatar != null)
               //     avatar.ModifiedByAvatarId = AvatarManager.LoggedInAvatar.Id.ToString();
                
                //avatar.ModifiedDate = DateTime.Now;
                await _dbContext.Avatar.ReplaceOneAsync(filter: g => g.Id == avatar.Id, replacement: (Avatar)avatar);

                //avatar.Id =  //TODO: Check if Mongo populates the id automatically or if we need to re-load it...
                return avatar;
            }
            catch
            {
                throw;
            }
        }
        public async Task<bool> Delete(Guid id, bool softDelete = true)
        {
            try
            {
                if (softDelete)
                {
                    Avatar avatar = await GetAvatar(id);

                    if (AvatarManager.LoggedInAvatar != null)
                        avatar.DeletedByAvatarId = AvatarManager.LoggedInAvatar.Id.ToString();
                    
                    avatar.DeletedDate = DateTime.Now;
                    await _dbContext.Avatar.ReplaceOneAsync(filter: g => g.Id == avatar.Id, replacement: avatar);
                    return true;
                }
                else
                {
                    FilterDefinition<Avatar> data = Builders<Avatar>.Filter.Eq("Id", id);
                    await _dbContext.Avatar.DeleteOneAsync(data);
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
