
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.STAR
{
    public interface IMultiverseDimensions
    {
        IFirstDimension FirstDimension { get; set; }
        ISecondDimension SecondDimension { get; set; }
        IThirdDimension ThirdDimension { get; set; }
        IFourthDimension FourthDimension { get; set; }
        IFifthDimension FifthDimension { get; set; }
        ISixthDimension SixthDimension { get; set; }
        ISeventhDimension SeventhDimension { get; set; }
        List<IDimension> CustomDimensions { get; set; } //TODO: This allows any custom dimensions to be added, but not sure if we should allow this? On one hand we want the engine/simulation to be as accurate as possible but on the other hand we want it to be as open and flexible as possible for expansion?
    }
}