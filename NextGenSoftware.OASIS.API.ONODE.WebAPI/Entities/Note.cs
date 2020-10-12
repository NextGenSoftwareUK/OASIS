
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI
{
    public class Note : BaseEntity
    {
        [BsonRepresentation(BsonType.ObjectId)]
        public string PhaseId { get; set; }

        public string NoteContent { get; set; }
    }
}