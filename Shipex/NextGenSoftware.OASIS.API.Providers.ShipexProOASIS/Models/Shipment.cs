using System;
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Models
{
    /// <summary>
    /// Shipment model - represents a shipment record with full lifecycle tracking.
    /// Full implementation will be completed in Task 1.2 (Schema Design) and Task 1.3 (Repository).
    /// </summary>
    public class Shipment
    {
        public Guid ShipmentId { get; set; }
        public Guid MerchantId { get; set; }
        public Guid QuoteId { get; set; }
        public string CarrierShipmentId { get; set; }
        public string TrackingNumber { get; set; }
        public ShipmentStatus Status { get; set; }
        public Label Label { get; set; }
        public decimal AmountCharged { get; set; }
        public decimal CarrierCost { get; set; }
        public decimal MarkupAmount { get; set; }
        public Guid? CustomerId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public List<StatusHistory> StatusHistory { get; set; }
    }

    public class Label
    {
        public string PdfUrl { get; set; }
        public string PdfBase64 { get; set; }
        public string SignedUrl { get; set; }
    }

    public class StatusHistory
    {
        public ShipmentStatus Status { get; set; }
        public DateTime Timestamp { get; set; }
        public string Source { get; set; } // "webhook" or "api"
    }

    public enum ShipmentStatus
    {
        QuoteRequested,
        QuoteProvided,
        QuoteAccepted,
        ShipmentCreated,
        LabelGenerated,
        InTransit,
        Delivered,
        Cancelled,
        Error
    }
}

