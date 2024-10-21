using System;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.ONode.Core.Interfaces
{
    public interface ITaskBase : IHolon
    {
        Guid StartedBy { get; set; }
        DateTime StartedOn { get; set; }
        Guid CompletedBy { get; set; }
        DateTime CompletedOn { get; set; }
    }
}