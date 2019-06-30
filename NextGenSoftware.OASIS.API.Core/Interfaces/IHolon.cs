using System;
using System.Collections.Generic;
using System.Text;

namespace NextGenSoftware.OASIS.API.Core
{
    public interface IHolon
    {
        Guid Id { get; set; }

        HolonType HolonType { get; set; }

    }
}
