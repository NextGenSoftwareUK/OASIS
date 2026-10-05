using System;

namespace NextGenSoftware.OASIS.API.Providers.ZcashOASIS.Models
{
    public class ShieldedTransaction
    {
        public string TransactionId { get; set; }
        public string FromAddress { get; set; }
        public string ToAddress { get; set; }
        public decimal Amount { get; set; }
        public string Memo { get; set; }
        public bool Confirmed { get; set; }
        public int Confirmations { get; set; }
        public DateTime CreatedAt { get; set; }
        public string OperationId { get; set; } // For z_sendmany operations
    }
}

