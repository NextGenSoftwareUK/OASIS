using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.WebAPI
{
    public class SmartContractManagementRepository : ISmartContractManagementRepository
    {
        MongoDbContext db = new MongoDbContext();
        public async Task Add(User User)
        {
            try
            {
                await db.User.InsertOneAsync(User);
            }
            catch
            {
                throw;
            }
        }
        public async Task<Sequence> GetSequence(string id)
        {
            try
            {
                FilterDefinition<Sequence> filter = Builders<Sequence>.Filter.Eq("Id", id);
                return await db.Sequence.Find(filter).FirstOrDefaultAsync();
            }
            catch
            {
                throw;
            }
        }
        public async Task<IEnumerable<Sequence>> GetAllSequences()
        {
            try
            {
                //return await db.Sequence.Find(_ => true).ToListAsync();
                return await db.Sequence.AsQueryable().ToListAsync();
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        public async Task Update(User User)
        {
            try
            {
                await db.User.ReplaceOneAsync(filter: g => g.Id == User.Id, replacement: User);
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
                FilterDefinition<User> data = Builders<User>.Filter.Eq("Id", id);
                await db.User.DeleteOneAsync(data);
            }
            catch
            {
                throw;
            }
        }

        public Task AddSequence(Sequence sequence)
        {
            throw new System.NotImplementedException();
        }

        public Task Update(Sequence sequence)
        {
            throw new System.NotImplementedException();
        }
    }
}
