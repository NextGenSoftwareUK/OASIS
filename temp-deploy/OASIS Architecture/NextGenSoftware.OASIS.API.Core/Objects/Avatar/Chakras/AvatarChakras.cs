

using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Core.Objects
{
    public class AvatarChakras : IAvatarChakras
    {
        public RootChakra Root { get; set; } = new RootChakra();
        public SacralChakra Sacral { get; set; } = new SacralChakra();
        public SolarPlexusChakra SolarPlexus { get; set; } = new SolarPlexusChakra();
        public HeartChakra Heart { get; set; } = new HeartChakra();
        public ThroatChakra Throat { get; set; } = new ThroatChakra();
        public ThirdEyeChakra ThirdEye { get; set; } = new ThirdEyeChakra();
        public CrownChakra Crown { get; set; } = new CrownChakra();
    }
}