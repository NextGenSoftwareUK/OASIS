using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.STAR
{
    public interface IMultiverseDimension : IDimension
    {
        public IUniverse Universe { get; set; }
    }
}