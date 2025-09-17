
using NextGenSoftware.Logging;

namespace NextGenSoftware.OASIS.API.Providers.OrionProtocolOASIS
{
    public class OrionProtocolConfig
    {
        public ErrorHandlingBehaviour ErrorHandlingBehaviour { get; set; } = ErrorHandlingBehaviour.OnlyThrowExceptionIfNoErrorHandlerSubscribedToOnErrorEvent;
    }
}