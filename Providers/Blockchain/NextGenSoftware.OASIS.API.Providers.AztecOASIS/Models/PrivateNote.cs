using System;

namespace NextGenSoftware.OASIS.API.Providers.AztecOASIS.Models
{
    public class PrivateNote
    {
        public string NoteId { get; set; }
        public string OwnerPublicKey { get; set; }
        public decimal Value { get; set; }
        public string EncryptedData { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Status { get; set; } // created, committed, nullified
    }
}

