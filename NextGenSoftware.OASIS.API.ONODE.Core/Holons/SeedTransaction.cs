using System;
using NextGenSoftware.OASIS.API.Core.CustomAttrbiutes;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.ONode.Core.Holons
{
    public class SeedTransaction : Holon, ISeedTransaction
    {
        [CustomOASISProperty]
        public Guid AvatarId { get; set; }

        [CustomOASISProperty]
        public string AvatarUserName { get; set; }

        [CustomOASISProperty]
        public int Amount { get; set; }

        [CustomOASISProperty]
        public string Memo { get; set; }
    }
}