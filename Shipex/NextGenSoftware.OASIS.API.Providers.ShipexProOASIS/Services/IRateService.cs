using System;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Models;

namespace NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Services
{
    /// <summary>
    /// Service interface for rate requests and quote management.
    /// This will be implemented by Agent E.
    /// </summary>
    public interface IRateService
    {
        /// <summary>
        /// Get shipping rates for a shipment request.
        /// Applies markups and returns quotes with both carrier rates and client prices.
        /// </summary>
        /// <param name="request">Rate request with shipment details</param>
        /// <returns>Quote response with available shipping options</returns>
        Task<OASISResult<QuoteResponse>> GetRatesAsync(RateRequest request);

        /// <summary>
        /// Get a quote by its ID.
        /// </summary>
        /// <param name="quoteId">Quote identifier</param>
        /// <returns>Quote details</returns>
        Task<OASISResult<Quote>> GetQuoteAsync(Guid quoteId);
    }

    /// <summary>
    /// Response model for rate requests
    /// </summary>
    public class QuoteResponse
    {
        public Guid QuoteId { get; set; }
        public List<ClientQuote> Quotes { get; set; } = new();
        public DateTime ExpiresAt { get; set; }
    }
}
