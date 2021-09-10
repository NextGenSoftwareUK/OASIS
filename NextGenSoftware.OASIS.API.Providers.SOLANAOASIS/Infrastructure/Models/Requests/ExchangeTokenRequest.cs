namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Models.Requests
{
    public class ExchangeTokenRequest
    {
        public int MintAccountIndex { get; set; }
        public int FromAccountIndex { get; set; }
        public int ToAccountIndex { get; set; }
        public string MemoText { get; set; }
        public ulong Amount { get; set; }
    }
}