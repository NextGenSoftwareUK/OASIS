using System;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Entities{

    public abstract class CelestialBodyAbstract{
        
        public String HolonId{ set; get; }

        public SpaceQuadrantType SpaceQuadrant { get; set; }
        public int SpaceSector { get; set; }
        public float SuperGalacticLatitute { get; set; }
        public float SuperGalacticLongitute { get; set; }
        public float GalacticLatitute { get; set; }
        public float GalacticLongitute { get; set; }
        public float HorizontalLatitute { get; set; }
        public float HorizontalLongitute { get; set; }
        public float EquatorialLatitute { get; set; }
        public float EquatorialLongitute { get; set; }
        public float EclipticLatitute { get; set; }
        public float EclipticLongitute { get; set; }
        public long Size { get; set; }
        public int Radius { get; set; }
        public long Age { get; set; }
        public long Mass { get; set; }
        public int Temperature { get; set; }
        public long Weight { get; set; }
        public long GravitaionalPull { get; set; }
        public int OrbitPositionFromParentStar { get; set; }
        public int CurrentOrbitAngleOfParentStar { get; set; } //Angle between 0 and 360 degrees of how far around the orbit it it of its parent star.
        public long DistanceFromParentStarInMetres { get; set; }
        public long RotationSpeed { get; set; }
        public int TiltAngle { get; set; }
        public int NumberRegisteredAvatars { get; set; }
        public int NumberActiveAvatars { get; set; }

        //ICelestialBodyCore CelestialBodyCore { get; set; }
        public GenesisType GenesisType { get; set; }
        public bool IsInitialized { get; }

        protected CelestialBodyAbstract(){}
    
    }
}