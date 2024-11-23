using NextGenSoftware.OASIS.API.Core.Interfaces;
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.ONode.Core.Interfaces.Holons
{
    public interface IOAPP : IHolon
    {
        //OAPPType OAPPType { get; set; }
        //GenesisType GenesisType { get; set; }
        //Guid CelestialBodyId { get; set; }
        //ICelestialBody CelestialBody { get; set; }
        //DateTime PublishedOn { get; set; }
        //Guid PublishedByAvatarId { get; set; }
        //string CreatedByAvatarUsername { get; set; }
        //string PublishedByAvatarUsername { get; set; }

        //ICelestialBody CelestialBody { get; set; } //TODO: Dont think we need this?
        //string OAPPDNAJSON { get; set; }
        IOAPPDNA OAPPDNA { get; set; }
        byte[] PublishedOAPP { get; set; }
        IList<IMission> Missions { get; set; }
    }
}