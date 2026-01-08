using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces.Agent;
using NextGenSoftware.OASIS.Common;
using Newtonsoft.Json;

namespace NextGenSoftware.OASIS.API.Core.Managers
{
    /// <summary>
    /// JSON-RPC 2.0 conversion methods for A2A Protocol
    /// </summary>
    public partial class A2AManager
    {
        /// <summary>
        /// Convert A2A message to JSON-RPC 2.0 request format
        /// </summary>
        public JsonRpc2Request ConvertToJsonRpc2Request(IA2AMessage message)
        {
            var request = new JsonRpc2Request
            {
                JsonRpc = "2.0",
                Id = message.MessageId.ToString(),
                Method = MapMessageTypeToMethod(message.MessageType),
                Params = new Dictionary<string, object>
                {
                    ["from_agent_id"] = message.FromAgentId.ToString(),
                    ["to_agent_id"] = message.ToAgentId.ToString(),
                    ["message_type"] = message.MessageType.ToString(),
                    ["content"] = message.Content ?? "",
                    ["payload"] = message.Payload ?? new Dictionary<string, object>(),
                    ["timestamp"] = message.Timestamp.ToString("O"),
                    ["priority"] = message.Priority.ToString()
                }
            };

            if (message.ExpiresAt.HasValue)
                request.Params["expires_at"] = message.ExpiresAt.Value.ToString("O");

            if (!string.IsNullOrEmpty(message.TransactionHash))
                request.Params["transaction_hash"] = message.TransactionHash;

            if (message.ResponseToMessageId.HasValue)
                request.Params["response_to_message_id"] = message.ResponseToMessageId.Value.ToString();

            if (message.Metadata != null && message.Metadata.Count > 0)
                request.Params["metadata"] = message.Metadata;

            return request;
        }

        /// <summary>
        /// Convert JSON-RPC 2.0 request to A2A message
        /// </summary>
        public IA2AMessage ConvertFromJsonRpc2Request(JsonRpc2Request request, Guid fromAgentId)
        {
            var message = new A2AMessage
            {
                MessageId = Guid.TryParse(request.Id, out var msgId) ? msgId : Guid.NewGuid(),
                FromAgentId = fromAgentId,
                MessageType = MapMethodToMessageType(request.Method),
                Timestamp = DateTime.UtcNow,
                Priority = MessagePriority.Normal
            };

            if (request.Params != null)
            {
                if (request.Params.ContainsKey("to_agent_id") && Guid.TryParse(request.Params["to_agent_id"].ToString(), out var toAgentId))
                    message.ToAgentId = toAgentId;

                if (request.Params.ContainsKey("content"))
                    message.Content = request.Params["content"].ToString();

                if (request.Params.ContainsKey("payload") && request.Params["payload"] is Dictionary<string, object> payload)
                    message.Payload = payload;

                if (request.Params.ContainsKey("timestamp") && DateTime.TryParse(request.Params["timestamp"].ToString(), out var timestamp))
                    message.Timestamp = timestamp;

                if (request.Params.ContainsKey("expires_at") && DateTime.TryParse(request.Params["expires_at"].ToString(), out var expiresAt))
                    message.ExpiresAt = expiresAt;

                if (request.Params.ContainsKey("transaction_hash"))
                    message.TransactionHash = request.Params["transaction_hash"].ToString();

                if (request.Params.ContainsKey("response_to_message_id") && Guid.TryParse(request.Params["response_to_message_id"].ToString(), out var responseToId))
                    message.ResponseToMessageId = responseToId;

                if (request.Params.ContainsKey("priority") && Enum.TryParse<MessagePriority>(request.Params["priority"].ToString(), out var priority))
                    message.Priority = priority;

                if (request.Params.ContainsKey("metadata") && request.Params["metadata"] is Dictionary<string, object> metadata)
                    message.Metadata = metadata;
            }

            return message;
        }

        /// <summary>
        /// Convert A2A message result to JSON-RPC 2.0 response
        /// </summary>
        public JsonRpc2Response CreateJsonRpc2Response(string requestId, object result, JsonRpc2Error error = null)
        {
            return new JsonRpc2Response
            {
                JsonRpc = "2.0",
                Id = requestId,
                Result = result,
                Error = error
            };
        }

        /// <summary>
        /// Create JSON-RPC 2.0 error response
        /// </summary>
        public JsonRpc2Response CreateJsonRpc2ErrorResponse(string requestId, int errorCode, string errorMessage, object errorData = null)
        {
            return new JsonRpc2Response
            {
                JsonRpc = "2.0",
                Id = requestId,
                Error = new JsonRpc2Error
                {
                    Code = errorCode,
                    Message = errorMessage,
                    Data = errorData
                }
            };
        }

        /// <summary>
        /// Map A2A message type to JSON-RPC 2.0 method name
        /// </summary>
        private string MapMessageTypeToMethod(A2AMessageType messageType)
        {
            return messageType switch
            {
                A2AMessageType.CapabilityQuery => A2AJsonRpcMethods.CapabilityQuery,
                A2AMessageType.CapabilityResponse => A2AJsonRpcMethods.CapabilityResponse,
                A2AMessageType.ServiceRequest => A2AJsonRpcMethods.ServiceRequest,
                A2AMessageType.ServiceOffer => A2AJsonRpcMethods.ServiceOffer,
                A2AMessageType.TaskDelegation => A2AJsonRpcMethods.TaskDelegation,
                A2AMessageType.TaskAcceptance => A2AJsonRpcMethods.TaskAcceptance,
                A2AMessageType.TaskRejection => A2AJsonRpcMethods.TaskRejection,
                A2AMessageType.TaskUpdate => A2AJsonRpcMethods.TaskUpdate,
                A2AMessageType.TaskCompletion => A2AJsonRpcMethods.TaskCompletion,
                A2AMessageType.PaymentRequest => A2AJsonRpcMethods.PaymentRequest,
                A2AMessageType.PaymentConfirmation => A2AJsonRpcMethods.PaymentConfirmation,
                A2AMessageType.PaymentRejection => A2AJsonRpcMethods.PaymentRejection,
                A2AMessageType.NegotiationStart => A2AJsonRpcMethods.NegotiationStart,
                A2AMessageType.NegotiationOffer => A2AJsonRpcMethods.NegotiationOffer,
                A2AMessageType.NegotiationAccept => A2AJsonRpcMethods.NegotiationAccept,
                A2AMessageType.NegotiationReject => A2AJsonRpcMethods.NegotiationReject,
                A2AMessageType.Ping => A2AJsonRpcMethods.Ping,
                A2AMessageType.Pong => A2AJsonRpcMethods.Pong,
                _ => "unknown_method"
            };
        }

        /// <summary>
        /// Map JSON-RPC 2.0 method name to A2A message type
        /// </summary>
        private A2AMessageType MapMethodToMessageType(string method)
        {
            return method switch
            {
                A2AJsonRpcMethods.CapabilityQuery => A2AMessageType.CapabilityQuery,
                A2AJsonRpcMethods.CapabilityResponse => A2AMessageType.CapabilityResponse,
                A2AJsonRpcMethods.ServiceRequest => A2AMessageType.ServiceRequest,
                A2AJsonRpcMethods.ServiceOffer => A2AMessageType.ServiceOffer,
                A2AJsonRpcMethods.TaskDelegation => A2AMessageType.TaskDelegation,
                A2AJsonRpcMethods.TaskAcceptance => A2AMessageType.TaskAcceptance,
                A2AJsonRpcMethods.TaskRejection => A2AMessageType.TaskRejection,
                A2AJsonRpcMethods.TaskUpdate => A2AMessageType.TaskUpdate,
                A2AJsonRpcMethods.TaskCompletion => A2AMessageType.TaskCompletion,
                A2AJsonRpcMethods.PaymentRequest => A2AMessageType.PaymentRequest,
                A2AJsonRpcMethods.PaymentConfirmation => A2AMessageType.PaymentConfirmation,
                A2AJsonRpcMethods.PaymentRejection => A2AMessageType.PaymentRejection,
                A2AJsonRpcMethods.NegotiationStart => A2AMessageType.NegotiationStart,
                A2AJsonRpcMethods.NegotiationOffer => A2AMessageType.NegotiationOffer,
                A2AJsonRpcMethods.NegotiationAccept => A2AMessageType.NegotiationAccept,
                A2AJsonRpcMethods.NegotiationReject => A2AMessageType.NegotiationReject,
                A2AJsonRpcMethods.Ping => A2AMessageType.Ping,
                A2AJsonRpcMethods.Pong => A2AMessageType.Pong,
                _ => A2AMessageType.Error
            };
        }

        /// <summary>
        /// Process JSON-RPC 2.0 request and return response
        /// </summary>
        public async Task<JsonRpc2Response> ProcessJsonRpc2RequestAsync(JsonRpc2Request request, Guid fromAgentId)
        {
            try
            {
                // Validate JSON-RPC version
                if (request.JsonRpc != "2.0")
                {
                    return CreateJsonRpc2ErrorResponse(request.Id, JsonRpc2ErrorCodes.InvalidRequest, "Invalid JSON-RPC version. Must be '2.0'");
                }

                // Convert to A2A message
                var message = ConvertFromJsonRpc2Request(request, fromAgentId);

                // Process based on method
                switch (request.Method)
                {
                    case A2AJsonRpcMethods.Ping:
                        return CreateJsonRpc2Response(request.Id, new { status = "pong", timestamp = DateTime.UtcNow });

                    case A2AJsonRpcMethods.CapabilityQuery:
                        var capabilitiesResult = await AgentManager.Instance.GetAgentCapabilitiesAsync(message.ToAgentId);
                        if (capabilitiesResult.IsError)
                        {
                            return CreateJsonRpc2ErrorResponse(request.Id, JsonRpc2ErrorCodes.AgentNotFound, $"Agent {message.ToAgentId} not found");
                        }
                        return CreateJsonRpc2Response(request.Id, new
                        {
                            agent_id = message.ToAgentId.ToString(),
                            services = capabilitiesResult.Result.Services,
                            skills = capabilitiesResult.Result.Skills,
                            status = capabilitiesResult.Result.Status.ToString(),
                            reputation = capabilitiesResult.Result.ReputationScore
                        });

                    case A2AJsonRpcMethods.ServiceRequest:
                    case A2AJsonRpcMethods.TaskDelegation:
                    case A2AJsonRpcMethods.PaymentRequest:
                        // Send A2A message
                        var sendResult = await SendA2AMessageAsync(message);
                        if (sendResult.IsError)
                        {
                            return CreateJsonRpc2ErrorResponse(request.Id, JsonRpc2ErrorCodes.InternalError, sendResult.Message);
                        }
                        return CreateJsonRpc2Response(request.Id, new
                        {
                            message_id = sendResult.Result.MessageId.ToString(),
                            status = "sent",
                            timestamp = sendResult.Result.Timestamp
                        });

                    default:
                        return CreateJsonRpc2ErrorResponse(request.Id, JsonRpc2ErrorCodes.MethodNotFound, $"Method '{request.Method}' not found");
                }
            }
            catch (Exception ex)
            {
                return CreateJsonRpc2ErrorResponse(request.Id, JsonRpc2ErrorCodes.InternalError, $"Internal error: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// JSON-RPC 2.0 Request implementation
    /// </summary>
    public class JsonRpc2Request : IJsonRpc2Request
    {
        [JsonProperty("jsonrpc")]
        public string JsonRpc { get; set; } = "2.0";

        [JsonProperty("method")]
        public string Method { get; set; }

        [JsonProperty("params")]
        public Dictionary<string, object> Params { get; set; } = new Dictionary<string, object>();

        [JsonProperty("id")]
        public string Id { get; set; }
    }

    /// <summary>
    /// JSON-RPC 2.0 Response implementation
    /// </summary>
    public class JsonRpc2Response : IJsonRpc2Response
    {
        [JsonProperty("jsonrpc")]
        public string JsonRpc { get; set; } = "2.0";

        [JsonProperty("result")]
        public object Result { get; set; }

        [JsonProperty("error")]
        public JsonRpc2Error Error { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }
    }
}































