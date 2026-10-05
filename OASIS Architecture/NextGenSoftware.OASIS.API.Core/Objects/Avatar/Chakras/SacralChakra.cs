using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Core.Objects
{
    public class SacralChakra : Chakra
    {
        public SacralChakra()
        {
            Type = new EnumValue<ChakraType>(ChakraType.Sacral);
        }
    }
}