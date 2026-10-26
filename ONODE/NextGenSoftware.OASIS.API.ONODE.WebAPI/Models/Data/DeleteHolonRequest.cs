
using System;
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data
{
    public class DeleteHolonRequest : BaseLoadHolonRequest, IHolonWriteIntentRequest
    {
        public Guid Id { get; set; }
        public bool SoftDelete { get; set; } = true;
        public Guid? OperationId { get; set; }
        public Dictionary<Guid, Guid?> ExpectedRevisions { get; set; }
    }
}
