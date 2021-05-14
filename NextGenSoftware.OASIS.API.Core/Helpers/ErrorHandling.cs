using System;
using NextGenSoftware.OASIS.API.Core.Managers;

namespace NextGenSoftware.OASIS.API.Core.Helpers
{
    public static class ErrorHandling
    {
        public static bool ShowStackTrace { get; set; } = false;
        public static bool ThrowExceptionsOnErrors { get; set; } = false;
        public static bool ThrowExceptionsOnWarnings { get; set; } = false;

        public static void HandleError<T>(ref OASISResult<T> result, string errorMessage, bool includeStackTrace = false, bool throwException = false)
        {
            //NOTE: If you are throwing an exception then you do not need to show an additional stack trace here because the exception has it already! ;-)
            if (includeStackTrace || ShowStackTrace)
                errorMessage = string.Concat(errorMessage, "StackTrace:\n", Environment.StackTrace);

            result.IsError = true;
            result.Message = errorMessage;
            LoggingManager.Log(errorMessage, Enums.LogType.Error);

            if (throwException || ThrowExceptionsOnErrors)
                throw new Exception(errorMessage);
        }

        public static void HandleWarning<T>(ref OASISResult<T> result, string message, bool includeStackTrace = false, bool throwException = false)
        {
            //NOTE: If you are throwing an exception then you do not need to show an additional stack trace here because the exception has it already! ;-)
            if (includeStackTrace || ShowStackTrace)
                message = string.Concat(message, "StackTrace:\n", Environment.StackTrace);

            result.IsWarning = true;
            result.Message = message;
            LoggingManager.Log(message, Enums.LogType.Warn);

            if (throwException || ThrowExceptionsOnWarnings)
                throw new Exception(message);
        }
    }
}