using System;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.Wallets.Response
{
    public class WalletTransaction : IWalletTransaction
    {
        public Guid TransactionId { get; set; }
        public string FromWalletAddress { get; set; }
        public string ToWalletAddress { get; set; }
        public double Amount { get; set; }
        public string Description { get; set; }
        public TransactionType TransactionType { get; set; }
        public TransactionCategory TransactionCategory { get; set; }
    }
}