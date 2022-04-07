using System;
using NextGenSoftware.OASIS.API.Core.Managers;

namespace NextGenSoftware.OASIS.API.Core.Helpers
{
    public static class ErrorHandling
    {
        //These are global overrides to the method calls below.
        //TODO: Need to add these to the OASISDNA so can be confgiured in the config file as well as code.
        public static bool ShowStackTrace { get; set; } = false; //This should ONLY be used in DEBUG/DEV mode.
        public static bool ThrowExceptionsOnErrors { get; set; } = false; //This should ONLY be used in DEBUG/DEV mode.
        public static bool ThrowExceptionsOnWarnings { get; set; } = false; //This should ONLY be used in DEBUG/DEV mode.
        public static bool LogAllErrors { get; set; } = true;
        public static bool LogAllWarnings { get; set; } = true;

        //WARNING: ONLY set includeStackTrace to true for debug/dev mode due to performance overhead. This param should never be needed because the ShowStackTrace flag will be used for Dev/Debug mode. 
        public static void HandleError<T>(ref OASISResult<T> result, string errorMessage, bool log = true, bool includeStackTrace = false, bool throwException = false, bool addToInnerMessages = false, bool incrementErrorCount = true, Exception ex = null, string detailedMessage = "")
        {
            //NOTE: If you are throwing an exception then you do not need to show an additional stack trace here because the exception has it already! ;-)
            if (includeStackTrace || ShowStackTrace)
                errorMessage = string.Concat(errorMessage, "StackTrace:\n", Environment.StackTrace);

            result.IsSaved = false;
            result.IsLoaded = false;
            result.IsError = true;
            result.Message = errorMessage;

            if (!string.IsNullOrEmpty(detailedMessage))
                result.DetailedMessage = detailedMessage;

            if (ex != null)
                result.Exception = ex;

            if (addToInnerMessages)
                result.InnerMessages.Add(errorMessage);

            if (incrementErrorCount)
                result.ErrorCount++;

            if (log || LogAllErrors)
                LoggingManager.Log(errorMessage, Enums.LogType.Error);

            if (throwException || ThrowExceptionsOnErrors)
                throw new Exception(errorMessage, ex);
        }

        public static void HandleError<T>(ref OASISResult<T> result, string errorMessage, Exception ex)
        {
            HandleError(ref result, errorMessage, true, false, false, false, true, ex);
        }

        public static void HandleError<T>(ref OASISResult<T> result, string errorMessage, string detailedMessage)
        {
            HandleError(ref result, errorMessage, true, false, false, false, true, null, detailedMessage);
        }

        public static void HandleError<T>(ref OASISResult<T> result, string errorMessage, string detailedMessage, Exception ex)
        {
            HandleError(ref result, errorMessage, true, false, false, false, true, ex, detailedMessage);
        }

        public static void HandleWarning<T>(ref OASISResult<T> result, string message, bool log = true, bool includeStackTrace = false, bool throwException = false, bool addToInnerMessages = true, bool incrementWarningCount = true, Exception ex = null, string detailedMessage = "")
        {
            //NOTE: If you are throwing an exception then you do not need to show an additional stack trace here because the exception has it already! ;-)
            if (includeStackTrace || ShowStackTrace)
                message = string.Concat(message, "StackTrace:\n", Environment.StackTrace);

            result.IsWarning = true;
            result.Message = message;

            if (!string.IsNullOrEmpty(detailedMessage))
                result.DetailedMessage = detailedMessage;

            if (ex != null)
                result.Exception = ex;

            if (addToInnerMessages)
                result.InnerMessages.Add(message);

            if (incrementWarningCount)
                result.WarningCount++;

            if (log || LogAllWarnings)
                LoggingManager.Log(message, Enums.LogType.Warn);

            if (throwException || ThrowExceptionsOnWarnings)
                throw new Exception(message, ex);
        }

        public static void HandleWarning<T>(ref OASISResult<T> result, string message, Exception ex)
        {
            HandleWarning(ref result, message, true, false, false, true, true, ex);
        }

        public static void HandleWarning<T>(ref OASISResult<T> result, string errorMessage, string detailedMessage)
        {
            HandleWarning(ref result, errorMessage, true, false, false, false, true, null, detailedMessage);
        }

        public static void HandleWarning<T>(ref OASISResult<T> result, string errorMessage, string detailedMessage, Exception ex)
        {
            HandleWarning(ref result, errorMessage, true, false, false, false, true, ex, detailedMessage);
        }
    }
}