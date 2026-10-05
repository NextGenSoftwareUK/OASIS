using System.Net;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Helpers
{
    public static class HttpResponseHelper
    {

        public static OASISHttpResponseMessage<T> FormatResponse<T>(OASISHttpResponseMessage<T> response, HttpStatusCode statusCode = HttpStatusCode.OK, bool showDetailedSettings = false)
        {
            //Make sure no Error Details are in the Message.
            if (response.Result.IsError && response.Result.Message.IndexOf("\n\nError Details:\n") > 0)
                response.Result.Message = response.Result.Message.Substring(0, response.Result.Message.IndexOf("\n\nError Details:\n"));

            //Replace unsupported chars.
            if (!string.IsNullOrEmpty(response.Result.Message))
            {
                response.Result.Message = response.Result.Message.Replace("\n", " ").Trim();
                response.Result.Message = response.Result.Message.Replace("\r", " ").Trim();
            }

            if (!string.IsNullOrEmpty(response.Result.DetailedMessage))
            {
                response.Result.DetailedMessage = response.Result.DetailedMessage.Replace("\n\n", " | ").Trim();
                response.Result.DetailedMessage = response.Result.DetailedMessage.Replace("\n", " ").Trim();
                response.Result.DetailedMessage = response.Result.DetailedMessage.Replace("\r", " ").Trim();
            }

            response.StatusCode = statusCode;
            response.ShowDetailedSettings = showDetailedSettings;

            return response;
        }

        public static OASISHttpResponseMessage<T> FormatResponse<T>(OASISResult<T> response, HttpStatusCode statusCode = HttpStatusCode.OK, bool showDetailedSettings = false)
        {
            return FormatResponse(new OASISHttpResponseMessage<T>(response), statusCode, showDetailedSettings);
        }

        public static OASISHttpResponseMessage<T> FormatResponse<T>(OASISResult<T> response)
        {
            return FormatResponse(response, HttpStatusCode.OK); 
        }
    }
}