using System;
using NextGenSoftware.OASIS.API.Core.CustomAttrbiutes;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Holons
{
    public abstract class TaskBase : STARHolon, ITaskBase
    {
        [CustomOASISProperty()]
        public DateTime StartedOn { get; set; }

        [CustomOASISProperty()]
        public Guid StartedBy { get; set; }

        [CustomOASISProperty()]
        public DateTime CompletedOn { get; set; }

        [CustomOASISProperty()]
        public Guid CompletedBy { get; set; }
    }
}