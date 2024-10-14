using System.Collections.Generic;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;

namespace NextGenSoftware.OASIS.API.ONode.Core.Interfaces
{
    public interface IChapter : ITaskBase
    {
        IList<IQuest> Quests { get; set; }
    }
}