using System;
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.Core.Interfaces
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
