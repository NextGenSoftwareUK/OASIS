using NextGenSoftware.OASIS.API.Core.Objects;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Models
{
    public sealed class OlandUnitDto : Oland
    {
        public string UnitSize => $"{TopSize}x{RightSize} {UnitOfMeasure}";
    }
}