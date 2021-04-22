
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Core.Objects
{
    public class Chakra : IChakra
    {
        public int Level { get; set; }
        public int Value { get; set; }
        public ChakraType Type { get; set; }
    }
}