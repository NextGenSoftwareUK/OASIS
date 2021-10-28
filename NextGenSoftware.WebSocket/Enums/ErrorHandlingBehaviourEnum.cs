
namespace NextGenSoftware.WebSocket
{
    public enum ErrorHandlingBehaviour
    {
        AlwaysThrowExceptionOnError,
        OnlyThrowExceptionIfNoErrorHandlerSubscribedToOnErrorEvent,
        NeverThrowExceptions
    }
}