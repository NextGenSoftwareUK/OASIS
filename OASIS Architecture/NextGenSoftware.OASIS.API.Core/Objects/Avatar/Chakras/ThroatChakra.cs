using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Core.Objects
{
    public class ThroatChakra : Chakra
    {
        public ThroatChakra()
        {
            Type = new EnumValue<ChakraType>(ChakraType.Throat);
        }
    }
}