using System;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.ONode.Core.Interfaces.Holons
{
    public interface IInstalledOAPP : IHolon
    {
        IOAPPDNA OAPPDNA { get; set; }
        public string InstalledPath { get; set; }
        public DateTime InstalledOn { get; set; }
        public Guid InstalledBy { get; set; }

        //IOAPP OAPP { get; set; }
        //Guid OAPPId { get; set; }
    }
}