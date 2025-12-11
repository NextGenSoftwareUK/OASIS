using System;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Models;

namespace NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Services
{
    /// <summary>
    /// Service interface for shipment lifecycle management.
    /// This will be implemented by Agent E.
    /// </summary>
    public interface IShipmentService
    {
        /// <summary>
        /// Create a shipment from an accepted quote.
        /// </summary>
        /// <param name="request">Shipment creation request</param>
        /// <returns>Shipment response with tracking number and label</returns>
        Task<OASISResult<ShipmentResponse>> CreateShipmentAsync(CreateShipmentRequest request);

        /// <summary>
        /// Get a shipment by its ID.
        /// </summary>
        /// <param name="shipmentId">Shipment identifier</param>
        /// <returns>Shipment details</returns>
        Task<OASISResult<Shipment>> GetShipmentAsync(Guid shipmentId);

        /// <summary>
        /// Update shipment status.
        /// </summary>
        /// <param name="shipmentId">Shipment identifier</param>
        /// <param name="status">New status</param>
        /// <returns>Updated shipment</returns>
        Task<OASISResult<Shipment>> UpdateShipmentStatusAsync(Guid shipmentId, ShipmentStatus status);

        /// <summary>
        /// Get shipment by tracking number.
        /// </summary>
        /// <param name="trackingNumber">Tracking number</param>
        /// <returns>Shipment details</returns>
        Task<OASISResult<Shipment>> GetShipmentByTrackingNumberAsync(string trackingNumber);
    }

    /// <summary>
    /// Request model for creating a shipment
    /// </summary>
    public class CreateShipmentRequest
    {
        public Guid QuoteId { get; set; }
        public string SelectedCarrier { get; set; } = string.Empty;
        public CustomerInfo CustomerInfo { get; set; } = new();
    }

    /// <summary>
    /// Response model for shipment creation
    /// </summary>
    public class ShipmentResponse
    {
        public Guid ShipmentId { get; set; }
        public string TrackingNumber { get; set; } = string.Empty;
        public Label Label { get; set; } = new();
        public string Status { get; set; } = string.Empty;
    }
}
