
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.STAR.CelestialSpace
{
    public class MultiverseDimensions : IMultiverseDimensions
    {
        public IFirstDimension FirstDimension { get; set; }
        public ISecondDimension SecondDimension { get; set; }
        public IThirdDimension ThirdDimension { get; set; }
        public IFourthDimension FourthDimension { get; set; }
        public IFifthDimension FifthDimension { get; set; }
        public ISixthDimension SixthDimension { get; set; }
        public ISeventhDimension SeventhDimension { get; set; }
    }
}