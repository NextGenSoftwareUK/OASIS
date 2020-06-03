using MongoDB.Bson;  
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace NextGenSoftware.OASIS.API.WebAPI
{
    public class Phase : BaseEntity
    {
        [BsonRepresentation(BsonType.ObjectId)]
        public string SequenceId { get; set; }

        public Sequence Sequence { get; set; }

        [BsonRepresentation(BsonType.DateTime)]
        public DateTime StartDate { get; set; }
        
        [BsonRepresentation(BsonType.DateTime)]
        public DateTime EndDate { get; set; }

        [BsonRepresentation(BsonType.DateTime)]
        public DateTime ActualStartDate { get; set; }

        [BsonRepresentation(BsonType.DateTime)]
        public DateTime ActualEndDate { get; set; }

        [BsonRepresentation(BsonType.DateTime)]
        public DateTime PhaseHandedOverDate { get; set; }


        public int PhaseNo { get; set; }
        public string PhaseTitle { get; set; }
        public string ScopeDescription { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        public string BIMModelFileId { get; set; }


    }
}