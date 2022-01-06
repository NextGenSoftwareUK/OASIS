using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.STAR.CelestialSpace
{
    public class ThirdDimension : MultiverseDimension, IThirdDimension
    {
        // Primary Universe that we are in now.
        public IUniverse UniversePrime { get; set; }

        // The MagicVerse we will all co-create together. This is the Universe that OAPPs, SOAPPs, etc will appear when being created through the STAR ODK. The other univrses (UniversePrime and ParallelUniverses) are part of the original OASIS Simulation and so are read-only so they accuratley reflect and simulate real life/reality.
        public IUniverse MagicVerse { get; set; }

        //Parallel Universes (everything that can happen does happen (Quantum Mechanics)).
        public List<IUniverse> ParallelUniverses { get; set; } = new List<IUniverse>();

        public ThirdDimension()
        {
            Init();
        }

        public ThirdDimension(IMultiverse multiverse = null)
        {
            Init(multiverse);
        }

        public ThirdDimension(Guid id, IMultiverse multiverse = null) : base(id, multiverse)
        {
            Init(multiverse);
        }

        public ThirdDimension(Dictionary<ProviderType, string> providerKey, IMultiverse multiverse = null) : base(providerKey, multiverse)
        {
            Init(multiverse);
        }

        private void Init(IMultiverse multiverse = null)
        {
            this.Name = "The Third Dimension";
            this.Description = "The Physical Plane - what people see and experience during day to day living.";
            this.DimensionLevel = DimensionLevel.Third;
            UniversePrime = new Universe(multiverse);
            MagicVerse = new Universe(multiverse);
        }
    }
}