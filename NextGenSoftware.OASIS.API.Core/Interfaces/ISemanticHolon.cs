using System;
using System.Collections.Generic;
using System.Text;

namespace NextGenSoftware.OASIS.API.Core
{
    public interface ISemanticHolon : IHolon
    {
        Guid ParentId { get; set; }
        string Name { get; set; }
        string Description { get; set; }
        List<IHolon> Children { get; }
        IHolon Parent { get; }
    }
}
