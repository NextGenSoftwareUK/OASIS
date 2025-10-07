
namespace NextGenSoftware.Holochain.HoloNET.Client.Core
{
    public enum ErrorHandlingBehaviour
    {
        AlwaysThrowExceptionOnError,
        OnlyThrowExceptionIfNoErrorHandlerSubscribedToOnErrorEvent,
        NeverThrowExceptions
    }
}