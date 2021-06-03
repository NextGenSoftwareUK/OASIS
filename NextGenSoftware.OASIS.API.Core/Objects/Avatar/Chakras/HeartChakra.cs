using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;

namespace NextGenSoftware.OASIS.API.Core.Objects
{
    public class HeartChakra : Chakra
    {
        public HeartChakra()
        {
            Type = new EnumValue<ChakraType>(ChakraType.Heart);
        }
    }
}