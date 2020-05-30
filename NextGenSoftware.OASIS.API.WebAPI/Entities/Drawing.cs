using MongoDB.Bson;  
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace NextGenSoftware.OASIS.API.WebAPI
{
    public class Drawing
    {
        [BsonId]  
        [BsonRepresentation(BsonType.ObjectId)]  
        public string Id { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        public string PhaseID { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        public string FileId { get; set; }
    }
}