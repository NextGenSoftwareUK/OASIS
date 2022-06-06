using System;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Entities.Models
{
    public class SolanaHolonDto
    {
        public Guid Id { get; set; }
        public Guid ParentOmniverseId { get; set; }
        public Guid ParentMultiverseId { get; set; }
        public Guid ParentUniverseId { get; set; }
        public Guid ParentZomeId { get; set; }
        public Guid ParentStarId { get; set; }
    }
}