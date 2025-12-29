using System;

namespace NextGenSoftware.OASIS.API.Providers.AztecOASIS.Models
{
    public class AztecBalanceResponse
    {
        public decimal? Balance { get; set; }
        public string Address { get; set; }
        public string Currency { get; set; }
        public DateTime? LastUpdated { get; set; }
    }
}

