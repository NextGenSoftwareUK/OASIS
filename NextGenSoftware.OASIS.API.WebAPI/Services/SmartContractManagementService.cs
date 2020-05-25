using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Driver;

namespace NextGenSoftware.OASIS.API.WebAPI
{
    public class SmartContractManagementService : ISmartContractManagementService
    {
        SmartContractManagementRepository _smartContractRepository = new SmartContractManagementRepository();
        private IEnumerable<Sequence> _sequences = null;

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


        public async Task<IEnumerable<Sequence>> GetAllSequences()
        {
            _sequences = await _smartContractRepository.GetAllSequences();
            return await Task.Run(() => _sequences.ToList());
        }
    }
}