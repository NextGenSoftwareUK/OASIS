
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI
{
    public class Log
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        public string RelatedToId { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        public string UserId { get; set; }

        [BsonRepresentation(BsonType.DateTime)]
        public DateTime Date { get; set; }

        public int RelatedToObjectVersion { get; set; }

        public TargetType Type { get; set; }
    }

//    //TODO: Lots more will be added here, it can link to ANY object within the B.E.B and OASIS Platforms... :)
//    public enum LogType
//    {
//        Contract,
//        Sequence,
//        Phase,
//        Contact,
//        Delivery,
//        Material,
//        InvestmentProfile,
//        Charity
//    }


}