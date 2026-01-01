using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.AI
{
    public class ParseIntentRequest
    {
        public string UserInput { get; set; }
        public AIContext Context { get; set; }
    }

    public class AIContext
    {
        public string Avatar { get; set; }
        public string AvatarId { get; set; }
        public List<string> AvailableProviders { get; set; }
        public string DefaultOnChainProvider { get; set; }
        public string DefaultOffChainProvider { get; set; }
    }
}

