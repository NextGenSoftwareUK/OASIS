using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Agent;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.Managers
{
    /// <summary>
    /// A2A Protocol integration with MNEE stablecoin payments
    /// Enables autonomous agent-to-agent payments using MNEE
    /// </summary>
    public partial class A2AManager
    {
        /// <summary>
        /// Send MNEE payment request and execute payment automatically
        /// </summary>
        public async Task<OASISResult<IA2AMessage>> SendMNEEPaymentRequestAsync(
            Guid fromAgentId,
            Guid toAgentId,
            decimal amount,
            string description = null,
            bool autoExecute = true)
        {
            var result = new OASISResult<IA2AMessage>();
            try
            {
                // Create payment request message
                var paymentRequest = new A2AMessage
                {
                    MessageId = Guid.NewGuid(),
                    FromAgentId = fromAgentId,
                    ToAgentId = toAgentId,
                    MessageType = A2AMessageType.PaymentRequest,
                    Content = $"MNEE payment request: {amount} MNEE for {description ?? "services"}",
                    Payload = new Dictionary<string, object>
                    {
                        ["amount"] = amount,
                        ["description"] = description ?? "Agent service payment",
                        ["currency"] = "MNEE",
                        ["contractAddress"] = "0x8ccedbAe4916b79da7F3F612EfB2EB93A2bFD6cF"
                    },
                    Timestamp = DateTime.UtcNow,
                    Priority = MessagePriority.High
                };

                // Send payment request message
                var messageResult = await SendA2AMessageAsync(paymentRequest);
                if (messageResult.IsError)
                {
                    return messageResult;
                }

                // Auto-execute payment if requested
                if (autoExecute)
                {
                    var paymentResult = await ExecuteMNEEPaymentAsync(fromAgentId, toAgentId, amount, paymentRequest.MessageId);
                    if (!paymentResult.IsError && !string.IsNullOrWhiteSpace(paymentResult.Result))
                    {
                        // Update message with transaction hash
                        paymentRequest.TransactionHash = paymentResult.Result;
                        paymentRequest.Payload["transactionHash"] = paymentResult.Result;
                        paymentRequest.Payload["paymentStatus"] = "completed";

                        // Send payment confirmation
                        await SendMNEEPaymentConfirmationAsync(fromAgentId, toAgentId, paymentRequest.MessageId, paymentResult.Result);
                    }
                    else
                    {
                        paymentRequest.Payload["paymentStatus"] = "failed";
                        paymentRequest.Payload["error"] = paymentResult.Message;
                    }
                }

                result.Result = paymentRequest;
                result.IsError = false;
                result.Message = "MNEE payment request sent successfully";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error sending MNEE payment request: {ex.Message}", ex);
            }
            return result;
        }

        /// <summary>
        /// Execute MNEE payment between agents
        /// </summary>
        public async Task<OASISResult<string>> ExecuteMNEEPaymentAsync(
            Guid fromAgentId,
            Guid toAgentId,
            decimal amount,
            Guid? paymentRequestId = null)
        {
            var result = new OASISResult<string>();
            try
            {
                // Get Ethereum provider using reflection to avoid direct dependency
                var providerResult = ProviderManager.Instance.GetProvider(ProviderType.EthereumOASIS);
                if (providerResult == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Ethereum provider is not available");
                    return result;
                }

                // Use reflection to call TransferMNEEBetweenAvatarsAsync
                var method = providerResult.GetType().GetMethod("TransferMNEEBetweenAvatarsAsync");
                if (method == null)
                {
                    OASISErrorHandling.HandleError(ref result, "MNEE transfer method not found on Ethereum provider");
                    return result;
                }

                var transferTask = (Task<OASISResult<string>>)method.Invoke(providerResult, new object[] { fromAgentId, toAgentId, amount });
                var transferResult = await transferTask;
                
                if (transferResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"MNEE transfer failed: {transferResult.Message}");
                    return result;
                }

                result.Result = transferResult.Result;
                result.IsError = false;
                result.Message = "MNEE payment executed successfully";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error executing MNEE payment: {ex.Message}", ex);
            }
            return result;
        }

        /// <summary>
        /// Send MNEE payment confirmation message
        /// </summary>
        public async Task<OASISResult<IA2AMessage>> SendMNEEPaymentConfirmationAsync(
            Guid fromAgentId,
            Guid toAgentId,
            Guid paymentRequestId,
            string transactionHash)
        {
            var result = new OASISResult<IA2AMessage>();
            try
            {
                var confirmationMessage = new A2AMessage
                {
                    MessageId = Guid.NewGuid(),
                    FromAgentId = fromAgentId,
                    ToAgentId = toAgentId,
                    MessageType = A2AMessageType.PaymentConfirmation,
                    Content = $"MNEE payment confirmed. Transaction: {transactionHash}",
                    Payload = new Dictionary<string, object>
                    {
                        ["paymentRequestId"] = paymentRequestId.ToString(),
                        ["transactionHash"] = transactionHash,
                        ["currency"] = "MNEE",
                        ["status"] = "confirmed"
                    },
                    ResponseToMessageId = paymentRequestId,
                    TransactionHash = transactionHash,
                    Timestamp = DateTime.UtcNow,
                    Priority = MessagePriority.High
                };

                return await SendA2AMessageAsync(confirmationMessage);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error sending payment confirmation: {ex.Message}", ex);
                return result;
            }
        }

        /// <summary>
        /// Get MNEE balance for an agent
        /// </summary>
        public async Task<OASISResult<decimal>> GetAgentMNEEBalanceAsync(Guid agentId)
        {
            var result = new OASISResult<decimal>();
            try
            {
                var providerResult = ProviderManager.Instance.GetProvider(ProviderType.EthereumOASIS);
                if (providerResult == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Ethereum provider is not available");
                    return result;
                }

                // Use reflection to call GetMNEEBalanceForAvatarAsync
                var method = providerResult.GetType().GetMethod("GetMNEEBalanceForAvatarAsync");
                if (method == null)
                {
                    OASISErrorHandling.HandleError(ref result, "MNEE balance method not found on Ethereum provider");
                    return result;
                }

                var balanceTask = (Task<OASISResult<decimal>>)method.Invoke(providerResult, new object[] { agentId });
                return await balanceTask;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting agent MNEE balance: {ex.Message}", ex);
                return result;
            }
        }
    }
}
