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
    /// A2A Protocol integration with SERV token payments on Base blockchain
    /// Enables autonomous agent-to-agent payments using SERV (OpenServ token)
    /// </summary>
    public partial class A2AManager
    {
        /// <summary>
        /// Send SERV payment request and execute payment automatically
        /// </summary>
        public async Task<OASISResult<IA2AMessage>> SendSERVPaymentRequestAsync(
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
                    Content = $"SERV payment request: {amount} SERV for {description ?? "services"}",
                    Payload = new Dictionary<string, object>
                    {
                        ["amount"] = amount,
                        ["description"] = description ?? "Agent service payment",
                        ["currency"] = "SERV",
                        ["contractAddress"] = "0x5576D6ed9181F2225afF5282Ac0ED29f755437Ea",
                        ["blockchain"] = "Base"
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
                    var paymentResult = await ExecuteSERVPaymentAsync(fromAgentId, toAgentId, amount, paymentRequest.MessageId);
                    if (!paymentResult.IsError && !string.IsNullOrWhiteSpace(paymentResult.Result))
                    {
                        // Update message with transaction hash
                        paymentRequest.TransactionHash = paymentResult.Result;
                        paymentRequest.Payload["transactionHash"] = paymentResult.Result;
                        paymentRequest.Payload["paymentStatus"] = "completed";

                        // Send payment confirmation
                        await SendSERVPaymentConfirmationAsync(fromAgentId, toAgentId, paymentRequest.MessageId, paymentResult.Result);
                    }
                    else
                    {
                        paymentRequest.Payload["paymentStatus"] = "failed";
                        paymentRequest.Payload["error"] = paymentResult.Message;
                    }
                }

                result.Result = paymentRequest;
                result.IsError = false;
                result.Message = "SERV payment request sent successfully";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error sending SERV payment request: {ex.Message}", ex);
            }
            return result;
        }

        /// <summary>
        /// Execute SERV payment between agents
        /// </summary>
        public async Task<OASISResult<string>> ExecuteSERVPaymentAsync(
            Guid fromAgentId,
            Guid toAgentId,
            decimal amount,
            Guid? paymentRequestId = null)
        {
            var result = new OASISResult<string>();
            try
            {
                // Get Base provider using reflection to avoid direct dependency
                var providerResult = ProviderManager.Instance.GetProvider(ProviderType.BaseOASIS);
                if (providerResult == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Base provider is not available");
                    return result;
                }

                // Use reflection to call TransferSERVBetweenAvatarsAsync
                var method = providerResult.GetType().GetMethod("TransferSERVBetweenAvatarsAsync");
                if (method == null)
                {
                    OASISErrorHandling.HandleError(ref result, "SERV transfer method not found on Base provider");
                    return result;
                }

                var transferTask = (Task<OASISResult<string>>)method.Invoke(providerResult, new object[] { fromAgentId, toAgentId, amount });
                var transferResult = await transferTask;
                
                if (transferResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"SERV transfer failed: {transferResult.Message}");
                    return result;
                }

                result.Result = transferResult.Result;
                result.IsError = false;
                result.Message = "SERV payment executed successfully";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error executing SERV payment: {ex.Message}", ex);
            }
            return result;
        }

        /// <summary>
        /// Send SERV payment confirmation message
        /// </summary>
        public async Task<OASISResult<IA2AMessage>> SendSERVPaymentConfirmationAsync(
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
                    Content = $"SERV payment confirmed. Transaction: {transactionHash}",
                    Payload = new Dictionary<string, object>
                    {
                        ["paymentRequestId"] = paymentRequestId.ToString(),
                        ["transactionHash"] = transactionHash,
                        ["currency"] = "SERV",
                        ["blockchain"] = "Base",
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
        /// Get SERV balance for an agent
        /// </summary>
        public async Task<OASISResult<decimal>> GetAgentSERVBalanceAsync(Guid agentId)
        {
            var result = new OASISResult<decimal>();
            try
            {
                var providerResult = ProviderManager.Instance.GetProvider(ProviderType.BaseOASIS);
                if (providerResult == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Base provider is not available");
                    return result;
                }

                // Use reflection to call GetSERVBalanceForAvatarAsync
                var method = providerResult.GetType().GetMethod("GetSERVBalanceForAvatarAsync");
                if (method == null)
                {
                    OASISErrorHandling.HandleError(ref result, "SERV balance method not found on Base provider");
                    return result;
                }

                var balanceTask = (Task<OASISResult<decimal>>)method.Invoke(providerResult, new object[] { agentId });
                return await balanceTask;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting agent SERV balance: {ex.Message}", ex);
                return result;
            }
        }
    }
}
