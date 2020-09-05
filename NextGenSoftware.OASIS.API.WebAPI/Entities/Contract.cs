using MongoDB.Bson;  
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace NextGenSoftware.OASIS.API.ORIAServices
{
    public class Contract : BaseEntityTitleDesc
    {
        [BsonRepresentation(BsonType.DateTime)]
        public DateTime StartDate { get; set; }

        [BsonRepresentation(BsonType.DateTime)]
        public DateTime EndDate { get; set; }

        public int Status { get; set; }
    }
}