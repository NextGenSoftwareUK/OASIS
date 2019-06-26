using System;
using System.Text;
using System.Threading;
using System.Net.WebSockets;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

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

        public delegate void GetInstancesCallBack(object sender, GetInstancesCallBackEventArgs e);
        public event GetInstancesCallBack OnGetInstancesCallBack;

        public delegate void Error(object sender, ErrorEventArgs e);
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

        public HoloNETClientBase(string holochainURI)
        {
            WebSocket = new ClientWebSocket();
            WebSocket.Options.KeepAliveInterval = TimeSpan.FromSeconds(Config.KeepAliveSeconds == 0 ? KeepAliveSecondsDefault : Config.KeepAliveSeconds);
            EndPoint = holochainURI;
            Config.ErrorHandlingBehaviour = ErrorHandlingBehaviour.OnlyThrowExceptionIfNoErrorHandlerSubscribedToOnErrorEvent;

            _cancellationToken = _cancellationTokenSource.Token; //TODO: do something with this!
        }

        public async Task Connect()
        {
            try
            {
                if (Logger == null)
                    throw new HoloNETException("ERROR: No Logger Has Been Specified! Please set a Logger with the Logger Property.");

                Logger.Log(string.Concat("Connecting to ", EndPoint, "..."), LogType.Info);

                await WebSocket.ConnectAsync(new Uri(EndPoint), CancellationToken.None);
                //NetworkServiceProvider.Connect(new Uri(EndPoint));
                //TODO: need to be able to await this.

                //if (NetworkServiceProvider.NetSocketState == NetSocketState.Open)
                if (WebSocket.State == WebSocketState.Open)
                {
                    Logger.Log(string.Concat("Connected to ", EndPoint), LogType.Info);
                    OnConnected?.Invoke(this, new ConnectedEventArgs(EndPoint));
                    StartListen();
                }
            }
            catch (Exception e)
            {
                HandleError(string.Concat("Error occured connecting to ", EndPoint), e);
            }
        }

        public async Task CallZomeFunctionAsync(string instanceId, string zome, string function, object paramsObject, bool cachReturnData = false)
        {
            _currentId++;
            await CallZomeFunctionAsync(_currentId.ToString(), instanceId, zome, function, null, paramsObject, true, cachReturnData);
        }

        public async Task CallZomeFunctionAsync(string id, string instanceId, string zome, string function, object paramsObject, bool matchIdToInstanceZomeFuncInCallback = true, bool cachReturnData = false)
        {
            _currentId++;
            await CallZomeFunctionAsync(id, instanceId, zome, function, null, paramsObject, matchIdToInstanceZomeFuncInCallback, cachReturnData);
        }

        public async Task CallZomeFunctionAsync(string instanceId, string zome, string function, ZomeFunctionCallBack callback, object paramsObject, bool cachReturnData = false)
        {
            _currentId ++;
            await CallZomeFunctionAsync(_currentId.ToString(), instanceId, zome, function, callback, paramsObject, true, cachReturnData);
        }

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

            await SendMessageAsync(JsonConvert.SerializeObject(
                new
                {
                    jsonrpc = "2.0",
                    id,
                    method = "call",
                    @params = new { instance_id = instanceId, zome, function, @params = paramsObject }
                }
            ));

            Logger.Log("CallZomeFunctionAsync EXIT", LogType.Debug);
        }

        public async Task SendMessageAsync(string message)
        {
            Logger.Log(string.Concat("Sending Message: ", message), LogType.Info);

            if (WebSocket.State != WebSocketState.Open)
            {
                string msg = "Connection is not open!";
                HandleError(msg, new InvalidOperationException(msg));
            }

            int sendChunkSize = Config.SendChunkSize == 0 ? SendChunkSizeDefault : Config.SendChunkSize;
            var messageBuffer = Encoding.UTF8.GetBytes(message);
            var messagesCount = (int)Math.Ceiling((double)messageBuffer.Length / sendChunkSize);

            for (var i = 0; i < messagesCount; i++)
            {
                var offset = (sendChunkSize * i);
                var count = sendChunkSize;
                var lastMessage = ((i + 1) == messagesCount);

                if ((count * (i + 1)) > messageBuffer.Length)
                    count = messageBuffer.Length - offset;

                Logger.Log(string.Concat("Sending Message ", i, " of ", messagesCount, "..."), LogType.Debug);
                await WebSocket.SendAsync(new ArraySegment<byte>(messageBuffer, offset, count), WebSocketMessageType.Text, lastMessage, _cancellationToken);
            }

            Logger.Log("Message Sent", LogType.Info);
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
                            OnDisconnected?.Invoke(this, new DisconnectedEventArgs(this.EndPoint, msg));
                            Logger.Log(msg, LogType.Info);

                            //AttemptReconnect(); //TODO: Not sure re-connect here?
                        }
                        else
                        {
                            var str = Encoding.UTF8.GetString(buffer, 0, result.Count);
                            stringResult.Append(str);
                        }

                    } while (!result.EndOfMessage);

                    string rawData = stringResult.ToString();
                    Logger.Log(string.Concat("Received Data: ", rawData), LogType.Info);
                    OnDataReceived?.Invoke(this, new DataReceivedEventArgs(EndPoint, rawData, result));

                    JObject data = JObject.Parse(rawData);
                    string id = data["id"].ToString();

                    if (data.ContainsKey("signal"))
                    {
                        Logger.Log(string.Concat("Signals data detected. Id: ", id, ", Raw JSON Data: ", rawData), LogType.Info);
                        OnSignalsCallBack?.Invoke(this, new SignalsCallBackEventArgs(id, EndPoint, true, rawData, result));

                        //TODO: Handle Signals here when hc fully implement them...
                    }
                    else if (data.SelectToken("result[0].agent") != null)
                    {
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
                        string rawZomeReturnData = data["result"].ToString();
                        string zomeReturnData = string.Empty;
                        bool isZomeCallSuccessful = false;

                        if (rawZomeReturnData.Substring(2, 2).ToUpper() == "OK")
                        {
                            isZomeCallSuccessful = true;
                            zomeReturnData = rawZomeReturnData.Substring(7, rawZomeReturnData.Length - 10);
                        }
                        //else if (rawZomeReturnData.Substring(0,3).ToUpper() == "ERR")
                        else
                            zomeReturnData = rawZomeReturnData.Substring(8, rawZomeReturnData.Length - 11);

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
                }
            }

            catch (TaskCanceledException ex)
            {
                string msg = string.Concat("Connection timed out after ", (Config.TimeOutSeconds == 0 ? TimeOutSecondsDefault : Config.TimeOutSeconds), " seconds.");
                OnDisconnected?.Invoke(this, new DisconnectedEventArgs(this.EndPoint, msg));
                HandleError(msg, ex);
                await AttemptReconnect();
            }

            catch (Exception ex)
            {
                OnDisconnected?.Invoke(this, new DisconnectedEventArgs(this.EndPoint, string.Concat("Error occured: ", ex)));
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

            await SendMessageAsync(JsonConvert.SerializeObject(
                new
                {
                    jsonrpc = "2.0",
                    id,
                    method = "info/instances"
                }
            ));

            //return await _taskCompletionSourceGetInstance.Task;

            Logger.Log("GetHolochainInstances EXIT", LogType.Debug);
        }

        private async Task AttemptReconnect()
        {
            for (int i = 0; i < (Config.ReconnectionAttempts == 0 ? ReconnectionAttemptsDefault : Config.ReconnectionAttempts); i++)
            {
                Logger.Log(string.Concat("Attempting to reconnect... Attempt ", +i), LogType.Info);
                await Connect();

                if (WebSocket.State == WebSocketState.Open)
                    break;

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

            OnError?.Invoke(this, new ErrorEventArgs(this.EndPoint, message, exception));

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
    }
}
