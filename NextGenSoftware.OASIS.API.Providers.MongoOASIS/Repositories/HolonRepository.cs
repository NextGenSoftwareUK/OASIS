using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Managers;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS
{
    public class HolonRepository : IHolonRepository
    {
        private MongoDbContext _dbContext;

        public HolonRepository(MongoDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<Holon> Add(Holon holon)
        {
            try
            {
                holon.Id = Guid.NewGuid().ToString();

                //TODO: Cant remember why this is commented out?! Need to look into... ;-)
                //if (AvatarManager.LoggedInAvatar != null)
                //    avatar.CreatedByAvatarId = AvatarManager.LoggedInAvatar.Id.ToString();

                //avatar.CreatedDate = DateTime.Now;

                await _dbContext.Holon.InsertOneAsync(holon);
                
                //avatar.Id =  //TODO: Check if Mongo populates the id automatically or if we need to re-load it...
                return holon;
            }
            catch
            {
                throw;
            }
        }
        public async Task<Holon> GetHolon(Guid id)
        {
            try
            {
                //FilterDefinition<Avatar> filter = Builders<Avatar>.Filter.Eq("Id", id);
                FilterDefinition<Holon> filter = Builders<Holon>.Filter.Eq("Id", id.ToString());
                return await _dbContext.Holon.Find(filter).FirstOrDefaultAsync();
            }
            catch
            {
                throw;
            }
        }

        public async Task<Holon> GetHolon(string providerKey)
        {
            try
            {
                FilterDefinition<Holon> filter = Builders<Holon>.Filter.Eq("ProviderKey", providerKey);
                return await _dbContext.Holon.Find(filter).FirstOrDefaultAsync();
            }
            catch
            {
                throw;
            }
        }

        public async Task<IEnumerable<Holon>> GetHolons()
        {
            try
            {
                return await _dbContext.Holon.Find(_ => true).ToListAsync();
            }
            catch
            {
                throw;
            }
        }
        public async Task<Holon> Update(Holon holon)
        {
            try
            {
                //TODO: Cant remember why this is commented out?! Need to look into... ;-)
               // if (AvatarManager.LoggedInAvatar != null)
               //     avatar.ModifiedByAvatarId = AvatarManager.LoggedInAvatar.Id.ToString();
                
                //avatar.ModifiedDate = DateTime.Now;
                await _dbContext.Holon.ReplaceOneAsync(filter: g => g.Id == holon.Id, replacement: (Holon)holon);

                //avatar.Id =  //TODO: Check if Mongo populates the id automatically or if we need to re-load it...
                return holon;
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
                    Holon holon = await GetHolon(id);
                    return await SoftDelete(holon);
                }
                else
                {
                    FilterDefinition<Holon> data = Builders<Holon>.Filter.Eq("Id", id);
                    await _dbContext.Holon.DeleteOneAsync(data);
                    return true;
                }
            }
            catch
            {
                throw;
            }
        }

        public async Task<bool> Delete(string providerKey, bool softDelete = true)
        {
            try
            {
                if (softDelete)
                {
                    Holon holon = await GetHolon(providerKey);
                    return await SoftDelete(holon);
                }
                else
                {
                    FilterDefinition<Holon> data = Builders<Holon>.Filter.Eq("ProviderKey", providerKey);
                    await _dbContext.Holon.DeleteOneAsync(data);
                    return true;
                }
            }
            catch
            {
                throw;
            }
        }

        private async Task<bool> SoftDelete(Holon holon)
        {
            try
            {
                if (AvatarManager.LoggedInAvatar != null)
                    holon.DeletedByAvatarId = AvatarManager.LoggedInAvatar.Id.ToString();

                holon.DeletedDate = DateTime.Now;
                await _dbContext.Holon.ReplaceOneAsync(filter: g => g.Id == holon.Id, replacement: holon);
                return true;
            }
            catch
            {
                throw;
            }
        }
    }
}
