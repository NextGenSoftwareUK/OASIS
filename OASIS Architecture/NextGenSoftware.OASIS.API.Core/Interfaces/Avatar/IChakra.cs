using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Core.Interfaces
{
    public interface IChakra
    {
        string Name { get; set; }
        EnumValue<ChakraType> Type { get; set; }
        string SanskritName { get; set; }
        string Description { get; set; }
       // Color Color { get; set; }
        EnumValue<ElementType> Element { get; set; }
        Crystal Crystal { get; set; }
        EnumValue<YogaPoseType> YogaPose { get; set; }
        string WhatItControls { get; set; }
        string WhenItDevelops { get; set; }
        int Level { get; set; }
        int Progress { get; set; }
        int XP { get; set; }
        List<AvatarGift> GiftsUnlocked { get; set; }
    }
}