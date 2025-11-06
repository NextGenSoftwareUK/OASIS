//using System;
//using System.Net.WebSockets;
//using System.Threading.Tasks;
//using NextGenSoftware.Logging;
//using NextGenSoftware.Logging.Interfaces;

////using NextGenSoftware.OASIS.API.Core.Enums;
//using NextGenSoftware.WebSocket;

//namespace NextGenSoftware.OASIS.API.Providers.OrionProtocolOASIS
//{
//    public class OrionProtocolOASIS
//    {
//        private OrionProtocolConfig _config = null;

//        public delegate void Connected(object sender, ConnectedEventArgs e);
//        public event Connected OnConnected;

//        public delegate void Disconnected(object sender, DisconnectedEventArgs e);
//        public event Disconnected OnDisconnected;

//        public delegate void DataReceived(object sender, DataReceivedEventArgs e);
//        public event DataReceived OnDataReceived;

//        public delegate void Error(object sender, WebSocketErrorEventArgs e);
//        public event Error OnError;

//        public WebSocket.WebSocket WebSocket { get; set; }

//        public OrionProtocolConfig Config
//        {
//            get
//            {
//                if (_config == null)
//                    _config = new OrionProtocolConfig();

//                return _config;
//            }
//        }

//        public WebSocketState State
//        {
//            get
//            {
//                return WebSocket.State;
//            }
//        }

//        public string EndPoint
//        {
//            get
//            {
//                if (WebSocket != null)
//                    return WebSocket.EndPoint;

//                return "";
//            }
//        }

//        public ILogger Logger { get; set; }

//        public OrionProtocolOASIS(string hostURI, ILogger logger)
//        {
//            Logger = logger;
//            WebSocket = new WebSocket.WebSocket(hostURI, logger);

//            WebSocket.OnConnected += WebSocket_OnConnected;
//            WebSocket.OnDataReceived += WebSocket_OnDataReceived;
//            WebSocket.OnDisconnected += WebSocket_OnDisconnected;
//            WebSocket.OnError += WebSocket_OnError;
//        }

//        private void WebSocket_OnError(object sender, WebSocketErrorEventArgs e)
//        {
//            OnError?.Invoke(this, e);
//        }

//        private void WebSocket_OnDisconnected(object sender, DisconnectedEventArgs e)
//        {
//            OnDisconnected?.Invoke(this, e);
//        }

//        private void WebSocket_OnDataReceived(object sender, DataReceivedEventArgs e)
//        {
//            OnDataReceived?.Invoke(this, e);
//        }

//        private void WebSocket_OnConnected(object sender, ConnectedEventArgs e)
//        {
//            OnConnected?.Invoke(this, e);
//        }

//        public async Task Connect()
//        {
//            try
//            {
//                if (Logger == null)
//                    throw new WebSocket.WebSocketException("ERROR: No Logger Has Been Specified! Please set a Logger with the Logger Property.");

//                if (WebSocket.State != WebSocketState.Connecting && WebSocket.State != WebSocketState.Open && WebSocket.State != WebSocketState.Aborted)
//                {
//                    Logger.Log(string.Concat("Connecting to ", WebSocket.EndPoint, "..."), LogType.Info);
//                    await WebSocket.Connect();
//                }
//            }
//            catch (Exception e)
//            {
//                HandleError(string.Concat("Error occured connecting to ", WebSocket.EndPoint), e);
//            }
//        }

//        public async Task Disconnect()
//        {
//            await WebSocket.Disconnect();
//        }

//        private void HandleError(string message, Exception exception)
//        {
//            message = string.Concat(message, "\nError Details: ", exception != null ? exception.ToString() : "");
//            Logger.Log(message, Logging.LogType.Error);

//            OnError?.Invoke(this, new WebSocketErrorEventArgs { EndPoint = WebSocket.EndPoint, Reason = message, ErrorDetails = exception });

//            switch (Config.ErrorHandlingBehaviour)
//            {
//                case Logging.ErrorHandlingBehaviour.AlwaysThrowExceptionOnError:
//                    throw new WebSocket.WebSocketException(message, exception, WebSocket.EndPoint);

//                case Logging.ErrorHandlingBehaviour.OnlyThrowExceptionIfNoErrorHandlerSubscribedToOnErrorEvent:
//                    {
//                        if (OnError == null)
//                            throw new WebSocket.WebSocketException(message, exception, WebSocket.EndPoint);
//                    }
//                    break;
//            }
//        }
//    }
//}