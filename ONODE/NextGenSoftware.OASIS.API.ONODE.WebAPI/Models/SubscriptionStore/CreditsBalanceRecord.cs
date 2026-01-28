using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.SubscriptionStore
{
    /// <summary>Prepaid credits balance per avatar. One document per avatar.</summary>
    public class CreditsBalanceRecord
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("avatarId")]
        public string AvatarId { get; set; }

        [BsonElement("balanceUsd")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal BalanceUsd { get; set; }

        [BsonElement("updatedAt")]
        [BsonRepresentation(BsonType.DateTime)]
        public DateTime UpdatedAt { get; set; }
    }
}
