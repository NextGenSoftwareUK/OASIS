using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;

namespace NextGenSoftware.OASIS.API.ONode.Core.Interfaces
{
    public interface IMission : ITaskBase, IPublishableHolon
    {
        //DateTime PublishedOn { get; set; }
        //Guid PublishedByAvatarId { get; set; }
        //IAvatar PublishedByAvatar { get; }
        IList<IQuest> Quests { get; set; }
        IList<IChapter> Chapters { get; set; } //optional (large collection of quests can be broken into chapters.)
    }
}