using System;
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.Providers.ZcashOASIS.Models
{
    public class PartialNote
    {
        public decimal TotalAmount { get; set; }
        public int NumberOfParts { get; set; }
        public List<PartialNotePart> Parts { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class PartialNotePart
    {
        public decimal Amount { get; set; }
        public int Index { get; set; }
        public string NoteId { get; set; }
        public string TransactionId { get; set; }
    }
}

