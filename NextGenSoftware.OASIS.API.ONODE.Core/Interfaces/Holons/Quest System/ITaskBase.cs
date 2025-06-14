using System;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons
{
    public interface ITaskBase : ISTARHolon
    {
        Guid StartedBy { get; set; }
        DateTime StartedOn { get; set; }
        Guid CompletedBy { get; set; }
        DateTime CompletedOn { get; set; }
    }
}