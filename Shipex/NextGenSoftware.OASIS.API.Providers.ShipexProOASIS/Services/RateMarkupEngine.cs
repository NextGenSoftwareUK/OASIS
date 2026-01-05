using System;
using System.Collections.Generic;
using System.Linq;
using NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Models;

namespace NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Services
{
    /// <summary>
    /// Engine for applying markups to carrier rates.
    /// Handles both fixed and percentage markups with proper selection logic.
    /// </summary>
    public class RateMarkupEngine
    {
        /// <summary>
        /// Applies a markup configuration to a carrier rate quote.
        /// </summary>
        /// <param name="carrierQuote">Original carrier rate quote</param>
        /// <param name="markup">Markup configuration to apply</param>
        /// <returns>Quote with markup applied (client price)</returns>
        public ClientQuote ApplyMarkup(CarrierRate carrierQuote, MarkupConfiguration markup)
        {
            if (markup == null)
            {
                // No markup - return quote with carrier rate as client price
                return new ClientQuote
                {
                    Carrier = carrierQuote.Carrier,
                    CarrierRate = carrierQuote.Rate,
                    ClientPrice = carrierQuote.Rate,
                    MarkupAmount = 0,
                    MarkupConfigId = null,
                    EstimatedDays = carrierQuote.EstimatedDays
                };
            }

            decimal clientPrice;
            
            if (markup.Type == MarkupType.Percentage)
            {
                // Percentage markup: multiply by (1 + percentage/100)
                clientPrice = carrierQuote.Rate * (1 + markup.Value / 100);
            }
            else // Fixed
            {
                // Fixed markup: add fixed amount
                clientPrice = carrierQuote.Rate + markup.Value;
            }
            
            return new ClientQuote
            {
                Carrier = carrierQuote.Carrier,
                CarrierRate = carrierQuote.Rate,
                ClientPrice = clientPrice,
                MarkupAmount = clientPrice - carrierQuote.Rate,
                MarkupConfigId = markup.MarkupId,
                EstimatedDays = carrierQuote.EstimatedDays
            };
        }
        
        /// <summary>
        /// Selects the appropriate markup configuration from a list.
        /// Priority: Merchant-specific > Carrier-specific > Global
        /// Filters by carrier, active status, and effective date ranges.
        /// </summary>
        /// <param name="markups">List of markup configurations</param>
        /// <param name="carrier">Carrier name to match</param>
        /// <param name="merchantId">Optional merchant ID for merchant-specific markups</param>
        /// <returns>Best matching markup configuration, or null if none found</returns>
        public MarkupConfiguration SelectMarkup(
            List<MarkupConfiguration> markups, 
            string carrier,
            Guid? merchantId = null)
        {
            if (markups == null || !markups.Any())
                return null;

            var now = DateTime.UtcNow;
            
            // Filter by carrier, active status, and effective date ranges
            var applicableMarkups = markups
                .Where(m => m.Carrier.Equals(carrier, StringComparison.OrdinalIgnoreCase) && m.IsActive)
                .Where(m => now >= m.EffectiveFrom)
                .Where(m => !m.EffectiveTo.HasValue || now <= m.EffectiveTo.Value)
                .ToList();

            if (!applicableMarkups.Any())
                return null;

            // Priority: Merchant-specific > Global (null MerchantId)
            // Order by MerchantId.HasValue descending (true first, then false)
            return applicableMarkups
                .OrderByDescending(m => merchantId.HasValue && m.MerchantId == merchantId) // Merchant-specific first
                .ThenByDescending(m => m.MerchantId.HasValue) // Then global markups
                .FirstOrDefault();
        }
    }
}

