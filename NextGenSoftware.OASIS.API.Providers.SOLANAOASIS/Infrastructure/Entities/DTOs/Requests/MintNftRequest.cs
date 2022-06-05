using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Entities.DTOs.Common;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Entities.DTOs.Requests
{
    public sealed class MintNftRequest : BaseExchangeRequest
    {
        public BaseAccountRequest MintAccount { get; set; }
        public int MintDecimals { get; set; }
    }
}