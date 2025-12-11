using System;
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Connectors.IShip.Models
{
    /// <summary>
    /// Response model from iShip shipment creation API.
    /// </summary>
    public class IShipShipmentResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public IShipShipmentData Shipment { get; set; }
        public string RequestId { get; set; }
    }

    /// <summary>
    /// Shipment data returned from iShip.
    /// </summary>
    public class IShipShipmentData
    {
        /// <summary>
        /// iShip shipment ID.
        /// </summary>
        public string ShipmentId { get; set; }

        /// <summary>
        /// Carrier tracking number.
        /// </summary>
        public string TrackingNumber { get; set; }

        /// <summary>
        /// Carrier name.
        /// </summary>
        public string Carrier { get; set; }

        /// <summary>
        /// Service code used.
        /// </summary>
        public string ServiceCode { get; set; }

        /// <summary>
        /// Shipping label information.
        /// </summary>
        public IShipLabel Label { get; set; }

        /// <summary>
        /// Shipping cost.
        /// </summary>
        public decimal Cost { get; set; }

        /// <summary>
        /// Currency code.
        /// </summary>
        public string Currency { get; set; } = "USD";

        /// <summary>
        /// Shipment status.
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// Created timestamp.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Additional carrier-specific data.
        /// </summary>
        public Dictionary<string, object> AdditionalData { get; set; }
    }

    /// <summary>
    /// Shipping label information.
    /// </summary>
    public class IShipLabel
    {
        /// <summary>
        /// Label as base64-encoded PDF string.
        /// </summary>
        public string PdfBase64 { get; set; }

        /// <summary>
        /// Label PDF URL (if available).
        /// </summary>
        public string PdfUrl { get; set; }

        /// <summary>
        /// Signed URL with expiration (if available).
        /// </summary>
        public string SignedUrl { get; set; }

        /// <summary>
        /// URL expiration time (for signed URLs).
        /// </summary>
        public DateTime? UrlExpiresAt { get; set; }

        /// <summary>
        /// Label format (e.g., "PDF", "ZPL", "EPL").
        /// </summary>
        public string Format { get; set; } = "PDF";
    }
}




