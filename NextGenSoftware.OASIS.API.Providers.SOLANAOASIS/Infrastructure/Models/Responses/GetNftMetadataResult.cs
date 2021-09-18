using Solnet.Extensions.Models;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Models.Responses
{
    public sealed class GetNftMetadataResult
    {
        public TokenWalletFilterList Accounts { get; set; }
        public int Count { get; set; }
    }
}