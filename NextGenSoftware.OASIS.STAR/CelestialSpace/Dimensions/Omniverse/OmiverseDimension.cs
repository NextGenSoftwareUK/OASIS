using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.STAR.CelestialSpace
{
    public class OmniverseDimension : Dimension, IOmniverseDimension
    {
        public ISuperVerse SuperVerse { get; set; }

        //TODO: Eighth Dimension and above is at the Omniverse level so spans ALL Multiverses/Universes so not sure what we would have here? Needs more thought...
        //Currently dimensions 8-12 each have a SuperVerse which spans all Multiverses/Universes in dimensions 1-7.
        public OmniverseDimension()
        {
            Init();
        }

        public OmniverseDimension(IOmiverse omniverse = null)
        {
            Init(omniverse);
        }

        public OmniverseDimension(Guid id, IOmiverse omniverse = null) : base(id)
        {
            Init(omniverse);
        }

        public OmniverseDimension(Dictionary<ProviderType, string> providerKey, IOmiverse omniverse = null) : base(providerKey)
        {
            Init(omniverse);
        }

        private void Init(IOmiverse omniverse = null)
        {
            if (omniverse != null)
            {
                Mapper<IOmiverse, OmniverseDimension>.MapParentCelestialBodyProperties(omniverse, this);
                this.ParentOmniverse = omniverse;
                this.ParentOmniverseId = omniverse.Id;
            }

            SuperVerse = new SuperVerse(omniverse) { Name = "SuperVerse", Description = "SuperVerse that spans ALL Multiverses/Universes." };
            base.RegisterCelestialSpaces(new List<ICelestialSpace>() { SuperVerse });
        }
    }
}