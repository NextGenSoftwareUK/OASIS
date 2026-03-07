using System;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses
{
    public interface IWalletTransaction
    {
        Guid TransactionId { get; set; }
        string FromWalletAddress { get; set; }
        string ToWalletAddress { get; set; }
        double Amount { get; set; }
        string Description { get; set; }
        DateTime CreatedDate { get; set; }
        TransactionType TransactionType { get; set; }
        TransactionCategory TransactionCategory { get; set; }
    }
}