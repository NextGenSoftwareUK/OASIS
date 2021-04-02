
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Models
{
    public abstract class SeedsKarmaBase
    {
        public KarmaSourceType ReceivingKarmaFor { get; set; }
        public string AppWebsiteServiceName { get; set; }
        public string AppWebsiteServiceDesc { get; set; }
        public string AppWebsiteServiceWebLink { get; set; }
    }
}