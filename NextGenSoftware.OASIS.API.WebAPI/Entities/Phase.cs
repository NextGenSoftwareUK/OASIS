using MongoDB.Bson;  
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace NextGenSoftware.OASIS.API.WebAPI
{
    public class Phase
    {
        [BsonId]  
        [BsonRepresentation(BsonType.ObjectId)]  
        public string Id { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        public string SequenceId { get; set; }

        [BsonRepresentation(BsonType.DateTime)]
        public DateTime StartDate { get; set; }
        
        [BsonRepresentation(BsonType.DateTime)]
        public DateTime EndDate { get; set; }


        public int PhaseNo { get; set; }
        public string PhaseTitle { get; set; }
        public string ScopeDescription { get; set; }

        
    }
}