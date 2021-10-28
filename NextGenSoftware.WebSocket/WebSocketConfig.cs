
namespace NextGenSoftware.WebSocket
{
    public class WebSocketConfig
    {
        public int TimeOutSeconds { get; set; } = 30;
        public bool NeverTimeOut { get; set; } = false;
        public int KeepAliveSeconds { get; set; } = 30;
        public int ReconnectionAttempts { get; set; } = 5;                                                                                                                                         
        public int ReconnectionIntervalSeconds { get; set; } = 5;
        public int SendChunkSize { get; set; } = 1024;
        public int ReceiveChunkSize { get; set; } = 1024;
        public ErrorHandlingBehaviour ErrorHandlingBehaviour { get; set; } = ErrorHandlingBehaviour.OnlyThrowExceptionIfNoErrorHandlerSubscribedToOnErrorEvent;
    }
}