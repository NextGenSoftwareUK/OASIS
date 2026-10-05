
using NextGenSoftware.ErrorHandling;

namespace NextGenSoftware.WebSocket
{
    public class WebSocketConfig
    {
        /// <summary>
        /// The time in seconds before the connection times out when calling either method `SendHoloNETRequest` or `CalLZomeFunction`. This defaults to 30 seconds.
        /// </summary>
        public int TimeOutSeconds { get; set; } = 999; //30;

        /// <summary>
        /// Set this to true if you wish the connection to never time out when making a call from methods `SendHoloNETRequest` and `CallZomeFunction`. This defaults to false.
        /// </summary>
        public bool NeverTimeOut { get; set; } = false;

        /// <summary>
        /// This is the time to keep the connection alive in seconds. This defaults to 30 seconds.
        /// </summary>
        public int KeepAliveSeconds { get; set; } = 30;

        /// <summary>
        /// The number of times HoloNETClient will attempt to re-connect if the connection is dropped. The default is 5.
        /// </summary>
        public int ReconnectionAttempts { get; set; } = 5;

        /// <summary>
        /// The time to wait between each re-connection attempt. The default is 5 seconds.
        /// </summary>
        public int ReconnectionIntervalSeconds { get; set; } = 5;

        /// <summary>
        /// The size of the buffer to use when sending data to the Holochain Conductor. The default is 1024 bytes.
        /// </summary>
        public int SendChunkSize { get; set; } = 4096; //1024;

        /// <summary>
        /// The size of the buffer to use when receiving data from the Holochain Conductor. The default is 1024 bytes.
        /// </summary>
        public int ReceiveChunkSize { get; set; } = 4096; //1024;

        /// <summary>
        /// An enum that specifies what to do when anm error occurs. The options are: `AlwaysThrowExceptionOnError`, `OnlyThrowExceptionIfNoErrorHandlerSubscribedToOnErrorEvent` & `NeverThrowExceptions`). The default is `OnlyThrowExceptionIfNoErrorHandlerSubscribedToOnErrorEvent` meaning it will only throw an error if the `OnError` event has not been subscribed to. This delegates error handling to the caller. If no event has been subscribed then HoloNETClient will throw an error. `AlwaysThrowExceptionOnError` will always throw an error even if the `OnError` event has been subscribed to. The `NeverThrowException` enum option will never throw an error even if the `OnError` event has not been subscribed to. Regardless of what enum is selected, the error will always be logged using whatever `ILogger`s have been injected into the constructor or set on the static Logging.Loggers property.
        /// </summary>
        //public ErrorHandlingBehaviour ErrorHandlingBehaviour { get; set; } = ErrorHandlingBehaviour.OnlyThrowExceptionIfNoErrorHandlerSubscribedToOnErrorEvent;
        public ErrorHandlingBehaviour ErrorHandlingBehaviour
        {
            get
            {
                return ErrorHandling.ErrorHandling.ErrorHandlingBehaviour;
            }
            set
            {
                ErrorHandling.ErrorHandling.ErrorHandlingBehaviour = value;
            }
        }
    }
}