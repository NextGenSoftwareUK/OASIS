using System.Collections.Generic;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;

namespace NextGenSoftware.OASIS.API.ONode.Core.Interfaces
{
    public interface IMission : ITaskBase
    {
        IList<IQuest> Quests { get; set; }
        IList<IChapter> Chapters { get; set; } //optional (large collection of quests can be broken into chapters.)
    }
}