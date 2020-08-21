
using NextGenSoftware.Holochain.HoloNET.Client.Core;
using System;
using System.Threading.Tasks;

namespace NextGenSoftware.Holochain.HoloNET.HDK.Core
{
    public abstract class HolochainBaseZome
    {
        //private const string ZOME_NAME = "zome_name";
        //private const string LOAD_HOLOCHAINDATAOBJECT_FUNC = "load_my_class";
        //private const string SAVE_HOLOCHAINDATAOBJECT_FUNC = "save_my_class";

        protected int _currentId = 0;
        protected string _hcinstance;
        protected TaskCompletionSource<string> _taskCompletionSourceGetInstance = new TaskCompletionSource<string>();

        public delegate void Initialized(object sender, EventArgs e);
        public event Initialized OnInitialized;

        public delegate void ZomeError(object sender, ZomeErrorEventArgs e);
        public event ZomeError OnZomeError;

        //TODO: Not sure if we want to expose the HoloNETClient events at this level? They can subscribe to them through the HoloNETClient property below...
        public delegate void Disconnected(object sender, DisconnectedEventArgs e);
        public event Disconnected OnDisconnected;

        public delegate void DataReceived(object sender, DataReceivedEventArgs e);
        public event DataReceived OnDataReceived;

        //TODO: If decide yes to above, finish passing through HoloNETClient events here...

        public string ZomeName { get; set; }

        public HoloNETClientBase HoloNETClient { get; private set; }

        public HolochainBaseZome(HoloNETClientBase holoNETClient, string zomeName)
        {
            this.HoloNETClient = holoNETClient;
            this.ZomeName = zomeName;

            Initialize();
        }

        public enum HoloNETType
        {
            Desktop,
            Unity
        }

        public HolochainBaseZome(string holochainConductorURI, HoloNETType type, string zomeName)
        {
            this.ZomeName = zomeName;

            switch (type)
            {
                case HoloNETType.Desktop:
                    this.HoloNETClient = new Client.Desktop.HoloNETClient(holochainConductorURI);
                    break;

                case HoloNETType.Unity:
                    this.HoloNETClient = new Client.Unity.HoloNETClient(holochainConductorURI);
                    break;
            }

            Initialize();
        }

        public async Task Initialize()
        {
            HoloNETClient.OnConnected += HoloNETClient_OnConnected;
            HoloNETClient.OnDisconnected += HoloNETClient_OnDisconnected;
            HoloNETClient.OnError += HoloNETClient_OnError;
            HoloNETClient.OnDataReceived += HoloNETClient_OnDataReceived;
            HoloNETClient.OnGetInstancesCallBack += HoloNETClient_OnGetInstancesCallBack;
            HoloNETClient.OnSignalsCallBack += HoloNETClient_OnSignalsCallBack;
            HoloNETClient.OnZomeFunctionCallBack += HoloNETClient_OnZomeFunctionCallBack;

            // HoloNETClient.Config.AutoStartConductor = true;
            //  HoloNETClient.Config.AutoShutdownConductor = true;
            //  HoloNETClient.Config.FullPathToExternalHolochainConductor = string.Concat(Directory.GetCurrentDirectory(), "\\hc.exe");
            //   HoloNETClient.Config.FullPathToHolochainAppDNA = string.Concat(Directory.GetCurrentDirectory(), "\\our_world\\dist\\our_world.dna.json"); 

            //await HoloNETClient.Connect();
        }

        private void HoloNETClient_OnZomeFunctionCallBack(object sender, ZomeFunctionCallBackEventArgs e)
        {

        }

        private void HoloNETClient_OnSignalsCallBack(object sender, SignalsCallBackEventArgs e)
        {

        }

        private void HoloNETClient_OnGetInstancesCallBack(object sender, GetInstancesCallBackEventArgs e)
        {
            _hcinstance = e.Instances[0];
            OnInitialized?.Invoke(this, new EventArgs());
            _taskCompletionSourceGetInstance.SetResult(_hcinstance);
        }

        private void HoloNETClient_OnDataReceived(object sender, DataReceivedEventArgs e)
        {
            OnDataReceived?.Invoke(this, e);
        }

        private void HoloNETClient_OnDisconnected(object sender, DisconnectedEventArgs e)
        {
            OnDisconnected?.Invoke(this, e);
        }

        private void HoloNETClient_OnConnected(object sender, ConnectedEventArgs e)
        {
            HoloNETClient.GetHolochainInstancesAsync();
        }

        private void HoloNETClient_OnError(object sender, HoloNETErrorEventArgs e)
        {
            HandleError("Error occured in HoloNET. See ErrorDetial for reason.", null, e);
        }


        /// <summary>
        /// Handles any errors thrown by HoloNET or HolochainBaseZome. It fires the OnZomeError error handler if there are any 
        /// subscriptions.
        /// </summary>
        /// <param name="reason"></param>
        /// <param name="errorDetails"></param>
        /// <param name="holoNETEventArgs"></param>
        protected void HandleError(string reason, Exception errorDetails, HoloNETErrorEventArgs holoNETEventArgs)
        {
            OnZomeError?.Invoke(this, new ZomeErrorEventArgs() { EndPoint = HoloNETClient.EndPoint, Reason = reason, ErrorDetails = errorDetails, HoloNETErrorDetails = holoNETEventArgs });
        }
    }
}
