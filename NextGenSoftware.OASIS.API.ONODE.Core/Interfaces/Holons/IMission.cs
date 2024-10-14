using System.Collections.Generic;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;

namespace NextGenSoftware.OASIS.API.ONode.Core.Interfaces
{
    public interface IMission : ITaskBase
    {
        IEnumerable<IQuest> Quests { get; set; }
        IEnumerable<IChapter> Chapters { get; set; } //optional (large collection of quests can be broken into chapters.)
    }
}