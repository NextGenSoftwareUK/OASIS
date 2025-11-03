using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.ErrorHandling;
using NextGenSoftware.Logging;
using NextGenSoftware.Logging.Interfaces;
using NextGenSoftware.Utilities;
using NextGenSoftware.WebSocket.Interfaces;

namespace NextGenSoftware.WebSocket
{
    public class WebSocket// : IWebSocket
    {
        private Thread _backgroundThread;
        private bool _connecting = false;
        private bool _disconnecting = false;
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private CancellationToken _cancellationToken;
        private WebSocketConfig _config = null;

        //Events
        public delegate void Connected(object sender, ConnectedEventArgs e);
        public event Connected OnConnected;

        public delegate void Disconnected(object sender, DisconnectedEventArgs e);
        public event Disconnected OnDisconnected;

        public delegate void DataSent(object sender, DataSentEventArgs e);
        public event DataSent OnDataSent;

        public delegate void DataReceived(object sender, DataReceivedEventArgs e);
        public event DataReceived OnDataReceived;

        public delegate void Error(object sender, WebSocketErrorEventArgs e);
        public event Error OnError;

        // Properties
        public Uri EndPoint { get; set; }
        //public ClientWebSocket ClientWebSocket { get; set; } // Original HoloNET WebSocket (still works):
        
        public UnityWebSocket UnityWebSocket { get; private set; } //Temporily using UnityWebSocket code until can find out why not working with RSM Conductor...

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
                //if (ClientWebSocket != null)
                //    return ClientWebSocket.State;
                //else
                //    return WebSocketState.Closed;

                if (UnityWebSocket != null)
                    return UnityWebSocket.ClientWebSocket.State;
                else
                    return WebSocketState.Closed;
            }
        }

        public ILogger Logger { get; set; } = new Logger();

        public WebSocket(bool logToConsole = true, bool logToFile = true, string releativePathToLogFolder = "Logs", string logFileName = "NextGenSoftwareWebSocket.log", int maxLogFileSize = 1000000, ILogger logger = null, bool insertExtraNewLineAfterLogMessage = false, int indentLogMessagesBy = 1, bool showColouredLogs = true, ConsoleColor debugColour = ConsoleColor.White, ConsoleColor infoColour = ConsoleColor.Green, ConsoleColor warningColour = ConsoleColor.Yellow, ConsoleColor errorColour = ConsoleColor.Red)
        {
            InitLogger(logger);
            Logger.AddLogProvider(new DefaultLogProvider(logToConsole, logToFile, releativePathToLogFolder, logFileName, maxLogFileSize, insertExtraNewLineAfterLogMessage, indentLogMessagesBy, showColouredLogs, debugColour, infoColour, warningColour, errorColour));
            Init();
        }

        public WebSocket(IEnumerable<ILogProvider> logProviders, bool alsoUseDefaultLogger = false)
        {
            InitLogger();
            Logger.AddLogProviders(logProviders);

            if (alsoUseDefaultLogger)
                Logger.AddLogProvider(new DefaultLogProvider());

            Init();
        }

        public WebSocket(ILogProvider logProvider, bool alsoUseDefaultLogger = false)
        {
            InitLogger();
            Logger.AddLogProvider(logProvider);

            if (alsoUseDefaultLogger)
                Logger.AddLogProvider(new DefaultLogProvider());

            Init();
        }

        public WebSocket(ILogger logger)
        {
            InitLogger(logger);
            Init();
        }

        private void InitLogger(ILogger logger = null)
        {
            if (logger != null)
                Logger = logger;

            else if (Logger == null)
                Logger = new Logger();
        }

        //public void RecycleWebSocket()
        //{
        //    ClientWebSocket = new ClientWebSocket();
        //}

        private void Init()
        {
            try
            {
                if (Logger.LogProviders.Count == 0)
                    Logger.AddLogProvider(new DefaultLogProvider());

               // ClientWebSocket = new ClientWebSocket(); // The original built-in HoloNET WebSocket
               // ClientWebSocket.Options.KeepAliveInterval = TimeSpan.FromSeconds(Config.KeepAliveSeconds);

                _cancellationToken = _cancellationTokenSource.Token; //TODO: do something with this!
            }
            catch (Exception ex)
            {
                HandleError("Error occurred in WebSocket.Init method.", ex);
            }
        }


        private void UnityWebSocket_OnMessage(byte[] data)
        {
            //OnDataReceived?.Invoke(this, new DataReceivedEventArgs("1", EndPoint, true, data, null, null));
            OnDataReceived?.Invoke(this, new DataReceivedEventArgs()
            {
                EndPoint = EndPoint,
                RawBinaryData = data
            });
        }

        private void UnityWebSocket_OnError(string errorMsg)
        {
            OnError?.Invoke(this, new WebSocketErrorEventArgs() { EndPoint = EndPoint, Reason = errorMsg });
        }

        private void UnityWebSocket_OnClose(WebSocketCloseCode closeCode)
        {
            OnDisconnected?.Invoke(this, new DisconnectedEventArgs() { EndPoint = EndPoint, Reason = closeCode.ToString() });
        }

        private void UnityWebSocket_OnOpen()
        {
            OnConnected?.Invoke(this, new ConnectedEventArgs { EndPoint = EndPoint });
        }


        public async Task ConnectAsync(Uri endpoint)
        {
            if (UnityWebSocket == null)
            {
                UnityWebSocket = new UnityWebSocket(endpoint.AbsoluteUri); //The Unity Web Socket code I ported wraps around the ClientWebSocket.
                UnityWebSocket.OnOpen += UnityWebSocket_OnOpen;
                UnityWebSocket.OnClose += UnityWebSocket_OnClose;

                UnityWebSocket.OnError += UnityWebSocket_OnError;
                UnityWebSocket.OnMessage += UnityWebSocket_OnMessage;
            }

            await UnityWebSocket.Connect();
            // await UnityWebSocket.Receive();
        }

        public async Task DisconnectAsync()
        {
            if (UnityWebSocket != null)
            {
                UnityWebSocket.Close();
            }
        }

        /// <summary>
        /// Connects to the specefied Endpoint.
        /// </summary>
        /// <param name="endpoint"></param>
        ///// <returns></returns>
        //public async Task ConnectAsync(string endpoint)
        //{
        //    await ConnectAsync(new Uri(endpoint));
        //}


        ///// <summary>
        ///// Connects to the specefied Endpoint.
        ///// </summary>
        ///// <param name="endpoint"></param>
        ///// <returns></returns>
        //public async Task ConnectAsync(Uri endpoint)
        //{
        //    try
        //    {
        //        if (Logger.LogProviders.Count == 0)
        //            throw new WebSocketException("ERROR: No LogProvider Has Been Specified! Please set a LogProvider with the Logger.AddLogProvider method.");

        //        this.EndPoint = endpoint;

        //        //if (ClientWebSocket == null)
        //        //{
        //            ClientWebSocket = new ClientWebSocket(); // The original built-in HoloNET WebSocket
        //            ClientWebSocket.Options.KeepAliveInterval = TimeSpan.FromSeconds(Config.KeepAliveSeconds);
        //        //}

        //        if (ClientWebSocket.State != WebSocketState.Connecting && ClientWebSocket.State != WebSocketState.Open && ClientWebSocket.State != WebSocketState.Aborted)
        //        {
        //            Logger.Log(string.Concat("Connecting to ", EndPoint, "..."), LogType.Info, true);

        //            _connecting = true;
        //            await ClientWebSocket.ConnectAsync(EndPoint, CancellationToken.None);
        //            //NetworkServiceProvider.Connect(new Uri(EndPoint));
        //            //TODO: need to be able to await this.

        //            //if (NetworkServiceProvider.NetSocketState == NetSocketState.Open)
        //            if (ClientWebSocket.State == WebSocketState.Open)
        //            {
        //                Logger.Log(string.Concat("Connected to ", EndPoint.AbsoluteUri), LogType.Info);
        //                OnConnected?.Invoke(this, new ConnectedEventArgs { EndPoint = EndPoint });
        //                _connecting = false;

        //                //_backgroundThread = new Thread(new ThreadStart(StartListenAsync));
        //                //_backgroundThread.Start();

        //                //await StartListenAsync();
        //                StartListenAsync();
        //            }
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        HandleError(string.Concat("Error occurred in WebSocket.Connect method connecting to ", EndPoint), e);
        //    }
        //}

        //public async Task DisconnectAsync()
        //{
        //    try
        //    {
        //        if (Logger.LogProviders.Count == 0)
        //            throw new WebSocketException("ERROR: No LogProvider Has Been Specified! Please set a LogProvider with the Logger.AddLogProvider method.");

        //        //if (UnityWebSocket.ClientWebSocket != null && UnityWebSocket.ClientWebSocket.State != WebSocketState.Connecting && UnityWebSocket.ClientWebSocket.State != WebSocketState.Closed && UnityWebSocket.ClientWebSocket.State != WebSocketState.Aborted && UnityWebSocket.ClientWebSocket.State != WebSocketState.CloseSent && UnityWebSocket.ClientWebSocket.State != WebSocketState.CloseReceived)
        //        //{
        //        //    Logger.Log(string.Concat("Disconnecting from ", EndPoint, "..."), LogType.Info, true);
        //        //    await UnityWebSocket.ClientWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client manually disconnected.", CancellationToken.None);

        //        //    if (UnityWebSocket.ClientWebSocket.State == WebSocketState.Closed)
        //        //    {
        //        //        Logger.Log(string.Concat("Disconnected from ", EndPoint), LogType.Info);
        //        //        OnDisconnected?.Invoke(this, new DisconnectedEventArgs { EndPoint = EndPoint, Reason = "Disconnected Method Called." });
        //        //    }
        //        //}

        //        if (ClientWebSocket != null && ClientWebSocket.State != WebSocketState.Connecting && ClientWebSocket.State != WebSocketState.Closed && ClientWebSocket.State != WebSocketState.Aborted && ClientWebSocket.State != WebSocketState.CloseSent && ClientWebSocket.State != WebSocketState.CloseReceived)
        //        {
        //            Logger.Log(string.Concat("Disconnecting from ", EndPoint, "..."), LogType.Info, false);
        //            _disconnecting = true;

        //            try
        //            {
        //                await ClientWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client manually disconnected.", CancellationToken.None);
        //            }
        //            catch (Exception ex)
        //            {

        //            }

        //            //Wait till the thread has terminated.
        //            //_backgroundThread?.Join();

        //            if (ClientWebSocket.State == WebSocketState.Closed)
        //            {
        //                _disconnecting = false;
        //                Logger.Log(string.Concat("Disconnected from ", EndPoint), LogType.Info);

        //                if (!_connecting && ClientWebSocket != null)
        //                {
        //                    ClientWebSocket.Dispose();
        //                    ClientWebSocket = null;
        //                }

        //                OnDisconnected?.Invoke(this, new DisconnectedEventArgs { EndPoint = EndPoint, Reason = "Disconnected Method Called." });
        //            }
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        HandleError(string.Concat("Error occurred in WebSocket.Disconnect disconnecting from ", EndPoint), e);
        //    }
        //    finally
        //    {
        //        if (!_connecting && ClientWebSocket != null)
        //        {
        //            ClientWebSocket.Dispose();
        //            ClientWebSocket = null;
        //        }
        //    }
        //}


        public async Task SendRawDataAsync(byte[] data)
        {
            try
            {
                string bytesDecoded = DataHelper.DecodeBinaryDataAsUTF8(data);
                string bytesAsString = DataHelper.ConvertBinaryDataToString(data);

                Logger.Log($"Sending Raw Data...", LogType.Info);
                Logger.Log($"Bytes: {bytesDecoded} ({bytesAsString})", LogType.Debug);

                await UnityWebSocket.Send(data);

                /*
                // Original HoloNET code (still works):
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

                    Logger.Log(string.Concat("Sending Data Packet ", (i + 1), " of ", messagesCount, "..."), LogType.Debug, true);
                    //await ClientWebSocket.SendAsync(new ArraySegment<byte>(data, offset, count), WebSocketMessageType.Text, lastMessage, _cancellationToken);
                    await ClientWebSocket.SendAsync(new ArraySegment<byte>(data, offset, count), WebSocketMessageType.Binary, lastMessage, _cancellationToken);
                }
                */

                //OnDataSent?.Invoke(this, new DataSentEventArgs { IsCallSuccessful = true, EndPoint = EndPoint,  RawBinaryData = data, RawBinaryDataAsString = bytesAsString, RawBinaryDataDecoded = bytesDecoded });
                OnDataSent?.Invoke(this, new DataSentEventArgs { EndPoint = EndPoint, RawBinaryData = data, RawBinaryDataAsString = bytesAsString, RawBinaryDataDecoded = bytesDecoded });
                Logger.Log("Sending Raw Data... Done!", LogType.Info);
            }
            catch (Exception ex)
            {
                HandleError("Error occurred in WebSocket.SendRawDataAsync method.", ex);
            }
        }

        /*
        private async Task StartListenAsync()
        //private void StartListenAsync()
        {
           // Task.Run(async () =>
           // {
                var buffer = new byte[Config.ReceiveChunkSize];
                Logger.Log(string.Concat("Listening on ", EndPoint, "..."), LogType.Info, true);

                try
                {
                    while (ClientWebSocket != null && ClientWebSocket.State == WebSocketState.Open)
                    {
                        var stringResult = new StringBuilder();
                        List<byte> dataResponse = new List<byte>();

                        WebSocketReceiveResult result;
                        do
                        {
                            if (ClientWebSocket.State != WebSocketState.Open)
                                break;

                            if (Config.NeverTimeOut)
                                result = await ClientWebSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                            else
                            {
                                using (var cts = new CancellationTokenSource((Config.TimeOutSeconds) * 1000))
                                    result = await ClientWebSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cts.Token);
                            }

                            if (result.MessageType == WebSocketMessageType.Close)
                            {
                                if (!_disconnecting && ClientWebSocket != null)
                                {
                                    string msg = "Closing because received close message."; //TODO: Move all strings to constants at top or resources.strings
                                    await ClientWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, msg, CancellationToken.None);
                                    OnDisconnected?.Invoke(this, new DisconnectedEventArgs { EndPoint = EndPoint, Reason = msg });
                                    Logger.Log(msg, LogType.Info);

                                    //AttemptReconnect(); //TODO: Not sure re-connect here?
                                }
                            }
                            else
                            {
                                stringResult.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
                                Logger.Log(string.Concat("Received Data: ", stringResult), LogType.Debug);
                                OnDataReceived?.Invoke(this, new DataReceivedEventArgs()
                                {
                                    EndPoint = EndPoint,
                                    //IsCallSuccessful = true,
                                    RawBinaryData = buffer,
                                    RawBinaryDataAsString = DataHelper.ConvertBinaryDataToString(buffer),
                                    RawBinaryDataDecoded = DataHelper.DecodeBinaryDataAsUTF8(buffer),
                                    RawJSONData = stringResult.ToString(),
                                    WebSocketResult = result
                                });
                            }
                        } while (!result.EndOfMessage);
                    }
                }


                catch (TaskCanceledException ex)
                {
                    if (!_connecting && !_disconnecting)
                    {
                        string msg = string.Concat("Error occurred in WebSocket.StartListen method. Connection timed out after ", (Config.TimeOutSeconds), " seconds.");
                        OnDisconnected?.Invoke(this, new DisconnectedEventArgs { EndPoint = EndPoint, Reason = msg });
                        HandleError(msg, ex);
                        await AttemptReconnectAsync();
                    }
                }

                catch (Exception ex)
                {
                    if (!_connecting && !_disconnecting)
                    {
                        OnDisconnected?.Invoke(this, new DisconnectedEventArgs { EndPoint = EndPoint, Reason = string.Concat("Error occurred: ", ex) });
                        HandleError("Error occurred in WebSocket.StartListen method. Disconnected because an error occurred.", ex);
                        await AttemptReconnectAsync();
                    }
                }

                finally
                {
                    //ClientWebSocket.Dispose();
                }
           // }).Wait();
        }

        private async Task AttemptReconnectAsync()
        {
            try
            {
                for (int i = 0; i < (Config.ReconnectionAttempts); i++)
                {
                    if (ClientWebSocket != null && ClientWebSocket.State == WebSocketState.Open)
                        break;

                    Logger.Log(string.Concat("Attempting to reconnect... Attempt ", +i), LogType.Info, true);
                    await ConnectAsync(EndPoint);
                    await Task.Delay(Config.ReconnectionIntervalSeconds);
                }
            }
            catch (Exception ex)
            {
                HandleError("Error occurred in WebSocket.AttemptReconnect method.", ex);
            }
        }*/

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