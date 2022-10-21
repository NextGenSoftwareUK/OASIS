using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.ONODE.BLL.Holons
{
    public class Quest : Holon, IQuest
    {
        public Quest()
        {
            this.HolonType = HolonType.Quest;
        }
    }
}