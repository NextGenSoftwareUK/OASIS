using System;
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data
{
    /// <summary>Conditional V2 write intent. Identity and roles come from authentication.</summary>
    public abstract class HolonWriteIntentRequest : BaseHolonRequest
    {
        /// <summary>Stable nonempty operation identity; retain it for outcome reconciliation.</summary>
        public Guid? OperationId { get; set; }

        /// <summary>Provider revision per selected ID. Null means create-only;
        /// Guid.Empty is an actual legacy revision, not absence.</summary>
        public Dictionary<Guid, Guid?> ExpectedRevisions { get; set; }

        public bool SaveChildren { get; set; } = true;
    }
}
