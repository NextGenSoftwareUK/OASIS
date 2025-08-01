using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using System.Drawing;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Holons
{
    public class Building : Holon, IBuilding
    {
        public Building()
        {
            this.HolonType = HolonType.Building; 
        }

        //The building must specify a 3D Object or 2D sprite, if neither is specified then it will render a square block using the dimensions below...
        public byte[] ThreeDObject { get; set; }
        public byte[] TwoDSprite { get; set; }
        public float BottomLeft { get; set; }
        public float BottomRight { get; set; }
        public float TopLeft { get; set; }
        public float TopRight { get; set; }
        public float Height { get; set; }
        public Color Colour { get; set; }
    }
}
