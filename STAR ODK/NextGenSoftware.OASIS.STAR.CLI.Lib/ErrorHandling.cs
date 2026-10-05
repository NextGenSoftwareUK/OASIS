using NextGenSoftware.CLI.Engine;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.STAR.CLI.Lib
{
    public static class ErrorHandling
    {
        public static void HandleBooleansResponse(bool isSuccess, string successMessage, string errorMessage)
        {
            if (isSuccess)
                CLIEngine.ShowSuccessMessage(successMessage);
            else
                CLIEngine.ShowErrorMessage(errorMessage);
        }

        public static void HandleOASISResponse<T>(OASISResult<T> result, string successMessage, string errorMessage)
        {
            if (!result.IsError && result.Result != null)
                CLIEngine.ShowSuccessMessage(successMessage);
            else
                CLIEngine.ShowErrorMessage($"{errorMessage}Reason: {result.Message}");
        }

        public static void HandleHolonsOASISResponse(OASISResult<IEnumerable<IHolon>> result)
        {
            if (!result.IsError && result.Result != null)
            {
                CLIEngine.ShowSuccessMessage($"{result.Result.Count()} Holon(s) Loaded:");
                STARCLI.Holons.ShowHolons(result.Result, false);
            }
            else
                CLIEngine.ShowErrorMessage($"Error Loading Holons. Reason: {result.Message}");
        }

        public static (OASISResult<T1>, T2) HandleResponse<T1, T2>(OASISResult<T1> parentResult, OASISResult<T2> result, string errorMessage, string successMessage = "")
        {
            ProcessSuccessMessage(result, successMessage);
            //(parentResult, result.Result) = OASISResultHelper.UnWrapOASISResultWithDefaultErrorMessage(parentResult, result, methodName);
            return OASISResultHelper.UnWrapOASISResult(parentResult, result, errorMessage);
            
        }

        public static (OASISResult<T1>, T2) HandleResponseWithDefaultErrorMessage<T1, T2>(OASISResult<T1> parentResult, OASISResult<T2> result, string methodName, string successMessage = "")
        {
            ProcessSuccessMessage(result, successMessage);
            return OASISResultHelper.UnWrapOASISResultWithDefaultErrorMessage(parentResult, result, methodName);
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