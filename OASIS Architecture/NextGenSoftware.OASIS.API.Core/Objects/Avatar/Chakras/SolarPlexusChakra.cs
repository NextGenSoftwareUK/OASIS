using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Core.Objects
{
    public class SolarPlexusChakra : Chakra
    {
        public SolarPlexusChakra()
        {
            Type = new EnumValue<ChakraType>(ChakraType.SolarPlexus);
        }
    }
}