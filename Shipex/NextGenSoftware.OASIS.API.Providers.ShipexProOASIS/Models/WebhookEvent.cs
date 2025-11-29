using System;

namespace NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Models
{
    /// <summary>
    /// WebhookEvent model - represents audit trail for all webhook events.
    /// Full implementation will be completed in Task 1.2 (Schema Design) and Task 1.3 (Repository).
    /// </summary>
    public class WebhookEvent
    {
        public Guid EventId { get; set; }
        public string Source { get; set; } = string.Empty; // "iShip", "Shipox", etc.
        public string EventType { get; set; } = string.Empty;
        public string Payload { get; set; } = string.Empty; // JSON string of full webhook data
        public string? Signature { get; set; }
        public string ProcessingStatus { get; set; } = "Pending"; // "Pending", "Processing", "Processed", "Failed", "Retrying", "Failed - Max Retries Exceeded"
        public string? ErrorMessage { get; set; }
        public int RetryCount { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public DateTime? ReceivedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? SourceIP { get; set; }
        public string? OrderId { get; set; }
        public string? TrackingNumber { get; set; }
        public DateTime? LastRetryAt { get; set; }
    }

    public enum ProcessingStatus
    {
        Pending,
        Processing,
        Processed,
        Failed,
        Retrying
    }
}

