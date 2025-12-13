using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;

namespace NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses
{
    public class SendWeb4NFTResponse : ISendWeb4NFTResponse
    {
        public string SendTransactionResult { get; set; }
        public string LockTransactionResult { get; set; }
        public string BurnTransactionResult { get; set; }
        public string UnlockTransactionResult { get; set; }
        public string BridgeOrderId { get; set; }
    }
}

