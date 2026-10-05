using System;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.Core.Holons
{
    /// <summary>
    /// Holon for persisting stablecoin position state
    /// Stores position information for tracking collateral and debt
    /// </summary>
    public class StablecoinPositionHolon : Holon
    {
        public StablecoinPositionHolon()
        {
            HolonType = HolonType.Default;
            Name = "Stablecoin Position";
        }

        /// <summary>
        /// Position ID (from StablecoinPosition DTO)
        /// </summary>
        public Guid PositionId { get; set; }

        /// <summary>
        /// Avatar ID of the position owner
        /// </summary>
        public Guid AvatarId { get; set; }

        /// <summary>
        /// Amount of ZEC locked as collateral
        /// </summary>
        public decimal CollateralAmount { get; set; }

        /// <summary>
        /// Amount of zUSD minted (debt)
        /// </summary>
        public decimal DebtAmount { get; set; }

        /// <summary>
        /// Collateral ratio (percentage)
        /// </summary>
        public decimal CollateralRatio { get; set; }

        /// <summary>
        /// Health status: Safe, Warning, Danger, Liquidated
        /// </summary>
        public string Health { get; set; } = "Safe";

        /// <summary>
        /// Hash of viewing key for private position tracking (optional)
        /// </summary>
        public string? ViewingKeyHash { get; set; }

        /// <summary>
        /// Zcash address used for collateral
        /// </summary>
        public string ZcashAddress { get; set; } = string.Empty;

        /// <summary>
        /// Aztec address receiving zUSD
        /// </summary>
        public string AztecAddress { get; set; } = string.Empty;
    }
}

