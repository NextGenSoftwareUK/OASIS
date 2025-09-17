using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Core.Objects
{
    public class RootChakra : Chakra
    {
        public RootChakra()
        {
            Type = new EnumValue<ChakraType>(ChakraType.Root);
            Name = "Root Chakra";
            SanskritName = "Muladhara";
            Description = "The Muladhara, or root chakra, represents our foundation. On the human body, it sits at the base of the spine and gives us the feeling of being grounded. When the root chakra is open, we feel confident in our ability to withstand challenges and stand on our own two feet. When it's blocked, we feel threatened, as if we're standing on unstable ground.";
            Crystal = new Hematite();
            YogaPose = new EnumValue<YogaPoseType>(YogaPoseType.Warrior1);
            Element = new EnumValue<ElementType>(ElementType.Earth);
        }
    }
}