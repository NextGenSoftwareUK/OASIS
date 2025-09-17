
namespace NextGenSoftware.OASIS.API.Core.Interfaces.STAR
{
    public interface IMultiverse : ICelestialSpace
    {
        IGrandSuperStar GrandSuperStar { get; set; } //Lets you jump between universes within this multiverse.
        IMultiverseDimensions Dimensions { get; set; }
    }
}