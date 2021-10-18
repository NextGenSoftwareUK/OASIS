using System;
using NextGenSoftware.OASIS.API.Core.CustomAttrbiutes;
using NextGenSoftware.OASIS.API.Core.Holons;

namespace NextGenSoftware.OASIS.API.ONODE.BLL.Holons
{
    public class SeedTransaction : Holon
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