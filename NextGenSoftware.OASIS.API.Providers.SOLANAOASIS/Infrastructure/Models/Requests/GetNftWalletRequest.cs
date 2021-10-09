using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Models.Common;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Models.Requests
{
    public sealed class GetNftWalletRequest
    {
        public BaseAccountRequest OwnerAccount { get; set; }
        public string MintSymbol { get; set; }
        public string MintToken { get; set; }
        public string MintName { get; set; }
        public int MintDecimal { get; set; }
    }
}