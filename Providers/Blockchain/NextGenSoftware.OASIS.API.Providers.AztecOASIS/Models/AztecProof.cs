using System;
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.Providers.AztecOASIS.Models
{
    public class AztecProof
    {
        public string ProofType { get; set; }
        public string ProofData { get; set; }
        public IList<string> PublicInputs { get; set; } = new List<string>();
        public DateTime CreatedAt { get; set; }
    }
}

