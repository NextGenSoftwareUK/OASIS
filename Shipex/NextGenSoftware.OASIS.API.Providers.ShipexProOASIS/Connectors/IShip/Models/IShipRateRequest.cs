using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Connectors.IShip.Models
{
    /// <summary>
    /// Request model for iShip rate API.
    /// Transformed from Shipex Pro's internal RateRequest format.
    /// </summary>
    public class IShipRateRequest
    {
        /// <summary>
        /// Package dimensions in inches.
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
        /// Service level code (e.g., "GROUND", "EXPRESS", "OVERNIGHT").
        /// </summary>
        public string ServiceLevel { get; set; }

        /// <summary>
        /// List of carrier codes to get rates for (optional, null = all carriers).
        /// </summary>
        public List<string> Carriers { get; set; }

        /// <summary>
        /// Package value for insurance (optional).
        /// </summary>
        public decimal? PackageValue { get; set; }

        /// <summary>
        /// Delivery confirmation type (optional).
        /// </summary>
        public string DeliveryConfirmation { get; set; }

        /// <summary>
        /// Signature required flag.
        /// </summary>
        public bool SignatureRequired { get; set; }
    }

    /// <summary>
    /// Package dimensions.
    /// </summary>
    public class IShipDimensions
    {
        public decimal Length { get; set; }
        public decimal Width { get; set; }
        public decimal Height { get; set; }
        public string Unit { get; set; } = "IN"; // IN for inches, CM for centimeters
    }

    /// <summary>
    /// Address model for iShip API.
    /// </summary>
    public class IShipAddress
    {
        public string Name { get; set; }
        public string Company { get; set; }
        public string AddressLine1 { get; set; }
        public string AddressLine2 { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string PostalCode { get; set; }
        public string Country { get; set; } = "US";
        public string Phone { get; set; }
        public string Email { get; set; }
    }
}




