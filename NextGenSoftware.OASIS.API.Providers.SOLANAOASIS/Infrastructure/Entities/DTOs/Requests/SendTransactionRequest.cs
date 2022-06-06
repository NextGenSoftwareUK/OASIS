using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Entities.DTOs.Common;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Entities.DTOs.Requests
{
    public sealed class SendTransactionRequest : BaseExchangeRequest
    {
        public ulong Lampposts { get; set; }
    }
}