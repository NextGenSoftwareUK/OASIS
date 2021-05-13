using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;

namespace NextGenSoftware.OASIS.API.Core.Objects
{
    public class CrownChakra : Chakra
    {
        public CrownChakra()
        {
            Type = new EnumValue<ChakraType>(ChakraType.Crown);
        }
    }
}