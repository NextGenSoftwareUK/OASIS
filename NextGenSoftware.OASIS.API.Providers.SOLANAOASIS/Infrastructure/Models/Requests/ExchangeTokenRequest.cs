using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Models.Common;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Models.Requests
{
    public sealed class ExchangeTokenRequest : BaseExchangeRequest
    {
        public BaseAccountRequest MintAccount { get; set; }
    }
}