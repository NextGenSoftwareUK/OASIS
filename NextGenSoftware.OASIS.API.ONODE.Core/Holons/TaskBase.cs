using System;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.ONode.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.ONode.Core.Holons
{
    public class TaskBase : Holon, ITaskBase
    {
        public DateTime StartedOn { get; set; }
        public Guid StartedBy { get; set; }
        public DateTime CompletedOn { get; set; }
        public Guid CompletedBy { get; set; }
    }
}