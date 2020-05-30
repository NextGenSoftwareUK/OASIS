using MongoDB.Bson;  
using MongoDB.Bson.Serialization.Attributes;

namespace NextGenSoftware.OASIS.API.WebAPI
{
    public class Contact : BaseEntity
    {
        [BsonRepresentation(BsonType.ObjectId)]
        public string PhaseId { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        public string UserId { get; set; }

        public int BEBUserType { get; set; }
    }
}