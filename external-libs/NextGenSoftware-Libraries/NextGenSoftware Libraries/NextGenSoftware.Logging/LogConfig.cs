
using NextGenSoftware.ErrorHandling;

namespace NextGenSoftware.Logging
{
    public static class LogConfig
    {
        public static LoggingMode FileLoggingMode = LoggingMode.WarningsErrorsInfoAndDebug;
        public static LoggingMode ConsoleLoggingMode = LoggingMode.WarningsErrorsInfoAndDebug;

        public static ErrorHandlingBehaviour ErrorHandlingBehaviour
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