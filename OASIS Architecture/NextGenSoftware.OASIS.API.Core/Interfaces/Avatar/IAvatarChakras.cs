
using NextGenSoftware.OASIS.API.Core.Objects;

namespace NextGenSoftware.OASIS.API.Core.Interfaces
{
    public interface IAvatarChakras
    {
        CrownChakra Crown { get; set; }
        HeartChakra Heart { get; set; }
        RootChakra Root { get; set; }
        SacralChakra Sacral { get; set; }
        SolarPlexusChakra SolarPlexus { get; set; }
        ThirdEyeChakra ThirdEye { get; set; }
        ThroatChakra Throat { get; set; }
    }
}