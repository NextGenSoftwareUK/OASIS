
using NextGenSoftware.WebSocket;

namespace NextGenSoftware.OASIS.API.Providers.OrionProtocolOASIS
{
    public class OrionProtocolConfig
    {
        public ErrorHandlingBehaviour ErrorHandlingBehaviour { get; set; } = ErrorHandlingBehaviour.OnlyThrowExceptionIfNoErrorHandlerSubscribedToOnErrorEvent;
    }
}