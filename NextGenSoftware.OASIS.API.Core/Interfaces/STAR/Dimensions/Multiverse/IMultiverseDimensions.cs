
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
    }
}