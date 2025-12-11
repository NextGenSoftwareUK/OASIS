using System;

namespace NextGenSoftware.OASIS.API.Providers.ZcashOASIS.Models
{
    public class ViewingKey
    {
        public string Address { get; set; }
        public string Key { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Purpose { get; set; } // "Auditability", "Compliance", etc.
    }
}

