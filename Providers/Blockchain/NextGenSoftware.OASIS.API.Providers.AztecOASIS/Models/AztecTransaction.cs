using System;

namespace NextGenSoftware.OASIS.API.Providers.AztecOASIS.Models
{
    public class AztecTransaction
    {
        public string TransactionId { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Description { get; set; }
        public string ProofType { get; set; }
    }
}

