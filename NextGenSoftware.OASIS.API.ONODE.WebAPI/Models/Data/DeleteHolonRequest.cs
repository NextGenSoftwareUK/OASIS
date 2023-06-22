
using System;

namespace NextGenSoftware.OASIS.API.ONode.WebAPI.Models.Data
{
    public class DeleteHolonRequest : BaseLoadHolonRequest
    {
        public Guid Id { get; set; }
        public bool SoftDelete { get; set; } = true;
    }
}