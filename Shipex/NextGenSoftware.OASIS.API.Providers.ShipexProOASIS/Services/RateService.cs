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
    /// Service for handling rate requests and markup application.
    /// Implements IRateService interface.
    /// </summary>
    public class RateService : IRateService
    {
        private readonly IShipConnectorService _iShipConnector;
        private readonly RateMarkupEngine _markupEngine;
        private readonly IShipexProRepository _repository;
        private readonly ILogger<RateService> _logger;

        public RateService(
            IShipConnectorService iShipConnector,
            RateMarkupEngine markupEngine,
            IShipexProRepository repository,
            ILogger<RateService> logger = null)
        {
            _iShipConnector = iShipConnector ?? throw new ArgumentNullException(nameof(iShipConnector));
            _markupEngine = markupEngine ?? throw new ArgumentNullException(nameof(markupEngine));
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _logger = logger;
        }
    
        public async Task<OASISResult<QuoteResponse>> GetRatesAsync(RateRequest request)
        {
            var result = new OASISResult<QuoteResponse>();

            try
            {
                _logger?.LogInformation($"Getting rates for merchant {request.MerchantId}");

                // 1. Get carrier rates from iShip
                var carrierRatesResult = await _iShipConnector.GetRatesAsync(request);
                if (carrierRatesResult.IsError)
                {
                    _logger?.LogError($"Failed to get carrier rates: {carrierRatesResult.Message}");
                    OASISErrorHandling.HandleError(ref result, carrierRatesResult.Message);
                    return result;
                }

                if (carrierRatesResult.Result == null || !carrierRatesResult.Result.Any())
                {
                    OASISErrorHandling.HandleError(ref result, "No carrier rates returned from iShip");
                    return result;
                }

                // 2. Get markup configurations for merchant
                var markupsResult = await _repository.GetActiveMarkupsAsync(request.MerchantId);
                var markups = markupsResult.Result ?? new List<MarkupConfiguration>();
                
                _logger?.LogInformation($"Retrieved {markups.Count} markup configurations for merchant {request.MerchantId}");
            
                // 3. Apply markup to each rate
                var quotes = new List<ClientQuote>();
                foreach (var carrierRate in carrierRatesResult.Result)
                {
                    var markup = _markupEngine.SelectMarkup(markups, carrierRate.Carrier, request.MerchantId);
                    var clientQuote = _markupEngine.ApplyMarkup(carrierRate, markup);
                    quotes.Add(clientQuote);
                }
            
                // 4. Store quote in database
                var quote = new Quote
                {
                    QuoteId = Guid.NewGuid(),
                    MerchantId = request.MerchantId,
                    ShipmentDetails = request,
                    CarrierRates = carrierRatesResult.Result,
                    ClientQuotes = quotes,
                    ExpiresAt = DateTime.UtcNow.AddHours(24),
                    CreatedAt = DateTime.UtcNow
                };
            
                var saveResult = await _repository.SaveQuoteAsync(quote);
                if (saveResult.IsError)
                {
                    _logger?.LogError($"Failed to save quote: {saveResult.Message}");
                    OASISErrorHandling.HandleError(ref result, $"Failed to save quote: {saveResult.Message}");
                    return result;
                }
            
                result.Result = new QuoteResponse
                {
                    QuoteId = quote.QuoteId,
                    Quotes = quotes,
                    ExpiresAt = quote.ExpiresAt
                };
                result.IsError = false;

                _logger?.LogInformation($"Successfully created quote {quote.QuoteId} with {quotes.Count} options");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Exception occurred while getting rates");
                OASISErrorHandling.HandleError(ref result, $"Failed to get rates: {ex.Message}");
            }

            return result;
        }

        public async Task<OASISResult<Quote>> GetQuoteAsync(Guid quoteId)
        {
            var result = new OASISResult<Quote>();

            try
            {
                _logger?.LogInformation($"Getting quote {quoteId}");

                var quoteResult = await _repository.GetQuoteAsync(quoteId);
                if (quoteResult.IsError)
                {
                    _logger?.LogError($"Failed to get quote: {quoteResult.Message}");
                    OASISErrorHandling.HandleError(ref result, quoteResult.Message);
                    return result;
                }

                if (quoteResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Quote {quoteId} not found");
                    return result;
                }

                result.Result = quoteResult.Result;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Exception occurred while getting quote {quoteId}");
                OASISErrorHandling.HandleError(ref result, $"Failed to get quote: {ex.Message}");
            }

            return result;
        }
    }
}




