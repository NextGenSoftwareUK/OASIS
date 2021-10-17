namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Models.Common
{
    public abstract class BaseExchangeRequest
    {
        public BaseAccountRequest FromAccount { get; set; }
        public BaseAccountRequest ToAccount { get; set; }
        public string MemoText { get; set; }
        public ulong Amount { get; set; }
    }
}