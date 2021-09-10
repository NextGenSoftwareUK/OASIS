namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Models.Requests
{
    public sealed class SendTransactionRequest
    {
        public int FromAccountIndex { get; set; }
        public int ToAccountIndex { get; set; }
        public ulong Lampposts { get; set; }
        public string MemoText { get; set; }
    }
}