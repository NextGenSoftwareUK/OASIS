using System;
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.Providers.MidenOASIS.Models
{
    public class STARKProof
    {
        public string ProofType { get; set; } // "STARK"
        public string ProofData { get; set; } // Serialized proof
        public IList<string> PublicInputs { get; set; } = new List<string>();
        public DateTime CreatedAt { get; set; }
        public string ProgramHash { get; set; } // Miden program hash
        public string StackOutputs { get; set; } // Stack outputs from execution
    }
}

