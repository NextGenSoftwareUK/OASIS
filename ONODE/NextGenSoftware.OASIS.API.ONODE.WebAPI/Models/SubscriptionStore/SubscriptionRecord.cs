using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.SubscriptionStore
{
    /// <summary>Stored subscription (Stripe plan) per avatar. One active record per avatar.</summary>
    public class SubscriptionRecord
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("avatarId")]
        public string AvatarId { get; set; }

        [BsonElement("planId")]
        public string PlanId { get; set; }

        [BsonElement("status")]
        public string Status { get; set; }

        [BsonElement("stripeCustomerId")]
        public string StripeCustomerId { get; set; }

        [BsonElement("stripeSubscriptionId")]
        public string StripeSubscriptionId { get; set; }

        [BsonElement("currentPeriodStart")]
        [BsonRepresentation(BsonType.DateTime)]
        public DateTime CurrentPeriodStart { get; set; }

        [BsonElement("currentPeriodEnd")]
        [BsonRepresentation(BsonType.DateTime)]
        public DateTime CurrentPeriodEnd { get; set; }

        [BsonElement("payAsYouGoEnabled")]
        public bool PayAsYouGoEnabled { get; set; }

        [BsonElement("updatedAt")]
        [BsonRepresentation(BsonType.DateTime)]
        public DateTime UpdatedAt { get; set; }
    }
}
