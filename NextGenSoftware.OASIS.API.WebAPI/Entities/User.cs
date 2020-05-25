using MongoDB.Bson;  
using MongoDB.Bson.Serialization.Attributes; 

namespace NextGenSoftware.OASIS.API.WebAPI
{
    public class User
    {
        [BsonId]  
        [BsonRepresentation(BsonType.ObjectId)]  
        public string Id { get; set; }  
        //public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
    }
}