using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.Core.Interfaces
{
    public interface IChakra
    {
        int Level { get; set; }
        ChakraType Type { get; set; }
        int XP { get; set; }
    }
}