namespace NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Connectors.IShip.Models
{
    /// <summary>
    /// Request model for registering a webhook with iShip.
    /// </summary>
    public class IShipWebhookRegistrationRequest
    {
        /// <summary>
        /// Shipment ID to register webhook for.
        /// </summary>
        public string ShipmentId { get; set; }

        /// <summary>
        /// Webhook URL where iShip will POST status updates.
        /// </summary>
        public string WebhookUrl { get; set; }

        /// <summary>
        /// Events to subscribe to (optional, null = all events).
        /// </summary>
        public string[] Events { get; set; }

        /// <summary>
        /// Secret key for webhook signature verification (optional).
        /// </summary>
        public string SecretKey { get; set; }
    }

    /// <summary>
    /// Response model for webhook registration.
    /// </summary>
    public class IShipWebhookRegistrationResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string WebhookId { get; set; }
    }
}

