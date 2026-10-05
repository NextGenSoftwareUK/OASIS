using System;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.STAR
{
    public interface ITaskBase : ISTARNETHolon
    {
        Guid StartedBy { get; set; }
        DateTime StartedOn { get; set; }
        Guid CompletedBy { get; set; }
        DateTime CompletedOn { get; set; }
    }
}