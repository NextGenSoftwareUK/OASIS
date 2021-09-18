using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Models.Common;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Models.Requests
{
    public sealed class MintNftRequest : BaseExchangeRequest
    {
        public int MintAccountIndex { get; set; }
        public int MintDecimals { get; set; }
    }
}