using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Repositories
{
    //TODO: Implement non async versions...
    public class HolonRepository : IHolonRepository
    {
        private MongoDbContext _dbContext;

        public HolonRepository(MongoDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<Holon> AddAsync(Holon holon)
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

        public Holon Add(Holon holon)
        {
            try
            {
                //holon.HolonId = Guid.NewGuid().ToString();
                holon.HolonId = Guid.NewGuid();

                //TODO: Cant remember why this is commented out?! Need to look into... ;-)
                //if (AvatarManager.LoggedInAvatar != null)
                //    avatar.CreatedByAvatarId = AvatarManager.LoggedInAvatar.Id.ToString();

                //avatar.CreatedDate = DateTime.Now;

                _dbContext.Holon.InsertOne(holon);

                //avatar.Id =  //TODO: Check if Mongo populates the id automatically or if we need to re-load it...
                return holon;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<Holon> GetHolonAsync(Guid id)
        {
            try
            {
                //FilterDefinition<Avatar> filter = Builders<Avatar>.Filter.Eq("Id", id);
                FilterDefinition<Holon> filter = Builders<Holon>.Filter.Eq("HolonId", id.ToString());
                return await _dbContext.Holon.FindAsync(filter).Result.FirstOrDefaultAsync();
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
                //FilterDefinition<Avatar> filter = Builders<Avatar>.Filter.Eq("Id", id);
                FilterDefinition<Holon> filter = Builders<Holon>.Filter.Eq("HolonId", id.ToString());
                return _dbContext.Holon.Find(filter).FirstOrDefault();
            }
            catch
            {
                throw;
            }
        }

        public async Task<Holon> GetHolonAsync(string providerKey)
        {
            try
            {
                FilterDefinition<Holon> filter = Builders<Holon>.Filter.Eq("ProviderKey", providerKey);
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
                FilterDefinition<Holon> filter = Builders<Holon>.Filter.Eq("ProviderKey", providerKey);
                return _dbContext.Holon.Find(filter).FirstOrDefault();
            }
            catch
            {
                throw;
            }
        }

        public async Task<IEnumerable<Holon>> GetAllHolonsAsync()
        {
            try
            {
                return await _dbContext.Holon.FindAsync(_ => true).Result.ToListAsync();
            }
            catch
            {
                throw;
            }
        }

        public IEnumerable<Holon> GetAllHolons()
        {
            try
            {
                return _dbContext.Holon.Find(_ => true).ToList();
            }
            catch
            {
                throw;
            }
        }

        public async Task<IEnumerable<Holon>> GetAllHolonsForParentAsync(Guid id, HolonType holonType)
        {
            try
            {
                FilterDefinition<Holon> filter = Builders<Holon>.Filter.And(
                    Builders<Holon>.Filter.Where(p => p.ParentHolonId == id),
                    Builders<Holon>.Filter.Where(p => p.HolonType == holonType));

                return await _dbContext.Holon.FindAsync(filter).Result.ToListAsync();
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
                FilterDefinition<Holon> filter = Builders<Holon>.Filter.And(
                    Builders<Holon>.Filter.Where(p => p.ParentHolonId == id),
                    Builders<Holon>.Filter.Where(p => p.HolonType == holonType));

                return _dbContext.Holon.Find(filter).ToList();
            }
            catch
            {
                throw;
            }
        }

        public async Task<IEnumerable<Holon>> GetAllHolonsForParentAsync(string providerKey, HolonType holonType)
        {
            try
            {
                FilterDefinition<Holon> filter = Builders<Holon>.Filter.And(
                    Builders<Holon>.Filter.Where(p => p.ProviderKey[ProviderType.MongoDBOASIS] == providerKey),
                    Builders<Holon>.Filter.Where(p => p.HolonType == holonType));

                return await _dbContext.Holon.FindAsync(filter).Result.ToListAsync();
            }
            catch
            {
                throw;
            }
        }

        public IEnumerable<Holon> GetAllHolonsForParent(string providerKey, HolonType holonType)
        {
            try
            {
                FilterDefinition<Holon> filter = Builders<Holon>.Filter.And(
                    Builders<Holon>.Filter.Where(p => p.ProviderKey[ProviderType.MongoDBOASIS] == providerKey),
                    Builders<Holon>.Filter.Where(p => p.HolonType == holonType));

                return _dbContext.Holon.Find(filter).ToList();
            }
            catch
            {
                throw;
            }
        }

        public async Task<Holon> UpdateAsync(Holon holon)
        {
            try
            {
                if (holon.Id == null)
                {
                    Holon originalHolon = await GetHolonAsync(holon.HolonId);

                    if (originalHolon != null)
                    {
                        holon.Id = originalHolon.Id;
                        holon.CreatedByAvatarId = originalHolon.CreatedByAvatarId;
                        holon.CreatedDate = originalHolon.CreatedDate;
                        holon.HolonType = originalHolon.HolonType;
                        //holon.ParentCelestialBody = originalHolon.ParentCelestialBody;

                        holon.ParentZome = originalHolon.ParentZome;
                        holon.ParentZomeId = originalHolon.ParentZomeId;
                        holon.ParentMoon = originalHolon.ParentMoon;
                        holon.ParentPlanet = originalHolon.ParentPlanet;
                        holon.ParentMoonId = originalHolon.ParentMoonId;
                        holon.ParentPlanetId = originalHolon.ParentPlanetId;
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

        public Holon Update(Holon holon)
        {
            try
            {
                if (holon.Id == null)
                {
                    Holon originalHolon = GetHolon(holon.HolonId);

                    if (originalHolon != null)
                    {
                        holon.Id = originalHolon.Id;
                        holon.CreatedByAvatarId = originalHolon.CreatedByAvatarId;
                        holon.CreatedDate = originalHolon.CreatedDate;
                        holon.HolonType = originalHolon.HolonType;
                        //holon.ParentCelestialBody = originalHolon.ParentCelestialBody;

                        holon.ParentZome = originalHolon.ParentZome;
                        holon.ParentZomeId = originalHolon.ParentZomeId;
                        holon.ParentMoon = originalHolon.ParentMoon;
                        holon.ParentPlanet = originalHolon.ParentPlanet;
                        holon.ParentMoonId = originalHolon.ParentMoonId;
                        holon.ParentPlanetId = originalHolon.ParentPlanetId;
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
                _dbContext.Holon.ReplaceOne(filter: g => g.HolonId == holon.HolonId, replacement: holon);

                //avatar.Id =  //TODO: Check if Mongo populates the id automatically or if we need to re-load it...
                return holon;
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
                    Holon holon = await GetHolonAsync(id);
                    return await SoftDeleteAsync(holon);
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

        public bool Delete(Guid id, bool softDelete = true)
        {
            try
            {
                if (softDelete)
                {
                    Holon holon = GetHolon(id);
                    return SoftDelete(holon);
                }
                else
                {
                    FilterDefinition<Holon> data = Builders<Holon>.Filter.Eq("Id", id);
                    _dbContext.Holon.DeleteOne(data);
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
                    Holon holon = await GetHolonAsync(providerKey);
                    return await SoftDeleteAsync(holon);
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

        public bool Delete(string providerKey, bool softDelete = true)
        {
            try
            {
                if (softDelete)
                {
                    Holon holon = GetHolon(providerKey);
                    return SoftDelete(holon);
                }
                else
                {
                    FilterDefinition<Holon> data = Builders<Holon>.Filter.Eq("ProviderKey", providerKey);
                    _dbContext.Holon.DeleteOne(data);
                    return true;
                }
            }
            catch
            {
                throw;
            }
        }

        private async Task<bool> SoftDeleteAsync(Holon holon)
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

        private bool SoftDelete(Holon holon)
        {
            try
            {
                if (AvatarManager.LoggedInAvatar != null)
                    //holon.DeletedByAvatarId = AvatarManager.LoggedInAvatar.Id;
                    holon.DeletedByAvatarId = AvatarManager.LoggedInAvatar.Id.ToString();

                holon.DeletedDate = DateTime.Now;
                _dbContext.Holon.ReplaceOne(filter: g => g.Id == holon.Id, replacement: holon);
                return true;
            }
            catch
            {
                throw;
            }
        }
    }
}