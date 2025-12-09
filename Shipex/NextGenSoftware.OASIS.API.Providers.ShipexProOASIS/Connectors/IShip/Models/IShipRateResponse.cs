using System;
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Connectors.IShip.Models
{
    /// <summary>
    /// Response model from iShip rate API.
    /// </summary>
    public class IShipRateResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public List<IShipRateQuote> Rates { get; set; }
        public string RequestId { get; set; }
    }

    /// <summary>
    /// Individual rate quote from a carrier.
    /// </summary>
    public class IShipRateQuote
    {
        /// <summary>
        /// Carrier name (e.g., "UPS", "FedEx", "DHL").
        /// </summary>
        public string Carrier { get; set; }

        /// <summary>
        /// Service name (e.g., "Ground", "2 Day Air", "Next Day Air").
        /// </summary>
        public string ServiceName { get; set; }

        /// <summary>
        /// Service code (e.g., "03", "02", "01").
        /// </summary>
        public string ServiceCode { get; set; }

        /// <summary>
        /// Shipping rate in USD.
        /// </summary>
        public decimal Rate { get; set; }

        /// <summary>
        /// Estimated delivery days.
        /// </summary>
        public int EstimatedDays { get; set; }

        /// <summary>
        /// Estimated delivery date.
        /// </summary>
        public DateTime? EstimatedDeliveryDate { get; set; }

        /// <summary>
        /// Currency code (default: "USD").
        /// </summary>
        public string Currency { get; set; } = "USD";

        /// <summary>
        /// Additional carrier-specific information.
        /// </summary>
        public Dictionary<string, object> AdditionalInfo { get; set; }
    }
}




