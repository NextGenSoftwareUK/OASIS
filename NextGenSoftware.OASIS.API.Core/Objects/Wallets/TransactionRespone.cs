
namespace NextGenSoftware.OASIS.API.Core.Objects.Wallets
{
    public class TransactionRespone
    {
        public string TransactionResult { get; set; }
        //public bool IsSuccess { get; set; } //Redundant because will always be wrapped in a OASISResult which contains IsSuccess and IsError etc...
    }
}