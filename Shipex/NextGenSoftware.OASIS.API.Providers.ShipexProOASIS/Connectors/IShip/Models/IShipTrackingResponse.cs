using System;
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Connectors.IShip.Models
{
    /// <summary>
    /// Response model from iShip tracking API.
    /// </summary>
    public class IShipTrackingResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public IShipTrackingData Tracking { get; set; }
        public string RequestId { get; set; }
    }

    /// <summary>
    /// Tracking data for a shipment.
    /// </summary>
    public class IShipTrackingData
    {
        /// <summary>
        /// Tracking number.
        /// </summary>
        public string TrackingNumber { get; set; }

        /// <summary>
        /// Carrier name.
        /// </summary>
        public string Carrier { get; set; }

        /// <summary>
        /// Current status (e.g., "InTransit", "Delivered", "Exception").
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// Current location (if available).
        /// </summary>
        public string CurrentLocation { get; set; }

        /// <summary>
        /// Estimated delivery date.
        /// </summary>
        public DateTime? EstimatedDeliveryDate { get; set; }

        /// <summary>
        /// Actual delivery date (if delivered).
        /// </summary>
        public DateTime? DeliveredDate { get; set; }

        /// <summary>
        /// Delivery address.
        /// </summary>
        public string DeliveryAddress { get; set; }

        /// <summary>
        /// Tracking history/events.
        /// </summary>
        public List<IShipTrackingEvent> History { get; set; }

        /// <summary>
        /// Additional tracking information.
        /// </summary>
        public Dictionary<string, object> AdditionalInfo { get; set; }
    }

    /// <summary>
    /// Individual tracking event/history entry.
    /// </summary>
    public class IShipTrackingEvent
    {
        /// <summary>
        /// Event status/description.
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// Event location.
        /// </summary>
        public string Location { get; set; }

        /// <summary>
        /// Event timestamp.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Event description/details.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Event code (carrier-specific).
        /// </summary>
        public string EventCode { get; set; }
    }
}




