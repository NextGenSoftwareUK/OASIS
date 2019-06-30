using System;
using System.Collections.Generic;
using System.Text;

namespace NextGenSoftware.OASIS.API.Core
{
    public class Holon : IHolon
    {
        public Guid Id { get; set; }

        public HolonType HolonType { get; set; }

    }
}
