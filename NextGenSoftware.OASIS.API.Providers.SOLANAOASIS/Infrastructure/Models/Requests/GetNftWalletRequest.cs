namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Models.Requests
{
    public sealed class GetNftWalletRequest
    {
        public int OwnerAccount { get; set; }
        public string MintSymbol { get; set; }
        public string MintToken { get; set; }
        public string MintName { get; set; }
        public int MintDecimal { get; set; }
    }
}