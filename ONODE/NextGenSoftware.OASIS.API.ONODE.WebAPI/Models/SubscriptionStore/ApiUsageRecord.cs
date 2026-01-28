using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.SubscriptionStore
{
    /// <summary>API request count per avatar per billing period (month). One document per avatar per period.</summary>
    public class ApiUsageRecord
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("avatarId")]
        public string AvatarId { get; set; }

        [BsonElement("periodStart")]
        [BsonRepresentation(BsonType.DateTime)]
        public DateTime PeriodStart { get; set; }

        [BsonElement("requestCount")]
        public int RequestCount { get; set; }

        [BsonElement("updatedAt")]
        [BsonRepresentation(BsonType.DateTime)]
        public DateTime UpdatedAt { get; set; }
    }
}
