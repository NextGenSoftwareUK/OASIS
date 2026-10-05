
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;

namespace NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses
{
    public class TransactionResponse : ITransactionResponse
    {
        public string TransactionResult { get; set; }
    }
}