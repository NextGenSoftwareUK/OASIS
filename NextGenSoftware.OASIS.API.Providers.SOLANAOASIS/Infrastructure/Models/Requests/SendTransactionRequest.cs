using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Models.Common;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Models.Requests
{
    public sealed class SendTransactionRequest : BaseExchangeRequest
    {
        public ulong Lampposts { get; set; }
    }
}