using System;
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Models
{
    /// <summary>
    /// Represents a failed operation that can be retried or moved to dead letter queue.
    /// </summary>
    public class FailedOperation
    {
        public Guid FailedOperationId { get; set; }
        public string OperationType { get; set; } = string.Empty; // "CreateShipment", "CreateInvoice", "WebhookProcessing", etc.
        public string OperationId { get; set; } = string.Empty; // ID of the operation (shipmentId, invoiceId, etc.)
        public object OperationData { get; set; } // Serialized operation data
        public string ErrorMessage { get; set; } = string.Empty;
        public string StackTrace { get; set; } = string.Empty;
        public int RetryCount { get; set; }
        public int MaxRetries { get; set; } = 3;
        public DateTime FirstFailedAt { get; set; }
        public DateTime LastRetryAt { get; set; }
        public DateTime? NextRetryAt { get; set; }
        public bool IsInDeadLetterQueue { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// Operation types for failed operations
    /// </summary>
    public static class OperationType
    {
        public const string CreateShipment = "CreateShipment";
        public const string CreateInvoice = "CreateInvoice";
        public const string WebhookProcessing = "WebhookProcessing";
        public const string UpdateShipmentStatus = "UpdateShipmentStatus";
        public const string RegisterWebhook = "RegisterWebhook";
    }
}




