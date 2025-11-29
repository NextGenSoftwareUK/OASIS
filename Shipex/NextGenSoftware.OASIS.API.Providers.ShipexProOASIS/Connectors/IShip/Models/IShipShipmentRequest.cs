using System;

namespace NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Connectors.IShip.Models
{
    /// <summary>
    /// Request model for iShip shipment creation API.
    /// </summary>
    public class IShipShipmentRequest
    {
        /// <summary>
        /// Quote ID from previous rate request (optional).
        /// </summary>
        public string QuoteId { get; set; }

        /// <summary>
        /// Selected carrier code (e.g., "UPS", "FedEx").
        /// </summary>
        public string Carrier { get; set; }

        /// <summary>
        /// Selected service code.
        /// </summary>
        public string ServiceCode { get; set; }

        /// <summary>
        /// Package dimensions.
        /// </summary>
        public IShipDimensions Dimensions { get; set; }

        /// <summary>
        /// Package weight in pounds.
        /// </summary>
        public decimal Weight { get; set; }

        /// <summary>
        /// Origin address.
        /// </summary>
        public IShipAddress Origin { get; set; }

        /// <summary>
        /// Destination address.
        /// </summary>
        public IShipAddress Destination { get; set; }

        /// <summary>
        /// Customer information.
        /// </summary>
        public IShipCustomerInfo CustomerInfo { get; set; }

        /// <summary>
        /// Package value for insurance.
        /// </summary>
        public decimal? PackageValue { get; set; }

        /// <summary>
        /// Delivery confirmation type.
        /// </summary>
        public string DeliveryConfirmation { get; set; }

        /// <summary>
        /// Signature required flag.
        /// </summary>
        public bool SignatureRequired { get; set; }

        /// <summary>
        /// Reference number for tracking (optional).
        /// </summary>
        public string ReferenceNumber { get; set; }

        /// <summary>
        /// Additional notes or instructions.
        /// </summary>
        public string Notes { get; set; }
    }

    /// <summary>
    /// Customer information for shipment.
    /// </summary>
    public class IShipCustomerInfo
    {
        public string Name { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Company { get; set; }
    }
}

