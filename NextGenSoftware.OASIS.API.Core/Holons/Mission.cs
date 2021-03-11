using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Core.Holons
{
    public class Mission : Holon, IMission
    {
        public List<Quest> Quests = new List<Quest>();
    }
}