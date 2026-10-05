
namespace NextGenSoftware.ErrorHandling
{
    public enum WarningHandlingBehaviour
    {
        AlwaysThrowExceptionOnWarning,
        OnlyThrowExceptionIfNoWarningHandlerSubscribedToOnWarningEvent,
        NeverThrowExceptions
    }
}