
using System;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data
{
    public class DeleteHolonRequest : BaseLoadHolonRequest
    {
        public Guid Id { get; set; }
        public bool SoftDelete { get; set; } = true;
    }
}