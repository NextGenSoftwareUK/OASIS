using System.Collections.Generic;
using System.Drawing;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Core.Objects
{
    public abstract class Chakra : IChakra
    {
        public string Name { get; set; }
        public string SanskritName { get; set; }
        public string Description { get; set; }
        public string WhatItControls { get; set; }
        public EnumValue<YogaPoseType> YogaPose { get; set; } 
        public string WhenItDevelops { get; set; }
        public EnumValue<ElementType> Element { get; set; } 
        public Crystal Crystal { get; set; }
        public Color Color { get; set; }
        public int Level { get; set; }
        public int Progress { get; set; }
        public int XP { get; set; }
        public EnumValue<ChakraType> Type { get; set; }
        public List<AvatarGift> GiftsUnlocked { get; set; } = new List<AvatarGift>();
    }
}