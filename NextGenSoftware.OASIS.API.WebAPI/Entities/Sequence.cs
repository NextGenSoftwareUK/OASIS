using MongoDB.Bson;  
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace NextGenSoftware.OASIS.API.WebAPI
{
    public class Sequence
    {
        [BsonId]  
        [BsonRepresentation(BsonType.ObjectId)]  
        public string Id { get; set; }
        //public int Id { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        public string ContractId { get; set; }

        [BsonRepresentation(BsonType.DateTime)]
        public DateTime StartDate { get; set; }
        
        [BsonRepresentation(BsonType.DateTime)]
        public DateTime EndDate { get; set; }


        public string SequenceNo { get; set; }
        public string SequenceTitle { get; set; }
        public string ProgrammedDates { get; set; }
        public string Description { get; set; }
    }
}