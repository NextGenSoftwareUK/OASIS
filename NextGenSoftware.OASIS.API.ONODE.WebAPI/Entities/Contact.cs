using MongoDB.Bson;  
using MongoDB.Bson.Serialization.Attributes;
using NextGenSoftware.OASIS.API.Core;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI
{
    public class Contact : Avatar
    {
        [BsonRepresentation(BsonType.ObjectId)]
        public string PhaseId { get; set; }

        public Phase Phase { get; set; }

        //[BsonRepresentation(BsonType.ObjectId)]
        public string AvatarId { get; set; }

        public BEBUserType BEBUserType { get; set; }

        public string BEBUserTypeDisplay
        {
            get
            {
                switch (BEBUserType)
                {
                    case BEBUserType.SkilledLabour:
                        return "Skilled Labour";

                    case BEBUserType.Management:
                        return "Management";

                    default:
                        return "Skilled Labour";
                }
            }
        }
    }

    //TODO: More types will be added later.
    public enum BEBUserType
    {
        Management,  //0
        SkilledLabour //1 
    }
}