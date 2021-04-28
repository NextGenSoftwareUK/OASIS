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
                //holon.HolonId = Guid.NewGuid().ToString();
                holon.HolonId = Guid.NewGuid();

                //TODO: Cant remember why this is commented out?! Need to look into... ;-)
                //if (AvatarManager.LoggedInAvatar != null)
                //    avatar.CreatedByAvatarId = AvatarManager.LoggedInAvatar.Id.ToString();

                //avatar.CreatedDate = DateTime.Now;

                await _dbContext.Holon.InsertOneAsync(holon);
                
                //avatar.Id =  //TODO: Check if Mongo populates the id automatically or if we need to re-load it...
                return holon;
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        public async Task<Holon> GetHolon(Guid id)
        {
            try
            {
                //FilterDefinition<Avatar> filter = Builders<Avatar>.Filter.Eq("Id", id);
                FilterDefinition<Holon> filter = Builders<Holon>.Filter.Eq("HolonId", id.ToString());
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

        public async Task<IEnumerable<Holon>> GetAllHolons()
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

        public async Task<List<Holon>> GetAllHolonsForParent(Guid id)
        {
            try
            {
                FilterDefinition<Holon> filter = Builders<Holon>.Filter.Eq("ParentId", id.ToString());
                return _dbContext.Holon.Find(filter).ToList();
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
                if (holon.Id == null)
                {
                    Holon originalHolon = GetHolon(holon.HolonId).Result;

                    if (originalHolon != null)
                    {
                        holon.Id = originalHolon.Id;
                        holon.CreatedByAvatarId = originalHolon.CreatedByAvatarId;
                        holon.CreatedDate = originalHolon.CreatedDate;
                        holon.HolonType = originalHolon.HolonType;
                        holon.ParentCelestialBody = originalHolon.ParentCelestialBody;
                        holon.Children = originalHolon.Children;
                        holon.DeletedByAvatarId = originalHolon.DeletedByAvatarId;
                        holon.DeletedDate = originalHolon.DeletedDate;
                        
                        //TODO: Needs more thought!
                    }
                }

              //   if (AvatarManager.LoggedInAvatar != null)
                //    holon.ModifiedByAvatarId = AvatarManager.LoggedInAvatar.Id.ToString();

             //   holon.ModifiedDate = DateTime.Now;
                //await _dbContext.Holon.ReplaceOneAsync(filter: g => g.Id == holon.Id, replacement: (Holon)holon);
                await _dbContext.Holon.ReplaceOneAsync(filter: g => g.HolonId == holon.HolonId, replacement: (Holon)holon);

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
                    //holon.DeletedByAvatarId = AvatarManager.LoggedInAvatar.Id;
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
