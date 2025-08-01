
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI
{
    public class Link : BaseEntityTitleDesc
    {
        [BsonRepresentation(BsonType.ObjectId)]
        public string PhaseId { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        public string TargetId { get; set; }

        public TargetType TargetType { get; set; }
    }

    //TODO: Lots more will be added here, it can link to ANY object within the B.E.B and OASIS Platforms... :)
    public enum TargetType
    {
        Contract,
        Sequence,
        Phase,
        Contact,
        Delivery,
        Material,
        InvestmentProfile,
        Charity,
        SYSTEM
    }
}