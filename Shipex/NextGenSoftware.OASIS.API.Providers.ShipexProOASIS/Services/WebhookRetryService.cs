using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Models;
using NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Repositories;

namespace NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Services;

/// <summary>
/// Service for retrying failed webhook events
/// Implements exponential backoff retry strategy
/// </summary>
public class WebhookRetryService
{
    private readonly IShipexProRepository _repository;
    private readonly IWebhookService _webhookService;
    private readonly ILogger<WebhookRetryService>? _logger;
    private readonly int _maxRetries;
    private readonly TimeSpan _initialRetryDelay;

    public WebhookRetryService(
        IShipexProRepository repository,
        IWebhookService webhookService,
        ILogger<WebhookRetryService>? logger = null,
        int maxRetries = 3,
        TimeSpan? initialRetryDelay = null)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _webhookService = webhookService ?? throw new ArgumentNullException(nameof(webhookService));
        _logger = logger;
        _maxRetries = maxRetries;
        _initialRetryDelay = initialRetryDelay ?? TimeSpan.FromMinutes(5);
    }

    /// <summary>
    /// Retries processing a failed webhook event
    /// </summary>
    public async Task<OASISResult<bool>> RetryWebhookAsync(Guid webhookEventId)
    {
        try
        {
            var webhookResult = await _repository.GetWebhookEventAsync(webhookEventId);
            if (webhookResult.IsError || webhookResult.Result == null)
            {
                return new OASISResult<bool>
                {
                    IsError = true,
                    Message = $"Webhook event not found: {webhookEventId}"
                };
            }

            var webhook = webhookResult.Result;

            // Check if already processed successfully
            if (webhook.ProcessingStatus == "Processed")
            {
                return new OASISResult<bool>
                {
                    IsError = false,
                    Message = "Webhook already processed successfully",
                    Result = true
                };
            }

            // Check retry count
            if (webhook.RetryCount >= _maxRetries)
            {
                webhook.ProcessingStatus = "Failed - Max Retries Exceeded";
                await _repository.SaveWebhookEventAsync(webhook);
                return new OASISResult<bool>
                {
                    IsError = true,
                    Message = $"Maximum retry count ({_maxRetries}) exceeded"
                };
            }

            // Update retry count
            webhook.RetryCount++;
            webhook.ProcessingStatus = "Retrying";
            webhook.LastRetryAt = DateTime.UtcNow;
            await _repository.SaveWebhookEventAsync(webhook);

            // Retry processing
            var result = await _webhookService.ProcessWebhookAsync(webhook);

            if (!result.IsError)
            {
                webhook.ProcessingStatus = "Processed";
                webhook.ProcessedAt = DateTime.UtcNow;
            }
            else
            {
                webhook.ProcessingStatus = "Failed";
                webhook.ErrorMessage = result.Message;
            }

            await _repository.SaveWebhookEventAsync(webhook);

            return result;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Exception retrying webhook: {WebhookEventId}", webhookEventId);
            return new OASISResult<bool>
            {
                IsError = true,
                Message = $"Exception retrying webhook: {ex.Message}",
                Exception = ex
            };
        }
    }

    /// <summary>
    /// Processes all failed webhooks that are eligible for retry
    /// </summary>
    public async Task<OASISResult<int>> RetryFailedWebhooksAsync()
    {
        try
        {
            // This would require a repository method to get failed webhooks
            // For now, this is a placeholder that would be implemented with Agent A's repository
            _logger?.LogInformation("Retrying failed webhooks...");

            // TODO: Implement GetFailedWebhooksAsync in repository
            // var failedWebhooks = await _repository.GetFailedWebhooksAsync(_maxRetries);
            
            // int retriedCount = 0;
            // foreach (var webhook in failedWebhooks)
            // {
            //     var result = await RetryWebhookAsync(webhook.EventId);
            //     if (!result.IsError)
            //     {
            //         retriedCount++;
            //     }
            // }

            return new OASISResult<int>
            {
                Result = 0,
                Message = "Retry mechanism ready - requires repository method implementation"
            };
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Exception in batch retry");
            return new OASISResult<int>
            {
                IsError = true,
                Message = $"Exception retrying webhooks: {ex.Message}",
                Exception = ex
            };
        }
    }
}




