using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.CustomAttrbiutes;
using NextGenSoftware.OASIS.API.ONode.Core.Interfaces.Holons;
using System;
using NextGenSoftware.OASIS.API.ONode.Core.Interfaces.Objects;

namespace NextGenSoftware.OASIS.API.ONode.Core.Holons
{
    public class InstalledOAPP : Holon, IInstalledOAPP
    {
        public InstalledOAPP()
        {
            this.HolonType = HolonType.InstalledOAPP;
        }

        [CustomOASISProperty]
        public IOAPPDNA OAPPDNA { get; set; }

        //[CustomOASISProperty]
        //public IOAPP OAPP { get; set; }

        //[CustomOASISProperty]
        //public Guid OAPPId { get; set; }

        //[CustomOASISProperty]
        //public Guid OAPPName { get; set; }

        //[CustomOASISProperty]
        //public DateTime InstalledOn { get; set; } //This could use the CreatedOn field instead?

        //[CustomOASISProperty]
        //public Guid InstalledBy { get; set; } //This could use the CreatedByAvatar field instead?
    }
}
