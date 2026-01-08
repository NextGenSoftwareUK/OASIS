using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Agent;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Core.Managers
{
    /// <summary>
    /// Manages Agent-to-Agent (A2A) communication protocol
    /// Handles message routing, delivery, and protocol compliance
    /// </summary>
    public partial class A2AManager : OASISManager
    {
        private static A2AManager _instance;
        private readonly Dictionary<Guid, List<IA2AMessage>> _messageQueue = new Dictionary<Guid, List<IA2AMessage>>();
        private readonly Dictionary<Guid, IA2AMessage> _pendingMessages = new Dictionary<Guid, IA2AMessage>();

        public static A2AManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new A2AManager(ProviderManager.Instance.CurrentStorageProvider);
                return _instance;
            }
        }

        public A2AManager(IOASISStorageProvider OASISStorageProvider, OASISDNA OASISDNA = null) : base(OASISStorageProvider, OASISDNA)
        {
        }

        /// <summary>
        /// Send an A2A message from one agent to another
        /// </summary>
        public async Task<OASISResult<IA2AMessage>> SendA2AMessageAsync(IA2AMessage message)
        {
            var result = new OASISResult<IA2AMessage>();
            try
            {
                // Validate sender is an agent
                var fromAvatarResult = await AvatarManager.Instance.LoadAvatarAsync(message.FromAgentId, false, true);
                if (fromAvatarResult.IsError || fromAvatarResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Sender agent {message.FromAgentId} not found");
                    return result;
                }

                if (fromAvatarResult.Result.AvatarType.Value != AvatarType.Agent)
                {
                    OASISErrorHandling.HandleError(ref result, $"Sender {message.FromAgentId} is not an Agent type");
                    return result;
                }

                // Validate recipient is an agent
                var toAvatarResult = await AvatarManager.Instance.LoadAvatarAsync(message.ToAgentId, false, true);
                if (toAvatarResult.IsError || toAvatarResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Recipient agent {message.ToAgentId} not found");
                    return result;
                }

                if (toAvatarResult.Result.AvatarType.Value != AvatarType.Agent)
                {
                    OASISErrorHandling.HandleError(ref result, $"Recipient {message.ToAgentId} is not an Agent type");
                    return result;
                }

                // Set message ID and timestamp if not set
                if (message.MessageId == Guid.Empty)
                    message.MessageId = Guid.NewGuid();
                
                if (message.Timestamp == default)
                    message.Timestamp = DateTime.UtcNow;

                // Store message in queue
                if (!_messageQueue.ContainsKey(message.ToAgentId))
                    _messageQueue[message.ToAgentId] = new List<IA2AMessage>();

                _messageQueue[message.ToAgentId].Add(message);
                _pendingMessages[message.MessageId] = message;

                // Create notification for recipient agent
                await MessagingManager.Instance.SendMessageToAvatarAsync(
                    message.FromAgentId,
                    message.ToAgentId,
                    $"A2A Message: {message.MessageType}",
                    MessagingType.Direct
                );

                result.Result = message;
                result.Message = "A2A message sent successfully";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error sending A2A message: {ex.Message}", ex);
            }
            return result;
        }

        /// <summary>
        /// Get pending A2A messages for an agent
        /// </summary>
        public async Task<OASISResult<List<IA2AMessage>>> GetPendingMessagesAsync(Guid agentId)
        {
            var result = new OASISResult<List<IA2AMessage>>();
            try
            {
                if (_messageQueue.ContainsKey(agentId))
                {
                    // Filter out expired messages
                    var validMessages = _messageQueue[agentId]
                        .Where(m => m.ExpiresAt == null || m.ExpiresAt > DateTime.UtcNow)
                        .OrderBy(m => m.Priority)
                        .ThenBy(m => m.Timestamp)
                        .ToList();

                    result.Result = validMessages;
                    result.Message = $"Found {validMessages.Count} pending messages";
                }
                else
                {
                    result.Result = new List<IA2AMessage>();
                    result.Message = "No pending messages";
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error retrieving pending messages: {ex.Message}", ex);
            }
            return await Task.FromResult(result);
        }

        /// <summary>
        /// Mark a message as processed/read
        /// </summary>
        public async Task<OASISResult<bool>> MarkMessageProcessedAsync(Guid agentId, Guid messageId)
        {
            var result = new OASISResult<bool>();
            try
            {
                if (_messageQueue.ContainsKey(agentId))
                {
                    var message = _messageQueue[agentId].FirstOrDefault(m => m.MessageId == messageId);
                    if (message != null)
                    {
                        _messageQueue[agentId].Remove(message);
                        _pendingMessages.Remove(messageId);
                        result.Result = true;
                        result.Message = "Message marked as processed";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, $"Message {messageId} not found in queue");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"No message queue found for agent {agentId}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error marking message as processed: {ex.Message}", ex);
            }
            return await Task.FromResult(result);
        }

        /// <summary>
        /// Send a service request message (A2A protocol)
        /// </summary>
        public async Task<OASISResult<IA2AMessage>> SendServiceRequestAsync(
            Guid fromAgentId,
            Guid toAgentId,
            string serviceName,
            Dictionary<string, object> serviceParameters = null)
        {
            var result = new OASISResult<IA2AMessage>();
            try
            {
                var message = new A2AMessage
                {
                    MessageId = Guid.NewGuid(),
                    FromAgentId = fromAgentId,
                    ToAgentId = toAgentId,
                    MessageType = A2AMessageType.ServiceRequest,
                    Content = $"Request for service: {serviceName}",
                    Payload = new Dictionary<string, object>
                    {
                        ["serviceName"] = serviceName,
                        ["parameters"] = serviceParameters ?? new Dictionary<string, object>()
                    },
                    Timestamp = DateTime.UtcNow,
                    Priority = MessagePriority.Normal
                };

                return await SendA2AMessageAsync(message);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error sending service request: {ex.Message}", ex);
                return result;
            }
        }

        /// <summary>
        /// Send a payment request message (A2A protocol)
        /// </summary>
        public async Task<OASISResult<IA2AMessage>> SendPaymentRequestAsync(
            Guid fromAgentId,
            Guid toAgentId,
            decimal amount,
            string description,
            string transactionHash = null)
        {
            var result = new OASISResult<IA2AMessage>();
            try
            {
                var message = new A2AMessage
                {
                    MessageId = Guid.NewGuid(),
                    FromAgentId = fromAgentId,
                    ToAgentId = toAgentId,
                    MessageType = A2AMessageType.PaymentRequest,
                    Content = $"Payment request: {amount} SOL for {description}",
                    Payload = new Dictionary<string, object>
                    {
                        ["amount"] = amount,
                        ["description"] = description,
                        ["currency"] = "SOL"
                    },
                    TransactionHash = transactionHash,
                    Timestamp = DateTime.UtcNow,
                    Priority = MessagePriority.High
                };

                return await SendA2AMessageAsync(message);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error sending payment request: {ex.Message}", ex);
                return result;
            }
        }
    }

    /// <summary>
    /// Implementation of IA2AMessage
    /// </summary>
    public class A2AMessage : IA2AMessage
    {
        public Guid MessageId { get; set; }
        public Guid FromAgentId { get; set; }
        public Guid ToAgentId { get; set; }
        public A2AMessageType MessageType { get; set; }
        public string Content { get; set; }
        public Dictionary<string, object> Payload { get; set; } = new Dictionary<string, object>();
        public DateTime Timestamp { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public string TransactionHash { get; set; }
        public Guid? ResponseToMessageId { get; set; }
        public MessagePriority Priority { get; set; } = MessagePriority.Normal;
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Implementation of IAgentCapabilities
    /// </summary>
    public class AgentCapabilities : IAgentCapabilities
    {
        public List<string> Services { get; set; } = new List<string>();
        public Dictionary<string, decimal> Pricing { get; set; } = new Dictionary<string, decimal>();
        public List<string> Skills { get; set; } = new List<string>();
        public AgentStatus Status { get; set; } = AgentStatus.Available;
        public decimal ReputationScore { get; set; } = 0;
        public int MaxConcurrentTasks { get; set; } = 1;
        public int ActiveTasks { get; set; } = 0;
        public string Description { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }
}































