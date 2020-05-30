using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.WebAPI
{
    public class SCMSRepository : ISCMSRepository
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

        public async Task<Phase> GetPhase(string id)
        {
            try
            {
                FilterDefinition<Phase> filter = Builders<Phase>.Filter.Eq("Id", id);
                return await db.Phase.Find(filter).FirstOrDefaultAsync();
            }
            catch
            {
                throw;
            }
        }

        public async Task<IEnumerable<Phase>> GetAllPhases()
        {
            try
            {
                //return await db.Sequence.Find(_ => true).ToListAsync();
                return await db.Phase.AsQueryable().ToListAsync();
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<Contract> GetContract(string id)
        {
            try
            {
                FilterDefinition<Contract> filter = Builders<Contract>.Filter.Eq("Id", id);
                return await db.Contract.Find(filter).FirstOrDefaultAsync();
            }
            catch
            {
                throw;
            }
        }

        public async Task<IEnumerable<Contract>> GetAllContracts()
        {
            try
            {
                //return await db.Sequence.Find(_ => true).ToListAsync();
                return await db.Contract.AsQueryable().ToListAsync();
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<Contact> GetContact(string id)
        {
            try
            {
                FilterDefinition<Contact> filter = Builders<Contact>.Filter.Eq("Id", id);
                return await db.Contact.Find(filter).FirstOrDefaultAsync();
            }
            catch
            {
                throw;
            }
        }

        public async Task<IEnumerable<Contact>> GetAllContacts()
        {
            try
            {
                //return await db.Sequence.Find(_ => true).ToListAsync();
                return await db.Contact.AsQueryable().ToListAsync();
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
