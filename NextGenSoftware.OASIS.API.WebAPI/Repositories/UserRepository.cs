using MongoDB.Driver;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.WebAPI
{
    public class UserRepository : IUserRepository
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
        public async Task<User> GetUser(string id)
        {
            try
            {
                FilterDefinition<User> filter = Builders<User>.Filter.Eq("Id", id);
                return await db.User.Find(filter).FirstOrDefaultAsync();
            }
            catch
            {
                throw;
            }
        }
        public async Task<IEnumerable<User>> GetUsers()
        {
            try
            {
                return await db.User.Find(_ => true).ToListAsync();
            }
            catch
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
    }
}
