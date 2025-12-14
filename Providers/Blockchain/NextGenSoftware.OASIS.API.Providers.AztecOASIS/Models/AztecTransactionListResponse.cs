using System;
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.Providers.AztecOASIS.Models
{
    public class AztecTransactionListResponse
    {
        public List<AztecTransactionItem> Transactions { get; set; }
        public int TotalCount { get; set; }
    }

    public class AztecTransactionItem
    {
        public string TransactionHash { get; set; }
        public string FromAddress { get; set; }
        public string ToAddress { get; set; }
        public decimal? Amount { get; set; }
        public DateTime? Timestamp { get; set; }
        public string Status { get; set; }
    }
}

