using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.Agent
{
    /// <summary>
    /// JSON-RPC 2.0 Request Message
    /// Based on: https://www.jsonrpc.org/specification
    /// </summary>
    public interface IJsonRpc2Request
    {
        [JsonProperty("jsonrpc")]
        string JsonRpc { get; set; }
        
        [JsonProperty("method")]
        string Method { get; set; }
        
        [JsonProperty("params")]
        Dictionary<string, object> Params { get; set; }
        
        [JsonProperty("id")]
        string Id { get; set; }
    }
    
    /// <summary>
    /// JSON-RPC 2.0 Response Message
    /// </summary>
    public interface IJsonRpc2Response
    {
        [JsonProperty("jsonrpc")]
        string JsonRpc { get; set; }
        
        [JsonProperty("result")]
        object Result { get; set; }
        
        [JsonProperty("error")]
        JsonRpc2Error Error { get; set; }
        
        [JsonProperty("id")]
        string Id { get; set; }
    }
    
    /// <summary>
    /// JSON-RPC 2.0 Error Object
    /// </summary>
    public class JsonRpc2Error
    {
        [JsonProperty("code")]
        public int Code { get; set; }
        
        [JsonProperty("message")]
        public string Message { get; set; }
        
        [JsonProperty("data")]
        public object Data { get; set; }
    }
    
    /// <summary>
    /// JSON-RPC 2.0 Error Codes
    /// </summary>
    public static class JsonRpc2ErrorCodes
    {
        public const int ParseError = -32700;
        public const int InvalidRequest = -32600;
        public const int MethodNotFound = -32601;
        public const int InvalidParams = -32602;
        public const int InternalError = -32603;
        
        // A2A Protocol specific error codes
        public const int AgentNotFound = -32001;
        public const int ServiceNotFound = -32002;
        public const int TaskNotFound = -32003;
        public const int PaymentFailed = -32004;
        public const int InsufficientFunds = -32005;
    }
    
    /// <summary>
    /// A2A Protocol JSON-RPC 2.0 Methods
    /// Based on: https://github.com/a2aproject/A2A
    /// </summary>
    public static class A2AJsonRpcMethods
    {
        // Service Discovery
        public const string CapabilityQuery = "capability_query";
        public const string CapabilityResponse = "capability_response";
        
        // Service Requests
        public const string ServiceRequest = "service_request";
        public const string ServiceOffer = "service_offer";
        
        // Task Management
        public const string TaskDelegation = "task_delegation";
        public const string TaskAcceptance = "task_acceptance";
        public const string TaskRejection = "task_rejection";
        public const string TaskUpdate = "task_update";
        public const string TaskCompletion = "task_completion";
        
        // Payment
        public const string PaymentRequest = "payment_request";
        public const string PaymentConfirmation = "payment_confirmation";
        public const string PaymentRejection = "payment_rejection";
        
        // Negotiation
        public const string NegotiationStart = "negotiation_start";
        public const string NegotiationOffer = "negotiation_offer";
        public const string NegotiationAccept = "negotiation_accept";
        public const string NegotiationReject = "negotiation_reject";
        
        // Health Checks
        public const string Ping = "ping";
        public const string Pong = "pong";
        
        // Agent Card
        public const string GetAgentCard = "get_agent_card";
    }
}































