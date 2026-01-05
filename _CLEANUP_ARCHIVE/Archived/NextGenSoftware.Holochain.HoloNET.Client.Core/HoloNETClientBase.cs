using System;
using System.Text;
using System.Net.WebSockets;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using MessagePack;
using NextGenSoftware.WebSocket;
using NextGenSoftware.Holochain.HoloNET.Client.MessagePack;

namespace NextGenSoftware.Holochain.HoloNET.Client.Core
{
    //[MessagePackObject]
    //[Serializable]
    //public class Temp
    //{
    //    [Key(0)]
    //    public string Name { get; set; }

    //    [Key(1)]
    //    public string Desc { get; set; }
    //}

    [MessagePackObject]
    //[Serializable]
    public class Temp
    {
        [Key(0)]
        public int number { get; set; }
    }

    public abstract class HoloNETClientBase
    {
        private TaskCompletionSource<GetInstancesCallBackEventArgs> _taskCompletionSourceGetInstance = new TaskCompletionSource<GetInstancesCallBackEventArgs>();
        private Dictionary<string, string> _instanceLookup = new Dictionary<string, string>();
        private Dictionary<string, string> _zomeLookup = new Dictionary<string, string>();
        private Dictionary<string, string> _funcLookup = new Dictionary<string, string>();
        private Dictionary<string, ZomeFunctionCallBack> _callbackLookup = new Dictionary<string, ZomeFunctionCallBack>();
        private Dictionary<string, ZomeFunctionCallBackEventArgs> _zomeReturnDataLookup = new Dictionary<string, ZomeFunctionCallBackEventArgs>();
        private Dictionary<string, bool> _cacheZomeReturnDataLookup = new Dictionary<string, bool>();
        private GetInstancesCallBackEventArgs _instancesCache = null;
        private bool _cachInstancesReturnData = false;
        private int _currentId = 0;
        private HoloNETConfig _config = null;

        //Events
        public delegate void Connected(object sender, ConnectedEventArgs e);
        public event Connected OnConnected;

        public delegate void Disconnected(object sender, DisconnectedEventArgs e);
        public event Disconnected OnDisconnected;

        public delegate void DataReceived(object sender, HoloNETDataReceivedEventArgs e);
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
        public WebSocket.WebSocket WebSocket { get; set; }

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

        public string EndPoint
        {
            get
            {
                if (WebSocket != null)
                    return WebSocket.EndPoint;

                return "";
            }
        }

        public ILogger Logger { get; set; }
        public HolochainVersion HolochainVersion { get; set; }
       
        public HoloNETClientBase(string holochainConductorURI, HolochainVersion version, ILogger logger)
        {
            HolochainVersion = version;
            Logger = logger;
            WebSocket = new WebSocket.WebSocket(holochainConductorURI, logger);

            //TODO: Impplemnt IDispoasable to unsubscribe event handlers to prevent memory leaks... 
            WebSocket.OnConnected += WebSocket_OnConnected;
            WebSocket.OnDataReceived += WebSocket_OnDataReceived;
            WebSocket.OnDisconnected += WebSocket_OnDisconnected;
            WebSocket.OnError += WebSocket_OnError;
        }

        private void WebSocket_OnError(object sender, WebSocketErrorEventArgs e)
        {
            HandleError(e.Reason, e.ErrorDetails);
        }

        private void WebSocket_OnDisconnected(object sender, DisconnectedEventArgs e)
        {
            ShutDownConductors();
            OnDisconnected?.Invoke(this, new DisconnectedEventArgs { EndPoint = e.EndPoint, Reason = e.Reason });
        }

        private void WebSocket_OnDataReceived(object sender, WebSocket.DataReceivedEventArgs e)
        {
            switch (HolochainVersion)
            {
                case HolochainVersion.Redux:
                    {
                        JObject data = JObject.Parse(e.RawJSONData);

                        bool isConductorDebugInfo = false;
                        string id = "";

                        if (data.ContainsKey("type"))
                            isConductorDebugInfo = true;

                        Logger.Log(string.Concat("Received Data: ", e.RawJSONData), LogType.Info);
                        OnDataReceived?.Invoke(this, new HoloNETDataReceivedEventArgs { EndPoint = e.EndPoint, RawJSONData = e.RawJSONData, RawBinaryData = e.RawBinaryData, WebSocketResult = e.WebSocketResult, IsConductorDebugInfo = isConductorDebugInfo });

                        if (data.ContainsKey("id"))
                            id = data["id"].ToString();

                        if (isConductorDebugInfo)
                        {
                            // Conducor Debug Info.
                            ConductorDebugCallBackEventArgs args = new ConductorDebugCallBackEventArgs { EndPoint = e.EndPoint, RawJSONData = e.RawJSONData, NumberDelayedValidations = Convert.ToInt32(data.SelectToken("instance_stats.test-instance.number_delayed_validations").ToString()), NumberHeldAspects = Convert.ToInt32(data.SelectToken("instance_stats.test-instance.number_held_aspects").ToString()), NumberHeldEntries = Convert.ToInt32(data.SelectToken("instance_stats.test-instance.number_held_entries").ToString()), NumberPendingValidations = Convert.ToInt32(data.SelectToken("instance_stats.test-instance.number_pending_validations").ToString()), NumberRunningZomeCalls = Convert.ToInt32(data.SelectToken("instance_stats.test-instance.number_running_zome_calls").ToString()), Offline = Convert.ToBoolean(data.SelectToken("instance_stats.test-instance.offline").ToString()), Type = data.SelectToken("type").ToString(), WebSocketResult = e.WebSocketResult };
                            Logger.Log(string.Concat("Conductor Debug Info Detected. Raw JSON Data: ", e.RawJSONData, "NumberDelayedValidations: ", args.NumberDelayedValidations, "NumberDelayedValidations: ", args.NumberHeldAspects, "NumberHeldEntries: ", args.NumberHeldEntries, "NumberPendingValidations: ", args.NumberPendingValidations, "NumberRunningZomeCalls; ", args.NumberRunningZomeCalls, "Offline: ", args.Offline, "Type: ", args.Type), LogType.Info);
                            OnConductorDebugCallBack?.Invoke(this, args);
                        }
                        else if (data.ContainsKey("signal"))
                        {
                            //Signals.
                            SignalsCallBackEventArgs args = new SignalsCallBackEventArgs(id, e.EndPoint, true, e.RawJSONData, (SignalsCallBackEventArgs.SignalTypes)Enum.Parse(typeof(SignalsCallBackEventArgs.SignalTypes), data.SelectToken("signal_type").ToString(), true), data.SelectToken("name").ToString(), data.SelectToken("arguments"), e.WebSocketResult);
                            Logger.Log(string.Concat("Signals data detected. Id: ", id, ", Raw JSON Data: ", e.RawJSONData, "Name: ", args.Name, "SignalType: ", args.SignalType, "Arguments: ", args.Arguments), LogType.Info);
                            OnSignalsCallBack?.Invoke(this, args);
                        }
                        else if (data.SelectToken("result[0].agent") != null)
                        {
                            // Get Instance Info.
                            GetInstancesCallBackEventArgs args = new GetInstancesCallBackEventArgs(id, e.EndPoint, true, data["result"].ToString(), new List<string>() { data.SelectToken("result[0].id").ToString() }, data.SelectToken("result[0].dna").ToString(), data.SelectToken("result[0].agent").ToString(), e.WebSocketResult);
                            Logger.Log(string.Concat("Get Instances data detected. Id: ", id, ", EndPoint: ", e.EndPoint, ", agent: ", args.Agent, ", DNA: ", args.DNA, ", Instances: ", string.Join(",", args.Instances), ", Raw JSON Data: ", e.RawJSONData), LogType.Info);

                            if (_cachInstancesReturnData)
                                _instancesCache = args;

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
                            }
                            else if (rawZomeReturnData.Length > 1)
                                zomeReturnData = rawZomeReturnData.Substring(1, rawZomeReturnData.Length - 2);

                            ZomeFunctionCallBackEventArgs args = new ZomeFunctionCallBackEventArgs(id, e.EndPoint, GetItemFromCache(id, _instanceLookup), GetItemFromCache(id, _zomeLookup), GetItemFromCache(id, _funcLookup), isZomeCallSuccessful, rawZomeReturnData, zomeReturnData, e.RawJSONData, e.WebSocketResult);
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
                    }
                    break;

                case HolochainVersion.RSM:
                    {
                        OnDataReceived?.Invoke(this, new HoloNETDataReceivedEventArgs { EndPoint = e.EndPoint, RawJSONData = e.RawJSONData, RawBinaryData = e.RawBinaryData, WebSocketResult = e.WebSocketResult });

                        var options = MessagePackSerializerOptions.Standard.WithSecurity(MessagePackSecurity.UntrustedData);
                        HoloNETResponse response = MessagePackSerializer.Deserialize<HoloNETResponse>(e.RawBinaryData, options);

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

        private void WebSocket_OnConnected(object sender, ConnectedEventArgs e)
        {
            OnConnected?.Invoke(this, new ConnectedEventArgs { EndPoint = e.EndPoint });
        }

        public async Task Connect()
        {
            try
            {
                if (Logger == null)
                    throw new HoloNETException("ERROR: No Logger Has Been Specified! Please set a Logger with the Logger Property.");

                if (WebSocket.State != WebSocketState.Connecting && WebSocket.State != WebSocketState.Open && WebSocket.State != WebSocketState.Aborted)
                {
                    if (Config.AutoStartConductor && !string.IsNullOrEmpty(Config.FullPathToHolochainAppDNA))
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

                    Logger.Log(string.Concat("Connecting to ", WebSocket.EndPoint, "..."), LogType.Info);
                    await WebSocket.Connect();
                }
            }
            catch (Exception e)
            {
                HandleError(string.Concat("Error occured connecting to ", WebSocket.EndPoint), e);
            }
        }

        public async Task Disconnect()
        {
            await WebSocket.Disconnect();
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

        public async Task CallZomeFunctionAsync(string id, string instanceId, string zome, string function, ZomeFunctionCallBack callback, object paramsObject, bool matchIdToInstanceZomeFuncInCallback = true, bool cachReturnData = false)
        {
            Logger.Log("CallZomeFunctionAsync ENTER", LogType.Debug);
            _cacheZomeReturnDataLookup[id] = cachReturnData;

            if (WebSocket.State == WebSocketState.Closed || WebSocket.State == WebSocketState.None)
                await Connect();

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


            switch (HolochainVersion)
            {
                case HolochainVersion.Redux:
                    {
                        if (WebSocket.State == WebSocketState.Open)
                        {
                            await WebSocket.SendRawDataAsync(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(
                                new
                                {
                                    jsonrpc = "2.0",
                                    id,
                                    method = "call",
                                    @params = new { instance_id = instanceId, zome, function, args = paramsObject }
                                }
                            )));
                        }
                    }
                    break;

                case HolochainVersion.RSM:
                    {
                        MessagePackFormatter formatter = new MessagePackFormatter();
                        HoloNETData holoNETData = new HoloNETData()
                        {
                            type = "zome_call",
                            data = new HoloNETDataZomeCall()
                            {
                                cell_id = new byte[2][] { Encoding.UTF8.GetBytes(Config.HoloHash), Encoding.UTF8.GetBytes(Config.AgentPubKey) },
                                fn_name = function,
                                zome_name = zome,
                                //payload = MessagePackSerializer.Serialize(paramsObject),
                                // payload = formatter.Serialize(paramsObject),
                                //payload = formatter.Serialize(new Temp() {number = 10 }),
                                //payload = MessagePackSerializer.Serialize(new Temp() { Name = "blah", Desc = "moooooo!" }),
                                payload = MessagePackSerializer.Serialize(new Temp() { number = 10 }),
                                //payload = formatter.Serialize(new Temp() { Name = "blah", Desc = "moooooo!" }),
                                provenance = Encoding.UTF8.GetBytes(Config.AgentPubKey),
                                cap = null
                            }
                        };

                        HoloNETRequest request = new HoloNETRequest()
                        {
                            id = Convert.ToUInt64(id),
                            //type = "\"Request\"",
                            type = "Request",
                            data = MessagePackSerializer.Serialize(holoNETData)
                            //data = formatter.Serialize(holoNETData)
                        };

                        // await webSocket2.Send(formatter.Serialize(request));
                        //await webSocket2.Send(MessagePackSerializer.Serialize(request));

                        //await WebSocket.SendRawDataAsync(formatter.Serialize(request));

                        if (WebSocket.State == WebSocketState.Open)
                            await WebSocket.SendRawDataAsync(MessagePackSerializer.Serialize(request));
                    }
                    break;
            }

            Logger.Log("CallZomeFunctionAsync EXIT", LogType.Debug);
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


        public async Task GetHolochainInstancesAsync(bool cachReturnData = false)
        {
            _currentId++;
            //return await GetHolochainInstancesAsync(_currentId.ToString(), cachReturnData);
            await GetHolochainInstancesAsync(_currentId.ToString(), cachReturnData);
        }

        public async Task GetHolochainInstancesAsync(string id, bool cachReturnData = false)
        {
            //TODO: Are there other admin functions we can wrap around?

            Logger.Log("GetHolochainInstances ENTER", LogType.Debug);

            if (cachReturnData && _instancesCache != null)
            {
                OnGetInstancesCallBack(this, _instancesCache);
                Logger.Log("Caching Enabled so returning data from cach...", LogType.Warn);
                Logger.Log(string.Concat("Id: ", _instancesCache.Id, ", Instances: ", string.Join(", ", _instancesCache.Instances), ", Is Call Successful: ", _instancesCache.IsCallSuccessful ? "True" : "False", ", JSON Raw Data: ", _instancesCache.RawJSONData), LogType.Info);
            }

            _cachInstancesReturnData = cachReturnData;

            await WebSocket.SendRawDataAsync(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(
                new
                {
                    jsonrpc = "2.0",
                    id,
                    method = "info/instances"
                }
            )));

            Logger.Log("GetHolochainInstances EXIT", LogType.Debug);
        }

        private string GetItemFromCache(string id, Dictionary<string, string> cache)
        {
            return cache.ContainsKey(id) ? cache[id] : null;
        }

        private void HandleError(string message, Exception exception)
        {
            message = string.Concat(message, "\nError Details: ", exception != null ? exception.ToString() : "");
            Logger.Log(message, LogType.Error);

            OnError?.Invoke(this, new HoloNETErrorEventArgs { EndPoint = WebSocket.EndPoint, Reason = message, ErrorDetails = exception });

            switch (Config.ErrorHandlingBehaviour)
            {
                case ErrorHandlingBehaviour.AlwaysThrowExceptionOnError:
                    throw new HoloNETException(message, exception, WebSocket.EndPoint);

                case ErrorHandlingBehaviour.OnlyThrowExceptionIfNoErrorHandlerSubscribedToOnErrorEvent:
                    {
                        if (OnError == null)
                            throw new HoloNETException(message, exception, WebSocket.EndPoint);
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