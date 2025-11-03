using System;
using NextGenSoftware.OASIS.API.Core.CustomAttrbiutes;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Holons
{
    public class SampleHolon : Holon, ISampleHolon
    {
        [CustomOASISProperty]
        public string CustomProperty { get; set; }

        [CustomOASISProperty]
        public string CustomProperty2 { get; set; }

        [CustomOASISProperty]
        public DateTime CustomDate { get; set; }

        [CustomOASISProperty]
        public int CustomNumber { get; set; }

        [CustomOASISProperty]
        public long CustomLongNumber { get; set; }

        [CustomOASISProperty]
        public Guid AvatarId { get; set; }
    }
}