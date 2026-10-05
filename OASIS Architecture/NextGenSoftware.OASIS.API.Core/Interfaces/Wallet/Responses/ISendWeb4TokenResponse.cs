
namespace NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses
{
    public interface ISendWeb4TokenResponse
    {
        public string SendTransactionResult { get; set; }
        public string LockTransactionResult { get; set; }
        public string BurnTransactionResult { get; set; }
        public string UnlockTransactionResult { get; set; }
    }
}