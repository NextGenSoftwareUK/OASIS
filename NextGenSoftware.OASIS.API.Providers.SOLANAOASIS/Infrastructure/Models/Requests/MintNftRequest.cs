namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Models.Requests
{
    public class MintNftRequest
    {
        public ulong Amount { get; set; }
        public string MemoText { get; set; }
        public int MintAccountIndex { get; set; }
        public int OwnerAccountIndex { get; set; }
        public int InitialAccountIndex { get; set; }
        public int MintDecimals { get; set; }
    }
}