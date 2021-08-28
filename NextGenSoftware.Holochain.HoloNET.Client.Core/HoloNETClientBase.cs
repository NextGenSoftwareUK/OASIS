using System;
using System.Text;
using System.Threading;
using System.Net.WebSockets;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using MessagePack;

namespace NextGenSoftware.Holochain.HoloNET.Client.Core
{
    public abstract class HoloNETClientBase
    {
        private const int ReceiveChunkSizeDefault = 1024;
        private const int SendChunkSizeDefault = 1024;
        private const int KeepAliveSecondsDefault = 30;
        private const int TimeOutSecondsDefault = 30;
        private const int ReconnectionAttemptsDefault = 5;
        private const int ReconnectionIntervalSecondsDefault = 5;
        private const int SecondsToWaitForConductorToStartDefault = 5;

        // private bool _useInternalHolochainConductor = false;
        private TaskCompletionSource<GetInstancesCallBackEventArgs> _taskCompletionSourceGetInstance = new TaskCompletionSource<GetInstancesCallBackEventArgs>();
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private readonly CancellationToken _cancellationToken;
        private Dictionary<string, string> _instanceLookup = new Dictionary<string, string>();
        private Dictionary<string, string> _zomeLookup = new Dictionary<string, string>();
        private Dictionary<string, string> _funcLookup = new Dictionary<string, string>();
        private Dictionary<string, ZomeFunctionCallBack> _callbackLookup = new Dictionary<string, ZomeFunctionCallBack>();
        private Dictionary<string, ZomeFunctionCallBackEventArgs> _zomeReturnDataLookup = new Dictionary<string, ZomeFunctionCallBackEventArgs>();
        private Dictionary<string, bool> _cacheZomeReturnDataLookup = new Dictionary<string, bool>();
        private GetInstancesCallBackEventArgs _instancesCache = null;
        private bool _cachInstancesReturnData = false;
        private ILogger _logger = null;
        private int _currentId = 0;
        private HoloNETConfig _config = null;

        //Events
        public delegate void Connected(object sender, ConnectedEventArgs e);
        public event Connected OnConnected;

        public delegate void Disconnected(object sender, DisconnectedEventArgs e);
        public event Disconnected OnDisconnected;

        public delegate void DataReceived(object sender, DataReceivedEventArgs e);
        public event DataReceived OnDataReceived;

        public delegate void ZomeFunctionCallBack(object sender, ZomeFunctionCallBackEventArgs e);
        public event ZomeFunctionCallBack OnZomeFunctionCallBack;

        public delegate void SignalsCallBack(object sender, SignalsCallBackEventArgs e);
        public event SignalsCallBack OnSignalsCallBack;

        public delegate void ConductorDebugCallBack(object sender, ConductorDebugCallBackEventArgs e);
        public event ConductorDebugCallBack OnConductorDebugCallBack;

        public delegate void GetInstancesCallBack(object sender, GetInstancesCallBackEventArgs e);
        public event GetInstancesCallBack OnGetInstancesCallBack;

        public delegate void Error(object sender, HoloNETErrorEventArgs e);
        public event Error OnError;

        // Properties
        public string EndPoint { get; private set; }
        public ClientWebSocket WebSocket { get; private set; }
        public HoloNETConfig Config
        {
            get
            {
                if (_config == null)
                    _config = new HoloNETConfig();

                return _config;
            }
        }

        public WebSocketState State
        {
            get
            {
                return WebSocket.State;
            }
        }

        public ILogger Logger { get; set; }

        public IHoloNETClientNET NetworkServiceProvider { get; set; }
        public NetworkServiceProviderMode NetworkServiceProviderMode { get; set; }
        public HolochainVersion HolochainVersion { get; set; }
        //public string AgentPubKey { get; set; } = "000000000000000000000000000000000000";
        public string AgentPubKey { get; set; } = "uhC0kTMixTG0lNZCF4SZfQMGozf2WfjQht7E06_wy3h29-zPpWxPQ";
        //public string HoloHash { get; set; } = "000000000000000000000000000000000000";
        public string HoloHash { get; set; } = "uhCAkt_cNGyYJZIp08b2ZzxoE6EqPndRPb_WwjVkM_mOBcFyq7zCw";
        

        public HoloNETClientBase(string holochainConductorURI, HolochainVersion version)
        {
            //  _useInternalHolochainConductor = useInternalHolochainConductor;
            WebSocket = new ClientWebSocket();
            WebSocket.Options.KeepAliveInterval = TimeSpan.FromSeconds(Config.KeepAliveSeconds == 0 ? KeepAliveSecondsDefault : Config.KeepAliveSeconds);
            EndPoint = holochainConductorURI;
            Config.ErrorHandlingBehaviour = ErrorHandlingBehaviour.OnlyThrowExceptionIfNoErrorHandlerSubscribedToOnErrorEvent;
            this.HolochainVersion = version;

            _cancellationToken = _cancellationTokenSource.Token; //TODO: do something with this!
        }

        public async Task Connect()
        {
            try
            {
                if (Logger == null)
                    throw new HoloNETException("ERROR: No Logger Has Been Specified! Please set a Logger with the Logger Property.");


                //MessagePack Test:

                /*
                switch (HolochainVersion)
                {
                    case HolochainVersion.Redux:
                    {

                    }
                    break;

                    case HolochainVersion.RSM:
                    {
                            try
                            {
                                byte[] bytes = MessagePackSerializer.Serialize(new HoloNETStream() { Age = 40, FirstName = "David", LastName = "Ellams" });

                                HoloNETStream stream = MessagePackSerializer.Deserialize<HoloNETStream>(bytes);

                                Console.WriteLine("HolosStream.FirstName: " + stream.FirstName);
                                Console.WriteLine("JSON = " + MessagePackSerializer.ConvertToJson(bytes));

                            }
                            catch (Exception ex)
                            {
                                string msg = ex.ToString();
                                Console.WriteLine("Error " + msg);
                            }
                    }
                    break;
                }
                */

                if (WebSocket.State != WebSocketState.Connecting && WebSocket.State != WebSocketState.Open && WebSocket.State != WebSocketState.Aborted)
                {
                    if (Config.AutoStartConductor)
                    {
                        DirectoryInfo info = new DirectoryInfo(Config.FullPathToHolochainAppDNA);
                        Logger.Log("Starting Holochain Conductor...", LogType.Info);

                        Process pProcess = new Process();
                        pProcess.StartInfo.WorkingDirectory = @"C:\holochain-holonix-v0.0.80-9-g6a1542d";
                        pProcess.StartInfo.FileName = "wsl";
                        // pProcess.StartInfo.Arguments = "run";
                        pProcess.StartInfo.UseShellExecute = false;
                        pProcess.StartInfo.RedirectStandardOutput = false;
                        pProcess.StartInfo.RedirectStandardInput = true;
                        pProcess.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
                        pProcess.StartInfo.CreateNoWindow = false;
                        pProcess.Start();

                        await Task.Delay(Config.SecondsToWaitForHolochainConductorToStart); // Give the conductor 5 seconds to start up...

                        // pProcess.StandardInput.WriteLine("nix-shell https://holochain.love");
                        //pProcess.StandardInput.WriteLine("nix-shell holochain-holonix-6a1542d");
                        pProcess.StandardInput.WriteLine("nix-shell C:\\holochain-holonix-v0.0.80-9-g6a1542d\\holochain-holonix-6a1542d");

                        await Task.Delay(Config.SecondsToWaitForHolochainConductorToStart); // Give the conductor 5 seconds to start up...

                        /*
                        //If no path to the conductor has been given then default to the current working directory.
                        if (string.IsNullOrEmpty(Config.FullPathToExternalHolochainConductor))
                            Config.FullPathToExternalHolochainConductor = string.Concat(Directory.GetCurrentDirectory(), "\\hc.exe"); //default to the current path

                        if (Config.SecondsToWaitForHolochainConductorToStart == 0)
                            Config.SecondsToWaitForHolochainConductorToStart = SecondsToWaitForConductorToStartDefault;

                        FileInfo conductorInfo = new FileInfo(Config.FullPathToExternalHolochainConductor);


                        //Make sure the condctor is not already running
                        if (!Process.GetProcesses().Any(x => x.ProcessName == conductorInfo.Name))
                        {
                            DirectoryInfo info = new DirectoryInfo(Config.FullPathToHolochainAppDNA);
                            Logger.Log("Starting Holochain Conductor...", LogType.Info);

                            Process pProcess = new Process();
                            pProcess.StartInfo.WorkingDirectory = info.Parent.Parent.FullName;
                            pProcess.StartInfo.FileName = Config.FullPathToExternalHolochainConductor;
                            pProcess.StartInfo.Arguments = "run";
                            pProcess.StartInfo.UseShellExecute = true;
                            pProcess.StartInfo.RedirectStandardOutput = false;
                            pProcess.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
                            pProcess.StartInfo.CreateNoWindow = false;
                            pProcess.Start();

                            await Task.Delay(Config.SecondsToWaitForHolochainConductorToStart); // Give the conductor 5 seconds to start up...
                        }*/
                    }

                    Logger.Log(string.Concat("Connecting to ", EndPoint, "..."), LogType.Info);

                    await WebSocket.ConnectAsync(new Uri(EndPoint), CancellationToken.None);
                    //NetworkServiceProvider.Connect(new Uri(EndPoint));
                    //TODO: need to be able to await this.

                    //if (NetworkServiceProvider.NetSocketState == NetSocketState.Open)
                    if (WebSocket.State == WebSocketState.Open)
                    {
                        Logger.Log(string.Concat("Connected to ", EndPoint), LogType.Info);
                        OnConnected?.Invoke(this, new ConnectedEventArgs { EndPoint = EndPoint });
                        StartListen();
                    }
                }
            }
            catch (Exception e)
            {
                HandleError(string.Concat("Error occured connecting to ", EndPoint), e);
            }
        }

        public async Task Disconnect()
        {
            try
            {
                if (Logger == null)
                    throw new HoloNETException("ERROR: No Logger Has Been Specified! Please set a Logger with the Logger Property.");

                if (WebSocket.State != WebSocketState.Connecting && WebSocket.State != WebSocketState.Closed && WebSocket.State != WebSocketState.Aborted && WebSocket.State != WebSocketState.CloseSent && WebSocket.State != WebSocketState.CloseReceived)
                {
                    Logger.Log(string.Concat("Disconnecting from ", EndPoint, "..."), LogType.Info);
                    await WebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client manually disconnected.", CancellationToken.None);

                    if (WebSocket.State == WebSocketState.Closed)
                    {
                        // Close any conductors down if necessary.
                        ShutDownConductors();

                        Logger.Log(string.Concat("Disconnected from ", EndPoint), LogType.Info);
                        OnDisconnected?.Invoke(this, new DisconnectedEventArgs { EndPoint = EndPoint, Reason = "Disconnected Method Called." });
                    }
                }
            }
            catch (Exception e)
            {
                HandleError(string.Concat("Error occured disconnecting from ", EndPoint), e);
            }
        }

        public async Task CallZomeFunctionAsync(string instanceId, string zome, string function, object paramsObject, bool cachReturnData = false)
        {
            _currentId++;
            await CallZomeFunctionAsync(_currentId.ToString(), instanceId, zome, function, null, paramsObject, true, cachReturnData);
        }

        public async Task CallZomeFunctionAsync(string id, string instanceId, string zome, string function, object paramsObject, bool matchIdToInstanceZomeFuncInCallback = true, bool cachReturnData = false)
        {
            await CallZomeFunctionAsync(id, instanceId, zome, function, null, paramsObject, matchIdToInstanceZomeFuncInCallback, cachReturnData);
        }

        public async Task CallZomeFunctionAsync(string instanceId, string zome, string function, ZomeFunctionCallBack callback, object paramsObject, bool cachReturnData = false)
        {
            _currentId++;
            await CallZomeFunctionAsync(_currentId.ToString(), instanceId, zome, function, callback, paramsObject, true, cachReturnData);
        }

        //public async Task CallZomeFunctionAsync(string id, string instanceId, string zome, string function, ZomeFunctionCallBack callback, object paramsObject, bool autoConvertFieldsToHCStandard = false, bool matchIdToInstanceZomeFuncInCallback = true, bool cachReturnData = false)
        public async Task CallZomeFunctionAsync(string id, string instanceId, string zome, string function, ZomeFunctionCallBack callback, object paramsObject, bool matchIdToInstanceZomeFuncInCallback = true, bool cachReturnData = false)
        {
            Logger.Log("CallZomeFunctionAsync ENTER", LogType.Debug);
            _cacheZomeReturnDataLookup[id] = cachReturnData;

            if (cachReturnData)
            {
                if (_zomeReturnDataLookup.ContainsKey(id))
                {
                    Logger.Log("Caching Enabled so returning data from cach...", LogType.Warn);
                    Logger.Log(string.Concat("Id: ", _zomeReturnDataLookup[id].Id, ", Instance: ", _zomeReturnDataLookup[id].Instance, ", Zome: ", _zomeReturnDataLookup[id].Zome, ", Zome Function: ", _zomeReturnDataLookup[id].ZomeFunction, ", Is Zome Call Successful: ", _zomeReturnDataLookup[id].IsCallSuccessful ? "True" : "False", ", Raw Zome Return Data: ", _zomeReturnDataLookup[id].RawZomeReturnData, ", Zome Return Data: ", _zomeReturnDataLookup[id].ZomeReturnData, ", JSON Raw Data: ", _zomeReturnDataLookup[id].RawJSONData), LogType.Info);

                    if (callback != null)
                        callback.DynamicInvoke(this, _zomeReturnDataLookup[id]);

                    OnZomeFunctionCallBack?.Invoke(this, _zomeReturnDataLookup[id]);
                    Logger.Log("CallZomeFunctionAsync EXIT", LogType.Debug);
                    return;
                }
            }

            if (matchIdToInstanceZomeFuncInCallback)
            {
                _instanceLookup[id] = instanceId;
                _zomeLookup[id] = zome;
                _funcLookup[id] = function;
            }

            if (callback != null)
                _callbackLookup[id] = callback;


            //if (autoConvertFieldsToHCStandard)
            //{
            //    string json = JsonConvert.SerializeObject(paramsObject);
            //    JObject data = JObject.Parse(json);
            //    //data.Next.Replace(new JToken()

            //    data.SelectToken("instance_stats.test-instance.number_delayed_validations").ToString()),
            //}


            switch (HolochainVersion)
            {
                case HolochainVersion.Redux:
                {
                        await SendRawDataAsync(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(
                            new
                            {
                                jsonrpc = "2.0",
                                id,
                                method = "call",
                                @params = new { instance_id = instanceId, zome, function, args = paramsObject }
                        }
                        )));
                    }
                break;

                case HolochainVersion.RSM:
                {
                    HoloNETData holoNETData = new HoloNETData()
                    {
                        type = "zome_call",
                        data = new Data()
                        {
                            cell_id = new byte[2][]{ Encoding.UTF8.GetBytes(HoloHash), Encoding.UTF8.GetBytes(AgentPubKey) },
                            fn_name = function,
                            zome_name = zome,
                            payload = MessagePackSerializer.Serialize(paramsObject),
                            provenance = Encoding.UTF8.GetBytes(AgentPubKey),
                            cap = null
                        }
                    };

                    //holoNETData.data.cell_id[0] = Encoding.UTF8.GetBytes(HoloHash);
                   // holoNETData.data.cell_id[1] = Encoding.UTF8.GetBytes(AgentPubKey);

                    // UInt32 holoHash = 000000000000000000000000000000000000;
                    // UInt32 agentPubKey = 000000000000000000000000000000000000;

                    //holoNETData.cell_id = new UInt32[2];
                    //holoNETData.cell_id[0] = holoHash;
                    //holoNETData.cell_id[1] = agentPubKey;

                    HoloNETRequest request = new HoloNETRequest()
                    {
                        id = Convert.ToUInt64(id),
                        type = "Request",
                        data = MessagePackSerializer.Serialize(holoNETData)
                    };

                    await SendRawDataAsync(MessagePackSerializer.Serialize(request));
                    //await SendRawDataAsync(MessagePackSerializer.Serialize(new HoloNETRequest() { id = id, type = "Request", data = MessagePackSerializer.Serialize(new HoloNETData() { fn_name = function, zome_name = zome, payload = MessagePackSerializer.Serialize(paramsObject), provenance = AgentPubKey, cap = null, cell_id = new string[1, 2] { { HoloHash, AgentPubKey } } }) } ));
                }
                break;
            }

            Logger.Log("CallZomeFunctionAsync EXIT", LogType.Debug);
        }


        //public async Task SendMessageAsync(string jsonMessage)
        //{
        //    Logger.Log(string.Concat("Sending Message: ", jsonMessage), LogType.Info);

        //    //if (autoConvertFieldsToHCStandard)
        //    //{
        //    //    JObject data = JObject.Parse(jsonMessage);
        //    //    data.SelectToken("instance_stats.test-instance.number_delayed_validations").ToString()),
        //    //}

        //    //if (WebSocket.State != WebSocketState.Open)
        //    //{
        //    //    string msg = "Connection is not open!";
        //    //    HandleError(msg, new InvalidOperationException(msg));
        //    //}

            
        //    await SendRawData(Encoding.UTF8.GetBytes(jsonMessage));


        //    
        //    var messagesCount = (int)Math.Ceiling((double)messageBuffer.Length / sendChunkSize);

        //    for (var i = 0; i < messagesCount; i++)
        //    {
        //        var offset = (sendChunkSize * i);
        //        var count = sendChunkSize;
        //        var lastMessage = ((i + 1) == messagesCount);

        //        if ((count * (i + 1)) > messageBuffer.Length)
        //            count = messageBuffer.Length - offset;

        //        Logger.Log(string.Concat("Sending Message ", i, " of ", messagesCount, "..."), LogType.Debug);
        //        await WebSocket.SendAsync(new ArraySegment<byte>(messageBuffer, offset, count), WebSocketMessageType.Text, lastMessage, _cancellationToken);
        //    }
        //    

        //    Logger.Log("Message Sent", LogType.Info);
        //}

        public async Task SendRawDataAsync(byte[] data)
        {
            Logger.Log("Sending Raw Data...", LogType.Info);

            if (WebSocket.State != WebSocketState.Open)
            {
                string msg = "Connection is not open!";
                HandleError(msg, new InvalidOperationException(msg));
            }

            int sendChunkSize = Config.SendChunkSize == 0 ? SendChunkSizeDefault : Config.SendChunkSize;
            var messagesCount = (int)Math.Ceiling((double)data.Length / sendChunkSize);

            for (var i = 0; i < messagesCount; i++)
            {
                var offset = (sendChunkSize * i);
                var count = sendChunkSize;
                var lastMessage = ((i + 1) == messagesCount);

                if ((count * (i + 1)) > data.Length)
                    count = data.Length - offset;

                Logger.Log(string.Concat("Sending Data Packet ", i, " of ", messagesCount, "..."), LogType.Debug);
                await WebSocket.SendAsync(new ArraySegment<byte>(data, offset, count), WebSocketMessageType.Text, lastMessage, _cancellationToken);
            }

            Logger.Log("Sending Raw Data... Done!", LogType.Info);
        }

        private async Task StartListen()
        {
            var buffer = new byte[Config.ReceiveChunkSize == 0 ? ReceiveChunkSizeDefault : Config.ReceiveChunkSize];
            Logger.Log(string.Concat("Listening on ", EndPoint, "..."), LogType.Info);

            try
            {
                while (WebSocket.State == WebSocketState.Open)
                {
                    var stringResult = new StringBuilder();
                    List<byte> dataResponse = new List<byte>();

                    WebSocketReceiveResult result;
                    do
                    {
                        if (Config.NeverTimeOut)
                            result = await WebSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                        else
                        {
                            using (var cts = new CancellationTokenSource((Config.TimeOutSeconds == 0 ? TimeOutSecondsDefault : Config.TimeOutSeconds) * 1000))
                                result = await WebSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cts.Token);
                        }

                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            string msg = "Closing because received close message from Holochain."; //TODO: Move all strings to constants at top or resources.strings
                            await WebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, msg, CancellationToken.None);
                            OnDisconnected?.Invoke(this, new DisconnectedEventArgs { EndPoint = EndPoint, Reason = msg });
                            Logger.Log(msg, LogType.Info);

                            ShutDownConductors();

                            //AttemptReconnect(); //TODO: Not sure re-connect here?
                        }
                        else
                        {
                            switch (HolochainVersion)
                            {
                                case HolochainVersion.Redux:
                                {
                                    var str = Encoding.UTF8.GetString(buffer, 0, result.Count);
                                    stringResult.Append(str);
                                }
                                break;

                                case HolochainVersion.RSM:
                                {
                                    for (int i = 0; i < result.Count; i++)
                                        dataResponse.Add(buffer[i]);
                                }
                                break;
                            }
                        }

                    } while (!result.EndOfMessage);

                    switch (HolochainVersion)
                    {
                        case HolochainVersion.Redux:
                        {
                                string rawData = stringResult.ToString();
                                JObject data = JObject.Parse(rawData);

                                bool isConductorDebugInfo = false;
                                string id = "";

                                if (data.ContainsKey("type"))
                                    isConductorDebugInfo = true;

                                Logger.Log(string.Concat("Received Data: ", rawData), LogType.Info);
                                OnDataReceived?.Invoke(this, new DataReceivedEventArgs { EndPoint = EndPoint, RawJSONData = rawData, WebSocketResult = result, IsConductorDebugInfo = isConductorDebugInfo });

                                if (data.ContainsKey("id"))
                                    id = data["id"].ToString();

                                if (isConductorDebugInfo)
                                {
                                    // Conducor Debug Info.
                                    ConductorDebugCallBackEventArgs args = new ConductorDebugCallBackEventArgs { EndPoint = EndPoint, RawJSONData = rawData, NumberDelayedValidations = Convert.ToInt32(data.SelectToken("instance_stats.test-instance.number_delayed_validations").ToString()), NumberHeldAspects = Convert.ToInt32(data.SelectToken("instance_stats.test-instance.number_held_aspects").ToString()), NumberHeldEntries = Convert.ToInt32(data.SelectToken("instance_stats.test-instance.number_held_entries").ToString()), NumberPendingValidations = Convert.ToInt32(data.SelectToken("instance_stats.test-instance.number_pending_validations").ToString()), NumberRunningZomeCalls = Convert.ToInt32(data.SelectToken("instance_stats.test-instance.number_running_zome_calls").ToString()), Offline = Convert.ToBoolean(data.SelectToken("instance_stats.test-instance.offline").ToString()), Type = data.SelectToken("type").ToString(), WebSocketResult = result };
                                    Logger.Log(string.Concat("Conductor Debug Info Detected. Raw JSON Data: ", rawData, "NumberDelayedValidations: ", args.NumberDelayedValidations, "NumberDelayedValidations: ", args.NumberHeldAspects, "NumberHeldEntries: ", args.NumberHeldEntries, "NumberPendingValidations: ", args.NumberPendingValidations, "NumberRunningZomeCalls; ", args.NumberRunningZomeCalls, "Offline: ", args.Offline, "Type: ", args.Type), LogType.Info);
                                    OnConductorDebugCallBack?.Invoke(this, args);
                                }
                                else if (data.ContainsKey("signal"))
                                {
                                    //Signals.
                                    SignalsCallBackEventArgs args = new SignalsCallBackEventArgs(id, EndPoint, true, rawData, (SignalsCallBackEventArgs.SignalTypes)Enum.Parse(typeof(SignalsCallBackEventArgs.SignalTypes), data.SelectToken("signal_type").ToString(), true), data.SelectToken("name").ToString(), data.SelectToken("arguments"), result);
                                    Logger.Log(string.Concat("Signals data detected. Id: ", id, ", Raw JSON Data: ", rawData, "Name: ", args.Name, "SignalType: ", args.SignalType, "Arguments: ", args.Arguments), LogType.Info);
                                    OnSignalsCallBack?.Invoke(this, args);
                                }
                                else if (data.SelectToken("result[0].agent") != null)
                                {
                                    // Get Instance Info.
                                    GetInstancesCallBackEventArgs args = new GetInstancesCallBackEventArgs(id, EndPoint, true, data["result"].ToString(), new List<string>() { data.SelectToken("result[0].id").ToString() }, data.SelectToken("result[0].dna").ToString(), data.SelectToken("result[0].agent").ToString(), result);
                                    Logger.Log(string.Concat("Get Instances data detected. Id: ", id, ", EndPoint: ", EndPoint, ", agent: ", args.Agent, ", DNA: ", args.DNA, ", Instances: ", string.Join(",", args.Instances), ", Raw JSON Data: ", rawData), LogType.Info);

                                    if (_cachInstancesReturnData)
                                        _instancesCache = args;

                                    // _taskCompletionSourceGetInstance.SetResult(args);
                                    OnGetInstancesCallBack?.Invoke(this, args);
                                }
                                else
                                {
                                    //Zome Return Call.
                                    string rawZomeReturnData = "";

                                    if (data.ContainsKey("result"))
                                        rawZomeReturnData = data["result"].ToString();

                                    string zomeReturnData = string.Empty;
                                    bool isZomeCallSuccessful = false;

                                    if (rawZomeReturnData.Length >= 4 && rawZomeReturnData.Substring(2, 2).ToUpper() == "OK")
                                    {
                                        isZomeCallSuccessful = true;
                                        zomeReturnData = rawZomeReturnData.Substring(7, rawZomeReturnData.Length - 9);
                                        //zomeReturnData = rawZomeReturnData.Substring(6, rawZomeReturnData.Length - 8);
                                    }
                                    //else if (rawZomeReturnData.Substring(0,3).ToUpper() == "ERR")
                                    else if (rawZomeReturnData.Length > 1)
                                        zomeReturnData = rawZomeReturnData.Substring(1, rawZomeReturnData.Length - 2);
                                    //zomeReturnData = rawZomeReturnData.Substring(8, rawZomeReturnData.Length - 11);

                                    ZomeFunctionCallBackEventArgs args = new ZomeFunctionCallBackEventArgs(id, this.EndPoint, GetItemFromCache(id, _instanceLookup), GetItemFromCache(id, _zomeLookup), GetItemFromCache(id, _funcLookup), isZomeCallSuccessful, rawZomeReturnData, zomeReturnData, rawData, result);
                                    Logger.Log(string.Concat("Zome result data detected. Id: ", args.Id, ", Instance: ", args.Instance, ", Zome: ", args.Zome, ", Zome Function: ", args.ZomeFunction, ", Is Zome Call Successful: ", args.IsCallSuccessful ? "True" : "False", ", Raw Zome Return Data: ", args.RawZomeReturnData, ", Zome Return Data: ", args.ZomeReturnData, ", JSON Raw Data: ", args.RawJSONData), LogType.Info);

                                    if (_callbackLookup.ContainsKey(id) && _callbackLookup[id] != null)
                                        _callbackLookup[id].DynamicInvoke(this, args);

                                    OnZomeFunctionCallBack?.Invoke(this, args);

                                    // If the zome call requested for this to be cached then stick it in cach.
                                    if (_cacheZomeReturnDataLookup[id])
                                        _zomeReturnDataLookup[id] = args;

                                    _instanceLookup.Remove(id);
                                    _zomeLookup.Remove(id);
                                    _funcLookup.Remove(id);
                                    _callbackLookup.Remove(id);
                                }
                        }break;

                        case HolochainVersion.RSM:
                        {
                                var options = MessagePackSerializerOptions.Standard.WithSecurity(MessagePackSecurity.UntrustedData);
                                HoloNETResponse response = MessagePackSerializer.Deserialize<HoloNETResponse>(dataResponse.ToArray(), options);

                                Console.WriteLine("RSM RESPONSE");
                                Console.WriteLine("ID: " + response.id);
                                Console.WriteLine("TYPE: " + response.type);
                                Console.WriteLine("ENCODED DATA: " + response.data);

                                string responseData2 = MessagePackSerializer.Deserialize<string>(response.data, options);
                                byte[] responseData = MessagePackSerializer.Deserialize<byte[]>(response.data, options);
                                var responseDataString = Encoding.UTF8.GetString(responseData, 0, responseData.Length);

                                Console.WriteLine("DECODED DATA: " + responseDataString);
                                Console.WriteLine("DECODED DATA2: " + responseData2);
                            }
                        break;
                    }
                }
            }

            catch (TaskCanceledException ex)
            {
                string msg = string.Concat("Connection timed out after ", (Config.TimeOutSeconds == 0 ? TimeOutSecondsDefault : Config.TimeOutSeconds), " seconds.");
                OnDisconnected?.Invoke(this, new DisconnectedEventArgs { EndPoint = EndPoint, Reason = msg });
                HandleError(msg, ex);
                await AttemptReconnect();
            }

            catch (Exception ex)
            {
                OnDisconnected?.Invoke(this, new DisconnectedEventArgs { EndPoint = EndPoint, Reason = string.Concat("Error occured: ", ex) });
                HandleError("Disconnected because an error occured.", ex);
                await AttemptReconnect();
            }

            finally
            {
                WebSocket.Dispose();
            }
        }

        public void ClearCache()
        {
            _zomeReturnDataLookup.Clear();
            _cacheZomeReturnDataLookup.Clear();
        }

        //public List<ZomeFunctionCall> GetResponsesWaitingFromHolochain()
        //{
        //    List<ZomeFunctionCall> list = new List<ZomeFunctionCall>();
        //    for (int i=0; i < _instanceLookup.Keys.Count; i++)
        //    {
        //        //list.Add(new ZomeFunctionCall { EndPoint = this.EndPoint, Id = _instanceLookup.Keys.})
        //    }
        //}


        //public async Task<GetInstancesCallBackEventArgs> GetHolochainInstancesAsync(bool cachReturnData = false)
        public async Task GetHolochainInstancesAsync(bool cachReturnData = false)
        {
            _currentId++;
            //return await GetHolochainInstancesAsync(_currentId.ToString(), cachReturnData);
            await GetHolochainInstancesAsync(_currentId.ToString(), cachReturnData);
        }

        //public async Task<GetInstancesCallBackEventArgs> GetHolochainInstancesAsync(string id, bool cachReturnData = false)
        public async Task GetHolochainInstancesAsync(string id, bool cachReturnData = false)
        {
            //TODO: Are there other admin functions we can wrap around?

            Logger.Log("GetHolochainInstances ENTER", LogType.Debug);

            if (cachReturnData && _instancesCache != null)
            {
                OnGetInstancesCallBack(this, _instancesCache);
                Logger.Log("Caching Enabled so returning data from cach...", LogType.Warn);
                Logger.Log(string.Concat("Id: ", _instancesCache.Id, ", Instances: ", string.Join(", ", _instancesCache.Instances), ", Is Call Successful: ", _instancesCache.IsCallSuccessful ? "True" : "False", ", JSON Raw Data: ", _instancesCache.RawJSONData), LogType.Info);
                //return null;
            }

            _cachInstancesReturnData = cachReturnData;

            await SendRawDataAsync(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(
                new
                {
                    jsonrpc = "2.0",
                    id,
                    method = "info/instances"
                }
            )));

            //return await _taskCompletionSourceGetInstance.Task;

            Logger.Log("GetHolochainInstances EXIT", LogType.Debug);
        }

        private async Task AttemptReconnect()
        {
            for (int i = 0; i < (Config.ReconnectionAttempts == 0 ? ReconnectionAttemptsDefault : Config.ReconnectionAttempts); i++)
            {
                if (WebSocket.State == WebSocketState.Open)
                    break;

                Logger.Log(string.Concat("Attempting to reconnect... Attempt ", +i), LogType.Info);
                await Connect();
                await Task.Delay(Config.ReconnectionIntervalSeconds == 0 ? ReconnectionIntervalSecondsDefault : Config.ReconnectionIntervalSeconds);
            }
        }

        private string GetItemFromCache(string id, Dictionary<string, string> cache)
        {
            return cache.ContainsKey(id) ? cache[id] : null;
        }

        private void HandleError(string message, Exception exception)
        {
            message = string.Concat(message, "\nError Details: ", exception != null ? exception.ToString() : "");
            Logger.Log(message, LogType.Error);

            OnError?.Invoke(this, new HoloNETErrorEventArgs { EndPoint = EndPoint, Reason = message, ErrorDetails = exception });

            switch (Config.ErrorHandlingBehaviour)
            {
                case ErrorHandlingBehaviour.AlwaysThrowExceptionOnError:
                    throw new HoloNETException(message, exception, this.EndPoint);

                case ErrorHandlingBehaviour.OnlyThrowExceptionIfNoErrorHandlerSubscribedToOnErrorEvent:
                    {

                        if (OnError == null)
                            throw new HoloNETException(message, exception, this.EndPoint);
                    }
                    break;
            }
        }

        private void ShutDownConductors()
        {
            // Close any conductors down if necessary.
            if (Config.AutoShutdownConductor)
            {
                FileInfo conductorInfo = new FileInfo(Config.FullPathToExternalHolochainConductor);
                foreach (Process process in Process.GetProcessesByName(conductorInfo.Name))
                    process.Kill();
            }
        }
    }
}
