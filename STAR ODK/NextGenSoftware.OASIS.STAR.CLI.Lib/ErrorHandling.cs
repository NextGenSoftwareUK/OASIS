using NextGenSoftware.CLI.Engine;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.STAR.CLI.Lib
{
    public static class ErrorHandling
    {
        public static void HandleResponse<T1, T2>(OASISResult<T1> parentResult, OASISResult<T2> result, string errorMessage, string successMessage = "")
        {
            //(parentResult, result.Result) = OASISResultHelper.UnWrapOASISResultWithDefaultErrorMessage(parentResult, result, methodName);
            OASISResultHelper.UnWrapOASISResult(parentResult, result, errorMessage);
            ProcessSuccessMessage(result, successMessage);
        }

        public static void HandleResponseWithDefaultErrorMessage<T1, T2>(OASISResult<T1> parentResult, OASISResult<T2> result, string methodName, string successMessage = "")
        {
            OASISResultHelper.UnWrapOASISResultWithDefaultErrorMessage(parentResult, result, methodName);
            ProcessSuccessMessage(result, successMessage);
        }

        private static void ProcessSuccessMessage<T>(OASISResult<T> result, string successMessage = "")
        {
            if (!result.IsError && result.Result != null)
            {
                if (string.IsNullOrEmpty(successMessage))
                {
                    if (string.IsNullOrEmpty(result.Message))
                        successMessage = $"Successfully Completed Operation.";
                    else
                        successMessage = result.Message;
                }

                CLIEngine.ShowSuccessMessage(successMessage);
            }
        }
    }
}