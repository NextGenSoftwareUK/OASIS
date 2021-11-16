using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NextGenSoftware.WebSocket
{
    public class WebSocket
    {
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private readonly CancellationToken _cancellationToken;
        private WebSocketConfig _config = null;

        //Events
        public delegate void Connected(object sender, ConnectedEventArgs e);
        public event Connected OnConnected;

        public delegate void Disconnected(object sender, DisconnectedEventArgs e);
        public event Disconnected OnDisconnected;

        public delegate void DataReceived(object sender, DataReceivedEventArgs e);
        public event DataReceived OnDataReceived;

        public delegate void Error(object sender, WebSocketErrorEventArgs e);
        public event Error OnError;

        // Properties
        public string EndPoint { get; private set; }
        public ClientWebSocket ClientWebSocket { get; private set; }
        public UnityWebSocket UnityWebSocket { get; private set; }
        public WebSocketConfig Config
        {
            get
            {
                if (_config == null)
                    _config = new WebSocketConfig();

                return _config;
            }
            set
            {
                _config = value;
            }
        }

        public WebSocketState State
        {
            get
            {
                //return ClientWebSocket.State;
                return UnityWebSocket.ClientWebSocket.State;
            }
        }

        //public WebSocketState2 State2
        //{
        //    get
        //    {
        //        return UnityWebSocket.State;
        //    }
        //}

         public ILogger Logger { get; set; }

        //public IWebSocketClientNET NetworkServiceProvider { get; set; }
        //public NetworkServiceProviderMode NetworkServiceProviderMode { get; set; }
        // public UnityWebSocket UnityWebSocket;

        public WebSocket(string endPointURI, ILogger logger)
        {
            Logger = logger;
            // ClientWebSocket = new ClientWebSocket();
            // ClientWebSocket.Options.KeepAliveInterval = TimeSpan.FromSeconds(Config.KeepAliveSeconds);
            
            UnityWebSocket = new UnityWebSocket(endPointURI);
            UnityWebSocket.OnOpen += UnityWebSocket_OnOpen;
            UnityWebSocket.OnClose += UnityWebSocket_OnClose;
            UnityWebSocket.OnError += UnityWebSocket_OnError;
            UnityWebSocket.OnMessage += UnityWebSocket_OnMessage;

            EndPoint = endPointURI;

            _cancellationToken = _cancellationTokenSource.Token; //TODO: do something with this!

            //UnityWebSocket = new UnityWebSocket(endPointURI);
            //UnityWebSocket.OnOpen += WebSocket2_OnOpen;
            //UnityWebSocket.OnClose += WebSocket2_OnClose;
            //UnityWebSocket.OnError += WebSocket2_OnError;
            //UnityWebSocket.OnMessage += WebSocket2_OnMessage;
        }

        private void UnityWebSocket_OnMessage(byte[] data)
        {
            //throw new NotImplementedException();
        }

        private void UnityWebSocket_OnError(string errorMsg)
        {
            //throw new NotImplementedException();
        }

        private void UnityWebSocket_OnClose(WebSocketCloseCode closeCode)
        {
          //  throw new NotImplementedException();
        }

        private void UnityWebSocket_OnOpen()
        {
          //  throw new NotImplementedException();
        }

        public async Task Connect()
        {
            await UnityWebSocket.Connect();
           // await UnityWebSocket.Receive();
        }

        //public async Task Connect()
        //{
        //    try
        //    {
        //        if (Logger == null)
        //            throw new WebSocketException("ERROR: No Logger Has Been Specified! Please set a Logger with the Logger Property.");

        //        if (ClientWebSocket.State != WebSocketState.Connecting && ClientWebSocket.State != WebSocketState.Open && ClientWebSocket.State != WebSocketState.Aborted)
        //        {
        //            Logger.Log(string.Concat("Connecting to ", EndPoint, "..."), LogType.Info);

        //            await ClientWebSocket.ConnectAsync(new Uri(EndPoint), CancellationToken.None);
        //            //NetworkServiceProvider.Connect(new Uri(EndPoint));
        //            //TODO: need to be able to await this.

        //            //if (NetworkServiceProvider.NetSocketState == NetSocketState.Open)
        //            if (ClientWebSocket.State == WebSocketState.Open)
        //            {
        //                Logger.Log(string.Concat("Connected to ", EndPoint), LogType.Info);
        //                OnConnected?.Invoke(this, new ConnectedEventArgs { EndPoint = EndPoint });
        //                StartListen();
        //            }
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        HandleError(string.Concat("Error occured connecting to ", EndPoint), e);
        //    }
        //}

        public async Task Disconnect()
        {
            try
            {
                if (Logger == null)
                    throw new WebSocketException("ERROR: No Logger Has Been Specified! Please set a Logger with the Logger Property.");

                if (ClientWebSocket != null && ClientWebSocket.State != WebSocketState.Connecting && ClientWebSocket.State != WebSocketState.Closed && ClientWebSocket.State != WebSocketState.Aborted && ClientWebSocket.State != WebSocketState.CloseSent && ClientWebSocket.State != WebSocketState.CloseReceived)
                {
                    Logger.Log(string.Concat("Disconnecting from ", EndPoint, "..."), LogType.Info);
                    await ClientWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client manually disconnected.", CancellationToken.None);

                    if (ClientWebSocket.State == WebSocketState.Closed)
                    {
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

        public async Task SendRawDataAsync(byte[] data)
        {
            Logger.Log("Sending Raw Data...", LogType.Info);
            await UnityWebSocket.Send(data);

            /*
            if (ClientWebSocket.State != WebSocketState.Open)
            {
                string msg = "Connection is not open!";
                HandleError(msg, new InvalidOperationException(msg));
            }

            int sendChunkSize = Config.SendChunkSize;
            var messagesCount = (int)Math.Ceiling((double)data.Length / sendChunkSize);

            for (var i = 0; i < messagesCount; i++)
            {
                var offset = (sendChunkSize * i);
                var count = sendChunkSize;
                var lastMessage = ((i + 1) == messagesCount);

                if ((count * (i + 1)) > data.Length)
                    count = data.Length - offset;

                Logger.Log(string.Concat("Sending Data Packet ", i, " of ", messagesCount, "..."), LogType.Debug);
                await ClientWebSocket.SendAsync(new ArraySegment<byte>(data, offset, count), WebSocketMessageType.Text, lastMessage, _cancellationToken);
            }
            */

            Logger.Log("Sending Raw Data... Done!", LogType.Info);
        }

        private async Task StartListen()
        {
            var buffer = new byte[Config.ReceiveChunkSize];
            Logger.Log(string.Concat("Listening on ", EndPoint, "..."), LogType.Info);

            try
            {
                while (ClientWebSocket.State == WebSocketState.Open)
                {
                    var stringResult = new StringBuilder();
                    List<byte> dataResponse = new List<byte>();

                    WebSocketReceiveResult result;
                    do
                    {
                        if (Config.NeverTimeOut)
                            result = await ClientWebSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                        else
                        {
                            using (var cts = new CancellationTokenSource((Config.TimeOutSeconds) * 1000))
                                result = await ClientWebSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cts.Token);
                        }

                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            string msg = "Closing because received close message."; //TODO: Move all strings to constants at top or resources.strings
                            await ClientWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, msg, CancellationToken.None);
                            OnDisconnected?.Invoke(this, new DisconnectedEventArgs { EndPoint = EndPoint, Reason = msg });
                            Logger.Log(msg, LogType.Info);

                            //AttemptReconnect(); //TODO: Not sure re-connect here?
                        }
                        else
                        {
                            stringResult.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));   
                            Logger.Log(string.Concat("Received Data: ", stringResult), LogType.Info);
                            OnDataReceived?.Invoke(this, new DataReceivedEventArgs { EndPoint = EndPoint, RawJSONData = stringResult.ToString(), RawBinaryData = buffer, WebSocketResult = result });
                        }
                    } while (!result.EndOfMessage);
                }
            }

            catch (TaskCanceledException ex)
            {
                string msg = string.Concat("Connection timed out after ", (Config.TimeOutSeconds), " seconds.");
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
                ClientWebSocket.Dispose();
            }
        }

        //TODO: May implement new cache later... :)
        //public void ClearCache()
        //{
        //    //_zomeReturnDataLookup.Clear();
        //    //_cacheZomeReturnDataLookup.Clear();
        //}

        //private string GetItemFromCache(string id, Dictionary<string, string> cache)
        //{
        //    return cache.ContainsKey(id) ? cache[id] : null;
        //}

        private async Task AttemptReconnect()
        {
            for (int i = 0; i < (Config.ReconnectionAttempts); i++)
            {
                if (ClientWebSocket.State == WebSocketState.Open)
                    break;

                Logger.Log(string.Concat("Attempting to reconnect... Attempt ", +i), LogType.Info);
                await Connect();
                await Task.Delay(Config.ReconnectionIntervalSeconds);
            }
        }

        private void HandleError(string message, Exception exception)
        {
            message = string.Concat(message, "\nError Details: ", exception != null ? exception.ToString() : "");
            Logger.Log(message, LogType.Error);

            OnError?.Invoke(this, new WebSocketErrorEventArgs { EndPoint = EndPoint, Reason = message, ErrorDetails = exception });

            switch (Config.ErrorHandlingBehaviour)
            {
                case ErrorHandlingBehaviour.AlwaysThrowExceptionOnError:
                    throw new WebSocketException(message, exception, this.EndPoint);

                case ErrorHandlingBehaviour.OnlyThrowExceptionIfNoErrorHandlerSubscribedToOnErrorEvent:
                    {
                        if (OnError == null)
                            throw new WebSocketException(message, exception, this.EndPoint);
                    }
                    break;
            }
        }
    }
}