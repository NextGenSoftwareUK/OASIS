
namespace NextGenSoftware.Holochain.HoloNET.Client.Core
{
    public class ZomeFunctionCall
    {
        public string Id { get; private set; }
        public string EndPoint { get; private set; }
        public string Instance { get; private set; }
        public string Zome { get; private set; }
        public string ZomeFunction { get; private set; }
    }
}
