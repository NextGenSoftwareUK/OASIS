using System;
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.Core
{
    public class Holon : IHolon
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string ProviderKey { get; set; }
        public HolonType HolonType { get; set; }
        public IPlanet Planet { get; set; }
        public IHolon Parent { get; set; }
        public List<IHolon> Children { get; set; }
    }
}
