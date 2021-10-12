using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Repositories
{
    public class OlandRepository : IOlandRepository
    {
        private MongoDbContext _dbContext;

        public OlandRepository(MongoDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<Oland> AddOlandAsync(Oland oland)
        {
            try
            {
                await _dbContext.Oland.InsertOneAsync(oland);
                return oland;
            }
            catch
            {
                return null;
            }
        }

        public Oland AddOland(Oland oland)
        {
            try
            {
                _dbContext.Oland.InsertOne(oland);
                return oland;
            }
            catch
            {
                return null;
            }
        }

        public async Task<Oland> UpdateOlandAsync(Oland oland)
        {
            try
            {
                var filter = Builders<Oland>.Filter.Where(x => x.Id == oland.Id);
                await _dbContext.Oland.ReplaceOneAsync(filter, oland);
                return oland;
            }
            catch
            {
                return null;
            }
        }

        public Oland UpdateOland(Oland oland)
        {
            try
            {
                var filter = Builders<Oland>.Filter.Where(x => x.Id == oland.Id);
                _dbContext.Oland.ReplaceOne(filter, oland);
                return oland;
            }
            catch
            {
                return null;
            }
        }

        public async Task<bool> DeleteOlandAsync(int id, bool safeDelete)
        {
            try
            {
                if (safeDelete)
                {
                    // TODO: Safe Delete
                    return true;
                }
                
                var filter = Builders<Oland>.Filter.Where(x => x.Id == id);
                await _dbContext.Oland.DeleteOneAsync(filter);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool DeleteOland(int id, bool safeDelete)
        {
            try
            {
                if (safeDelete)
                {
                    // TODO: Safe Delete
                    return true;
                }

                var filter = Builders<Oland>.Filter.Where(x => x.Id == id); 
                _dbContext.Oland.DeleteOne(filter);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<Oland> GetOlandAsync(int id)
        {
            try
            {
                var filter = Builders<Oland>.Filter.Where(x => x.Id == id);
                var findResult = await _dbContext.Oland.FindAsync(filter);
                return await findResult.FirstOrDefaultAsync();
            }
            catch
            {
                return null;
            }
        }

        public Oland GetOland(int id)
        {
            try
            {
                var filter = Builders<Oland>.Filter.Where(x => x.Id == id);
                var findResult = _dbContext.Oland.Find(filter);
                return findResult.FirstOrDefault();
            }
            catch
            {
                return null;
            }
        }

        public async Task<IEnumerable<Oland>> GetAllOlandsAsync()
        {
            try
            {
                var cursor = await _dbContext.Oland.FindAsync(_ => true);
                return cursor.ToEnumerable();
            }
            catch
            {
                return null;
            }
        }

        public IEnumerable<Oland> GetAllOlands()
        {
            try
            {
                return _dbContext.Oland.Find(_ => true).ToEnumerable();
            }
            catch
            {
                return null;
            }
        }
    }
}