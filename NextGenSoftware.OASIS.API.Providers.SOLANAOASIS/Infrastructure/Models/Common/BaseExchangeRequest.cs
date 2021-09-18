namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Models.Common
{
    public abstract class BaseExchangeRequest
    {
        public int FromAccountIndex { get; set; }
        public int ToAccountIndex { get; set; }
        public string MemoText { get; set; }
        public ulong Amount { get; set; }
    }
}