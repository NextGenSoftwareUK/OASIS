
using System.Drawing;

namespace NextGenSoftware.OASIS.API.Core.Interfaces
{
    //The building must specify a 3D Object or 2D sprite, if neither is specified then it will render a square block using the dimensions below...
    public interface IBuilding : IHolon
    {
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