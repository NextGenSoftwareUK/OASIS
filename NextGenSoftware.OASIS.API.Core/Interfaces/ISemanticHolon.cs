using System;
using System.Collections.Generic;
using System.Text;

namespace NextGenSoftware.OASIS.API.Core
{
    // TODO:  Not sure need this anymore?
    public interface ISemanticHolon : IHolon
    {
        Guid ParentId { get; set; }
        string Name { get; set; }
        string Description { get; set; }
        List<IHolon> Children { get; }
        IHolon Parent { get; }
    }
}
