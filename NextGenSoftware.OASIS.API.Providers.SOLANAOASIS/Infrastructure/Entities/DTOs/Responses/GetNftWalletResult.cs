using Solnet.Extensions;
using Solnet.Extensions.Models;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Entities.DTOs.Responses
{
    public sealed class GetNftWalletResult
    {
        public TokenWalletFilterList Accounts { get; set; }
        public TokenWalletBalance[] Balances { get; set; }
    }
}