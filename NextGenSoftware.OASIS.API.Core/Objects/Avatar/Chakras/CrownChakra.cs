using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.Utilities;

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