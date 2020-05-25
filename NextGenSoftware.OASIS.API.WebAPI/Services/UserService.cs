using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Driver;

namespace NextGenSoftware.OASIS.API.WebAPI
{
    public class UserService : IUserService
    {
        UserRepository _userRepository = new UserRepository();
        private IEnumerable<User> _users = null;

       // private IMongoDatabase _mongoDb;
       // private List<User> _users = null;
        // users hardcoded for simplicity, store in a db with hashed passwords in production applications
        // private List<User> _users = new List<User>
        // {
        //     new User { Id = "1", FirstName = "Test2", LastName = "User", Username = "test2", Password = "test2" }
        // };


        private void Init()
        {
            //string databaseName = "school";
            //string connectionString = "mongodb://cluster0-shard-00-00-9jpsk.mongodb.net:27017/";
            // MongoClient client = new MongoClient(connectionString + databaseName);


          //  MongoClient mongoClient = new MongoClient("mongodb+srv://dbadmin:PlRuNP9u4rG2nRdN@oasisapi-oipck.mongodb.net/test?retryWrites=true&w=majority");
           // _mongoDb = mongoClient.GetDatabase("OASISAPI");  
        }

        public async Task<User> Authenticate(string username, string password)
        {
          //  if (_users == null)
             _users = await _userRepository.GetUsers();

            var user = await Task.Run(() => _users.SingleOrDefault(x => x.Username == username && x.Password == password));
            
            // return null if user not found
            if (user == null)
                return null;

            // authentication successful so return user details without password
            return user.WithoutPassword();
        }

        public async Task<IEnumerable<User>> GetAll()
        {
             _users = await _userRepository.GetUsers();
            return await Task.Run(() => _users.WithoutPasswords());
        }

        Task<User> IUserService.Authenticate(string username, string password)
        {
            throw new System.NotImplementedException();
        }

        Task<IEnumerable<User>> IUserService.GetAll()
        {
            throw new System.NotImplementedException();
        }
    }
}