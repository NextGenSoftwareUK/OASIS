using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Connectors.IShip.Models;
using NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Models;
using NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Services;

namespace NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Connectors.IShip
{
    /// <summary>
    /// iShip Connector Service - Handles all communication with iShip API.
    /// Implements rate requests, shipment creation, tracking, and webhook registration.
    /// </summary>
    public class IShipConnectorService
    {
        private readonly IShipApiClient _apiClient;
        private readonly ILogger<IShipConnectorService> _logger;
        private bool _initialized = false;

        public IShipConnectorService(string baseUrl, ISecretVaultService secretVault, Guid? merchantId = null, ILogger<IShipConnectorService> logger = null)
        {
            _logger = logger;
            _apiClient = new IShipApiClient(baseUrl, secretVault, merchantId, logger);
        }

        /// <summary>
        /// Ensures the API client is initialized with credentials from vault
        /// </summary>
        private async Task EnsureInitializedAsync()
        {
            if (!_initialized)
            {
                await _apiClient.InitializeAsync();
                _initialized = true;
            }
        }

        /// <summary>
        /// Gets shipping rates from iShip for the given rate request.
        /// Transforms internal RateRequest to iShip format, calls API, and transforms response.
        /// </summary>
        /// <param name="request">Internal rate request model</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of rate quotes from multiple carriers</returns>
        public async Task<OASISResult<List<CarrierRate>>> GetRatesAsync(
            RateRequest request,
            CancellationToken cancellationToken = default)
        {
            var result = new OASISResult<List<CarrierRate>>();

            try
            {
                await EnsureInitializedAsync();
                _logger?.LogInformation($"Getting rates for merchant {request.MerchantId}");

                // 1. Transform internal RateRequest to iShip format
                var iShipRequest = TransformToIShipRateRequest(request);

                // 2. Call iShip API
                var apiResult = await _apiClient.PostAsync<IShipRateResponse>(
                    "/api/rates",
                    iShipRequest,
                    cancellationToken);

                if (apiResult.IsError || !apiResult.Result.Success)
                {
                    var errorMessage = apiResult.IsError 
                        ? apiResult.Message 
                        : apiResult.Result?.Message ?? "Unknown error from iShip API";
                    
                    _logger?.LogError($"Failed to get rates from iShip: {errorMessage}");
                    OASISErrorHandling.HandleError(ref result, errorMessage);
                    return result;
                }

                // 3. Transform iShip response to internal CarrierRate format
                var quotes = apiResult.Result.Rates?.Select(TransformToCarrierRate).ToList() ?? new List<CarrierRate>();

                result.Result = quotes;
                result.IsError = false;

                _logger?.LogInformation($"Successfully retrieved {quotes.Count} rate quotes from iShip");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Exception occurred while getting rates from iShip");
                OASISErrorHandling.HandleError(ref result, $"Exception getting rates: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Creates a shipment via iShip API and retrieves tracking number and label.
        /// </summary>
        /// <param name="request">Shipment creation request (OrderRequest)</param>
        /// <param name="quote">Original quote with shipment details</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Shipment response with tracking number and label</returns>
        public async Task<OASISResult<Shipment>> CreateShipmentAsync(
            OrderRequest request,
            Quote quote,
            CancellationToken cancellationToken = default)
        {
            var result = new OASISResult<Shipment>();

            try
            {
                await EnsureInitializedAsync();
                _logger?.LogInformation($"Creating shipment for quote {request.QuoteId} with carrier {request.SelectedCarrier}");

                // 1. Transform to iShip shipment format
                var iShipRequest = TransformToIShipShipmentRequest(request, quote);

                // 2. Call iShip create shipment API
                var apiResult = await _apiClient.PostAsync<IShipShipmentResponse>(
                    "/api/shipments",
                    iShipRequest,
                    cancellationToken);

                if (apiResult.IsError || !apiResult.Result.Success)
                {
                    var errorMessage = apiResult.IsError 
                        ? apiResult.Message 
                        : apiResult.Result?.Message ?? "Unknown error from iShip API";
                    
                    _logger?.LogError($"Failed to create shipment in iShip: {errorMessage}");
                    OASISErrorHandling.HandleError(ref result, errorMessage);
                    return result;
                }

                // 3. Extract tracking number and label
                var shipmentData = apiResult.Result.Shipment;

                // 4. Transform to internal Shipment model
                var shipment = new Shipment
                {
                    ShipmentId = Guid.NewGuid(), // Will be set by repository
                    MerchantId = quote.MerchantId,
                    QuoteId = request.QuoteId,
                    CarrierShipmentId = shipmentData.ShipmentId,
                    TrackingNumber = shipmentData.TrackingNumber,
                    Status = ShipmentStatus.ShipmentCreated,
                    Label = new Label
                    {
                        PdfBase64 = shipmentData.Label?.PdfBase64,
                        PdfUrl = shipmentData.Label?.PdfUrl,
                        SignedUrl = shipmentData.Label?.SignedUrl
                    },
                    CarrierCost = shipmentData.Cost,
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

                result.Result = shipment;
                result.IsError = false;

                _logger?.LogInformation($"Successfully created shipment with tracking number {shipmentData.TrackingNumber}");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Exception occurred while creating shipment in iShip");
                OASISErrorHandling.HandleError(ref result, $"Exception creating shipment: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Tracks a shipment by tracking number.
        /// Returns tracking information that can be used to update shipment status.
        /// </summary>
        /// <param name="trackingNumber">Carrier tracking number</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Tracking information with status and history</returns>
        public async Task<OASISResult<IShipTrackingData>> TrackShipmentAsync(
            string trackingNumber,
            CancellationToken cancellationToken = default)
        {
            var result = new OASISResult<IShipTrackingData>();

            try
            {
                await EnsureInitializedAsync();
                _logger?.LogInformation($"Tracking shipment {trackingNumber}");

                // 1. Call iShip tracking API
                var apiResult = await _apiClient.GetAsync<IShipTrackingResponse>(
                    $"/api/tracking/{trackingNumber}",
                    cancellationToken);

                if (apiResult.IsError || !apiResult.Result.Success)
                {
                    var errorMessage = apiResult.IsError 
                        ? apiResult.Message 
                        : apiResult.Result?.Message ?? "Unknown error from iShip API";
                    
                    _logger?.LogError($"Failed to track shipment in iShip: {errorMessage}");
                    OASISErrorHandling.HandleError(ref result, errorMessage);
                    return result;
                }

                result.Result = apiResult.Result.Tracking;
                result.IsError = false;

                _logger?.LogInformation($"Successfully retrieved tracking info for {trackingNumber}, status: {apiResult.Result.Tracking.Status}");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Exception occurred while tracking shipment {trackingNumber}");
                OASISErrorHandling.HandleError(ref result, $"Exception tracking shipment: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Registers a webhook with iShip for status updates.
        /// </summary>
        /// <param name="shipmentId">iShip shipment ID</param>
        /// <param name="webhookUrl">URL where iShip will POST status updates</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Success/failure result</returns>
        public async Task<OASISResult<bool>> RegisterWebhookAsync(
            string shipmentId,
            string webhookUrl,
            CancellationToken cancellationToken = default)
        {
            var result = new OASISResult<bool>();

            try
            {
                await EnsureInitializedAsync();
                _logger?.LogInformation($"Registering webhook for shipment {shipmentId} to {webhookUrl}");

                var webhookRequest = new IShipWebhookRegistrationRequest
                {
                    ShipmentId = shipmentId,
                    WebhookUrl = webhookUrl,
                    Events = null // Subscribe to all events
                };

                // 1. Call iShip webhook registration API
                var apiResult = await _apiClient.PostAsync<IShipWebhookRegistrationResponse>(
                    "/api/webhooks/register",
                    webhookRequest,
                    cancellationToken);

                if (apiResult.IsError || !apiResult.Result.Success)
                {
                    var errorMessage = apiResult.IsError 
                        ? apiResult.Message 
                        : apiResult.Result?.Message ?? "Unknown error from iShip API";
                    
                    _logger?.LogError($"Failed to register webhook in iShip: {errorMessage}");
                    OASISErrorHandling.HandleError(ref result, errorMessage);
                    return result;
                }

                result.Result = true;
                result.IsError = false;

                _logger?.LogInformation($"Successfully registered webhook for shipment {shipmentId}");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Exception occurred while registering webhook for shipment {shipmentId}");
                OASISErrorHandling.HandleError(ref result, $"Exception registering webhook: {ex.Message}");
            }

            return result;
        }

        #region Transformation Methods

        /// <summary>
        /// Transforms internal RateRequest to iShip API format.
        /// </summary>
        private IShipRateRequest TransformToIShipRateRequest(RateRequest request)
        {
            return new IShipRateRequest
            {
                Dimensions = new IShipDimensions
                {
                    Length = request.Dimensions.Length,
                    Width = request.Dimensions.Width,
                    Height = request.Dimensions.Height,
                    Unit = "IN"
                },
                Weight = request.Weight,
                Origin = TransformToIShipAddress(request.Origin),
                Destination = TransformToIShipAddress(request.Destination),
                ServiceLevel = request.ServiceLevel,
                PackageValue = request.PackageValue,
                SignatureRequired = request.SignatureRequired
            };
        }

        /// <summary>
        /// Transforms iShip rate quote to internal CarrierRate format.
        /// </summary>
        private CarrierRate TransformToCarrierRate(IShipRateQuote iShipQuote)
        {
            return new CarrierRate
            {
                Carrier = iShipQuote.Carrier,
                ServiceName = iShipQuote.ServiceName,
                Rate = iShipQuote.Rate,
                EstimatedDays = iShipQuote.EstimatedDays
            };
        }

        /// <summary>
        /// Transforms internal OrderRequest to iShip shipment format.
        /// </summary>
        private IShipShipmentRequest TransformToIShipShipmentRequest(OrderRequest request, Quote quote)
        {
            var selectedCarrierRate = quote.CarrierRates.FirstOrDefault(r => r.Carrier == request.SelectedCarrier);
            var shipmentDetails = quote.ShipmentDetails;

            return new IShipShipmentRequest
            {
                QuoteId = quote.QuoteId.ToString(),
                Carrier = request.SelectedCarrier,
                ServiceCode = selectedCarrierRate?.ServiceName, // May need mapping
                Dimensions = new IShipDimensions
                {
                    Length = shipmentDetails.Dimensions.Length,
                    Width = shipmentDetails.Dimensions.Width,
                    Height = shipmentDetails.Dimensions.Height,
                    Unit = "IN"
                },
                Weight = shipmentDetails.Weight,
                Origin = TransformToIShipAddress(shipmentDetails.Origin),
                Destination = TransformToIShipAddress(shipmentDetails.Destination),
                CustomerInfo = new IShipCustomerInfo
                {
                    Name = request.CustomerInfo.Name,
                    Email = request.CustomerInfo.Email,
                    Phone = request.CustomerInfo.Phone,
                    Company = request.CustomerInfo.Name // Using Name as Company fallback
                }
            };
        }

        /// <summary>
        /// Transforms internal Address to iShip format.
        /// </summary>
        private IShipAddress TransformToIShipAddress(Address address)
        {
            return new IShipAddress
            {
                AddressLine1 = address.Street,
                City = address.City,
                State = address.State,
                PostalCode = address.PostalCode,
                Country = string.IsNullOrEmpty(address.Country) ? "US" : address.Country
            };
        }

        #endregion

        public void Dispose()
        {
            _apiClient?.Dispose();
        }
    }
}

