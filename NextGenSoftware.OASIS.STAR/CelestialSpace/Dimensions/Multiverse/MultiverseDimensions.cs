using System.Collections.Generic;
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
        public List<IDimension> CustomDimensions { get; set; } //TODO: This allows any custom dimensions to be added, but not sure if we should allow this? On one hand we want the engine/simulation to be as accurate as possible but on the other hand we want it to be as open and flexible as possible for expansion?
    }
}