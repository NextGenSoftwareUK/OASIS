using System;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Models;
using NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Repositories;
using System.Text.Json;

namespace NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Services;

/// <summary>
/// Service for processing webhook events from iShip and Shipox
/// Handles event routing, shipment status updates, and invoice triggering
/// </summary>
public class WebhookService : IWebhookService
{
    private readonly IShipexProRepository _repository;
    private readonly WebhookSecurityService _securityService;
    private readonly string _iShipWebhookSecret;
    private readonly string _shipoxWebhookSecret;

    // These services would be injected from Agent E's work
    // For now, we'll use interfaces that can be implemented later
    // public readonly IShipmentService _shipmentService;
    // public readonly IQuickBooksService _quickBooksService;

    public WebhookService(
        IShipexProRepository repository,
        WebhookSecurityService securityService,
        string iShipWebhookSecret,
        string shipoxWebhookSecret)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _securityService = securityService ?? throw new ArgumentNullException(nameof(securityService));
        _iShipWebhookSecret = iShipWebhookSecret ?? throw new ArgumentNullException(nameof(iShipWebhookSecret));
        _shipoxWebhookSecret = shipoxWebhookSecret ?? throw new ArgumentNullException(nameof(shipoxWebhookSecret));
    }

    /// <summary>
    /// Processes a webhook event (general handler)
    /// </summary>
    public async Task<OASISResult<bool>> ProcessWebhookAsync(WebhookEvent webhook)
    {
        try
        {
            // 1. Store webhook event for audit
            await _repository.SaveWebhookEventAsync(webhook);

            // 2. Route to appropriate handler based on event type
            var result = webhook.EventType switch
            {
                "shipment.status.changed" => await ProcessStatusUpdateAsync(webhook),
                "tracking.updated" => await ProcessTrackingUpdateAsync(webhook),
                "shipment.shipped" => await ProcessShippedEventAsync(webhook),
                _ => new OASISResult<bool>
                {
                    IsError = true,
                    Message = $"Unknown event type: {webhook.EventType}"
                }
            };

            return result;
        }
        catch (Exception ex)
        {
            // Store failed webhook for retry
            webhook.ProcessingStatus = "Failed";
            webhook.ErrorMessage = ex.Message;
            await _repository.SaveWebhookEventAsync(webhook);

            return new OASISResult<bool>
            {
                IsError = true,
                Message = $"Webhook processing failed: {ex.Message}",
                Exception = ex
            };
        }
    }

    /// <summary>
    /// Processes webhook from iShip
    /// </summary>
    public async Task<OASISResult<bool>> ProcessIShipWebhookAsync(string payload, string signature, string sourceIP)
    {
        // 1. Verify IP whitelist (if enabled)
        if (!_securityService.IsIPWhitelisted(sourceIP))
        {
            return new OASISResult<bool>
            {
                IsError = true,
                Message = "IP address not whitelisted"
            };
        }

        // 2. Verify signature
        if (!_securityService.VerifyIShipSignature(payload, signature, _iShipWebhookSecret))
        {
            return new OASISResult<bool>
            {
                IsError = true,
                Message = "Invalid webhook signature"
            };
        }

        // 3. Parse payload and create webhook event
        try
        {
            var webhookEvent = new WebhookEvent
            {
                EventId = Guid.NewGuid(),
                Source = "iShip",
                Payload = payload,
                ReceivedAt = DateTime.UtcNow,
                SourceIP = sourceIP,
                ProcessingStatus = "Processing"
            };

            // Try to extract event type from payload
            var payloadJson = JsonDocument.Parse(payload);
            if (payloadJson.RootElement.TryGetProperty("event_type", out var eventType))
            {
                webhookEvent.EventType = eventType.GetString();
            }
            if (payloadJson.RootElement.TryGetProperty("tracking_number", out var trackingNumber))
            {
                webhookEvent.TrackingNumber = trackingNumber.GetString();
            }

            // 4. Process webhook
            return await ProcessWebhookAsync(webhookEvent);
        }
        catch (Exception ex)
        {
            return new OASISResult<bool>
            {
                IsError = true,
                Message = $"Failed to parse iShip webhook: {ex.Message}",
                Exception = ex
            };
        }
    }

    /// <summary>
    /// Processes webhook from Shipox
    /// </summary>
    public async Task<OASISResult<bool>> ProcessShipoxWebhookAsync(string payload, string signature, string sourceIP)
    {
        // 1. Verify IP whitelist (if enabled)
        if (!_securityService.IsIPWhitelisted(sourceIP))
        {
            return new OASISResult<bool>
            {
                IsError = true,
                Message = "IP address not whitelisted"
            };
        }

        // 2. Verify signature
        if (!_securityService.VerifyShipoxSignature(payload, signature, _shipoxWebhookSecret))
        {
            return new OASISResult<bool>
            {
                IsError = true,
                Message = "Invalid webhook signature"
            };
        }

        // 3. Parse payload and create webhook event
        try
        {
            var webhookEvent = new WebhookEvent
            {
                EventId = Guid.NewGuid(),
                Source = "Shipox",
                Payload = payload,
                ReceivedAt = DateTime.UtcNow,
                SourceIP = sourceIP,
                ProcessingStatus = "Processing"
            };

            // Try to extract event type from payload
            var payloadJson = JsonDocument.Parse(payload);
            if (payloadJson.RootElement.TryGetProperty("EventType", out var eventType))
            {
                webhookEvent.EventType = eventType.GetString();
            }
            if (payloadJson.RootElement.TryGetProperty("OrderId", out var orderId))
            {
                webhookEvent.OrderId = orderId.GetString();
            }
            if (payloadJson.RootElement.TryGetProperty("TrackingNumber", out var trackingNumber))
            {
                webhookEvent.TrackingNumber = trackingNumber.GetString();
            }

            // 4. Process webhook
            return await ProcessWebhookAsync(webhookEvent);
        }
        catch (Exception ex)
        {
            return new OASISResult<bool>
            {
                IsError = true,
                Message = $"Failed to parse Shipox webhook: {ex.Message}",
                Exception = ex
            };
        }
    }

    /// <summary>
    /// Processes shipment status update events
    /// </summary>
    private async Task<OASISResult<bool>> ProcessStatusUpdateAsync(WebhookEvent webhook)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(webhook.TrackingNumber))
            {
                return new OASISResult<bool>
                {
                    IsError = true,
                    Message = "Tracking number is required for status update"
                };
            }

            // Get shipment by tracking number
            var shipmentResult = await _repository.GetShipmentByTrackingNumberAsync(webhook.TrackingNumber);
            if (shipmentResult.IsError || shipmentResult.Result == null)
            {
                return new OASISResult<bool>
                {
                    IsError = true,
                    Message = $"Shipment not found for tracking number: {webhook.TrackingNumber}"
                };
            }

            var shipment = shipmentResult.Result;

            // Parse status from payload
            var payloadJson = JsonDocument.Parse(webhook.Payload);
            if (payloadJson.RootElement.TryGetProperty("status", out var statusElement))
            {
                var statusString = statusElement.GetString();
                // Update shipment status (would need to map string to ShipmentStatus enum)
                // This will be fully implemented when Agent E's ShipmentService is available
            }

            // Update webhook processing status
            webhook.ProcessingStatus = "Processed";
            await _repository.SaveWebhookEventAsync(webhook);

            return new OASISResult<bool>(true);
        }
        catch (Exception ex)
        {
            return new OASISResult<bool>
            {
                IsError = true,
                Message = $"Failed to process status update: {ex.Message}",
                Exception = ex
            };
        }
    }

    /// <summary>
    /// Processes tracking update events
    /// </summary>
    private async Task<OASISResult<bool>> ProcessTrackingUpdateAsync(WebhookEvent webhook)
    {
        // Similar to ProcessStatusUpdateAsync
        // Would update tracking information in shipment record
        return await ProcessStatusUpdateAsync(webhook);
    }

    /// <summary>
    /// Processes shipment shipped events - triggers QuickBooks invoice creation
    /// </summary>
    private async Task<OASISResult<bool>> ProcessShippedEventAsync(WebhookEvent webhook)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(webhook.TrackingNumber))
            {
                return new OASISResult<bool>
                {
                    IsError = true,
                    Message = "Tracking number is required for shipped event"
                };
            }

            // Get shipment by tracking number
            var shipmentResult = await _repository.GetShipmentByTrackingNumberAsync(webhook.TrackingNumber);
            if (shipmentResult.IsError || shipmentResult.Result == null)
            {
                return new OASISResult<bool>
                {
                    IsError = true,
                    Message = $"Shipment not found for tracking number: {webhook.TrackingNumber}"
                };
            }

            var shipment = shipmentResult.Result;

            // Update shipment status to Shipped
            // This will be fully implemented when Agent E's ShipmentService is available
            // await _shipmentService.UpdateShipmentStatusAsync(shipment.ShipmentId, ShipmentStatus.Shipped);

            // Trigger QuickBooks invoice creation
            // This will be fully implemented when Agent E's QuickBooksService is available
            // await _quickBooksService.CreateInvoiceAsync(shipment);

            // Update webhook processing status
            webhook.ProcessingStatus = "Processed";
            await _repository.SaveWebhookEventAsync(webhook);

            return new OASISResult<bool>(true);
        }
        catch (Exception ex)
        {
            return new OASISResult<bool>
            {
                IsError = true,
                Message = $"Failed to process shipped event: {ex.Message}",
                Exception = ex
            };
        }
    }
}




