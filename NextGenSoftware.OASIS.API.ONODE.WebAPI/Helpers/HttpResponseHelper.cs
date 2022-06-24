using System.Net;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Helpers
{
    public static class HttpResponseHelper
    {
        public static OASISHttpResponseMessage<T> FormatResponse<T>(OASISResult<T> response, HttpStatusCode statusCode , bool showDetailedSettings = false, AutoFailOverMode autoFailOverMode = AutoFailOverMode.NotSet, AutoReplicationMode autoReplicationMode = AutoReplicationMode.NotSet, AutoLoadBalanceMode autoLoadBalanceMode = AutoLoadBalanceMode.NotSet)
        {
            //Make sure no Error Details are in the Message.
            if (response.IsError && response.Message.IndexOf("\n\nError Details:\n") > 0)
                response.Message = response.Message.Substring(0, response.Message.IndexOf("\n\nError Details:\n"));

            //Replace unsupported chars.
            if (!string.IsNullOrEmpty(response.Message))
            {
                response.Message = response.Message.Replace("\n", " ").Trim();
                response.Message = response.Message.Replace("\r", " ").Trim();
            }

            if (!string.IsNullOrEmpty(response.DetailedMessage))
            {
                response.DetailedMessage = response.DetailedMessage.Replace("\n\n", " | ").Trim();
                response.DetailedMessage = response.DetailedMessage.Replace("\n", " ").Trim();
                response.DetailedMessage = response.DetailedMessage.Replace("\r", " ").Trim();
            }

            return new OASISHttpResponseMessage<T>(response, statusCode, showDetailedSettings, autoFailOverMode, autoReplicationMode, autoLoadBalanceMode);
        }

        public static OASISHttpResponseMessage<T> FormatResponse<T>(OASISResult<T> response)
        {
            return FormatResponse(response, HttpStatusCode.OK); 
        }
    }
}