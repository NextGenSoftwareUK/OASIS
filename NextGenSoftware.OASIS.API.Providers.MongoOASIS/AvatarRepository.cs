using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Providers.MongoOASIS
{
    public class AvatarRepository : IAvatarRepository
    {
        private MongoDbContext _dbContext;

        public AvatarRepository(MongoDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IAvatar> Add(IAvatar avatar)
        {
            try
            {
                await _dbContext.Avatar.InsertOneAsync((Avatar)avatar);
                
                //avatar.Id =  //TODO: Check if Mongo populates the id automatically or if we need to re-load it...
                return avatar;
            }
            catch
            {
                throw;
            }
        }
        public async Task<IAvatar> GetAvatar(string id)
        {
            try
            {
                FilterDefinition<Avatar> filter = Builders<Avatar>.Filter.Eq("Id", id);
                return await _dbContext.Avatar.Find(filter).FirstOrDefaultAsync();
            }
            catch
            {
                throw;
            }
        }

        public async Task<IAvatar> GetAvatar(string username, string password)
        {
            try
            {
                FilterDefinition<Avatar> filter = Builders<Avatar>.Filter.Eq("Id", id);
                return await _dbContext.Avatar.Find(filter).FirstOrDefaultAsync();
            }
            catch
            {
                throw;
            }
        }

        public async Task<IEnumerable<IAvatar>> GetAvatars()
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
        public async Task<IAvatar> Update(IAvatar avatar)
        {
            try
            {
                await _dbContext.Avatar.ReplaceOneAsync(filter: g => g.Id == avatar.Id, replacement: (Avatar)avatar);

                //avatar.Id =  //TODO: Check if Mongo populates the id automatically or if we need to re-load it...
                return avatar;
            }
            catch
            {
                throw;
            }
        }
        public async Task Delete(string id)
        {
            try
            {
                FilterDefinition<Avatar> data = Builders<Avatar>.Filter.Eq("Id", id);
                await _dbContext.Avatar.DeleteOneAsync(data);
            }
            catch
            {
                throw;
            }
        }
    }
}
