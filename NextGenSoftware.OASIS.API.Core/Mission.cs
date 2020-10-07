using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.Core
{
    public class Mission : Holon, IMission
    {
        public List<Quest> Quests = new List<Quest>();
    }
}