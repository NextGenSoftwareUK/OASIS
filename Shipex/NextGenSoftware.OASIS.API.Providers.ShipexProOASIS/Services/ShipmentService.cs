using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Connectors.IShip;
using NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Models;
using NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Repositories;

namespace NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Services
{
    /// <summary>
    /// Service for orchestrating the complete shipment lifecycle.
    /// Implements IShipmentService interface.
    /// </summary>
    public class ShipmentService : IShipmentService
    {
        private readonly IShipConnectorService _iShipConnector;
        private readonly IShipexProRepository _repository;
        private readonly IQuickBooksService _quickBooksService;
        private readonly ILogger<ShipmentService> _logger;
        private readonly string _webhookBaseUrl;

        public ShipmentService(
            IShipConnectorService iShipConnector,
            IShipexProRepository repository,
            IQuickBooksService quickBooksService,
            ILogger<ShipmentService> logger = null,
            string webhookBaseUrl = null)
        {
            _iShipConnector = iShipConnector ?? throw new ArgumentNullException(nameof(iShipConnector));
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _quickBooksService = quickBooksService ?? throw new ArgumentNullException(nameof(quickBooksService));
            _logger = logger;
            _webhookBaseUrl = webhookBaseUrl ?? "https://api.shipexpro.com/api/shipexpro/webhooks/iship";
        }
    
        public async Task<OASISResult<ShipmentResponse>> CreateShipmentAsync(CreateShipmentRequest request)
        {
            var result = new OASISResult<ShipmentResponse>();

            try
            {
                _logger?.LogInformation($"Creating shipment for quote {request.QuoteId} with carrier {request.SelectedCarrier}");

                // 1. Load quote
                var quoteResult = await _repository.GetQuoteAsync(request.QuoteId);
                if (quoteResult.IsError || quoteResult.Result == null)
                {
                    var errorMsg = quoteResult.IsError ? quoteResult.Message : "Quote not found";
                    _logger?.LogError($"Failed to load quote: {errorMsg}");
                    OASISErrorHandling.HandleError(ref result, errorMsg);
                    return result;
                }

                var quote = quoteResult.Result;

                // Check if quote is expired
                if (quote.ExpiresAt < DateTime.UtcNow)
                {
                    _logger?.LogWarning($"Quote {request.QuoteId} has expired");
                    OASISErrorHandling.HandleError(ref result, "Quote has expired");
                    return result;
                }

                // 2. Find selected quote
                var selectedQuote = quote.ClientQuotes.FirstOrDefault(q => q.Carrier == request.SelectedCarrier);
                if (selectedQuote == null)
                {
                    _logger?.LogError($"Selected carrier {request.SelectedCarrier} not found in quote");
                    OASISErrorHandling.HandleError(ref result, $"Carrier {request.SelectedCarrier} not found in quote");
                    return result;
                }

                // 3. Create shipment with iShip
                var orderRequest = new OrderRequest
                {
                    QuoteId = request.QuoteId,
                    SelectedCarrier = request.SelectedCarrier,
                    CustomerInfo = request.CustomerInfo
                };

                var iShipResult = await _iShipConnector.CreateShipmentAsync(orderRequest, quote);
                if (iShipResult.IsError || iShipResult.Result == null)
                {
                    var errorMsg = iShipResult.IsError ? iShipResult.Message : "Failed to create shipment in iShip";
                    _logger?.LogError($"Failed to create shipment in iShip: {errorMsg}");
                    OASISErrorHandling.HandleError(ref result, errorMsg);
                    return result;
                }

                var iShipShipment = iShipResult.Result;

                // 4. Store shipment in database
                var shipment = new Shipment
                {
                    ShipmentId = Guid.NewGuid(),
                    MerchantId = quote.MerchantId,
                    QuoteId = request.QuoteId,
                    CarrierShipmentId = iShipShipment.CarrierShipmentId,
                    TrackingNumber = iShipShipment.TrackingNumber,
                    Label = iShipShipment.Label ?? new Label(),
                    Status = ShipmentStatus.ShipmentCreated,
                    AmountCharged = selectedQuote.ClientPrice,
                    CarrierCost = quote.CarrierRates.First(r => r.Carrier == request.SelectedCarrier).Rate,
                    MarkupAmount = selectedQuote.MarkupAmount,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    StatusHistory = new List<StatusHistory>
                    {
                        new StatusHistory
                        {
                            Status = ShipmentStatus.ShipmentCreated,
                            Timestamp = DateTime.UtcNow,
                            Source = "api"
                        }
                    }
                };
            
                var saveResult = await _repository.SaveShipmentAsync(shipment);
                if (saveResult.IsError)
                {
                    _logger?.LogError($"Failed to save shipment: {saveResult.Message}");
                    OASISErrorHandling.HandleError(ref result, $"Failed to save shipment: {saveResult.Message}");
                    return result;
                }
            
                // 5. Register webhook for status updates
                if (!string.IsNullOrEmpty(shipment.CarrierShipmentId))
                {
                    var webhookResult = await _iShipConnector.RegisterWebhookAsync(
                        shipment.CarrierShipmentId,
                        _webhookBaseUrl);
                    
                    if (webhookResult.IsError)
                    {
                        _logger?.LogWarning($"Failed to register webhook for shipment {shipment.ShipmentId}: {webhookResult.Message}");
                        // Don't fail the shipment creation if webhook registration fails
                    }
                    else
                    {
                        _logger?.LogInformation($"Registered webhook for shipment {shipment.ShipmentId}");
                    }
                }
            
                result.Result = new ShipmentResponse
                {
                    ShipmentId = shipment.ShipmentId,
                    TrackingNumber = shipment.TrackingNumber,
                    Label = shipment.Label,
                    Status = shipment.Status.ToString()
                };
                result.IsError = false;

                _logger?.LogInformation($"Successfully created shipment {shipment.ShipmentId} with tracking number {shipment.TrackingNumber}");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Exception occurred while creating shipment");
                OASISErrorHandling.HandleError(ref result, $"Failed to create shipment: {ex.Message}");
            }

            return result;
        }

        public async Task<OASISResult<Shipment>> GetShipmentAsync(Guid shipmentId)
        {
            var result = new OASISResult<Shipment>();

            try
            {
                _logger?.LogInformation($"Getting shipment {shipmentId}");

                var shipmentResult = await _repository.GetShipmentAsync(shipmentId);
                if (shipmentResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, shipmentResult.Message);
                    return result;
                }

                if (shipmentResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Shipment {shipmentId} not found");
                    return result;
                }

                result.Result = shipmentResult.Result;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Exception occurred while getting shipment {shipmentId}");
                OASISErrorHandling.HandleError(ref result, $"Failed to get shipment: {ex.Message}");
            }

            return result;
        }

        public async Task<OASISResult<Shipment>> UpdateShipmentStatusAsync(Guid shipmentId, ShipmentStatus status)
        {
            var result = new OASISResult<Shipment>();

            try
            {
                _logger?.LogInformation($"Updating shipment {shipmentId} status to {status}");

                // 1. Get shipment
                var shipmentResult = await _repository.GetShipmentAsync(shipmentId);
                if (shipmentResult.IsError || shipmentResult.Result == null)
                {
                    var errorMsg = shipmentResult.IsError ? shipmentResult.Message : "Shipment not found";
                    OASISErrorHandling.HandleError(ref result, errorMsg);
                    return result;
                }

                var shipment = shipmentResult.Result;
        
                // 2. Validate status transition
                if (!ShipmentStatusValidator.IsValidTransition(shipment.Status, status))
                {
                    var errorMsg = ShipmentStatusValidator.GetTransitionErrorMessage(shipment.Status, status);
                    _logger?.LogWarning($"Invalid status transition for shipment {shipmentId}: {errorMsg}");
                    OASISErrorHandling.HandleError(ref result, errorMsg);
                    return result;
                }
        
                // 3. Update status
                shipment.Status = status;
                shipment.UpdatedAt = DateTime.UtcNow;
                
                // Initialize StatusHistory if null
                if (shipment.StatusHistory == null)
                {
                    shipment.StatusHistory = new List<StatusHistory>();
                }
                
                shipment.StatusHistory.Add(new StatusHistory
                {
                    Status = status,
                    Timestamp = DateTime.UtcNow,
                    Source = "api"
                });
        
                // 4. Save updated shipment
                var updateResult = await _repository.UpdateShipmentAsync(shipment);
                if (updateResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, updateResult.Message);
                    return result;
                }
        
                // 5. If delivered, trigger QuickBooks invoice creation
                if (status == ShipmentStatus.Delivered)
                {
                    _logger?.LogInformation($"Shipment {shipmentId} delivered, triggering QuickBooks invoice creation");
                    
                    var invoiceResult = await _quickBooksService.CreateInvoiceAsync(shipment);
                    if (invoiceResult.IsError)
                    {
                        _logger?.LogError($"Failed to create QuickBooks invoice for shipment {shipmentId}: {invoiceResult.Message}");
                        // Don't fail the status update if invoice creation fails - it can be retried
                    }
                    else
                    {
                        _logger?.LogInformation($"Successfully created QuickBooks invoice for shipment {shipmentId}");
                    }
                }
        
                result.Result = shipment;
                result.IsError = false;

                _logger?.LogInformation($"Successfully updated shipment {shipmentId} status to {status}");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Exception occurred while updating shipment {shipmentId} status");
                OASISErrorHandling.HandleError(ref result, $"Failed to update shipment status: {ex.Message}");
            }

            return result;
        }

        public async Task<OASISResult<Shipment>> GetShipmentByTrackingNumberAsync(string trackingNumber)
        {
            var result = new OASISResult<Shipment>();

            try
            {
                _logger?.LogInformation($"Getting shipment by tracking number {trackingNumber}");

                var shipmentResult = await _repository.GetShipmentByTrackingNumberAsync(trackingNumber);
                if (shipmentResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, shipmentResult.Message);
                    return result;
                }

                if (shipmentResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Shipment with tracking number {trackingNumber} not found");
                    return result;
                }

                result.Result = shipmentResult.Result;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Exception occurred while getting shipment by tracking number {trackingNumber}");
                OASISErrorHandling.HandleError(ref result, $"Failed to get shipment: {ex.Message}");
            }

            return result;
        }
    }
}




