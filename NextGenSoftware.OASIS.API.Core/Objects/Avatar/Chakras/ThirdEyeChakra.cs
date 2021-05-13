using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;

namespace NextGenSoftware.OASIS.API.Core.Objects
{
    public class ThirdEyeChakra : Chakra
    {
        public ThirdEyeChakra()
        {
            Type = new EnumValue<ChakraType>(ChakraType.ThirdEye);
        }
    }
}