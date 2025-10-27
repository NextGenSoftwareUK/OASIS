using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.STAR
{
    public interface IDimension : ICelestialSpace
    {
        public DimensionLevel DimensionLevel { get; set; }
    }
}