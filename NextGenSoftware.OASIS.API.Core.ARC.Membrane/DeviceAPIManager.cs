using System.Threading.Tasks;
//using EdgeJs;

using NextGenSoftware.NodeManager;

namespace NextGenSoftware.OASIS.ARC.Core
{
    public class DeviceAPIManager
    {
        public async Task<string> CallNodeAddNumbers()
        {
            return await NodeManager.NodeManager.CallNodeMethod("./addNumbers", 1, 2);
        }

        //public async Task<string> CallNodeAddNumbersExternal()
        //{
        //    return await NodeManager.NodeManager.CallNodeMethod("./Test", 1, 2);
        //}
    }
}
