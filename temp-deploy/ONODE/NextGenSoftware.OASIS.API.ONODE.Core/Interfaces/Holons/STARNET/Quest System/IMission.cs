using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons
{
    public interface IMission : ITaskBase, ISTARNETHolon//, IPublishableHolon
    {
        //DateTime PublishedOn { get; set; }
        //Guid PublishedByAvatarId { get; set; }
        //IAvatar PublishedByAvatar { get; }
        //IList<IQuest> Quests { get; set; }
        IList<IChapter> Chapters { get; set; } //optional (large collection of quests can be broken into chapters.)
    }
}