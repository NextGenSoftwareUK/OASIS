
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI
{
    public class Material : BaseEntity
    {
        [BsonRepresentation(BsonType.ObjectId)]
        public string PhaseId { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        public string FileId { get; set; }

        public File File { get; set; }

    }
}