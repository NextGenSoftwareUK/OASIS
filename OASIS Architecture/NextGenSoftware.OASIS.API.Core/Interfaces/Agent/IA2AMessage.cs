using System;
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.Agent
{
    /// <summary>
    /// Agent-to-Agent (A2A) message protocol interface
    /// Defines the structure for messages between autonomous agents
    /// </summary>
    public interface IA2AMessage
    {
        /// <summary>
        /// Unique message ID
        /// </summary>
        Guid MessageId { get; set; }
        
        /// <summary>
        /// Sender agent avatar ID
        /// </summary>
        Guid FromAgentId { get; set; }
        
        /// <summary>
        /// Recipient agent avatar ID
        /// </summary>
        Guid ToAgentId { get; set; }
        
        /// <summary>
        /// Type of A2A message
        /// </summary>
        A2AMessageType MessageType { get; set; }
        
        /// <summary>
        /// Message content/payload (can be JSON string, task description, etc.)
        /// </summary>
        string Content { get; set; }
        
        /// <summary>
        /// Structured payload for complex messages
        /// </summary>
        Dictionary<string, object> Payload { get; set; }
        
        /// <summary>
        /// Message timestamp
        /// </summary>
        DateTime Timestamp { get; set; }
        
        /// <summary>
        /// Message expiration time (optional)
        /// </summary>
        DateTime? ExpiresAt { get; set; }
        
        /// <summary>
        /// Related transaction ID if this message involves a payment
        /// </summary>
        string TransactionHash { get; set; }
        
        /// <summary>
        /// Response to this message (for request-response patterns)
        /// </summary>
        Guid? ResponseToMessageId { get; set; }
        
        /// <summary>
        /// Message priority (Low, Normal, High, Urgent)
        /// </summary>
        MessagePriority Priority { get; set; }
        
        /// <summary>
        /// Additional metadata
        /// </summary>
        Dictionary<string, object> Metadata { get; set; }
    }
    
    /// <summary>
    /// Types of A2A messages
    /// </summary>
    public enum A2AMessageType
    {
        // Service Discovery
        CapabilityQuery,        // "What can you do?"
        CapabilityResponse,     // "I can do X, Y, Z"
        ServiceRequest,         // "Can you do X?"
        ServiceOffer,           // "I can do X for Y SOL"
        
        // Task Management
        TaskDelegation,         // "Please do X"
        TaskAcceptance,         // "I accept the task"
        TaskRejection,          // "I cannot do this task"
        TaskUpdate,             // "Task progress update"
        TaskCompletion,         // "Task completed"
        
        // Payment
        PaymentRequest,         // "Pay me Y SOL for X"
        PaymentConfirmation,    // "Payment received"
        PaymentRejection,       // "Payment failed"
        
        // Negotiation
        NegotiationStart,       // "Let's negotiate terms"
        NegotiationOffer,       // "I offer X"
        NegotiationAccept,      // "I accept"
        NegotiationReject,      // "I reject"
        
        // Reputation
        ReputationQuery,        // "What's your reputation?"
        ReputationUpdate,       // "Reputation changed"
        
        // General
        Ping,                   // "Are you alive?"
        Pong,                   // "Yes, I'm alive"
        Error                   // "Error occurred"
    }
    
    /// <summary>
    /// Message priority levels
    /// </summary>
    public enum MessagePriority
    {
        Low,
        Normal,
        High,
        Urgent
    }
}































